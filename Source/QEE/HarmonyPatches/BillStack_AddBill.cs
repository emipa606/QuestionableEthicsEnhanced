using HarmonyLib;
using RimWorld;

namespace QEthics;

[HarmonyPatch(typeof(BillStack), nameof(BillStack.AddBill))]
public class BillStack_AddBill
{
    private static void Postfix(BillStack __instance, Bill bill)
    {
        if (__instance.billGiver is IBillGiverExtension extension)
        {
            extension.Notify_BillAdded(bill);
        }
    }
}