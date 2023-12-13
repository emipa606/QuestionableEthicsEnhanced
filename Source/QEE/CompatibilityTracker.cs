using System.Reflection;
using HarmonyLib;
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
                continue;
            }

            if (modName.Contains("Psychic Awakening"))
            {
                PsychicAwakeningActive = PsychicAwakeningCompat.Init(m.assemblies.loadedAssemblies);
                QEEMod.TryLog("Psychic Awakening detected");
                continue;
            }

            if (modName.Contains("Animal Genetics"))
            {
                AnimalGeneticsActive = true;
                QEEMod.TryLog("Animal Genetics detected");
                continue;
            }

            if (modName.Contains("Rah's Bionics and Surgery Expansion") ||
                modName.Contains("RBSE Hardcore Edition"))
            {
                RBSEActive = true;
                QEEMod.TryLog("RBSE detected. Make sure it loads first");
            }

            if (!modName.Contains("Facial Animation"))
            {
                continue;
            }

            FacialAnimationActive = true;
            HeadTypeField = AccessTools.Field(AccessTools.TypeByName("HeadControllerComp"), "faceType");
            EyeColorField = AccessTools.Field(AccessTools.TypeByName("EyeballControllerComp"), "color");
            EyeTypeField = AccessTools.Field(AccessTools.TypeByName("EyeballControllerComp"), "faceType");
            LidTypeField = AccessTools.Field(AccessTools.TypeByName("LidControllerComp"), "faceType");
            BrowTypeField = AccessTools.Field(AccessTools.TypeByName("BrowControllerComp"), "faceType");
            MouthTypeField = AccessTools.Field(AccessTools.TypeByName("MouthControllerComp"), "faceType");
            SkinTypeField = AccessTools.Field(AccessTools.TypeByName("SkinControllerComp"), "faceType");
            QEEMod.TryLog("Facial Animation detected.");
        }
    }

    public static bool FacialAnimationActive { get; }

    public static bool AlienRacesActive { get; }

    public static bool PsychicAwakeningActive { get; }

    public static bool RBSEActive { get; }

    public static bool AnimalGeneticsActive { get; }

    public static FieldInfo HeadTypeField { get; }
    public static FieldInfo EyeColorField { get; }
    public static FieldInfo EyeTypeField { get; }
    public static FieldInfo LidTypeField { get; }
    public static FieldInfo BrowTypeField { get; }
    public static FieldInfo MouthTypeField { get; }
    public static FieldInfo SkinTypeField { get; }
}