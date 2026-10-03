using BrudvikWhiteHilt.Items.Painting;
using BrudvikWhiteHilt.Painting;
using BrudvikWhiteHilt.Pieces.Painting;
using HarmonyLib;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Painting;

/// <summary>
/// Hooks for painting: saved paint on pieces, the tint surviving the build highlight, loading the brush from a pot,
/// the colour window at the Paint Bench and the pot's colour in its tooltip.
/// </summary>
[HarmonyPatch]
public static class PaintPatches
{
    /// <summary>
    /// Shows a piece's saved paint and listens for new paint.
    /// </summary>
    /// <param name="__instance">The piece.</param>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Awake))]
    [HarmonyPostfix]
    public static void WearNTearAwake(WearNTear __instance)
    {
        PaintedPieces.OnAwake(__instance);
    }

    /// <summary>
    /// Puts a painted piece's tint back when the build highlight or anything else resets the colour.
    /// </summary>
    /// <param name="go">The game object.</param>
    /// <param name="nameID">The shader property reset.</param>
    [HarmonyPatch(typeof(MaterialMan), nameof(MaterialMan.ResetValue), new[] { typeof(GameObject), typeof(int) })]
    [HarmonyPostfix]
    public static void MaterialManResetValue(GameObject go, int nameID)
    {
        if (nameID == ShaderProps._Color)
        {
            PaintedPieces.Reapply(go);
        }
    }

    /// <summary>
    /// Using a paint pot loads the brush instead of the vanilla item use.
    /// </summary>
    /// <param name="__instance">The character.</param>
    /// <param name="item">The item used.</param>
    /// <returns>False for a paint pot.</returns>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UseItem))]
    [HarmonyPrefix]
    public static bool UsePot(Humanoid __instance, ItemDrop.ItemData item)
    {
        if (!WhiteHiltPaintPot.IsPot(item) || __instance is not Player player || player != Player.m_localPlayer)
        {
            return true;
        }

        if (!global::BrudvikWhiteHilt.Textiles.Dyeing.TryUse(player, item))
        {
            PaintBrush.LoadFrom(player, item);
        }

        return false;
    }

    /// <summary>
    /// Shift + Use on the Paint Bench opens the colour window.
    /// </summary>
    /// <param name="__instance">The station.</param>
    /// <param name="user">The character using it.</param>
    /// <param name="alt">True with the alternative key held.</param>
    /// <param name="__result">True when the window opened.</param>
    /// <returns>False when the window takes over.</returns>
    [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.Interact))]
    [HarmonyPrefix]
    public static bool BenchInteract(CraftingStation __instance, Humanoid user, bool alt, ref bool __result)
    {
        if (!alt || user != Player.m_localPlayer || !PaintBench.IsBench(__instance))
        {
            return true;
        }

        PaintMixerPanel.Open(__instance);
        __result = true;
        return false;
    }

    /// <summary>
    /// Tells how to open the colour window.
    /// </summary>
    /// <param name="__instance">The station.</param>
    /// <param name="__result">The hover text.</param>
    [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.GetHoverText))]
    [HarmonyPostfix]
    public static void BenchHoverText(CraftingStation __instance, ref string __result)
    {
        if (PaintBench.IsBench(__instance))
        {
            __result += Localization.instance.Localize("\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $whitehilt_paint_mix");
        }
    }

    /// <summary>
    /// Shows a paint pot's colour in its tooltip.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="__result">The tooltip.</param>
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip),
        new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool) })]
    [HarmonyPostfix]
    public static void PotTooltip(ItemDrop.ItemData item, ref string __result)
    {
        if (WhiteHiltPaintPot.IsPot(item) && WhiteHiltPaintPot.TryGetColor(item, out Color32 color))
        {
            __result += $"\n$whitehilt_paint_tooltip: {PaintColor.Swatch(color)}";
        }
    }

    /// <summary>
    /// Runs the brush radius, preview and read-out for the local player.
    /// </summary>
    /// <param name="__instance">The player.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    [HarmonyPostfix]
    public static void PlayerUpdate(Player __instance)
    {
        if (__instance == Player.m_localPlayer)
        {
            PaintBrush.Tick(__instance);
        }
    }
}
