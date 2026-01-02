using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Ships;

/// <summary>
/// Patch to ensure that ships derived from WhiteHiltShipBase have tailwind enabled.
/// </summary>
[HarmonyPatch(typeof(Ship), "IsWindControllActive")]
public class TailwindPatch
{
    /// <summary>
    /// Postfix method to modify the result of IsWindControllActive for ships derived from WhiteHiltShipBase.
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="__result"></param>
    static void Postfix(Ship __instance, ref bool __result)
    {
        // Ensure tailwind for ships derived from WhiteHiltShipBase
        if (__instance != null && __instance.name.Contains("WhiteHilt"))
        {
            __result = true;
        }
    }
}
