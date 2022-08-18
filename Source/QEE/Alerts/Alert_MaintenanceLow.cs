using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace QEthics;

public class Alert_MaintenanceLow : Alert_Critical
{
    public Alert_MaintenanceLow()
    {
        defaultLabel = "QE_AlertMaintenanceRequiredLabel".Translate();
        defaultExplanation = "QE_AlertMaintenanceRequiredExplanation".Translate();
    }

    public IEnumerable<Building> GrowersNeedingMaintenance()
    {
        var maintAlertPercent = 0.20f;
        return Find.CurrentMap.listerBuildings.allBuildingsColonist.Where(
            building => building is Building_GrowerBase_WorkTable { status: CrafterStatus.Crafting }
                            and IMaintainableGrower maintainable &&
                        (maintainable.DoctorMaintenance < maintAlertPercent ||
                         maintainable.ScientistMaintenance < maintAlertPercent));
    }

    public override AlertReport GetReport()
    {
        var growersNeedingMaintenance = GrowersNeedingMaintenance();
        if (growersNeedingMaintenance == null)
        {
            return false;
        }

        if (!growersNeedingMaintenance.Any())
        {
            return false;
        }

        var culprits = new List<GlobalTargetInfo>();
        foreach (var grower in growersNeedingMaintenance)
        {
            culprits.Add(grower);
        }

        return AlertReport.CulpritsAre(culprits);
    }
}