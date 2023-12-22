using RimWorld;
using Verse;

namespace TerrenSecurity;

[DefOf]
public static class ThingDefOf
{
    public static ThingDef PlanetaryFortress;

    static ThingDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(ThingDefOf));
    }
}