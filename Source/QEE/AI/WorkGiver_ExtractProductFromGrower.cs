using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace QEthics;

/// <summary>
///     WorkGiver for order processors.
/// </summary>
public class WorkGiver_ExtractProductFromGrower : WorkGiver_Scanner
{
    public override ThingRequest PotentialWorkThingRequest =>
        ThingRequest.ForDef(def.fixedBillGiverDefs.FirstOrDefault());

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not Building_GrowerBase_WorkTable grower)
        {
            return false;
        }

        if (!grower.GrowerProps.productRequireManualExtraction)
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

        return grower.status == CrafterStatus.Finished;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        _ = t as Building_GrowerBase_WorkTable;

        var job = new Job(QEJobDefOf.QE_ExtractProductFromGrowerJob, t);
        return job;
    }
}