using System;
using HarmonyLib;
using Verse;

namespace QEthics;

public static class RBSECompat
{
    public static bool GetOrganRejectionSetting()
    {
        //only set rejectionEnabled to true if the mod loads successfully, and we can retrieve the value from settings

        var rbseModType = GenTypes.GetTypeInAnyAssembly("RBSE.Mod", "RBSE");
        if (rbseModType == null)
        {
            QEEMod.TryLog("Class 'RBSE.Mod' not found. Organ rejection hediff will not be added.");
            return false;
        }

        var rbseMod = LoadedModManager.GetMod(rbseModType);

        if (rbseMod == null)
        {
            QEEMod.TryLog("GetMod() returned null for RBSE. Organ rejection hediff will not be added.");
            return false;
        }

        //The RBSE.Settings class is not available w/ public modifier. Use Reflection to get the Type
        var rbseSettingsType = rbseModType.Assembly.GetType("RBSE.Settings");
        if (rbseSettingsType == null)
        {
            QEEMod.TryLog("RBSE class 'RBSE.Settings' not found. Organ rejection hediff will not be added.");
            return false;
        }

        var settings = typeof(Mod).GetMethod(nameof(Mod.GetSettings), Type.EmptyTypes)
            ?.MakeGenericMethod(rbseSettingsType).Invoke(rbseMod, []);
        if (settings == null)
        {
            QEEMod.TryLog(
                "Error making generic GetSettings() method for RBSE compatibility. Organ rejection hediff will not be added.");
            return false;
        }

        var toggleActiveRejection = settings.GetType()
            .GetField("ToggleActiveRejection", AccessTools.all)?.GetValue(settings);
        if (toggleActiveRejection == null)
        {
            QEEMod.TryLog(
                "Problem retrieving toggleActiveRejection ModSetting value from RBSE mod. Organ rejection hediff will not be added.");
            return false;
        }

        QEEMod.TryLog($"toggleActiveRejection value: {toggleActiveRejection}");

        return (bool)toggleActiveRejection;
    }
}