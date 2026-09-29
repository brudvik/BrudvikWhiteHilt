using BrudvikWhiteHilt.Pieces.Waste;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Waste;

/// <summary>
/// Shift + Use on the Waste Well switches collecting from the ground, and its hover text says what it does.
/// </summary>
[HarmonyPatch]
public static class WasteWellPatches
{
    /// <summary>
    /// Switches collecting on Shift + Use instead of opening the well.
    /// </summary>
    /// <param name="__instance">The container.</param>
    /// <param name="hold">True while the key is held.</param>
    /// <param name="alt">True with the alternative key.</param>
    /// <param name="__result">True when handled.</param>
    /// <returns>False when handled here.</returns>
    [HarmonyPatch(typeof(Container), nameof(Container.Interact))]
    [HarmonyPrefix]
    public static bool Interact(Container __instance, bool hold, bool alt, ref bool __result)
    {
        if (!alt || hold || __instance.GetComponent<WasteWellComponent>() is not WasteWellComponent well)
        {
            return true;
        }

        if (PrivateArea.CheckAccess(__instance.transform.position))
        {
            well.ToggleCollecting();
        }

        __result = true;
        return false;
    }

    /// <summary>
    /// Adds the well's state and keys to the hover text.
    /// </summary>
    /// <param name="__instance">The container.</param>
    /// <param name="__result">The hover text.</param>
    [HarmonyPatch(typeof(Container), nameof(Container.GetHoverText))]
    [HarmonyPostfix]
    public static void GetHoverText(Container __instance, ref string __result)
    {
        if (__instance.GetComponent<WasteWellComponent>() is WasteWellComponent well)
        {
            __result += well.GetHoverText();
        }
    }
}
