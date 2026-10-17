using BrudvikWhiteHilt.Crafting.Refinement;
using HarmonyLib;
using System;

namespace BrudvikWhiteHilt.Patches.Crafting;

/// <summary>
/// The Forge of Potential under White Hilt's rules (<see cref="Refinement"/>): the roll is decided before the game makes
/// its try, and what the game's branches do not cover is done after it.
/// </summary>
[HarmonyPatch]
public static class RefinementPatches
{
    /// <summary>
    /// Notes whether extra idols are offered as a refinement is begun.
    /// </summary>
    /// <param name="__instance">The inventory screen.</param>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnCraftPressed))]
    [HarmonyPostfix]
    public static void AfterCraftPressed(InventoryGui __instance)
    {
        Refinement.OnCraftPressed(__instance);
    }

    /// <summary>
    /// Replaces the game's roll with one that leads it to the outcome White Hilt's rules decide.
    /// </summary>
    /// <param name="__instance">The player.</param>
    /// <param name="__result">The game's roll, then the steered one.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.GetItemUpgradeChance))]
    [HarmonyPostfix]
    public static void AfterUpgradeChance(Player __instance, ref float __result)
    {
        __result = Refinement.Steer(__instance, __result);
    }

    /// <summary>
    /// Does what the game's try leaves out: the kept level, the failed tries, the extra idols.
    /// </summary>
    /// <param name="player">The player who crafted.</param>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
    [HarmonyPostfix]
    public static void AfterCrafting(Player player)
    {
        Refinement.Finish(player);
    }

    /// <summary>
    /// Gives the idol back its own chances, however the craft ended.
    /// </summary>
    /// <param name="__exception">An exception the craft threw, passed on.</param>
    /// <returns>The exception.</returns>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
    [HarmonyFinalizer]
    public static Exception CraftingEnded(Exception __exception)
    {
        Refinement.EndCraft();
        return __exception;
    }

    /// <summary>
    /// Says on the forge's recipe how to offer extra idols.
    /// </summary>
    /// <param name="__instance">The inventory screen.</param>
    /// <param name="player">The player at the screen.</param>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe))]
    [HarmonyPostfix]
    public static void AfterUpdateRecipe(InventoryGui __instance, Player player)
    {
        if (__instance.m_selectedRecipe.ItemData == null || __instance.m_recipeDecription == null)
        {
            return;
        }

        string hint = Refinement.ExtraIdolHint(player?.GetCurrentCraftingStation());
        if (hint != null)
        {
            __instance.m_recipeDecription.text += $"\n\n<color=#FFD966>{hint}</color>";
        }
    }
}
