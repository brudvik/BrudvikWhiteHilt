using HarmonyLib;
using System.Collections.Generic;

namespace BrudvikWhiteHilt.Patches.Crafting;

/// <summary>
/// Hides and blocks recipes that need a specific extension next to their crafting station, such as chains at the forge.
/// Vanilla recipes can only ask for a station level, not for a particular extension.
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.RequiredCraftingStation))]
public static class ChainBenchRecipePatch
{
    private static readonly Dictionary<string, string> requiredExtensions = new();

    /// <summary>
    /// Makes a recipe need an extension attached to the station.
    /// </summary>
    /// <param name="recipeName">Name of the recipe.</param>
    /// <param name="extensionPrefabName">Prefab name of the extension.</param>
    public static void Register(string recipeName, string extensionPrefabName)
    {
        requiredExtensions[recipeName] = extensionPrefabName;
    }

    // Available recipes and crafting both go through RequiredCraftingStation, so one check covers the list and the button.
    [HarmonyPostfix]
    private static void Postfix(Player __instance, Recipe recipe, ref bool __result)
    {
        if (!__result || recipe == null || !requiredExtensions.TryGetValue(recipe.name, out string extensionName))
        {
            return;
        }

        CraftingStation station = __instance.m_currentStation;
        if (station == null || station.m_upgrader)
        {
            return;
        }

        station.GetLevel();
        __result = station.m_attachedExtensions.Exists(extension => extension != null && global::Utils.GetPrefabName(extension.gameObject) == extensionName);
    }
}
