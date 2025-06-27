using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace TerrenSecurity;

[StaticConstructorOnStartup]
public class Building_CanSetForcedTargetTurret : Building_Turret
{
    private const int TryStartShootSomethingIntervalTicks = 10;

    public static Material ForcedTargetLineMat =
        MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.Transparent, new Color(1f, 0.5f, 0.5f));

    private readonly TurretTop top;

    private int burstCooldownTicksLeft;

    private int burstWarmupTicksLeft;

    private LocalTargetInfo currentTargetInt = LocalTargetInfo.Invalid;

    private CompCanBeDormant dormantComp;

    private Thing gun;

    private bool holdFire;

    private CompInitiatable initiatableComp;

    private CompMannable mannableComp;

    private CompPowerTrader powerComp;

    private Effecter progressBarEffecter;

    public Building_CanSetForcedTargetTurret()
    {
        top = new TurretTop(this);
    }

    private bool Active => (powerComp == null || powerComp.PowerOn) && (dormantComp == null || dormantComp.Awake) &&
                           (initiatableComp == null || initiatableComp.Initiated);

    private CompEquippable GunCompEq => gun.TryGetComp<CompEquippable>();

    public override LocalTargetInfo CurrentTarget => currentTargetInt;

    private bool WarmingUp => burstWarmupTicksLeft > 0;

    public override Verb AttackVerb => GunCompEq.PrimaryVerb;

    public bool IsMannable => mannableComp != null;

    private bool PlayerControlled => (Faction == Faction.OfPlayer || MannedByColonist) && !MannedByNonColonist;

    private bool CanSetForcedTarget => (powerComp == null || powerComp.PowerOn) &&
                                       (dormantComp == null || dormantComp.Awake) &&
                                       (initiatableComp == null || initiatableComp.Initiated);

    private bool CanToggleHoldFire => PlayerControlled;

    private bool IsMortar => def.building.IsMortar;

    private bool IsMortarOrProjectileFliesOverhead => AttackVerb.ProjectileFliesOverhead() || IsMortar;

    private bool CanExtractShell
    {
        get
        {
            if (!PlayerControlled)
            {
                return false;
            }

            return gun.TryGetComp<CompChangeableProjectile>()?.Loaded ?? false;
        }
    }

    private bool MannedByColonist => mannableComp is { ManningPawn: not null } &&
                                     mannableComp.ManningPawn.Faction == Faction.OfPlayer;

    private bool MannedByNonColonist => mannableComp is { ManningPawn: not null } &&
                                        mannableComp.ManningPawn.Faction != Faction.OfPlayer;

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        dormantComp = GetComp<CompCanBeDormant>();
        initiatableComp = GetComp<CompInitiatable>();
        powerComp = GetComp<CompPowerTrader>();
        mannableComp = GetComp<CompMannable>();
        if (respawningAfterLoad)
        {
            return;
        }

        top.SetRotationFromOrientation();
        burstCooldownTicksLeft = def.building.turretInitialCooldownTime.SecondsToTicks();
    }

    public override void PostMake()
    {
        base.PostMake();
        makeGun();
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        base.DeSpawn(mode);
        resetCurrentTarget();
        progressBarEffecter?.Cleanup();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref burstCooldownTicksLeft, "burstCooldownTicksLeft");
        Scribe_Values.Look(ref burstWarmupTicksLeft, "burstWarmupTicksLeft");
        Scribe_TargetInfo.Look(ref currentTargetInt, "currentTarget");
        Scribe_Values.Look(ref holdFire, "holdFire");
        Scribe_Deep.Look(ref gun, "gun");
        BackCompatibility.PostExposeData(this);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            updateGunVerbs();
        }
    }

    public override AcceptanceReport ClaimableBy(Faction by)
    {
        return base.ClaimableBy(by) && mannableComp?.ManningPawn == null &&
               (!Active || mannableComp != null) &&
               ((dormantComp == null || dormantComp.Awake) && (initiatableComp == null || initiatableComp.Initiated) ||
                powerComp is { PowerOn: false });
    }

    public override void OrderAttack(LocalTargetInfo targ)
    {
        if (!targ.IsValid)
        {
            if (forcedTarget.IsValid)
            {
                resetForcedTarget();
            }

            return;
        }

        if ((targ.Cell - Position).LengthHorizontal < AttackVerb.verbProps.EffectiveMinRange(targ, this))
        {
            Messages.Message("MessageTargetBelowMinimumRange".Translate(), this, MessageTypeDefOf.RejectInput, false);
            return;
        }

        if ((targ.Cell - Position).LengthHorizontal > AttackVerb.verbProps.range)
        {
            Messages.Message("MessageTargetBeyondMaximumRange".Translate(), this, MessageTypeDefOf.RejectInput, false);
            return;
        }

        if (forcedTarget != targ)
        {
            forcedTarget = targ;
            if (burstCooldownTicksLeft <= 0)
            {
                tryStartShootSomething(false);
            }
        }

        if (holdFire)
        {
            Messages.Message("MessageTurretWontFireBecauseHoldFire".Translate(def.label), this,
                MessageTypeDefOf.RejectInput, false);
        }
    }

    protected override void Tick()
    {
        base.Tick();
        if (CanExtractShell && MannedByColonist)
        {
            var compChangeableProjectile = gun.TryGetComp<CompChangeableProjectile>();
            if (!compChangeableProjectile.allowedShellsSettings.AllowedToAccept(compChangeableProjectile.LoadedShell))
            {
                extractShell();
            }
        }

        if (forcedTarget.IsValid && !CanSetForcedTarget)
        {
            resetForcedTarget();
        }

        if (!CanToggleHoldFire)
        {
            holdFire = false;
        }

        if (forcedTarget.ThingDestroyed)
        {
            resetForcedTarget();
        }

        if (Active && (mannableComp == null || mannableComp.MannedNow) && !IsStunned && Spawned)
        {
            GunCompEq.verbTracker.VerbsTick();
            if (AttackVerb.state == VerbState.Bursting)
            {
                return;
            }

            if (WarmingUp)
            {
                burstWarmupTicksLeft--;
                if (burstWarmupTicksLeft == 0)
                {
                    beginBurst();
                }
            }
            else
            {
                if (burstCooldownTicksLeft > 0)
                {
                    burstCooldownTicksLeft--;
                    if (IsMortar)
                    {
                        progressBarEffecter ??= EffecterDefOf.ProgressBar.Spawn();

                        progressBarEffecter.EffectTick(this, TargetInfo.Invalid);
                        var mote = ((SubEffecter_ProgressBar)progressBarEffecter.children[0]).mote;
                        mote.progress = 1f - (Math.Max(burstCooldownTicksLeft, 0) /
                                              (float)burstCooldownTime().SecondsToTicks());
                        mote.offsetZ = -0.8f;
                    }
                }

                if (burstCooldownTicksLeft <= 0 && this.IsHashIntervalTick(10))
                {
                    tryStartShootSomething(true);
                }
            }

            top.TurretTopTick();
        }
        else
        {
            resetCurrentTarget();
        }
    }

    private void tryStartShootSomething(bool canBeginBurstImmediately)
    {
        if (progressBarEffecter != null)
        {
            progressBarEffecter.Cleanup();
            progressBarEffecter = null;
        }

        if (!Spawned || holdFire && CanToggleHoldFire ||
            AttackVerb.ProjectileFliesOverhead() && Map.roofGrid.Roofed(Position) || !AttackVerb.Available())
        {
            resetCurrentTarget();
            return;
        }

        var isValid = currentTargetInt.IsValid;
        currentTargetInt = forcedTarget.IsValid ? forcedTarget : tryFindNewTarget();

        if (!isValid && currentTargetInt.IsValid)
        {
            SoundDefOf.TurretAcquireTarget.PlayOneShot(new TargetInfo(Position, Map));
        }

        if (!currentTargetInt.IsValid)
        {
            resetCurrentTarget();
        }
        else if (def.building.turretBurstWarmupTime.Average > 0f)
        {
            burstWarmupTicksLeft = def.building.turretBurstWarmupTime.Average.SecondsToTicks();
        }
        else if (canBeginBurstImmediately)
        {
            beginBurst();
        }
        else
        {
            burstWarmupTicksLeft = 1;
        }
    }

    private LocalTargetInfo tryFindNewTarget()
    {
        var attackTargetSearcher = targSearcher();
        var faction = attackTargetSearcher.Thing.Faction;
        var range = AttackVerb.verbProps.range;
        if (Rand.Value < 0.5f && AttackVerb.ProjectileFliesOverhead() && faction.HostileTo(Faction.OfPlayer) && Map
                .listerBuildings.allBuildingsColonist.Where(delegate(Building x)
                {
                    var num = AttackVerb.verbProps.EffectiveMinRange(x, this);
                    float num2 = x.Position.DistanceToSquared(Position);
                    return num2 > num * num && num2 < range * range;
                }).TryRandomElement(out var result))
        {
            return result;
        }

        var losToAll = TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable;
        if (!AttackVerb.ProjectileFliesOverhead())
        {
            losToAll |= TargetScanFlags.NeedLOSToAll;
            losToAll |= TargetScanFlags.LOSBlockableByGas;
        }

        if (AttackVerb.IsIncendiary_Ranged())
        {
            losToAll |= TargetScanFlags.NeedNonBurning;
        }

        if (IsMortar)
        {
            losToAll |= TargetScanFlags.NeedNotUnderThickRoof;
        }

        return (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(attackTargetSearcher, losToAll,
            isValidTarget);
    }

    private IAttackTargetSearcher targSearcher()
    {
        if (mannableComp is { MannedNow: true })
        {
            return mannableComp.ManningPawn;
        }

        return this;
    }

    private bool isValidTarget(Thing t)
    {
        if (t is not Pawn pawn)
        {
            return true;
        }

        if (Faction == Faction.OfPlayer && pawn.GuestStatus is GuestStatus.Prisoner)
        {
            return false;
        }

        if (AttackVerb.ProjectileFliesOverhead())
        {
            var roofDef = Map.roofGrid.RoofAt(t.Position);
            if (roofDef is { isThickRoof: true })
            {
                return false;
            }
        }

        if (mannableComp == null)
        {
            return !GenAI.MachinesLike(Faction, pawn);
        }

        return !pawn.RaceProps.Animal || pawn.Faction != Faction.OfPlayer;
    }

    private void beginBurst()
    {
        AttackVerb.TryStartCastOn(CurrentTarget);
        OnAttackedTarget(CurrentTarget);
    }

    private void burstComplete()
    {
        burstCooldownTicksLeft = burstCooldownTime().SecondsToTicks();
    }

    private float burstCooldownTime()
    {
        return def.building.turretBurstCooldownTime >= 0f
            ? def.building.turretBurstCooldownTime
            : AttackVerb.verbProps.defaultCooldownTime;
    }

    public override string GetInspectString()
    {
        var stringBuilder = new StringBuilder();
        var inspectString = base.GetInspectString();
        if (!inspectString.NullOrEmpty())
        {
            stringBuilder.AppendLine(inspectString);
        }

        if (AttackVerb.verbProps.minRange > 0f)
        {
            stringBuilder.AppendLine("MinimumRange".Translate() + ": " + AttackVerb.verbProps.minRange.ToString("F0"));
        }

        if (Spawned && IsMortarOrProjectileFliesOverhead && Position.Roofed(Map))
        {
            stringBuilder.AppendLine("CannotFire".Translate() + ": " + "Roofed".Translate().CapitalizeFirst());
        }
        else if (Spawned && burstCooldownTicksLeft > 0 && burstCooldownTime() > 5f)
        {
            stringBuilder.AppendLine("CanFireIn".Translate() + ": " +
                                     burstCooldownTicksLeft.ToStringSecondsFromTicks());
        }

        var compChangeableProjectile = gun.TryGetComp<CompChangeableProjectile>();
        if (compChangeableProjectile == null)
        {
            return stringBuilder.ToString().TrimEndNewlines();
        }

        if (compChangeableProjectile.Loaded)
        {
            stringBuilder.AppendLine("ShellLoaded".Translate(compChangeableProjectile.LoadedShell.LabelCap,
                compChangeableProjectile.LoadedShell));
        }
        else
        {
            stringBuilder.AppendLine("ShellNotLoaded".Translate());
        }

        return stringBuilder.ToString().TrimEndNewlines();
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        var drawOffset = Vector3.zero;
        var angleOffset = 0f;
        if (IsMortar)
        {
            EquipmentUtility.Recoil(def.building.turretGunDef, (Verb_LaunchProjectile)AttackVerb, out drawOffset,
                out angleOffset, top.CurRotation);
        }

        top.DrawTurret(drawLoc, drawOffset, angleOffset);
        base.DrawAt(drawLoc, flip);
    }

    public override void DrawExtraSelectionOverlays()
    {
        var range = AttackVerb.verbProps.range;
        if (range < 90f)
        {
            GenDraw.DrawRadiusRing(Position, range);
        }

        var num = AttackVerb.verbProps.EffectiveMinRange(true);
        if (num is < 90f and > 0.1f)
        {
            GenDraw.DrawRadiusRing(Position, num);
        }

        if (WarmingUp)
        {
            var degreesWide = (int)(burstWarmupTicksLeft * 0.5f);
            GenDraw.DrawAimPie(this, CurrentTarget, degreesWide, def.size.x * 0.5f);
        }

        if (!forcedTarget.IsValid || forcedTarget.HasThing && !forcedTarget.Thing.Spawned)
        {
            return;
        }

        var b = !forcedTarget.HasThing ? forcedTarget.Cell.ToVector3Shifted() : forcedTarget.Thing.TrueCenter();
        var a = this.TrueCenter();
        b.y = AltitudeLayer.MetaOverlays.AltitudeFor();
        a.y = b.y;
        GenDraw.DrawLineBetween(a, b, Building_TurretGun.ForcedTargetLineMat);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }

        if (CanExtractShell)
        {
            var compChangeableProjectile = gun.TryGetComp<CompChangeableProjectile>();
            yield return new Command_Action
            {
                defaultLabel = "CommandExtractShell".Translate(),
                defaultDesc = "CommandExtractShellDesc".Translate(),
                icon = compChangeableProjectile.LoadedShell.uiIcon,
                iconAngle = compChangeableProjectile.LoadedShell.uiIconAngle,
                iconOffset = compChangeableProjectile.LoadedShell.uiIconOffset,
                iconDrawScale = GenUI.IconDrawScale(compChangeableProjectile.LoadedShell),
                action = extractShell
            };
        }

        var compChangeableProjectile2 = gun.TryGetComp<CompChangeableProjectile>();
        if (compChangeableProjectile2 != null)
        {
            var storeSettings = compChangeableProjectile2.GetStoreSettings();
            foreach (var item in StorageSettingsClipboard.CopyPasteGizmosFor(storeSettings))
            {
                yield return item;
            }
        }

        if (CanSetForcedTarget)
        {
            var command_VerbTarget = new Command_VerbTarget
            {
                defaultLabel = "CommandSetForceAttackTarget".Translate(),
                defaultDesc = "CommandSetForceAttackTargetDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack"),
                verb = AttackVerb,
                hotKey = KeyBindingDefOf.Misc4,
                drawRadius = false
            };
            if (Spawned && IsMortarOrProjectileFliesOverhead && Position.Roofed(Map))
            {
                command_VerbTarget.Disable("CannotFire".Translate() + ": " + "Roofed".Translate().CapitalizeFirst());
            }

            yield return command_VerbTarget;
        }

        if (forcedTarget.IsValid)
        {
            var command_Action = new Command_Action
            {
                defaultLabel = "CommandStopForceAttack".Translate(),
                defaultDesc = "CommandStopForceAttackDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Halt"),
                action = delegate
                {
                    resetForcedTarget();
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                }
            };
            if (!forcedTarget.IsValid)
            {
                command_Action.Disable("CommandStopAttackFailNotForceAttacking".Translate());
            }

            command_Action.hotKey = KeyBindingDefOf.Misc5;
            yield return command_Action;
        }

        if (!CanToggleHoldFire)
        {
            yield break;
        }

        yield return new Command_Toggle
        {
            defaultLabel = "CommandHoldFire".Translate(),
            defaultDesc = "CommandHoldFireDesc".Translate(),
            icon = ContentFinder<Texture2D>.Get("UI/Commands/HoldFire"),
            hotKey = KeyBindingDefOf.Misc6,
            toggleAction = delegate
            {
                holdFire = !holdFire;
                if (holdFire)
                {
                    resetForcedTarget();
                }
            },
            isActive = () => holdFire
        };
    }

    private void extractShell()
    {
        GenPlace.TryPlaceThing(gun.TryGetComp<CompChangeableProjectile>().RemoveShell(), Position, Map,
            ThingPlaceMode.Near);
    }

    private void resetForcedTarget()
    {
        forcedTarget = LocalTargetInfo.Invalid;
        burstWarmupTicksLeft = 0;
        if (burstCooldownTicksLeft <= 0)
        {
            tryStartShootSomething(false);
        }
    }

    private void resetCurrentTarget()
    {
        currentTargetInt = LocalTargetInfo.Invalid;
        burstWarmupTicksLeft = 0;
    }

    private void makeGun()
    {
        gun = ThingMaker.MakeThing(def.building.turretGunDef);
        updateGunVerbs();
    }

    private void updateGunVerbs()
    {
        var allVerbs = gun.TryGetComp<CompEquippable>().AllVerbs;
        foreach (var verb in allVerbs)
        {
            verb.caster = this;
            verb.castCompleteCallback = burstComplete;
        }
    }
}