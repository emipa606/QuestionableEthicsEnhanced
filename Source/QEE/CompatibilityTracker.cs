using Verse;

namespace QEthics;

[StaticConstructorOnStartup]
public static class CompatibilityTracker
{
    static CompatibilityTracker()
    {
        foreach (var m in LoadedModManager.RunningMods)
        {
            var modName = m.Name;

            //Fully incompatible mods
            if (modName == "Questionable Ethics")
            {
                Log.Error($"Questionable Ethics Enhanced is incompatible with {modName}");
                continue;
            }

            //Partially incompatible mods

            if (modName == "Multiplayer")
            {
                Log.Warning(
                    "Questionable Ethics Enhanced works with the Multiplayer mod, but is incompatible with the Multiplayer game mode.");
                continue;
            }

            //Enhanced compatibility mods

            if (modName.Contains("Humanoid Alien Races"))
            {
                AlienRacesActive = true;
                QEEMod.TryLog("Humanoid Alien Races detected");
            }

            else if (modName.Contains("Psychic Awakening"))
            {
                QEEMod.TryLog("Psychic Awakening detected");

                PsychicAwakeningActive = PsychicAwakeningCompat.Init(m.assemblies.loadedAssemblies);
            }
            else if (modName.Contains("Rah's Bionics and Surgery Expansion") ||
                     modName.Contains("RBSE Hardcore Edition"))
            {
                RBSEActive = true;
                QEEMod.TryLog("RBSE detected. Make sure it loads first");
            }
        }
    }

    public static bool AlienRacesActive { get; }

    public static bool PsychicAwakeningActive { get; }

    public static bool RBSEActive { get; }
}