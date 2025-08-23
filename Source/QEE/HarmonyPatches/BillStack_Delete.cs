using HarmonyLib;
using RimWorld;

namespace QEthics;

[HarmonyPatch(typeof(BillStack), nameof(BillStack.Delete))]
public class BillStack_Delete
{
    private static void Postfix(BillStack __instance, Bill bill)
    {
        if (__instance.billGiver is IBillGiverExtension extension)
        {
            extension.Notify_BillDeleted(bill);
        }
    }
}