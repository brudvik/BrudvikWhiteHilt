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
    /// <param name="__instance">The ship asked. Harmony passes the object a patched method runs on in a parameter
    /// named <c>__instance</c>.</param>
    /// <param name="__result">The vanilla answer, which a postfix may change: Harmony passes a method's return value
    /// by reference in a parameter named <c>__result</c>.</param>
    static void Postfix(Ship __instance, ref bool __result)
    {
        // Ensure tailwind for ships derived from WhiteHiltShipBase
        if (__instance != null && Pieces.Ships.ShipSettings.AlwaysTailwind.Value && __instance.GetComponent<Pieces.Ships.WhiteHiltShip.WhiteHiltShipUpgrades>() != null)
        {
            __result = true;
        }
    }
}
