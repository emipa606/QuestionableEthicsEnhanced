using Verse;
using RimWorld;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse.AI;

namespace QEthics
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("KongMD.QEE");
            //HarmonyInstance.DEBUG = true;

            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        /// <summary>
        /// This patch will cause IsClean() to return false if BodyParts with child parts, like 'arm' or 'leg', have any bad hediffs. 
        /// With this patch, only clean limbs can be harvested.
        /// </summary>
        [HarmonyPatch(typeof(MedicalRecipesUtility))]
        [HarmonyPatch(nameof(MedicalRecipesUtility.IsClean))]
        static class IsClean_Patch
        {
            [HarmonyPostfix]
            static void IsCleanPostfix(ref bool __result, Pawn pawn, BodyPartRecord part)
            {
                if (pawn.Dead)
                {
                    __result = false;
                    return;
                }

                BodyPartRecord diseasedPart = null;
                if (PawnUtility.bodyPartOrChildHasHediffs(pawn, part, out diseasedPart))
                {
                    QEEMod.TryLog("IsClean() false for " + part.Label + " because " + diseasedPart.Label + " has bad Hediffs");
                    __result = false;
                    return;
                }

                __result = true;
            }//end postfix
        }//end patch class

        /// <summary>
        /// This function spawns a BodyPart item on the map after surgery. This patch allows an arm to spawn if 
        /// the shoulder is the BodyPart being operated on.
        /// </summary>
        [HarmonyPatch(typeof(MedicalRecipesUtility))]
        [HarmonyPatch(nameof(MedicalRecipesUtility.SpawnNaturalPartIfClean))]
        static class SpawnNaturalPartIfClean_Patch
        {
            [HarmonyPostfix]
            static void SpawnNaturalPartIfCleanPostfix(ref Thing __result, Pawn pawn, BodyPartRecord part, IntVec3 pos, Map map)
            {
                //spawn a biological arm when a shoulder is removed with a healthy arm attached (e.g. from installing a prosthetic on a healthy arm)
                if (part.LabelShort == "shoulder")
                {
                    foreach (BodyPartRecord childPart in part.parts)
                    {
                        bool isHealthy = MedicalRecipesUtility.IsClean(pawn, childPart);
                        QEEMod.TryLog("body part: " + childPart.LabelShort + " IsClean: " + isHealthy);

                        if (childPart.def == BodyPartDefOf.Arm && isHealthy)
                        {
                            QEEMod.TryLog("Spawn natural arm from shoulder replacement");
                            __result = GenSpawn.Spawn(QEThingDefOf.QE_Organ_Arm, pos, map);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This patch allows GetWorkgiver() to return our custom workgiver, WorkGiver_DoBill_Grower. It will continue on to the vanilla function
        /// if no BillGivers are of this class. 
        /// </summary>
        [HarmonyPatch(typeof(BillUtility))]
        [HarmonyPatch(nameof(BillUtility.GetWorkgiver))]
        class GetWorkgiver_Patch
        {
            [HarmonyPrefix]
            static bool GetWorkgiver_Prefix(ref WorkGiverDef __result, IBillGiver billGiver) //pass the __result by ref to alter it.
            {
                Thing thing = billGiver as Thing;
                if (thing == null)
                {
                    Log.ErrorOnce($"Attempting to get the workgiver for a non-Thing IBillGiver {billGiver.ToString()}", 96810282);
                    __result = null;
                    return false;
                }
                List<WorkGiverDef> allDefsListForReading = DefDatabase<WorkGiverDef>.AllDefsListForReading;
                for (int i = 0; i < allDefsListForReading.Count; i++)
                {
                    WorkGiverDef workGiverDef = allDefsListForReading[i];
                    WorkGiver_DoBill_Grower workGiver_DoBill = workGiverDef.Worker as WorkGiver_DoBill_Grower;
                    if (workGiver_DoBill != null && workGiver_DoBill.ThingIsUsableBillGiver(thing))
                    {
                        __result = workGiverDef;
                        return false;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(BillStack))]
        [HarmonyPatch(nameof(BillStack.AddBill))]
        class AddBill_Patch
        {
            [HarmonyPostfix]
            static void AddBillPostfix(BillStack __instance, Bill bill)
            {
                IBillGiverExtension extension = __instance.billGiver as IBillGiverExtension;
                if (extension != null)
                {
                    extension.Notify_BillAdded(bill);
                }
            }
        }

        [HarmonyPatch(typeof(BillStack))]
        [HarmonyPatch(nameof(BillStack.Delete))]
        class DeleteBill_Patch
        {
            [HarmonyPostfix]
            static void DeleteBillPostfix(BillStack __instance, Bill bill)
            {
                IBillGiverExtension extension = __instance.billGiver as IBillGiverExtension;
                if (extension != null)
                {
                    extension.Notify_BillDeleted(bill);
                }
            }
        }
    }
}
