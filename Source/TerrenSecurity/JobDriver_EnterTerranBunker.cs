using System.Collections.Generic;
using Verse.AI;

namespace TerrenSecurity;

public class JobDriver_EnterTerranBunker : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return true;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(TargetIndex.A);
        yield return Toils_bunker.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
        var enter = new Toil
        {
            initAction = delegate
            {
                var pod = (Building_TerranBunker)pawn.CurJob.targetA.Thing;
                action();
                return;

                void action()
                {
                    if (pod.GetInner().InnerListForReading.Count >= pod.maxCount)
                    {
                        return;
                    }

                    pawn.DeSpawn();
                    pod.TryAcceptThing(pawn);
                }
            },
            defaultCompleteMode = ToilCompleteMode.Instant
        };
        yield return enter;
    }
}