using System;
using System.Collections.Generic;
using System.Globalization;
using Biome = Heightmap.Biome;

namespace BrudvikWhiteHilt.Helpers;

/// <summary>
/// The biomes a character has been in. The game keeps them only as the names of the biome sectors it found, translated
/// into the language played at the time and changed by the world's variants (a prefix, a suffix or a name of its own),
/// so a name alone often does not tell the biome. Each biome entered is therefore also saved by its identity, and the
/// names saved before are matched every way they can be.
/// </summary>
public static class KnownBiomes
{
    // The key the bestiary saved them under first; kept, so what it saved still counts.
    private const string Key = "whitehilt_bestiary_biomes";

    private static Dictionary<string, Biome> sectorNames = new();
    private static int sectorCount = -1;
    private static string sectorLanguage;

    /// <summary>
    /// Saves a biome the character has entered. Call each time the game notes one, also when it knew it already, so a
    /// biome entered before this was saved is caught on the next visit.
    /// </summary>
    /// <param name="player">The character.</param>
    /// <param name="biome">The biome entered.</param>
    public static void Remember(Player player, Biome biome)
    {
        if (player == null || biome == Biome.None)
        {
            return;
        }

        Biome known = Saved(player);
        if ((known & biome) != biome)
        {
            player.m_customData[Key] = ((int)(known | biome)).ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// The biomes the character has been in: those saved, and those the game's names can be matched to.
    /// </summary>
    /// <param name="player">The character.</param>
    /// <returns>The biomes as flags; None for no character.</returns>
    public static Biome Of(Player player)
    {
        if (player == null)
        {
            return Biome.None;
        }

        Biome known = Saved(player);
        foreach (string name in player.m_knownBiome)
        {
            known |= FromName(name);
        }

        return known;
    }

    /// <summary>
    /// The biome a name of the game's list stands for, or None: the name of a sector of this world, the biome's own
    /// translated name, its translation key, or its English name with or without spaces ("Black Forest").
    /// </summary>
    /// <param name="name">A name from <see cref="Player.m_knownBiome"/>.</param>
    /// <returns>The biome.</returns>
    public static Biome FromName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return Biome.None;
        }

        if (SectorNames().TryGetValue(name, out Biome sector))
        {
            return sector;
        }

        string compact = name.Replace(" ", string.Empty);
        foreach (Biome biome in Enum.GetValues(typeof(Biome)))
        {
            if (biome == Biome.None || biome == Biome.All)
            {
                continue;
            }

            string token = BiomeSector.GetBiomeName(biome);
            string localized = Localization.instance?.Localize(token);
            if (name == token || name == localized
                || compact.IndexOf(biome.ToString(), StringComparison.OrdinalIgnoreCase) >= 0
                || (!string.IsNullOrEmpty(localized) && name.IndexOf(localized, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return biome;
            }
        }

        return Biome.None;
    }

    private static Biome Saved(Player player)
    {
        return player.m_customData.TryGetValue(Key, out string saved)
            && int.TryParse(saved, NumberStyles.Integer, CultureInfo.InvariantCulture, out int mask) ? (Biome)mask : Biome.None;
    }

    // Every sector name of this world in the language played now, made anew when the world or the language changes.
    private static Dictionary<string, Biome> SectorNames()
    {
        List<BiomeSector> sectors = ZoneSystem.instance?.m_biomeSectors;
        string language = Localization.instance?.GetSelectedLanguage();
        int count = sectors?.Count ?? 0;
        if (count == sectorCount && language == sectorLanguage)
        {
            return sectorNames;
        }

        Dictionary<string, Biome> names = new();
        if (sectors != null)
        {
            foreach (BiomeSector sector in sectors)
            {
                string name = sector?.GetName();
                if (!string.IsNullOrEmpty(name) && sector.Biome != Biome.None && !names.ContainsKey(name))
                {
                    names[name] = sector.Biome;
                }
            }
        }

        sectorNames = names;
        sectorCount = count;
        sectorLanguage = language;
        return names;
    }
}
