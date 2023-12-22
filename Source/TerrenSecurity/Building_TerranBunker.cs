using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TerrenSecurity;

public class Building_TerranBunker : Building_TurretGun, IThingHolder
{
    public readonly int maxCount = 4;
    public int direc;

    protected ThingOwner<Pawn> innerContainer;

    public Building_TerranBunker()
    {
        innerContainer = new ThingOwner<Pawn>(this, false);
    }

    public bool HasAnyContents => innerContainer.Count > 0;

    public Thing ContainedThing => innerContainer.Count != 0 ? innerContainer[0] : null;

    public bool CanOpen => HasAnyContents;

    public ThingOwner GetDirectlyHeldThings()
    {
        return innerContainer;
    }

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    public ThingOwner<Pawn> GetInner()
    {
        return innerContainer;
    }

    public override void TickRare()
    {
        base.TickRare();
        innerContainer.ThingOwnerTickRare();
    }

    public override void Tick()
    {
        if (innerContainer.Count < 1)
        {
            return;
        }

        base.Tick();
        innerContainer.ThingOwnerTick();
    }

    public virtual void Open()
    {
        if (HasAnyContents)
        {
            EjectAllContents();
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref direc, "direc");
        Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
    }

    public virtual bool ClaimableBy(Faction fac)
    {
        if (!innerContainer.Any)
        {
            return base.ClaimableBy(fac);
        }

        foreach (var item in innerContainer)
        {
            if (item.Faction != fac)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    protected virtual bool Accepts(Thing thing)
    {
        return innerContainer.CanAcceptAnyOf(thing);
    }

    public virtual void TryAcceptThing(Thing thing, bool allowSpecialEffects = true)
    {
        if (!Accepts(thing))
        {
            return;
        }

        if (thing.holdingOwner != null)
        {
            thing.holdingOwner.TryTransferToContainer(thing, innerContainer, thing.stackCount);
        }
        else
        {
            innerContainer.TryAdd(thing);
        }
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        if (innerContainer.Count > 0 &&
            (mode == DestroyMode.KillFinalizeLeavingsOnly || mode == DestroyMode.KillFinalize))
        {
            EjectAllContents();
        }

        innerContainer.ClearAndDestroyContents();
        base.Destroy(mode);
    }

    protected virtual void EjectAllContents()
    {
        (AttackVerb as Verb_TerranBunker)?.ResetVerb();
        innerContainer.TryDropAll(Toils_bunker.GetEnterOutLoc(this), Map, ThingPlaceMode.Near);
    }

    public override string GetInspectString()
    {
        var text = base.GetInspectString();
        var str = $"{innerContainer.Count}/{maxCount}";
        if (!text.NullOrEmpty())
        {
            text += "\n";
        }

        return text + "CasketContains".Translate() + ": " + str.CapitalizeFirst() +
               (innerContainer.Count == maxCount ? "(Full)" : "");
    }

    public override IEnumerable<FloatMenuOption> GetMultiSelectFloatMenuOptions(List<Pawn> selPawns)
    {
        foreach (var multiSelectFloatMenuOption in base.GetMultiSelectFloatMenuOptions(selPawns))
        {
            yield return multiSelectFloatMenuOption;
        }

        if (innerContainer.Count == maxCount)
        {
            yield break;
        }

        var assignedPawns = innerContainer.Count;
        var pawnList = new List<Pawn>();
        foreach (var pawn in selPawns)
        {
            if (assignedPawns < maxCount)
            {
                pawnList.Add(pawn);
                continue;
            }

            yield break;
        }

        yield return new FloatMenuOption("EnterTerranBunker".Translate(), jobAction);
        yield break;

        void jobAction()
        {
            MultiEnter(pawnList);
        }
    }

    private void MultiEnter(List<Pawn> pawnsToEnter)
    {
        var named = DefDatabase<JobDef>.GetNamed("EnterTerranBunker");
        foreach (var item in pawnsToEnter)
        {
            var job = new Job(named, this);
            item.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }
    }

    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
    {
        foreach (var floatMenuOption in base.GetFloatMenuOptions(myPawn))
        {
            yield return floatMenuOption;
        }

        JobDef jobDef;
        if (innerContainer.Count < maxCount)
        {
            jobDef = DefDatabase<JobDef>.GetNamed("EnterTerranBunker");
            string jobStr = "EnterTerranBunker".Translate();
            yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(jobStr, jobAction), myPawn,
                (LocalTargetInfo)this);
        }

        yield break;

        void jobAction()
        {
            var job = new Job(jobDef, this);
            myPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }

        if (Faction == Faction.OfPlayer && innerContainer.Count > 0)
        {
            var eject = new Command_Action
            {
                action = SelectColonist,
                defaultLabel = "ExitBunker".Translate(),
                defaultDesc = "ExitBunkerDesc".Translate()
            };
            if (innerContainer.Count == 0)
            {
                eject.Disable("CommandPodEjectFailEmpty".Translate());
            }

            eject.hotKey = KeyBindingDefOf.Misc1;
            eject.icon = ContentFinder<Texture2D>.Get("UI/Commands/PodEject");
            yield return eject;
        }

        var direcs = new[] { "North", "East", "South", "West" };
        yield return new Command_Action
        {
            defaultLabel = $"{"NowDirection".Translate()}\n{direcs[direc]}",
            defaultDesc = "ClickToChangeEnterDirection".Translate(),
            icon = TexCommand.GatherSpotActive,
            action = delegate
            {
                if (direc > 2)
                {
                    direc = 0;
                }
                else
                {
                    direc++;
                }
            }
        };
    }

    private void SelectColonist()
    {
        var source = new List<FloatMenuOption>();
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (innerContainer.Count == 0)
        {
            return;
        }

        var list = source.OrderBy(option => option.Label).ToList();
        list.Add(new FloatMenuOption("Everyone".Translate(), EjectAllContents, MenuOptionPriority.Default, null,
            null, 29f));
        Find.WindowStack.Add(new FloatMenu(list));
    }
}