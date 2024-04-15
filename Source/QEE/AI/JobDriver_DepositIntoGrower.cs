using System.Collections.Generic;
using Verse.AI;

namespace QEthics;

/// <summary>
///     THIS IS CLASS WILL BE DEPRECATED SOON. Do not use in new development work. It is now only used in the Pawn Vat.
/// </summary>
public class JobDriver_DepositIntoGrower : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (!pawn.CanReserve(TargetThingA))
        {
            return false;
        }

        if (!pawn.CanReserve(TargetThingB))
        {
            return false;
        }

        return pawn.Reserve(TargetThingA, job, errorOnFailed: errorOnFailed) &&
               pawn.Reserve(TargetThingB, job, errorOnFailed: errorOnFailed);
    }

    public override IEnumerable<Toil> MakeNewToils()
    {
        //Conditions and reserve
        this.FailOn(() => TargetThingA is Building_GrowerBase vat && vat.status != CrafterStatus.Filling);
        this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
        this.FailOnDestroyedNullOrForbidden(TargetIndex.B);
        yield return Toils_Reserve.Reserve(TargetIndex.A);

        var reserveIngredient = Toils_Reserve.Reserve(TargetIndex.B);
        yield return reserveIngredient;

        //Go and get the thing to carry.
        yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.OnCell);
        var carryThing = Toils_Haul.StartCarryThing(TargetIndex.B, subtractNumTakenFromJobCount: true);
        carryThing.AddFinishAction(
            delegate
            {
                if (TargetThingA is Building_GrowerBase grower)
                {
                    grower.Notify_StartedCarryThing(GetActor());
                }
            });
        yield return carryThing;

        //Opportunistically haul a nearby ingredient of same ThingDef. Checks 8 square radius.
        yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveIngredient, TargetIndex.B, TargetIndex.None,
            true);

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
        yield return Toils_General.WaitWith(TargetIndex.A, 100, true);
        yield return new Toil
        {
            initAction = delegate
            {
                if (TargetThingA is Building_GrowerBase grower)
                {
                    grower.FillThing(GetActor().carryTracker.CarriedThing);
                }
            }
        };
    }
}