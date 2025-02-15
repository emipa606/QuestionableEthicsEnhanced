using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace QEthics.Compatibility;

[HarmonyPatch]
internal static class SpawnThoseGenesCompatibility
{
    public static bool Prepare()
    {
        if (!LoadedModManager.RunningMods.Any(mod => mod.Name.Contains("SpawnThoseGenes")))
        {
            return false;
        }

        QEEMod.TryLog("Adding compatibility with SpawnThoseGenes");
        return true;
    }

    public static IEnumerable<MethodBase> TargetMethods()
    {
        var maybeMainClass = AccessTools.TypeByName("SpawnThoseGenes.SpawnThoseGenesMod");
        if (maybeMainClass == null)
        {
            QEEMod.TryLog("Failed to find SpawnThoseGenesMod-class in SpawnThoseGenes, cannot add compatibility");
            yield break;
        }

        var method = maybeMainClass.GetMethod("PostfixGenerator", BindingFlags.NonPublic | BindingFlags.Static, null,
            [typeof(Pawn), typeof(XenotypeDef), typeof(PawnGenerationRequest)], null);
        if (method == null)
        {
            QEEMod.TryLog("Failed to find SpawnThoseGenesMod-method called PostfixGenerator, cannot add compatibility");
            yield break;
        }

        yield return method;
    }

    public static bool Prefix()
    {
        return !GenomeUtility.CloningInProgress;
    }
}