using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace QEthics;

/// <summary>
///     THIS IS CLASS WILL BE DEPRECATED SOON. Do not use in new development work. It is now only used in the Pawn Vat.
/// </summary>
public class WorkGiver_MaintainGrower : WorkGiver_Scanner
{
    public override ThingRequest PotentialWorkThingRequest =>
        ThingRequest.ForDef(def.fixedBillGiverDefs.FirstOrDefault());

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not Building_GrowerBase grower)
        {
            return false;
        }

        if (grower is not IMaintainableGrower maintanable)
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

        if (grower.status != CrafterStatus.Crafting)
        {
            return false;
        }

        var maintainScience = true;

        if (maintanable.ScientistMaintenance > QEESettings.instance.maintWorkThresholdFloat)
        {
            maintainScience = !(maintanable.ScientistMaintenance > 0.90f) && forced;
        }

        var maintainDoctor = true;

        if (maintanable.DoctorMaintenance > QEESettings.instance.maintWorkThresholdFloat)
        {
            maintainDoctor = !(maintanable.DoctorMaintenance > 0.90f) && forced;
        }

        var maintainAtAll = maintainScience || maintainDoctor;

        return maintainAtAll;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        //Building_GrowerBase grower = t as Building_GrowerBase;
        Job job = null;

        var maintanable = t as IMaintainableGrower;

        if (maintanable != null && maintanable.ScientistMaintenance < QEESettings.instance.maintWorkThresholdFloat)
        {
            job = new Job(QEJobDefOf.QE_MaintainGrowerJob_Intellectual, t);
        }

        if (maintanable != null && maintanable.DoctorMaintenance < QEESettings.instance.maintWorkThresholdFloat)
        {
            job = new Job(QEJobDefOf.QE_MaintainGrowerJob_Medicine, t);
        }

        return job;
    }
}