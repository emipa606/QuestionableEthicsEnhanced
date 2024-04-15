using System.Reflection;
using HarmonyLib;
using Verse;

namespace QEthics;

[StaticConstructorOnStartup]
public static class HarmonyPatches
{
    static HarmonyPatches()
    {
        var harmony = new Harmony("KongMD.QEE");
        //HarmonyInstance.DEBUG = true;

        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}

//end patch class