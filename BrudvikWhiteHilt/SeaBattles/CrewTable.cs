using System;
using System.Collections.Generic;
using System.Globalization;

namespace BrudvikWhiteHilt.SeaBattles;

/// <summary>One kind of creature in a crew and how many of it.</summary>
public readonly struct CrewEntry
{
    /// <summary>Creates an entry.</summary>
    /// <param name="prefab">Prefab name of the creature.</param>
    /// <param name="count">How many of it.</param>
    /// <param name="archer">Whether it shoots from a distance and holds its post.</param>
    public CrewEntry(string prefab, int count, bool archer)
    {
        Prefab = prefab;
        Count = count;
        Archer = archer;
    }

    /// <summary>Prefab name of the creature.</summary>
    public string Prefab { get; }

    /// <summary>How many of it.</summary>
    public int Count { get; }

    /// <summary>Whether it is an archer; the others fight in melee.</summary>
    public bool Archer { get; }
}

/// <summary>The crew that belongs to one global key.</summary>
public sealed class CrewTier
{
    /// <summary>Creates a tier.</summary>
    /// <param name="index">Position among the valid tiers of the text; this is what the ship's ZDO stores.</param>
    /// <param name="key">The global key that selects the tier.</param>
    /// <param name="entries">The creatures.</param>
    public CrewTier(int index, string key, IReadOnlyList<CrewEntry> entries)
    {
        Index = index;
        Key = key;
        Entries = entries;
    }

    /// <summary>Position among the valid tiers of the text.</summary>
    public int Index { get; }

    /// <summary>The global key that selects the tier.</summary>
    public string Key { get; }

    /// <summary>The creatures.</summary>
    public IReadOnlyList<CrewEntry> Entries { get; }
}

/// <summary>
/// Reads the <c>[SeaBattles] CrewTiers</c> text ("key=Prefab:count,Prefab:count; key=...") and picks the tier the world
/// has reached. Pure, so it is tested without the game; it never throws on a broken text.
/// </summary>
public static class CrewTable
{
    /// <summary>
    /// Reads the text. Spaces are ignored; a part without a key, without a valid creature or with a broken count is
    /// skipped, and so is a creature without a name.
    /// </summary>
    /// <param name="text">The config text.</param>
    /// <returns>The tiers in the order of the text.</returns>
    public static IReadOnlyList<CrewTier> Parse(string text)
    {
        List<CrewTier> tiers = new();
        foreach (string part in (text ?? string.Empty).Split(';'))
        {
            int equals = part.IndexOf('=');
            if (equals <= 0)
            {
                continue;
            }

            string key = part.Substring(0, equals).Trim();
            List<CrewEntry> entries = ParseEntries(part.Substring(equals + 1));
            if (key.Length > 0 && entries.Count > 0)
            {
                tiers.Add(new CrewTier(tiers.Count, key, entries));
            }
        }

        return tiers;
    }

    /// <summary>
    /// Reads a plain creature list ("Prefab:count, Prefab:count"), as the raid guards are written.
    /// </summary>
    /// <param name="text">The list.</param>
    /// <returns>The valid entries.</returns>
    public static List<CrewEntry> ParseEntries(string text)
    {
        List<CrewEntry> entries = new();
        foreach (string item in (text ?? string.Empty).Split(','))
        {
            string[] fields = item.Split(':');
            string prefab = fields[0].Trim();
            if (prefab.Length == 0)
            {
                continue;
            }

            int count = 1;
            if (fields.Length > 1 && (!int.TryParse(fields[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out count) || count <= 0))
            {
                continue;
            }

            entries.Add(new CrewEntry(prefab, count, IsArcher(prefab)));
        }

        return entries;
    }

    /// <summary>
    /// Picks the tier of the last key that is set.
    /// </summary>
    /// <param name="tiers">The tiers, from <see cref="Parse"/>.</param>
    /// <param name="hasKey">Tells whether a global key is set.</param>
    /// <returns>The tier, or null when no key is set.</returns>
    public static CrewTier Pick(IReadOnlyList<CrewTier> tiers, Func<string, bool> hasKey)
    {
        CrewTier picked = null;
        if (tiers == null || hasKey == null)
        {
            return null;
        }

        foreach (CrewTier tier in tiers)
        {
            if (hasKey(tier.Key))
            {
                picked = tier;
            }
        }

        return picked;
    }

    /// <summary>
    /// Whether a creature shoots from a distance, by its prefab name.
    /// </summary>
    /// <param name="prefab">The prefab name.</param>
    /// <returns>True for names with Ranged or Archer in them.</returns>
    public static bool IsArcher(string prefab)
    {
        return prefab != null && (prefab.IndexOf("Ranged", StringComparison.OrdinalIgnoreCase) >= 0
            || prefab.IndexOf("Archer", StringComparison.OrdinalIgnoreCase) >= 0);
    }
}
