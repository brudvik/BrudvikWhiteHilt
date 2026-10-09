using BrudvikWhiteHilt.Crafting;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Crafting;

/// <summary>
/// Adds a search field, a Craftable button, category tabs and favourites to the recipe list at every station, and
/// sorts it by name.
/// </summary>
[HarmonyPatch]
public static class RecipeListPatches
{
    /// <summary>
    /// Filters, sorts and marks the recipes vanilla has just listed.
    /// </summary>
    /// <param name="__instance">The inventory screen.</param>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipeList))]
    [HarmonyPostfix]
    public static void AfterRecipeList(InventoryGui __instance)
    {
        RecipeList.AfterRecipeList(__instance);
    }

    /// <summary>
    /// Keeps the game's keys quiet while typing in the search field.
    /// </summary>
    /// <param name="__instance">The inventory screen.</param>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe))]
    [HarmonyPostfix]
    public static void AfterUpdateRecipe(InventoryGui __instance)
    {
        RecipeList.Tick(__instance);
    }

    /// <summary>
    /// Empties the search when the inventory closes.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
    [HarmonyPostfix]
    public static void AfterHide()
    {
        RecipeList.OnHide();
    }
}
