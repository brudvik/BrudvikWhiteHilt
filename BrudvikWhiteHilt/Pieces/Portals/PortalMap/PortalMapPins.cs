using BrudvikWhiteHilt.Patches.Portals;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Portals.PortalMap;

/// <summary>
/// Shows the portals from <see cref="PortalMapService"/> as pins on the local player's map. The pins are not saved
/// and cannot be removed by the player; they follow the server's list.
/// </summary>
public static class PortalMapPins
{
    private const string PortalIconPrefab = "portal_wood";

    private static readonly List<(Minimap.PinData Pin, PortalMapEntry Entry)> pins = new();
    private static List<PortalMapEntry> entries = new();
    private static Minimap boundMap;
    private static Sprite icon;

    /// <summary>
    /// Replaces the portals shown on the map.
    /// </summary>
    /// <param name="newEntries">The portals the player may see.</param>
    public static void SetEntries(List<PortalMapEntry> newEntries)
    {
        entries = newEntries;
        Refresh();
    }

    /// <summary>
    /// Puts the pins on the current map again, e.g. after the map was created or cleared its pins.
    /// </summary>
    public static void Refresh()
    {
        Minimap map = Minimap.instance;
        if (map == null)
        {
            return;
        }

        if (boundMap == map)
        {
            pins.ForEach(pin => map.RemovePin(pin.Pin));
        }

        pins.Clear();
        boundMap = map;
        Sprite sprite = GetIcon();
        foreach (PortalMapEntry entry in entries)
        {
            Minimap.PinData pin = map.AddPin(entry.Position, Minimap.PinType.None, Localization.instance.Localize(Label(entry)), false, false);
            pin.m_icon = sprite;
            pins.Add((pin, entry));
        }
    }

    /// <summary>
    /// Adds the pins again if the map lost them, which happens when it resets its pin list.
    /// </summary>
    /// <param name="map">The map.</param>
    public static void EnsurePins(Minimap map)
    {
        if (entries.Count > 0 && (boundMap != map || pins.Count == 0 || !map.m_pins.Contains(pins[0].Pin)))
        {
            Refresh();
        }
    }

    /// <summary>
    /// Text for the portal pin under the cursor on the large map, or null if there is none.
    /// </summary>
    /// <param name="worldPosition">World position under the cursor.</param>
    /// <param name="radius">How close the cursor must be, in metres.</param>
    /// <returns>Name, privacy and runes of the portal, localized.</returns>
    public static string GetHoverText(Vector3 worldPosition, float radius)
    {
        (Minimap.PinData Pin, PortalMapEntry Entry) closest = pins
            .Where(pin => pin.Pin.m_uiElement != null && pin.Pin.m_uiElement.gameObject.activeInHierarchy)
            .Select(pin => (pin, distance: Utils.DistanceXZ(worldPosition, pin.Pin.m_pos)))
            .Where(candidate => candidate.distance < radius)
            .OrderBy(candidate => candidate.distance)
            .Select(candidate => candidate.pin)
            .FirstOrDefault();
        if (closest.Entry == null)
        {
            return null;
        }

        return $"{Localization.instance.Localize(Label(closest.Entry))}\n{RunePortalPatch.FormatRunes(closest.Entry.RuneMask, closest.Entry.Everything)}";
    }

    private static string Label(PortalMapEntry entry)
    {
        string name = string.IsNullOrEmpty(entry.Name) ? "$whitehilt_portalmap_unnamed" : entry.Name;
        string privacy = entry.Privacy switch
        {
            PortalMapEntry.Public => string.Empty,
            PortalMapEntry.Private => " ($whitehilt_portalmap_private)",
            3 => " ($whitehilt_portalmap_group)",
            _ => " ($whitehilt_portalmap_guild)"
        };
        return name + privacy;
    }

    // The vanilla portal's build icon, so a portal pin looks like the portal in the build menu.
    private static Sprite GetIcon()
    {
        if (icon == null)
        {
            icon = ZNetScene.instance?.GetPrefab(PortalIconPrefab)?.GetComponent<Piece>()?.m_icon;
        }

        return icon;
    }
}
