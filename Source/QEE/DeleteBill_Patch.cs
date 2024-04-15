using HarmonyLib;
using RimWorld;

namespace QEthics;

[HarmonyPatch(typeof(BillStack), nameof(BillStack.Delete))]
public class DeleteBill_Patch
{
    [HarmonyPostfix]
    private static void DeleteBillPostfix(BillStack __instance, Bill bill)
    {
        if (__instance.billGiver is IBillGiverExtension extension)
        {
            extension.Notify_BillDeleted(bill);
        }
    }
}