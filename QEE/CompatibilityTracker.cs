using HarmonyLib;
using System.Diagnostics;
using System.Linq;
using Verse;

namespace QEthics
{
    [StaticConstructorOnStartup]
    public static class CompatibilityTracker
    {
        private static bool alienRacesActiveInt = false;
        private static bool psychicAwakeningActiveInt = false;
        private static bool rbseActiveInt = false;

        public static bool AlienRacesActive
        {
            get
            {
                return alienRacesActiveInt;
            }
        }

        public static bool PsychicAwakeningActive
        {
            get
            {
                return psychicAwakeningActiveInt;
            }
        }

        public static bool RBSEActive
        {
            get
            {
                return rbseActiveInt;
            }
        }

        static CompatibilityTracker()
        {
            foreach(ModContentPack m in LoadedModManager.RunningMods)
            {
                string modName = m.Name;

                //Fully incompatible mods
                if (modName == "Questionable Ethics")
                {
                    Log.Error("Questionable Ethics Enhanced is incompatible with " + modName);
                    continue;
                }

                //Partially incompatible mods
                else if (modName == "Multiplayer")
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

                else if (modName.Contains("Psychic Awakening"))
                {
                    QEEMod.TryLog("Psychic Awakening detected");

                    psychicAwakeningActiveInt = PsychicAwakeningCompat.Init(m.assemblies.loadedAssemblies);
                }
                else if (modName.Contains("Rah's Bionics and Surgery Expansion") || modName.Contains("RBSE Hardcore Edition"))
                {
                    rbseActiveInt = true;
                    QEEMod.TryLog("RBSE detected. Make sure it loads first");
                }
            }
        }
    }
}
