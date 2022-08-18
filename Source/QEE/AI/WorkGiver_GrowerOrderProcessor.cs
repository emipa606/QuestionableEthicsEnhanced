using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace QEthics;

/// <summary>
///     THIS IS CLASS WILL BE DEPRECATED SOON. Do not use in new development work. It is now only used in the Pawn Vat.
///     WorkGiver for order processors.
/// </summary>
public class WorkGiver_GrowerOrderProcessor : WorkGiver_Scanner
{
    public override ThingRequest PotentialWorkThingRequest =>
        ThingRequest.ForDef(def.fixedBillGiverDefs.FirstOrDefault());

    //JobDriver_HaulToContainer is Job 'HaulToContainer'
    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not Building_GrowerBase grower)
        {
            return false;
        }

        if (t.IsForbidden(pawn))
        {
            return false;
        }

        if (!pawn.CanReserve(t))
        {
            return false;
        }

        if (grower.status != CrafterStatus.Filling)
        {
            return false;
        }

        if (!grower.orderProcessor.PendingRequests.Any())
        {
            return false;
        }

        return IngredientUtility.FindClosestRequestForThingOrderProcessor(grower.orderProcessor, pawn) is
            { } cloestThing && pawn.CanReserve(cloestThing);
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        var grower = t as Building_GrowerBase;
        var fillThing = IngredientUtility.FindClosestRequestForThingOrderProcessor(grower?.orderProcessor, pawn);
        if (grower == null)
        {
            return null;
        }

        var fillCount = grower.orderProcessor.PendingRequests.First(req => req.ThingMatches(fillThing)).amount;

        var job = new Job(QEJobDefOf.QE_DepositIntoGrowerJob, t, fillThing)
        {
            count = fillCount
        };
        return job;
    }
}