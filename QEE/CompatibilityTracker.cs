using System.Diagnostics;
using System.Linq;
using Verse;

namespace QEthics
{
    [StaticConstructorOnStartup]
    public static class CompatibilityTracker
    {
        private static bool alienRacesActiveInt = false;

        public static bool AlienRacesActive
        {
            get
            {
                return alienRacesActiveInt;
            }
        }

        static CompatibilityTracker()
        {
            foreach (ModMetaData m in ModsConfig.ActiveModsInLoadOrder)
            {
                string modName = m.Name;

                //Fully incompatible mods
                if (modName == "Questionable Ethics")
                {
                    Log.Error("Questionable Ethics Enhanced is incompatible with " + modName);
                    continue;
                }

                //Partially incompatible mods
                else if(modName == "Multiplayer")
                {
                    Log.Warning("Questionable Ethics Enhanced works with the Multiplayer mod, but is incompatible with the Multiplayer game mode.");
                    continue;
                }

                //Enhanced compatibility mods
                else if (modName.Contains("Humanoid Alien Races 2.0"))
                {
                    alienRacesActiveInt = true;
                    QEEMod.TryLog("Humanoid Alien Races detected");
                }
            }
        }
    }
}
