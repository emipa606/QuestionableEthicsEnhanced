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
        var harmony = new Harmony("KongMD.QEE");
        //HarmonyInstance.DEBUG = true;

        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}

//end patch class