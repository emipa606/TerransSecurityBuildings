using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TerrenSecurity;

public class ModSetting : Mod
{
    public static ModSetting mod;
    public readonly SettingIndex settings;

    public ModSetting(ModContentPack con)
        : base(con)
    {
        settings = GetSettings<SettingIndex>();
        mod = this;
    }

    public override string SettingsCategory()
    {
        return "TerrenSecurity".Translate();
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listing_Standard = new Listing_Standard();
        listing_Standard.Begin(inRect);
        listing_Standard.Settings_IntegerBox("BunkerHPLabel".Translate(), ref mod.settings.BunkerHP, 500f, 24f, 1,
            999999);
        listing_Standard.Settings_IntegerBox("AutoTurretHPLabel".Translate(), ref mod.settings.AutoTurretHP, 500f, 24f,
            1, 999999);
        listing_Standard.Settings_IntegerBox("PlanetaryFortressHPLabel".Translate(),
            ref mod.settings.PlanetaryFortressHP, 500f, 24f, 1, 999999);
        Widgets.Label(new Rect(0f, 144f, 600f, 200f), "TipsLabel".Translate());
        listing_Standard.End();
        base.DoSettingsWindowContents(inRect);
    }

    public override void WriteSettings()
    {
        UpdateChanges();
        base.WriteSettings();
    }

    public static void UpdateChanges()
    {
        DefDatabase<ThingDef>.GetNamed("TerranBunker").statBases
            .First(statBase => statBase.stat == StatDefOf.MaxHitPoints).value = mod.settings.BunkerHP;
        DefDatabase<ThingDef>.GetNamed("AutoTurret").statBases
            .First(statBase => statBase.stat == StatDefOf.MaxHitPoints).value = mod.settings.AutoTurretHP;
        DefDatabase<ThingDef>.GetNamed("PlanetaryFortress").statBases
                .First(statBase => statBase.stat == StatDefOf.MaxHitPoints).value =
            mod.settings.PlanetaryFortressHP;
    }
}