using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace QEthics;

/// <summary>
///     THIS IS CLASS WILL BE DEPRECATED SOON. Do not use in new development work. It is now only used in the Pawn Vat.
///     WorkGiver for order processors
/// </summary>
public class WorkGiver_GrowerExtractProduct : WorkGiver_Scanner
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

        if (grower.status != CrafterStatus.Finished)
        {
            return false;
        }

        return true;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        var unused = t as Building_GrowerBase;

        var job = new Job(QEJobDefOf.QE_ExtractFromGrowerJob, t);
        return job;
    }
}