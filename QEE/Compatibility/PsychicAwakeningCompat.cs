using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Harmony;
using Verse;

namespace QEthics
{
    public static class PsychicAwakeningCompat
    {
        public static Type HediffPsychicAwakenedType, PsychicPowerDefType;
        public static FieldInfo powersKnownField;

        /// <summary>
        /// Instantiate any Types that exist in the mod for later use in the QEE codebase.
        /// </summary>
        /// <returns>this value indicates whether all types and members were loaded successfully from the other mod</returns>
        public static bool Init()
        {
            HediffPsychicAwakenedType = GenTypes.GetTypeInAnyAssemblyNew("RimWorld.HediffPsychicAwakened",null);

            if (HediffPsychicAwakenedType != null)
            {           
                PsychicPowerDefType = HediffPsychicAwakenedType.Assembly.GetType("RimWorld.PsychicPowerDef");

                if (PsychicPowerDefType == null)
                {
                    QEEMod.TryLog("Psychic Awakened mod detected, but 'PsychicPowerDef' type missing. Psychic Awakening functionality disabled.");
                    return false;
                }

                powersKnownField = HediffPsychicAwakenedType.GetField("powersKnown", BindingFlags.Public | BindingFlags.Instance);

                if (powersKnownField == null)
                {
                    QEEMod.TryLog("Psychic Awakened mod detected, but 'powersKnown' field missing. Psychic Awakening functionality disabled.");
                    return false;
                }
            }
            else
            {
                QEEMod.TryLog("Psychic Awakened mod detected, but attempts to get HediffPsychicAwakened type failed. Psychic Awakening functionality disabled.");
                return false;
            }

            return true;
        }
    }
}
