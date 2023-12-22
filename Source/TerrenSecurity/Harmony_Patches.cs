using HarmonyLib;
using RimWorld;
using Verse;

namespace TerrenSecurity;

[StaticConstructorOnStartup]
internal class Harmony_Patches
{
    static Harmony_Patches()
    {
        var harmony = new Harmony("rimworld.scarjit.TerranBunker");
        var method = typeof(GameEnder).GetMethod("CheckOrUpdateGameOver");
        var method2 = typeof(Patches).GetMethod("CheckOrUpdateGameOver_Postfix");
        harmony.Patch(method, null, new HarmonyMethod(method2));
        harmony.PatchAll();
    }
}