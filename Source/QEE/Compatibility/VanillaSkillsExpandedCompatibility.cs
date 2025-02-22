using HarmonyLib;
using System;
using System.Linq;
using Verse;
using VSE;
using VSE.Expertise;

namespace QEthics
{
    public static class VanillaSkillsExpandedCompatibility
    {
        public static void GetFieldsFromVanillaSkillsExpanded(Pawn pawn, BrainScanTemplate brainScan)
        {
            var expertiseTracker = pawn.Expertise();
            foreach (var expertiseRecord in expertiseTracker.AllExpertise)
            {
                var defName = expertiseRecord.def.defName;
                var totalXp = expertiseRecord.XpTotalEarned;
                brainScan.expertises.Add(new ComparableExpertiseRecord
                {
                    defName = defName,
                    totalXp = totalXp,
                });
            }
        }

        public static void SetFieldsToVanillaSkillsExpanded(Pawn pawn, BrainScanTemplate brainScan)
        {
            var expertiseTracker = pawn.Expertise();
            expertiseTracker.ClearExpertise();
            foreach (var expertise in brainScan.expertises)
            {
                var def = DefDatabase<ExpertiseDef>.GetNamedSilentFail(expertise.defName);
                if(def == null)
                {
                    QEEMod.TryLog($"Cannot find ExpertiseDef {expertise.defName}.");
                    continue;
                }
                expertiseTracker.AddExpertise(def);
                expertiseTracker.AllExpertise.Last().Learn(expertise.totalXp);
            }
        }

        private static Type cachedExpertiseDefType = null;
        private static bool? cachedExpertiseDefTypeFound = null;

        public static TaggedString ExpertiseDefLabelCap(string defName)
        {
            if (!CompatibilityTracker.SkillsExpandedActive) return defName;
            if (cachedExpertiseDefTypeFound == null)
            {
                cachedExpertiseDefType = AccessTools.TypeByName("VSE.Expertise.ExpertiseDef");
                cachedExpertiseDefTypeFound = cachedExpertiseDefType != null;
            }
            if(cachedExpertiseDefType == null)
                return defName;
            return GenDefDatabase.GetDefSilentFail(cachedExpertiseDefType, defName)?.LabelCap ?? defName;
        }
    }
}
