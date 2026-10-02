using BrudvikWhiteHilt.Items.Armors;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace BrudvikWhiteHilt.Patches.Armors;

/// <summary>
/// Lets White Hilt armor be upgraded through the biome levels: their costs per level, their armor and the station
/// level they need.
/// </summary>
[HarmonyPatch]
public static class ArmorUpgradePatches
{
    /// <summary>
    /// Gives the requirements of White Hilt armor recipes their cost at the quality crafted or upgraded to.
    /// </summary>
    /// <param name="__instance">The requirement.</param>
    /// <param name="qualityLevel">The quality.</param>
    /// <param name="__result">The amount needed.</param>
    /// <returns>False when the amount is set here.</returns>
    [HarmonyPatch(typeof(Piece.Requirement), nameof(Piece.Requirement.GetAmount))]
    [HarmonyPrefix]
    public static bool GetAmount(Piece.Requirement __instance, int qualityLevel, ref int __result)
    {
        return !ArmorUpgrades.TryGetAmount(__instance, qualityLevel, out __result);
    }

    /// <summary>
    /// Gives White Hilt armor above quality 4 the armor of its biome levels.
    /// </summary>
    /// <param name="__instance">The item.</param>
    /// <param name="quality">The quality.</param>
    /// <param name="__result">The armor.</param>
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetArmor), new[] { typeof(int), typeof(float) })]
    [HarmonyPostfix]
    public static void GetArmor(ItemDrop.ItemData __instance, int quality, ref float __result)
    {
        __result += ArmorUpgrades.ArmorCorrection(__instance.m_shared, quality);
    }

    /// <summary>
    /// Checks the station level of the biome levels with <see cref="ArmorUpgrades.RequiredStationLevel"/>.
    /// </summary>
    /// <param name="instructions">The original code.</param>
    /// <returns>The patched code.</returns>
    [HarmonyPatch(typeof(Player), nameof(Player.RequiredCraftingStation))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> RequiredCraftingStation(IEnumerable<CodeInstruction> instructions)
    {
        return ReplaceStationLevel(instructions);
    }

    /// <summary>
    /// Shows the station level of the biome levels from <see cref="ArmorUpgrades.RequiredStationLevel"/>.
    /// </summary>
    /// <param name="instructions">The original code.</param>
    /// <returns>The patched code.</returns>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> UpdateRecipe(IEnumerable<CodeInstruction> instructions)
    {
        return ReplaceStationLevel(instructions);
    }

    // Callers are patched rather than the method: it is small enough for the JIT to inline.
    private static IEnumerable<CodeInstruction> ReplaceStationLevel(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo original = AccessTools.Method(typeof(Recipe), nameof(Recipe.GetRequiredStationLevel));
        MethodInfo replacement = AccessTools.Method(typeof(ArmorUpgrades), nameof(ArmorUpgrades.RequiredStationLevel));
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.Calls(original))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
            }

            yield return instruction;
        }
    }
}
