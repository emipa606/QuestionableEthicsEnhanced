using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace QEthics
{
    public static class RBSECompat
    {

        public static bool GetOrganRejectionSetting()
        {
            //only set rejectionEnabled to true if the mod loads successfully and we can retrieve the value from settings
            bool rejectionEnabled = false;

            var rbseModType = GenTypes.GetTypeInAnyAssembly("RBSE.Mod", "RBSE");
            if (rbseModType != null)
            {
                var rbseMod = LoadedModManager.GetMod(rbseModType);

                if (rbseMod != null)
                {
                    //The RBSE.Settings class is not available w/ public modifier. Use Reflection to get the Type
                    var rbseSettingsType = rbseModType.Assembly.GetType("RBSE.Settings");
                    if (rbseSettingsType != null)
                    {
                        var settings = typeof(Mod)?.GetMethod(nameof(Mod.GetSettings), Type.EmptyTypes)?.MakeGenericMethod(rbseSettingsType)?.Invoke(rbseMod, new object[0]);
                        if (settings != null)
                        {
                            var toggleActiveRejection = settings?.GetType()?.GetField("ToggleActiveRejection", AccessTools.all)?.GetValue(settings);
                            if (toggleActiveRejection != null)
                            {
                                QEEMod.TryLog("toggleActiveRejection value: " + toggleActiveRejection);
                                rejectionEnabled = (bool)toggleActiveRejection;
                            }
                            else
                            {
                                QEEMod.TryLog("Problem retrieving toggleActiveRejection ModSetting value from RBSE mod. Organ rejection hediff will not be added.");
                            }
                        }
                        else
                        {
                            QEEMod.TryLog("Error making generic GetSettings() method for RBSE compatibility. Organ rejection hediff will not be added.");
                        }
                    }
                    else
                    {
                        QEEMod.TryLog("RBSE class 'RBSE.Settings' not found. Organ rejection hediff will not be added.");
                    }
                }
                else
                {
                    QEEMod.TryLog("GetMod() returned null for RBSE. Organ rejection hediff will not be added.");
                }
            }
            else
            {
                QEEMod.TryLog("Class 'RBSE.Mod' not found. Organ rejection hediff will not be added.");
            }

            return rejectionEnabled;
        }
    }
}
