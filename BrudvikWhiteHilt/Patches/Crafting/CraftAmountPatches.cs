using BrudvikWhiteHilt.Crafting;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Crafting;

/// <summary>
/// Lets the player choose how many to craft at once with arrows next to the Craft button.
/// </summary>
[HarmonyPatch]
public static class CraftAmountPatches
{
    /// <summary>
    /// Hands the chosen amount to vanilla before it shows the recipe's requirements and the Craft button.
    /// </summary>
    /// <param name="__instance">The inventory screen.</param>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe))]
    [HarmonyPrefix]
    public static void BeforeUpdateRecipe(InventoryGui __instance)
    {
        CraftAmountSelector.BeforeUpdate(__instance);
    }

    /// <summary>
    /// Shows the arrows and the chosen amount.
    /// </summary>
    /// <param name="__instance">The inventory screen.</param>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe))]
    [HarmonyPostfix]
    public static void AfterUpdateRecipe(InventoryGui __instance)
    {
        CraftAmountSelector.AfterUpdate(__instance);
    }

    /// <summary>
    /// Hands the chosen amount to vanilla when the Craft button is pressed.
    /// </summary>
    /// <param name="__instance">The inventory screen.</param>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnCraftPressed))]
    [HarmonyPrefix]
    public static void BeforeCraftPressed(InventoryGui __instance)
    {
        CraftAmountSelector.BeforeCraft(__instance);
    }
}
