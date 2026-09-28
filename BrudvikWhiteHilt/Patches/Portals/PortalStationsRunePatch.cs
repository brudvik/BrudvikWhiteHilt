using BepInEx.Bootstrap;
using BrudvikWhiteHilt.Pieces.Portals.RuneRack;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Portals;

/// <summary>
/// Lets the stations of the Portal Stations mod carry the metals whose runes hang on rune posts nearby.
/// Its stations are not <see cref="TeleportWorld"/>s and check the inventory themselves.
/// </summary>
[HarmonyPatch]
public static class PortalStationsRunePatch
{
    /// <summary>
    /// GUID of the Portal Stations mod.
    /// </summary>
    public const string ModGuid = "RustyMods.PortalStations";

    private static bool Prepare()
    {
        return Chainloader.PluginInfos.ContainsKey(ModGuid) && TargetMethod() != null;
    }

    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("PortalStations.Stations.StationManagerHelpers:CanUsePortalStation");
    }

    // Arguments are (Player player, PortalStation station, bool msg); PortalStation is only known at runtime.
    private static bool Prefix(object[] __args, ref bool __result)
    {
        if (__args[0] is not Player player || __args[1] is not Component station)
        {
            return true;
        }

        int mask = RunePortalRules.GetRunes(station.transform.position, out bool everything);
        if ((mask == 0 && !everything) || !RunePortalRules.IsTeleportable(player.GetInventory(), mask, everything))
        {
            return true;
        }

        __result = true;
        return false;
    }
}

/// <summary>
/// Shows the runes near a Portal Stations station in its hover text.
/// </summary>
[HarmonyPatch]
public static class PortalStationsHoverPatch
{
    private static bool Prepare()
    {
        return Chainloader.PluginInfos.ContainsKey(PortalStationsRunePatch.ModGuid) && TargetMethod() != null;
    }

    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("PortalStations.Stations.PortalStation:GetHoverText");
    }

    private static void Postfix(Component __instance, ref string __result)
    {
        __result += RunePortalPatch.GetRuneHoverText(__instance.transform.position);
    }
}
