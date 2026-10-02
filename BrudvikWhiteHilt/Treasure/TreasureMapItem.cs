using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Treasure;

/// <summary>
/// A landmark drawn on a treasure map: a vanilla location of a kind Munin's Perch knows.
/// </summary>
public readonly struct TreasureLandmark
{
    /// <summary>
    /// Creates a landmark.
    /// </summary>
    /// <param name="kind">Discovery kind key, e.g. <c>l:burialchamber</c>.</param>
    /// <param name="position">World position; y is ignored.</param>
    public TreasureLandmark(string kind, Vector2 position)
    {
        Kind = kind;
        Position = position;
    }

    /// <summary>Discovery kind key of the location.</summary>
    public string Kind { get; }

    /// <summary>World x and z.</summary>
    public Vector2 Position { get; }
}

/// <summary>
/// Where a map's treasure lies and what the map shows.
/// </summary>
public sealed class TreasureSite
{
    /// <summary>World x and z of the treasure.</summary>
    public Vector2 Chest { get; set; }

    /// <summary>World x and z of the middle of the map.</summary>
    public Vector2 Centre { get; set; }

    /// <summary>Width of the land on the map, in metres.</summary>
    public float Size { get; set; }

    /// <summary>Quarter turns the map is drawn with, 0 to 3.</summary>
    public int Rotation { get; set; }

    /// <summary>Seed of the map's stains, tears and faded parts.</summary>
    public int Seed { get; set; }

    /// <summary>The landmarks on the map.</summary>
    public List<TreasureLandmark> Landmarks { get; } = new();
}

/// <summary>
/// The treasure map item: what is kept in a map's own item data, so it can be traded, stored and read by anyone.
/// </summary>
public static class TreasureMapItem
{
    /// <summary>Prefab name of the map.</summary>
    public const string PrefabName = "WhiteHiltTreasureMap";

    private const string IdKey = "whitehilt_treasure_id";
    private const string SiteKey = "whitehilt_treasure_site";
    private const string MarksKey = "whitehilt_treasure_marks";
    private const string MoundKey = "whitehilt_treasure_mound";
    private const string StateKey = "whitehilt_treasure_state";
    private const string PaidKey = "whitehilt_treasure_paid";
    private const string Plundered = "plundered";

    /// <summary>
    /// Whether an item is a treasure map.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>True for a treasure map.</returns>
    public static bool IsMap(ItemDrop.ItemData item)
    {
        return item?.m_dropPrefab != null && item.m_dropPrefab.name == PrefabName;
    }

    /// <summary>
    /// The map's treasure id, or null for a map nobody has marked yet.
    /// </summary>
    /// <param name="item">The map.</param>
    /// <returns>The id.</returns>
    public static string GetId(ItemDrop.ItemData item)
    {
        return Get(item, IdKey);
    }

    /// <summary>
    /// Gives an unmarked map an id, so a treasure can be buried for it.
    /// </summary>
    /// <param name="item">The map.</param>
    /// <param name="paid">Coins paid for it, given back if no treasure can be buried.</param>
    /// <returns>The id.</returns>
    public static string Assign(ItemDrop.ItemData item, int paid)
    {
        string id = GetId(item);
        if (id != null)
        {
            return id;
        }

        id = Guid.NewGuid().ToString("N");
        item.m_customData[IdKey] = id;
        item.m_customData[PaidKey] = paid.ToString(CultureInfo.InvariantCulture);
        return id;
    }

    /// <summary>
    /// Coins paid for the map.
    /// </summary>
    /// <param name="item">The map.</param>
    /// <returns>The coins, 0 for a map not bought.</returns>
    public static int GetPaid(ItemDrop.ItemData item)
    {
        return int.TryParse(Get(item, PaidKey), NumberStyles.Integer, CultureInfo.InvariantCulture, out int paid) ? paid : 0;
    }

    /// <summary>
    /// Whether the treasure of the map has been dug up.
    /// </summary>
    /// <param name="item">The map.</param>
    /// <returns>True once plundered.</returns>
    public static bool IsPlundered(ItemDrop.ItemData item)
    {
        return Get(item, StateKey) == Plundered;
    }

    /// <summary>
    /// Marks the map's treasure as dug up.
    /// </summary>
    /// <param name="item">The map.</param>
    public static void SetPlundered(ItemDrop.ItemData item)
    {
        item.m_customData[StateKey] = Plundered;
    }

    /// <summary>
    /// The buried mound of the map's treasure.
    /// </summary>
    /// <param name="item">The map.</param>
    /// <returns>Its object id, or <see cref="ZDOID.None"/>.</returns>
    public static ZDOID GetMound(ItemDrop.ItemData item)
    {
        string[] parts = (Get(item, MoundKey) ?? string.Empty).Split(':');
        return parts.Length == 2 && long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out long user)
            && uint.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint id)
            ? new ZDOID(user, id)
            : ZDOID.None;
    }

    /// <summary>
    /// Writes where the treasure lies into the map.
    /// </summary>
    /// <param name="item">The map.</param>
    /// <param name="site">The site.</param>
    /// <param name="mound">The buried mound.</param>
    public static void SetSite(ItemDrop.ItemData item, TreasureSite site, ZDOID mound)
    {
        item.m_customData[SiteKey] = string.Join(";", new[]
        {
            Format(site.Chest.x), Format(site.Chest.y), Format(site.Centre.x), Format(site.Centre.y), Format(site.Size),
            site.Rotation.ToString(CultureInfo.InvariantCulture), site.Seed.ToString(CultureInfo.InvariantCulture)
        });
        item.m_customData[MarksKey] = string.Join(";", site.Landmarks.Select(mark => $"{mark.Kind}|{Format(mark.Position.x)}|{Format(mark.Position.y)}"));
        item.m_customData[MoundKey] = $"{mound.UserID.ToString(CultureInfo.InvariantCulture)}:{mound.ID.ToString(CultureInfo.InvariantCulture)}";
    }

    /// <summary>
    /// Reads where the treasure lies.
    /// </summary>
    /// <param name="item">The map.</param>
    /// <returns>The site, or null while the map is not marked yet.</returns>
    public static TreasureSite GetSite(ItemDrop.ItemData item)
    {
        string[] parts = (Get(item, SiteKey) ?? string.Empty).Split(';');
        if (parts.Length < 7)
        {
            return null;
        }

        TreasureSite site = new()
        {
            Chest = new Vector2(Parse(parts[0]), Parse(parts[1])),
            Centre = new Vector2(Parse(parts[2]), Parse(parts[3])),
            Size = Parse(parts[4]),
            Rotation = int.TryParse(parts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out int rotation) ? rotation & 3 : 0,
            Seed = int.TryParse(parts[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out int seed) ? seed : 0
        };
        foreach (string mark in (Get(item, MarksKey) ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string[] fields = mark.Split('|');
            if (fields.Length == 3)
            {
                site.Landmarks.Add(new TreasureLandmark(fields[0], new Vector2(Parse(fields[1]), Parse(fields[2]))));
            }
        }

        return site;
    }

    /// <summary>
    /// Finds a map with the given id in an inventory.
    /// </summary>
    /// <param name="inventory">The inventory.</param>
    /// <param name="id">Treasure id.</param>
    /// <returns>The map, or null.</returns>
    public static ItemDrop.ItemData Find(Inventory inventory, string id)
    {
        return inventory?.GetAllItems().FirstOrDefault(item => IsMap(item) && GetId(item) == id);
    }

    /// <summary>
    /// Whether a player carries the map of a treasure that is still in the ground.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="id">Treasure id.</param>
    /// <returns>True if the player has the unplundered map.</returns>
    public static bool Carries(Player player, string id)
    {
        ItemDrop.ItemData map = Find(player?.GetInventory(), id);
        return map != null && !IsPlundered(map);
    }

    private static string Get(ItemDrop.ItemData item, string key)
    {
        return item?.m_customData != null && item.m_customData.TryGetValue(key, out string value) ? value : null;
    }

    private static string Format(float value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static float Parse(string value)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) ? result : 0f;
    }
}
