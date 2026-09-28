using BrudvikWhiteHilt.Items.Runes;
using BrudvikWhiteHilt.Pieces.Portals.RuneRack;
using HarmonyLib;
using System;
using System.Linq;

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
        if (__instance.m_allowAllItems)
        {
            return;
        }

        int mask = RunePortalRules.GetRunes(__instance.transform.position, out bool everything);
        if (everything)
        {
            __result += Localization.instance.Localize("\n<color=orange>$whitehilt_portal_everything</color>");
        }
        else if (mask != 0)
        {
            string runes = string.Join(", ", Enumerable.Range(0, WhiteHiltRuneBase.Count)
                .Where(i => (mask & (1 << i)) != 0)
                .Select(i => WhiteHiltRuneBase.Get(i)?.NameToken)
                .Where(name => name != null));
            __result += Localization.instance.Localize($"\n$whitehilt_portal_runes: {runes}");
        }
    }

    private static void Enter(TeleportWorld portal)
    {
        activeMask = RunePortalRules.GetRunes(portal.transform.position, out activeEverything);
        active = true;
    }
}
