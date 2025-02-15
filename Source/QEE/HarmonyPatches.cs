using System.Reflection;
using HarmonyLib;
using Verse;

namespace QEthics;

[StaticConstructorOnStartup]
public static class HarmonyPatches
{
    public static readonly bool VNPELoaded;

    static HarmonyPatches()
    {
        VNPELoaded = ModsConfig.IsActive("VanillaExpanded.VNutrientE");
        //HarmonyInstance.DEBUG = true;

        new Harmony("KongMD.QEE").PatchAll(Assembly.GetExecutingAssembly());
    }
}