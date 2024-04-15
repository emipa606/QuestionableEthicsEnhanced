using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace QEthics;

public static class PsychicAwakeningCompat
{
    public static Type HediffPsychicAwakenedType, PsychicPowerDefType;
    public static FieldInfo powersKnownField;

    /// <summary>
    ///     Instantiate any Types that exist in the other mod for later use in the QEE codebase.
    /// </summary>
    /// <param name="assemblies">The list of the other mod's loaded assemblies</param>
    /// <returns>Indicates whether all types and members were loaded successfully from the other mod</returns>
    public static bool Init(List<Assembly> assemblies)
    {
        HediffPsychicAwakenedType = assemblies.Select(assembly => assembly.GetType("RimWorld.HediffPsychicAwakened"))
            .FirstOrDefault(type => type != null);

        if (HediffPsychicAwakenedType != null)
        {
            PsychicPowerDefType = HediffPsychicAwakenedType.Assembly.GetType("RimWorld.PsychicPowerDef");

            if (PsychicPowerDefType == null)
            {
                QEEMod.TryLog(
                    "Psychic Awakened mod detected, but 'PsychicPowerDef' type missing. Psychic Awakening functionality disabled.");
                return false;
            }

            powersKnownField =
                HediffPsychicAwakenedType.GetField("powersKnown", BindingFlags.Public | BindingFlags.Instance);

            if (powersKnownField != null)
            {
                return true;
            }

            QEEMod.TryLog(
                "Psychic Awakened mod detected, but 'powersKnown' field missing. Psychic Awakening functionality disabled.");
            return false;
        }

        QEEMod.TryLog(
            "Psychic Awakened mod detected, but attempts to get HediffPsychicAwakened type failed. Psychic Awakening functionality disabled.");
        return false;
    }
}