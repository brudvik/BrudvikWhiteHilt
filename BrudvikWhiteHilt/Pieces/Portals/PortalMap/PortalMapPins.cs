using BrudvikWhiteHilt.Patches.Portals;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Portals.PortalMap;

/// <summary>
/// Shows the portals and ships from <see cref="PortalMapService"/> as pins on the local player's map. The pins are
/// not saved and cannot be removed by the player; they follow the server's list.
/// </summary>
public static class PortalMapPins
{
    private const string PortalIconPrefab = "portal_wood";

    private static readonly List<(Minimap.PinData Pin, PortalMapEntry Entry)> pins = new();
    private static List<PortalMapEntry> entries = new();
    private static Minimap boundMap;
    private static Sprite portalIcon;

    /// <summary>
    /// Replaces the portals and ships shown on the map.
    /// </summary>
    /// <param name="newEntries">What the player may see.</param>
    public static void SetEntries(List<PortalMapEntry> newEntries)
    {
        bool sameMarkers = newEntries.Count == entries.Count && newEntries.Zip(entries, (a, b) => a.SameMarker(b)).All(same => same);
        entries = newEntries;
        Minimap map = Minimap.instance;
        if (!sameMarkers || map == null || boundMap != map || pins.Count != entries.Count)
        {
            Refresh();
            return;
        }

        // Only positions or runes changed, e.g. a sailing ship: move the pins instead of making them again.
        for (int i = 0; i < pins.Count; i++)
        {
            pins[i].Pin.m_pos = entries[i].Position;
            pins[i] = (pins[i].Pin, entries[i]);
        }

        map.m_pinUpdateRequired = true;
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
        foreach (PortalMapEntry entry in entries)
        {
            Minimap.PinData pin = map.AddPin(entry.Position, Minimap.PinType.None, Localization.instance.Localize(Label(entry)), false, false);
            pin.m_icon = GetIcon(entry);
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
    /// Text for the pin under the cursor on the large map, or null if there is none.
    /// </summary>
    /// <param name="worldPosition">World position under the cursor.</param>
    /// <param name="radius">How close the cursor must be, in metres.</param>
    /// <returns>A portal's name, privacy and runes, or a ship's type and builder, localized.</returns>
    public static string GetHoverText(Vector3 worldPosition, float radius)
    {
        PortalMapEntry entry = pins
            .Where(pin => pin.Pin.m_uiElement != null && pin.Pin.m_uiElement.gameObject.activeInHierarchy)
            .Select(pin => (pin.Entry, Distance: Utils.DistanceXZ(worldPosition, pin.Pin.m_pos)))
            .Where(candidate => candidate.Distance < radius)
            .OrderBy(candidate => candidate.Distance)
            .Select(candidate => candidate.Entry)
            .FirstOrDefault();
        if (entry == null)
        {
            return null;
        }

        string label = Localization.instance.Localize(Label(entry));
        if (entry.Kind == PortalMapEntry.ShipKind)
        {
            return string.IsNullOrEmpty(entry.Builder)
                ? label
                : $"{label}\n{Localization.instance.Localize("$whitehilt_shipmap_builder")}: {entry.Builder}";
        }

        return $"{label}\n{RunePortalPatch.FormatRunes(entry.RuneMask, entry.Everything)}";
    }

    private static string Label(PortalMapEntry entry)
    {
        if (entry.Kind == PortalMapEntry.ShipKind)
        {
            Piece ship = GetPrefabPiece(entry.Prefab);
            return ship != null ? ship.m_name : "$whitehilt_shipmap_ship";
        }

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

    // Portals get the vanilla portal's build icon, ships their own, so a Karve and a Longship look different.
    private static Sprite GetIcon(PortalMapEntry entry)
    {
        if (entry.Kind == PortalMapEntry.ShipKind)
        {
            Sprite shipIcon = GetPrefabPiece(entry.Prefab)?.m_icon;
            if (shipIcon != null)
            {
                return shipIcon;
            }
        }

        if (portalIcon == null)
        {
            portalIcon = GetPrefabPiece(PortalIconPrefab.GetStableHashCode())?.m_icon;
        }

        return portalIcon;
    }

    private static Piece GetPrefabPiece(int prefabHash)
    {
        GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefabHash) : null;
        return prefab != null ? prefab.GetComponent<Piece>() : null;
    }
}
