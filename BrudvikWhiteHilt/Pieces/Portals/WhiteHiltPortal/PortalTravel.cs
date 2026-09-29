using BrudvikWhiteHilt.Pieces.Portals.RuneRack;
using BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Portals.WhiteHiltPortal;

/// <summary>
/// The portals the local player can travel to, their home portal, and the journey itself.
/// </summary>
public static class PortalTravel
{
    private const string HomeKey = "whitehilt_home_portal";

    private static List<PortalDestination> destinations = new();

    /// <summary>
    /// Raised when the server sends a new list.
    /// </summary>
    public static event Action Changed;

    /// <summary>
    /// The portals the local player may travel to.
    /// </summary>
    public static IReadOnlyList<PortalDestination> Destinations => destinations;

    /// <summary>
    /// Replaces the list; called when the server sends it.
    /// </summary>
    /// <param name="newDestinations">The portals the player may travel to.</param>
    public static void SetDestinations(List<PortalDestination> newDestinations)
    {
        destinations = newDestinations;
        Changed?.Invoke();
    }

    /// <summary>
    /// Finds a portal by id.
    /// </summary>
    /// <param name="id">The portal's id.</param>
    /// <returns>The portal, or null.</returns>
    public static PortalDestination Find(string id)
    {
        return string.IsNullOrEmpty(id) ? null : destinations.FirstOrDefault(destination => destination.Id == id);
    }

    /// <summary>
    /// Id of the player's home portal, or empty.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The id.</returns>
    public static string GetHome(Player player)
    {
        return player.m_customData.TryGetValue(HomeKey, out string id) ? id : string.Empty;
    }

    /// <summary>
    /// Makes a portal the player's home. It is saved with the character.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="id">The portal's id.</param>
    public static void SetHome(Player player, string id)
    {
        player.m_customData[HomeKey] = id;
        player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_portal_home_set");
    }

    /// <summary>
    /// Takes the player to a portal, if the world and their inventory allow it.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="destination">The portal to travel to.</param>
    /// <param name="runesFrom">Position whose rune posts count, i.e. the portal travelled from; null for the ordinary rules.</param>
    /// <returns>True if the player is on their way.</returns>
    public static bool TryTravel(Player player, PortalDestination destination, Vector3? runesFrom)
    {
        if (destination == null)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_portal_gone");
            return false;
        }

        if (!TryTravelTo(player, destination.ArrivalPoint(), destination.Yaw, runesFrom))
        {
            return false;
        }

        ShipPortalArrival.Expect(destination.ShipId);
        return true;
    }

    /// <summary>
    /// Takes the player to a spot in the world, if the world and their inventory allow it, as a portal would.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="position">Where to arrive.</param>
    /// <param name="yaw">Which way to face, in degrees.</param>
    /// <param name="runesFrom">Position whose rune posts count; null for the ordinary rules.</param>
    /// <returns>True if the player is on their way.</returns>
    public static bool TryTravelTo(Player player, Vector3 position, float yaw, Vector3? runesFrom)
    {
        if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoPortals))
        {
            player.Message(MessageHud.MessageType.Center, "$msg_blocked");
            return false;
        }

        if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoBossPortals)
            && (RandEventSystem.instance.GetBossEvent() != null || (ZoneSystem.instance.GetGlobalKey(GlobalKeys.activeBosses, out float bosses) && bosses > 0f)))
        {
            player.Message(MessageHud.MessageType.Center, "$msg_blockedbyboss");
            return false;
        }

        if (!CanCarry(player, runesFrom))
        {
            player.Message(MessageHud.MessageType.Center, "$msg_noteleport");
            return false;
        }

        player.TeleportTo(position, Quaternion.Euler(0f, yaw, 0f), distantTeleport: true);
        Game.instance.IncrementPlayerStat(PlayerStatType.PortalsUsed);
        return true;
    }

    private static bool CanCarry(Player player, Vector3? runesFrom)
    {
        if (PortalSettings.TeleportAnything)
        {
            return true;
        }

        if (!runesFrom.HasValue)
        {
            return player.IsTeleportable(allowAllItems: false);
        }

        int mask = RunePortalRules.GetRunes(runesFrom.Value, out bool everything);
        return RunePortalRules.IsTeleportable(player.GetInventory(), mask, everything);
    }
}
