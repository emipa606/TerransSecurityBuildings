using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace TerrenSecurity;

public static class Toils_bunker
{
    public static Toil GotoThing(TargetIndex ind, PathEndMode peMode)
    {
        var toil = new Toil();
        toil.initAction = delegate
        {
            var actor = toil.actor;
            actor.pather.StartPath(GetBunkerNearCell(actor.jobs.curJob.GetTarget(ind)), peMode);
        };
        toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
        toil.FailOnDespawnedOrNull(ind);
        return toil;
    }

    private static LocalTargetInfo GetBunkerNearCell(LocalTargetInfo bunker)
    {
        var map = bunker.Thing.Map;
        var cell = bunker.Cell;
        var building_TerranBunker = (Building_TerranBunker)bunker.Thing;
        if (building_TerranBunker == null)
        {
            return null;
        }

        var direc = building_TerranBunker.direc;
        var list = new List<IntVec3>();
        for (var i = -2; i < 3; i++)
        {
            for (var j = -2; j < 3; j++)
            {
                if (Math.Abs(i) <= 1 && Math.Abs(j) <= 1 || Math.Abs(i * j) != 2 || (direc != 0 || j <= 0) &&
                    (direc != 1 || i <= 0) && (direc != 2 || j >= 0) && (direc != 3 || i >= 0))
                {
                    continue;
                }

                var intVec = new IntVec3(cell.x + i, cell.y, cell.z + j);
                if (CanOut(intVec, map))
                {
                    list.Add(intVec);
                }
            }
        }

        return list.RandomElement();
    }

    public static IntVec3 GetEnterOutLoc(Building_TerranBunker bunker)
    {
        var map = bunker.Map;
        var position = bunker.Position;
        var direc = bunker.direc;
        var list = new List<IntVec3>();
        for (var i = -2; i < 3; i++)
        {
            for (var j = -2; j < 3; j++)
            {
                if (Math.Abs(i) <= 1 && Math.Abs(j) <= 1 || Math.Abs(i * j) == 4 || (direc != 0 || j <= 0) &&
                    (direc != 1 || i <= 0) && (direc != 2 || j >= 0) && (direc != 3 || i >= 0))
                {
                    continue;
                }

                var intVec = new IntVec3(position.x + i, position.y, position.z + j);
                if (CanOut(intVec, map))
                {
                    list.Add(intVec);
                }
            }
        }

        return list.RandomElement();
    }

    public static List<IntVec3> GetAllEnterOutLoc(IntVec3 bunker)
    {
        var list = new List<IntVec3>();
        for (var i = -2; i < 3; i++)
        {
            for (var j = -2; j < 3; j++)
            {
                if (Math.Abs(i) <= 1 && Math.Abs(j) <= 1 || Math.Abs(i * j) == 4)
                {
                    continue;
                }

                var intVec2 = new IntVec3(bunker.x + i, bunker.y, bunker.z + j);
                if (CanOut(intVec2, Find.CurrentMap))
                {
                    list.Add(intVec2);
                }
            }
        }

        return list;
    }

    private static bool CanOut(IntVec3 cell, Map map)
    {
        return map.thingGrid.ThingsListAt(cell).FindAll(x => x.def.passability == Traversability.Impassable).Count == 0;
    }
}