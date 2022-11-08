using System.Collections.Generic;
using Verse.AI;

namespace QEthics;

/// <summary>
///     THIS IS A DEPRECATED CLASS. It is here for save compatibility only.
///     Extracts the product out of a Grower.
/// </summary>
public class JobDriver_ExtractGrowerProduct : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.CanReserve(TargetThingA) && pawn.Reserve(TargetThingA, job, errorOnFailed: errorOnFailed);
    }

    public override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOnDestroyedOrNull(TargetIndex.A);
        yield return Toils_Reserve.Reserve(TargetIndex.A);

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
        yield return Toils_General.WaitWith(TargetIndex.A, 200, true);
        yield return new Toil
        {
            initAction = delegate
            {
                if (TargetThingA is not Building_GrowerBase grower)
                {
                    return;
                }

                grower.TryExtractProduct(GetActor());

                EndJobWith(JobCondition.Succeeded);
            }
        };
    }
}