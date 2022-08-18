using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace QEthics;

public class WorkGiver_GrowerMaintenance : WorkGiver_Scanner
{
    public override ThingRequest PotentialWorkThingRequest =>
        ThingRequest.ForDef(def.fixedBillGiverDefs.FirstOrDefault());

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not Building_GrowerBase_WorkTable grower)
        {
            return false;
        }

        if (grower is not IMaintainableGrower maintainable)
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

        var shouldMaintainScience = true;

        if (maintainable.ScientistMaintenance > QEESettings.instance.maintWorkThresholdFloat)
        {
            shouldMaintainScience = !(maintainable.ScientistMaintenance > 0.90f) && forced;
        }

        var shouldMaintainDoctor = true;

        if (maintainable.DoctorMaintenance > QEESettings.instance.maintWorkThresholdFloat)
        {
            shouldMaintainDoctor = !(maintainable.DoctorMaintenance > 0.90f) && forced;
        }

        var maintainAtAll = shouldMaintainScience || shouldMaintainDoctor;

        return maintainAtAll;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        Job job = null;

        var maintainable = t as IMaintainableGrower;

        if (maintainable != null && maintainable.ScientistMaintenance < QEESettings.instance.maintWorkThresholdFloat)
        {
            job = new Job(QEJobDefOf.QE_GrowerMaintenanceJob_Intellectual, t);
        }

        if (maintainable != null && maintainable.DoctorMaintenance < QEESettings.instance.maintWorkThresholdFloat)
        {
            job = new Job(QEJobDefOf.QE_GrowerMaintenanceJob_Medicine, t);
        }

        return job;
    }
}