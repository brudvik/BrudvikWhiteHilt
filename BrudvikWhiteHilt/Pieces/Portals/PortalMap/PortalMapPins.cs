using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Patches.Portals;
using System;
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
    private const int BadgeSize = 64;
    private const int BadgeIconSize = 44;
    private const float BadgeRimWidth = 4f;

    // Badge size against the map's normal pin size at the reference zoom; it grows zoomed in and shrinks zoomed out.
    private const float PinScale = 0.8f;
    private const float LargeReferenceZoom = 0.1f;
    private const float SmallReferenceZoom = 0.01f;
    private const float ZoomPower = 0.4f;
    private const float MinZoomScale = 0.5f;
    private const float MaxZoomScale = 1.6f;
    private const float SelectedScale = 1.4f;

    private static readonly Color32 portalRim = new(190, 130, 255, 255);
    private static readonly Color32 shipRim = new(255, 205, 90, 255);
    private static readonly Color32 badgeFill = new(18, 18, 22, 215);
    private static readonly List<(Minimap.PinData Pin, PortalMapEntry Entry)> pins = new();
    private static readonly Dictionary<Sprite, Sprite> badges = new();
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

    /// <summary>
    /// Sizes the portal and ship badges, also those of the travel map, by the zoom: larger zoomed in, smaller zoomed out.
    /// A badge marked double size (the portal picked in the travel map) is a little larger. Called after the map places
    /// its pins.
    /// </summary>
    /// <param name="map">The map.</param>
    public static void ScalePins(Minimap map)
    {
        if (badges.Count == 0)
        {
            return;
        }

        bool large = map.m_mode == Minimap.MapMode.Large;
        float zoom = large ? map.LargeZoom : map.SmallZoom;
        float reference = large ? LargeReferenceZoom : SmallReferenceZoom;
        float zoomScale = Mathf.Clamp(Mathf.Pow(reference / Mathf.Max(zoom, 0.0001f), ZoomPower), MinZoomScale, MaxZoomScale);
        float baseSize = (large ? map.m_pinSizeLarge : map.m_pinSizeSmall) * PinScale * zoomScale;
        foreach (Minimap.PinData pin in map.m_pins)
        {
            if (pin.m_uiElement == null || pin.m_icon == null || !badges.ContainsValue(pin.m_icon))
            {
                continue;
            }

            float size = pin.m_doubleSize ? baseSize * SelectedScale : baseSize;
            if (!Mathf.Approximately(pin.m_uiElement.rect.width, size))
            {
                pin.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
                pin.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
            }
        }
    }

    /// <summary>
    /// Draws the player and ship markers above all pins, so a portal or the ship you sail cannot hide you.
    /// </summary>
    /// <param name="map">The map.</param>
    public static void MarkersOnTop(Minimap map)
    {
        BringAbove(map.m_largeShipMarker, map.m_pinRootLarge);
        BringAbove(map.m_largeMarker, map.m_pinRootLarge);
        BringAbove(map.m_smallShipMarker, map.m_pinRootSmall);
        BringAbove(map.m_smallMarker, map.m_pinRootSmall);
    }

    // Moves the marker's branch after the pins' branch under their closest shared parent.
    private static void BringAbove(Transform marker, Transform pinRoot)
    {
        if (marker == null || pinRoot == null)
        {
            return;
        }

        Transform markerBranch = marker;
        while (markerBranch.parent != null && !pinRoot.IsChildOf(markerBranch.parent))
        {
            markerBranch = markerBranch.parent;
        }

        Transform shared = markerBranch.parent;
        if (shared == null || pinRoot.IsChildOf(markerBranch))
        {
            return;
        }

        Transform pinBranch = pinRoot;
        while (pinBranch.parent != shared)
        {
            pinBranch = pinBranch.parent;
        }

        if (markerBranch.GetSiblingIndex() < pinBranch.GetSiblingIndex())
        {
            markerBranch.SetSiblingIndex(pinBranch.GetSiblingIndex());
        }
    }

    /// <summary>
    /// The portal pin's icon: the vanilla portal on a dark disc with a purple rim.
    /// </summary>
    /// <returns>The icon, or null before the game has loaded.</returns>
    public static Sprite GetPortalBadge()
    {
        return GetIcon(new PortalMapEntry());
    }

    // A pin's label: the ship's name, or the portal's name with whether it is private, for a group or for a guild.
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
                return GetBadge(shipIcon, shipRim);
            }
        }

        if (portalIcon == null)
        {
            portalIcon = GetPrefabPiece(PortalIconPrefab.GetStableHashCode())?.m_icon;
        }

        return GetBadge(portalIcon, portalRim);
    }

    // The build icons alone are thin and brown and vanish on busy terrain, so they sit on a dark disc with a coloured rim.
    private static Sprite GetBadge(Sprite icon, Color32 rim)
    {
        if (icon == null)
        {
            return null;
        }

        if (badges.TryGetValue(icon, out Sprite badge))
        {
            return badge;
        }

        try
        {
            Color32[] iconPixels = VisualHelper.ReadPixels(icon.texture, BadgeIconSize, BadgeIconSize, icon.textureRect);
            Color32[] pixels = new Color32[BadgeSize * BadgeSize];
            float center = (BadgeSize - 1) / 2f;
            float radius = BadgeSize / 2f - 1f;
            int offset = (BadgeSize - BadgeIconSize) / 2;
            for (int y = 0; y < BadgeSize; y++)
            {
                for (int x = 0; x < BadgeSize; x++)
                {
                    float distance = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                    Color32 color = distance > radius - BadgeRimWidth ? rim : badgeFill;
                    color.a = (byte)(color.a * Mathf.Clamp01(radius + 1f - distance));

                    int iconX = x - offset;
                    int iconY = y - offset;
                    if (iconX >= 0 && iconX < BadgeIconSize && iconY >= 0 && iconY < BadgeIconSize)
                    {
                        Color32 source = iconPixels[iconY * BadgeIconSize + iconX];
                        Color32 lit = new((byte)Mathf.Min(255, source.r * 1.3f), (byte)Mathf.Min(255, source.g * 1.3f), (byte)Mathf.Min(255, source.b * 1.3f), 255);
                        color = Color32.Lerp(color, lit, source.a / 255f);
                    }

                    pixels[y * BadgeSize + x] = color;
                }
            }

            Texture2D texture = VisualHelper.CreateTexture($"{icon.name}_mapbadge", BadgeSize, BadgeSize, pixels);
            badge = Sprite.Create(texture, new Rect(0, 0, BadgeSize, BadgeSize), new Vector2(0.5f, 0.5f));
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Portal map: keeping the plain icon for {icon.name}: {ex.Message}");
            badge = icon;
        }

        badges[icon] = badge;
        return badge;
    }

    private static Piece GetPrefabPiece(int prefabHash)
    {
        GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefabHash) : null;
        return prefab != null ? prefab.GetComponent<Piece>() : null;
    }
}
