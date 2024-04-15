using HarmonyLib;
using RimWorld;
using Verse;

namespace QEthics;

/// <summary>
///     This patch will cause IsClean() to return false if BodyParts with child parts, like 'arm' or 'leg', have any bad
///     hediffs.
///     With this patch, only clean limbs can be harvested.
/// </summary>
[HarmonyPatch(typeof(MedicalRecipesUtility), nameof(MedicalRecipesUtility.IsClean))]
public static class IsClean_Patch
{
    [HarmonyPostfix]
    private static void IsCleanPostfix(ref bool __result, Pawn pawn, BodyPartRecord part)
    {
        if (pawn.Dead)
        {
            __result = false;
            return;
        }

        if (PawnUtility.bodyPartOrChildHasHediffs(pawn, part, out var diseasedPart))
        {
            QEEMod.TryLog($"IsClean() false for {part.Label} because {diseasedPart.Label} has bad Hediffs");
            __result = false;
            return;
        }

        __result = true;
    } //end postfix
}