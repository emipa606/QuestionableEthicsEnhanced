using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace QEthics;

/// <summary>
///     THIS IS CLASS WILL BE DEPRECATED SOON. Do not use in new development work. It is now only used in the Pawn Vat.
/// </summary>
public class Alert_GrowerMaintenance : Alert_Critical
{
    public Alert_GrowerMaintenance()
    {
        defaultLabel = "QE_AlertMaintenanceRequiredLabel".Translate();
        defaultExplanation = "QE_AlertMaintenanceRequiredExplanation".Translate();
    }

    public IEnumerable<Building> GrowersNeedingMaintenance()
    {
        var maintAlertPercent = 0.20f;
        return Find.CurrentMap.listerBuildings.allBuildingsColonist.Where(
            building => building is Building_GrowerBase { status: CrafterStatus.Crafting }
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