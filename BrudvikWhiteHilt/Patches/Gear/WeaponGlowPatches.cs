using BrudvikWhiteHilt.Items.Runes;
using BrudvikWhiteHilt.Items.Runes.FlameRune;
using BrudvikWhiteHilt.Items.Runes.GlowRune;
using BrudvikWhiteHilt.Items.Weapons;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Gear;

/// <summary>
/// Shows the glow and flame etched into White Hilt gear, in hand and on the ground, and shows them and a glow or flame
/// rune's colour in the tooltip.
/// </summary>
[HarmonyPatch]
public static class WeaponGlowPatches
{
    /// <summary>
    /// Keeps the gear in every character's hands glowing and burning as etched.
    /// </summary>
    /// <param name="__instance">The equipment visuals.</param>
    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.UpdateVisuals))]
    [HarmonyPostfix]
    public static void UpdateVisuals(VisEquipment __instance)
    {
        WeaponGlow.Refresh(__instance);
        WeaponFlame.Refresh(__instance);
    }

    /// <summary>
    /// Shows the glow and flame of an item lying on the ground.
    /// </summary>
    /// <param name="__instance">The dropped item.</param>
    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Start))]
    [HarmonyPostfix]
    public static void Start(ItemDrop __instance)
    {
        WeaponGlow.ApplyDropped(__instance);
        WeaponFlame.ApplyDropped(__instance);
        ColourRunes.ApplyDropped(__instance, GlowRune.Name);
        ColourRunes.ApplyDropped(__instance, FlameRune.Name);
    }

    /// <summary>
    /// Adds the glow and flame of an item, or the colour of a glow or flame rune, to its tooltip.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="__result">The tooltip.</param>
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip),
        new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool) })]
    [HarmonyPostfix]
    public static void GetTooltip(ItemDrop.ItemData item, ref string __result)
    {
        __result += WeaponGlow.TooltipText(item) + WeaponFlame.TooltipText(item)
            + ColourRunes.TooltipText(item, GlowRune.Name) + ColourRunes.TooltipText(item, FlameRune.Name);
    }
}
