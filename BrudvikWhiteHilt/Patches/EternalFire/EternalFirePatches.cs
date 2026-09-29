using BrudvikWhiteHilt.Pieces.EternalFire;
using HarmonyLib;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.EternalFire;

/// <summary>
/// Keeps fires, ovens and smelters burning while <see cref="EternalFireRules"/> says so.
/// </summary>
[HarmonyPatch]
public static class EternalFirePatches
{
    private const string BurningLine = "\n<color=#FFB84D>$whitehilt_eternalfire_burning</color>";

    /// <summary>
    /// Makes a fire infinite while eternal fire is on. It is filled up once, so a torch can still be put out and
    /// burns down normally when eternal fire ends.
    /// </summary>
    /// <param name="__instance">The fire.</param>
    [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.UpdateFireplace))]
    [HarmonyPrefix]
    public static void KeepFireBurning(Fireplace __instance)
    {
        if (__instance.m_nview == null || !__instance.m_nview.IsValid())
        {
            return;
        }

        bool eternal = EternalFireRules.Applies(__instance);
        __instance.m_infiniteFuel = eternal || EternalFireRules.IsVanillaInfinite(__instance);
        if (eternal && __instance.m_nview.IsOwner() && __instance.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel) < __instance.m_maxFuel)
        {
            __instance.m_nview.GetZDO().Set(ZDOVars.s_fuel, __instance.m_maxFuel);
        }
    }

    /// <summary>
    /// Rain and wind do not put out or dim an eternal fire.
    /// </summary>
    /// <param name="__instance">The fire.</param>
    /// <returns>False to skip the wet check.</returns>
    [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.CheckWet))]
    [HarmonyPrefix]
    public static bool IgnoreRain(Fireplace __instance)
    {
        if (!EternalFireRules.Applies(__instance))
        {
            return true;
        }

        __instance.m_wet = false;
        return false;
    }

    /// <summary>
    /// Vanilla shows nothing for an infinite fire; an eternal one shows its name, that it is eternal, and the on/off key.
    /// </summary>
    /// <param name="__instance">The fire.</param>
    /// <param name="__result">The hover text.</param>
    [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.GetHoverText))]
    [HarmonyPostfix]
    public static void FireHoverText(Fireplace __instance, ref string __result)
    {
        if (!EternalFireRules.Applies(__instance) || EternalFireRules.IsVanillaInfinite(__instance))
        {
            return;
        }

        string text = __instance.m_name + BurningLine;
        if (__instance.m_canTurnOff)
        {
            text += "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_use";
        }

        __result = Localization.instance.Localize(text);
    }

    /// <summary>
    /// An eternal oven burns no fuel.
    /// </summary>
    /// <param name="__instance">The cooking station.</param>
    /// <returns>False to skip burning fuel.</returns>
    [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.UpdateFuel))]
    [HarmonyPrefix]
    public static bool KeepOvenBurning(CookingStation __instance)
    {
        if (!EternalFireRules.Applies(__instance))
        {
            return true;
        }

        if (__instance.m_nview.IsOwner() && __instance.GetFuel() < __instance.m_maxFuel)
        {
            __instance.SetFuel(__instance.m_maxFuel);
        }

        return false;
    }

    /// <summary>
    /// Shows that an oven's fire is eternal.
    /// </summary>
    /// <param name="__instance">The cooking station.</param>
    /// <param name="__result">The hover text of the fuel switch.</param>
    [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.OnHoverFuelSwitch))]
    [HarmonyPostfix]
    public static void OvenHoverText(CookingStation __instance, ref string __result)
    {
        if (EternalFireRules.Applies(__instance))
        {
            __result += Localization.instance.Localize(BurningLine);
        }
    }

    /// <summary>
    /// Keeps an eternal smelter's fuel full before it smelts.
    /// </summary>
    /// <param name="__instance">The smelter.</param>
    [HarmonyPatch(typeof(Smelter), nameof(Smelter.UpdateSmelter))]
    [HarmonyPrefix]
    public static void KeepSmelterFuelled(Smelter __instance)
    {
        if (__instance.m_nview != null && __instance.m_nview.IsValid() && __instance.m_nview.IsOwner() && EternalFireRules.Applies(__instance)
            && __instance.GetFuel() < __instance.m_maxFuel)
        {
            __instance.SetFuel(Mathf.Max(__instance.GetFuel(), __instance.m_maxFuel));
        }
    }

    /// <summary>
    /// Shows that a smelter's fire is eternal.
    /// </summary>
    /// <param name="__instance">The smelter.</param>
    /// <param name="__result">The hover text of the fuel switch.</param>
    [HarmonyPatch(typeof(Smelter), nameof(Smelter.OnHoverAddFuel))]
    [HarmonyPostfix]
    public static void SmelterHoverText(Smelter __instance, ref string __result)
    {
        if (EternalFireRules.Applies(__instance))
        {
            __result += Localization.instance.Localize(BurningLine);
        }
    }
}
