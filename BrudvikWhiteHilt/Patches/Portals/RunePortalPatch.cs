using BrudvikWhiteHilt.Items.Runes;
using BrudvikWhiteHilt.Pieces.Portals.RuneRack;
using HarmonyLib;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Portals;

/// <summary>
/// Lets portals carry the metals whose runes hang on rune posts nearby. Works for every <see cref="TeleportWorld"/>,
/// including portals from other mods that build on it.
/// </summary>
[HarmonyPatch]
public static class RunePortalPatch
{
    private static bool active;
    private static int activeMask;
    private static bool activeEverything;

    // Teleport and UpdatePortal both ask the player's inventory, which has no idea which portal is asking.
    // So the portal's runes are set here for the duration of the call.
    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Teleport))]
    [HarmonyPrefix]
    private static void TeleportPrefix(TeleportWorld __instance)
    {
        Enter(__instance);
    }

    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Teleport))]
    [HarmonyFinalizer]
    private static Exception TeleportFinalizer(Exception __exception)
    {
        active = false;
        return __exception;
    }

    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.UpdatePortal))]
    [HarmonyPrefix]
    private static void UpdatePortalPrefix(TeleportWorld __instance)
    {
        Enter(__instance);
    }

    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.UpdatePortal))]
    [HarmonyFinalizer]
    private static Exception UpdatePortalFinalizer(Exception __exception)
    {
        active = false;
        return __exception;
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.IsTeleportable))]
    [HarmonyPostfix]
    private static void IsTeleportablePostfix(Inventory __instance, ref bool __result)
    {
        if (active && !__result && (activeMask != 0 || activeEverything))
        {
            __result = RunePortalRules.IsTeleportable(__instance, activeMask, activeEverything);
        }
    }

    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.GetHoverText))]
    [HarmonyPostfix]
    private static void GetHoverTextPostfix(TeleportWorld __instance, ref string __result)
    {
        if (!__instance.m_allowAllItems)
        {
            __result += GetRuneHoverText(__instance.transform.position);
        }
    }

    /// <summary>
    /// Hover line telling which runes a portal at <paramref name="portalPosition"/> has, or empty without runes.
    /// </summary>
    /// <param name="portalPosition">Position of the portal.</param>
    /// <returns>The localized line, starting with a newline.</returns>
    internal static string GetRuneHoverText(Vector3 portalPosition)
    {
        int mask = RunePortalRules.GetRunes(portalPosition, out bool everything);
        return mask == 0 && !everything ? string.Empty : "\n" + FormatRunes(mask, everything);
    }

    /// <summary>
    /// Localized text for a portal's runes: "everything", the rune names, or "no runes".
    /// </summary>
    /// <param name="mask">Bit mask of the runes near the portal.</param>
    /// <param name="everything">True if one post near the portal holds every rune.</param>
    /// <returns>The localized text.</returns>
    internal static string FormatRunes(int mask, bool everything)
    {
        if (everything)
        {
            return Localization.instance.Localize("<color=orange>$whitehilt_portal_everything</color>");
        }

        if (mask == 0)
        {
            return Localization.instance.Localize("$whitehilt_runerack_empty");
        }

        string runes = string.Join(", ", Enumerable.Range(0, WhiteHiltRuneBase.Count)
            .Where(i => (mask & (1 << i)) != 0)
            .Select(i => WhiteHiltRuneBase.Get(i)?.NameToken)
            .Where(name => name != null));
        return Localization.instance.Localize($"$whitehilt_portal_runes: {runes}");
    }

    private static void Enter(TeleportWorld portal)
    {
        activeMask = RunePortalRules.GetRunes(portal.transform.position, out activeEverything);
        active = true;
    }
}
