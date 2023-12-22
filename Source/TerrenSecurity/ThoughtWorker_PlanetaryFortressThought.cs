using RimWorld;
using Verse;

namespace TerrenSecurity;

public class ThoughtWorker_PlanetaryFortressThought : ThoughtWorker
{
    private const float Radius = 25f;

    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (!p.IsColonist)
        {
            return false;
        }

        var list = p.Map.listerThings.ThingsOfDef(ThingDefOf.PlanetaryFortress);
        foreach (var thing in list)
        {
            var compPowerTrader = thing.TryGetComp<CompPowerTrader>();
            if ((compPowerTrader == null || compPowerTrader.PowerOn) && p.Position.InHorDistOf(thing.Position, 25f))
            {
                return true;
            }
        }

        return false;
    }
}