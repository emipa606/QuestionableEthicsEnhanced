using HarmonyLib;
using RimWorld;
using Verse;

namespace QEthics;

/// <summary>
///     This function spawns a BodyPart item on the map after surgery. This patch allows an arm to spawn if
///     the shoulder is the BodyPart being operated on.
/// </summary>
[HarmonyPatch(typeof(MedicalRecipesUtility), nameof(MedicalRecipesUtility.SpawnNaturalPartIfClean))]
public static class SpawnNaturalPartIfClean_Patch
{
    [HarmonyPostfix]
    private static void SpawnNaturalPartIfCleanPostfix(ref Thing __result, Pawn pawn, BodyPartRecord part,
        IntVec3 pos, Map map)
    {
        //spawn a biological arm when a shoulder is removed with a healthy arm attached (e.g. from installing a prosthetic on a healthy arm)
        if (part.LabelShort != "shoulder")
        {
            return;
        }

        foreach (var childPart in part.parts)
        {
            var isHealthy = MedicalRecipesUtility.IsClean(pawn, childPart);
            QEEMod.TryLog($"body part: {childPart.LabelShort} IsClean: {isHealthy}");

            if (childPart.def != BodyPartDefOf.Arm || !isHealthy)
            {
                continue;
            }

            QEEMod.TryLog("Spawn natural arm from shoulder replacement");
            __result = GenSpawn.Spawn(QEThingDefOf.QE_Organ_Arm, pos, map);
        }
    }
}