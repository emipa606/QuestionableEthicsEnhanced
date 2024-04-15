using HarmonyLib;
using RimWorld;
using Verse;

namespace QEthics;

/// <summary>
///     This patch allows GetWorkgiver() to return our custom workgiver, WorkGiver_DoBill_Grower. It will continue on to
///     the vanilla function
///     if no BillGivers are of this class.
/// </summary>
[HarmonyPatch(typeof(BillUtility), nameof(BillUtility.GetWorkgiver))]
public class GetWorkgiver_Patch
{
    [HarmonyPrefix]
    private static bool
        GetWorkgiver_Prefix(ref WorkGiverDef __result, IBillGiver billGiver) //pass the __result by ref to alter it.
    {
        if (billGiver is not Thing thing)
        {
            Log.ErrorOnce($"Attempting to get the workgiver for a non-Thing IBillGiver {billGiver}", 96810282);
            __result = null;
            return false;
        }

        var allDefsListForReading = DefDatabase<WorkGiverDef>.AllDefsListForReading;
        foreach (var workGiverDef in allDefsListForReading)
        {
            if (workGiverDef.Worker is not WorkGiver_DoBill_Grower workGiver_DoBill ||
                !workGiver_DoBill.ThingIsUsableBillGiver(thing))
            {
                continue;
            }

            __result = workGiverDef;
            return false;
        }

        return true;
    }
}