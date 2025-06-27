using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TerrenSecurity;

public class ModSetting : Mod
{
    private static ModSetting mod;
    private readonly SettingIndex settings;

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
        var listingStandard = new Listing_Standard();
        listingStandard.Begin(inRect);
        listingStandard.Settings_IntegerBox("BunkerHPLabel".Translate(), ref mod.settings.BunkerHP, 500f, 24f, 1,
            999999);
        listingStandard.Settings_IntegerBox("AutoTurretHPLabel".Translate(), ref mod.settings.AutoTurretHP, 500f, 24f,
            1, 999999);
        listingStandard.Settings_IntegerBox("PlanetaryFortressHPLabel".Translate(),
            ref mod.settings.PlanetaryFortressHP, 500f, 24f, 1, 999999);
        Widgets.Label(new Rect(0f, 144f, 600f, 200f), "TipsLabel".Translate());
        listingStandard.End();
        base.DoSettingsWindowContents(inRect);
    }

    public override void WriteSettings()
    {
        updateChanges();
        base.WriteSettings();
    }

    private static void updateChanges()
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