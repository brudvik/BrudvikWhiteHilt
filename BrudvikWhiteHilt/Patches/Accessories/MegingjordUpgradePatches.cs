using BrudvikWhiteHilt.Items.Accessories;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Accessories;

/// <summary>
/// Adds the carry weight of upgraded Megingjords and shows it in the belt's tooltip.
/// </summary>
[HarmonyPatch]
public static class MegingjordUpgradePatches
{
    /// <summary>
    /// Adds the upgrades of the worn belts to the carry weight.
    /// </summary>
    /// <param name="__instance">The player.</param>
    /// <param name="__result">The carry weight.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.GetMaxCarryWeight))]
    [HarmonyPostfix]
    public static void GetMaxCarryWeight(Player __instance, ref float __result)
    {
        __result += MegingjordUpgrade.WornBonus(__instance) * Game.m_carryWeightRate;
    }

    /// <summary>
    /// Shows the belt's whole carry weight at the quality shown.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="qualityLevel">The quality shown.</param>
    /// <param name="__result">The tooltip.</param>
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip),
        new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool) })]
    [HarmonyPostfix]
    public static void GetTooltip(ItemDrop.ItemData item, int qualityLevel, ref string __result)
    {
        if (MegingjordUpgrade.IsBelt(item))
        {
            __result += $"\n$whitehilt_megingjord_total: <color=orange>+{MegingjordUpgrade.TotalBonus(item, qualityLevel):0}</color>";
        }
    }
}
