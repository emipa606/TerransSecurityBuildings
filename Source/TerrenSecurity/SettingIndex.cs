using Verse;

namespace TerrenSecurity;

public class SettingIndex : ModSettings
{
    public int AutoTurretHP = 530;
    public int BunkerHP = 700;

    public int PlanetaryFortressHP = 2600;

    protected float weaponDamageMultiplier = 1f;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref BunkerHP, "BunkerHP");
        Scribe_Values.Look(ref AutoTurretHP, "AutoTurretHP");
        Scribe_Values.Look(ref PlanetaryFortressHP, "PlanetaryFortressHP");
    }
}