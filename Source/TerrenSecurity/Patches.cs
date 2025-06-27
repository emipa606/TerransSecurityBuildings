using RimWorld;
using Verse;

namespace TerrenSecurity;

internal class Patches
{
    public static void GameEnder_CheckOrUpdateGameOver(GameEnder __instance)
    {
        var maps = Find.Maps;
        foreach (var item in maps)
        {
            var list = item.listerThings.ThingsInGroup(ThingRequestGroup.HasGUIOverlay);
            foreach (var item2 in list)
            {
                if (item2 is not Building_TerranBunker { HasAnyContents: not false })
                {
                    continue;
                }

                __instance.gameEnding = false;
                return;
            }
        }
    }
}