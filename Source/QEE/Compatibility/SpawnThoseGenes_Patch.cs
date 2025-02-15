using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;

namespace QEthics.Compatibility
{
    internal static class SpawnThoseGenes_Patch
    {
        public static bool ShouldPatch()
        {
            return LoadedModManager.RunningMods.Any(mod => mod.Name.Contains("SpawnThoseGenes"));
        }

        public static void Patch(Harmony harmony)
        {
            Type maybeMainClass = AccessTools.TypeByName("SpawnThoseGenes.SpawnThoseGenesMod");
            if (maybeMainClass == null) return;
            var method = maybeMainClass.GetMethod("PostfixGenerator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static, null, [typeof(Pawn), typeof(XenotypeDef), typeof(PawnGenerationRequest)], null);
            if (method == null) return;
            harmony.Patch(method, new HarmonyMethod(MuteExtraGenesSpawning));
        }

        static bool MuteExtraGenesSpawning(Pawn pawn, XenotypeDef xenotype, PawnGenerationRequest request)
        {
            return !GenomeUtility.CloningInProgress;
        } 
    }
}
