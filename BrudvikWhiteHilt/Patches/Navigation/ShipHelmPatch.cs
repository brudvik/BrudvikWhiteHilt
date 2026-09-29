using BrudvikWhiteHilt.Pieces.Navigation;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Navigation;

/// <summary>
/// The ship's helm is where the Navigator's Table is set up (use the item on it) and taken back (Shift + Use).
/// </summary>
[HarmonyPatch(typeof(ShipControlls))]
public static class ShipHelmPatch
{
    /// <summary>
    /// Sets up the table when the item is used on the helm.
    /// </summary>
    /// <param name="__instance">The helm.</param>
    /// <param name="user">The player.</param>
    /// <param name="item">The item used.</param>
    /// <param name="__result">True if the item was used.</param>
    [HarmonyPatch(nameof(ShipControlls.UseItem))]
    [HarmonyPostfix]
    public static void UseItem(ShipControlls __instance, Humanoid user, ItemDrop.ItemData item, ref bool __result)
    {
        ShipChartTable table = GetTable(__instance);
        if (!__result && table != null)
        {
            __result = table.UseItem(user, item);
        }
    }

    /// <summary>
    /// Shift + Use takes the table back instead of taking the helm.
    /// </summary>
    /// <param name="__instance">The helm.</param>
    /// <param name="repeat">True while the key is held.</param>
    /// <param name="alt">True with Shift.</param>
    /// <param name="__result">Result of the interaction.</param>
    /// <returns>False to skip taking the helm.</returns>
    [HarmonyPatch(nameof(ShipControlls.Interact))]
    [HarmonyPrefix]
    public static bool Interact(ShipControlls __instance, bool repeat, bool alt, ref bool __result)
    {
        ShipChartTable table = GetTable(__instance);
        if (!alt || repeat || table == null || !table.Take())
        {
            return true;
        }

        __result = true;
        return false;
    }

    /// <summary>
    /// Adds the table's key and the current sight to the helm's hover text.
    /// </summary>
    /// <param name="__instance">The helm.</param>
    /// <param name="__result">The localized hover text.</param>
    [HarmonyPatch(nameof(ShipControlls.GetHoverText))]
    [HarmonyPostfix]
    public static void GetHoverText(ShipControlls __instance, ref string __result)
    {
        string extra = GetTable(__instance)?.GetHoverText();
        if (!string.IsNullOrEmpty(extra))
        {
            __result += Localization.instance.Localize(extra);
        }
    }

    private static ShipChartTable GetTable(ShipControlls helm)
    {
        return helm.m_ship != null ? helm.m_ship.GetComponent<ShipChartTable>() : null;
    }
}
