using BrudvikWhiteHilt.Items;
using BrudvikWhiteHilt.Items.Binding;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace BrudvikWhiteHilt.Patches.Gear;

/// <summary>
/// Lets White Hilt armor, weapons and shields be upgraded through the biome levels: their costs per level, their
/// armor, damage and block power, and the station level they need.
/// </summary>
[HarmonyPatch]
public static class GearUpgradePatches
{
    /// <summary>
    /// Gives the requirements of White Hilt gear recipes their cost at the quality crafted or upgraded to.
    /// </summary>
    /// <param name="__instance">The requirement.</param>
    /// <param name="qualityLevel">The quality.</param>
    /// <param name="__result">The amount needed.</param>
    /// <returns>False when the amount is set here.</returns>
    [HarmonyPatch(typeof(Piece.Requirement), nameof(Piece.Requirement.GetAmount))]
    [HarmonyPrefix]
    public static bool GetAmount(Piece.Requirement __instance, int qualityLevel, ref int __result)
    {
        return !GearUpgrades.TryGetAmount(__instance, qualityLevel, out __result);
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
        __result += GearUpgrades.ArmorCorrection(__instance.m_shared, quality);
    }

    /// <summary>
    /// Gives White Hilt weapons above quality 4 the damage of their biome levels, then a bound trophy's bonus and an
    /// etched rune's damage.
    /// </summary>
    /// <param name="__instance">The item.</param>
    /// <param name="quality">The quality.</param>
    /// <param name="__result">The damage.</param>
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDamage), new[] { typeof(int), typeof(float) })]
    [HarmonyPostfix]
    public static void GetDamage(ItemDrop.ItemData __instance, int quality, ref HitData.DamageTypes __result)
    {
        GearUpgrades.CorrectDamage(__instance.m_shared, quality, ref __result);
        GearBinding.ModifyDamage(__instance, ref __result);
    }

    /// <summary>
    /// Gives White Hilt shields above quality 4 the block power of their biome levels, then a bound trophy's bonus.
    /// </summary>
    /// <param name="__instance">The item.</param>
    /// <param name="quality">The quality.</param>
    /// <param name="__result">The block power before the Blocking skill.</param>
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetBaseBlockPower), new[] { typeof(int) })]
    [HarmonyPostfix]
    public static void GetBaseBlockPower(ItemDrop.ItemData __instance, int quality, ref float __result)
    {
        __result += GearUpgrades.BlockCorrection(__instance.m_shared, quality);
        __result = GearBinding.ModifyBlockPower(__instance, __result);
    }

    /// <summary>
    /// Checks the station level of the biome levels with <see cref="GearUpgrades.RequiredStationLevel"/>.
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
    /// Shows the station level of the biome levels from <see cref="GearUpgrades.RequiredStationLevel"/>.
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
        MethodInfo replacement = AccessTools.Method(typeof(GearUpgrades), nameof(GearUpgrades.RequiredStationLevel));
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
