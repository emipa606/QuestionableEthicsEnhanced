using Harmony;
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
            var rbseMod = LoadedModManager.GetMod<RBSE.Mod>();

            if (rbseMod != null)
            {
                //The RBSE.Settings class is not available w/ public modifier. Use Reflection to get the Type
                var rbseSettingsType = typeof(RBSE.Mod).Assembly.GetType("RBSE.Settings");
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
                            QEEMod.TryLog("Problem retrieving toggleActiveRejection ModSetting value from RBSE mod");
                        }
                    }
                    else
                    {
                        QEEMod.TryLog("Error making generic GetSettings() method for RBSE compatibility");
                    }
                }
                else
                {
                    QEEMod.TryLog("RBSE loaded but class 'RBSE.Settings' not found");
                }
            }
            else
            {
                QEEMod.TryLog("RBSE not loaded");
            }

            return rejectionEnabled;
        }
    }
}
