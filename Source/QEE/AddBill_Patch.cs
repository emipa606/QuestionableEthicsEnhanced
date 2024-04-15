using HarmonyLib;
using RimWorld;

namespace QEthics;

[HarmonyPatch(typeof(BillStack), nameof(BillStack.AddBill))]
public class AddBill_Patch
{
    [HarmonyPostfix]
    private static void AddBillPostfix(BillStack __instance, Bill bill)
    {
        if (__instance.billGiver is IBillGiverExtension extension)
        {
            extension.Notify_BillAdded(bill);
        }
    }
}