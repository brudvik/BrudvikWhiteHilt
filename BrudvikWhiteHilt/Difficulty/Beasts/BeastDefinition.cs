using BrudvikWhiteHilt.Helpers;
using System;
using System.Linq;

namespace BrudvikWhiteHilt.Difficulty.Beasts;

/// <summary>
/// A black 5 star beast of the dark hour: the vanilla creature it is cloned from, where it comes and what unlocks it.
/// </summary>
public sealed class BeastDefinition
{
    /// <summary>
    /// Every beast, one per biome, and two at sea.
    /// </summary>
    public static readonly BeastDefinition[] All =
    {
        new("BlackTroll", "Black Troll", "Troll", "TrophyFrostTroll", Heightmap.Biome.BlackForest, false, "defeated_gdking"),
        new("BlackAbomination", "Black Abomination", "Abomination", "TrophyAbomination", Heightmap.Biome.Swamp, false, "defeated_bonemass"),
        new("BlackStoneGolem", "Black Stone Golem", "StoneGolem", "TrophySGolem", Heightmap.Biome.Mountain, false, "defeated_dragon"),
        new("BlackFulingBerserker", "Black Fuling Berserker", "GoblinBrute", "TrophyGoblinBrute", Heightmap.Biome.Plains, false, "defeated_goblinking"),
        new("BlackSeekerSoldier", "Black Seeker Soldier", "SeekerBrute", "TrophySeekerBrute", Heightmap.Biome.Mistlands, false, "defeated_queen"),
        new("BlackMorgen", "Black Morgen", "Morgen", "TrophyMorgen", Heightmap.Biome.AshLands, false, "defeated_fader"),
        new("BlackSerpent", "Black Serpent", "Serpent", "TrophySerpent", Heightmap.Biome.Ocean, true, "defeated_bonemass"),
        new("BlackBonemaw", "Black Bonemaw", "BonemawSerpent", "TrophyBonemawSerpent", Heightmap.Biome.AshLands, true, "defeated_fader")
    };

    /// <summary>
    /// Global keys of the defeated bosses.
    /// </summary>
    public static readonly string[] BossKeys =
    {
        "defeated_eikthyr", "defeated_gdking", "defeated_bonemass", "defeated_dragon", "defeated_goblinking", "defeated_queen", "defeated_fader"
    };

    private BeastDefinition(string key, string englishName, string baseCreature, string baseTrophy, Heightmap.Biome biome, bool sea, string bossKey)
    {
        Key = key;
        EnglishName = englishName;
        BaseCreature = baseCreature;
        BaseTrophy = baseTrophy;
        Biome = biome;
        Sea = sea;
        BossKey = bossKey;
    }

    /// <summary>Identifier, used in config and commands.</summary>
    public string Key { get; }

    /// <summary>English name.</summary>
    public string EnglishName { get; }

    /// <summary>Vanilla creature it is cloned from.</summary>
    public string BaseCreature { get; }

    /// <summary>Vanilla trophy its trophy is cloned from.</summary>
    public string BaseTrophy { get; }

    /// <summary>Biome it comes in.</summary>
    public Heightmap.Biome Biome { get; }

    /// <summary>Whether it comes for players on a ship.</summary>
    public bool Sea { get; }

    /// <summary>Global key of the boss that unlocks it.</summary>
    public string BossKey { get; }

    /// <summary>Prefab name of the creature.</summary>
    public string PrefabName => $"WhiteHilt_{Key}";

    /// <summary>Prefab name of the trophy.</summary>
    public string TrophyName => $"WhiteHilt_Trophy{Key}";

    /// <summary>Translation key of the creature's name.</summary>
    public string NameKey => $"enemy_whitehilt_{Key.ToLowerInvariant()}";

    /// <summary>Translation key of the trophy's name.</summary>
    public string TrophyKey => Translations.ItemKey(TrophyName);

    /// <summary>
    /// The beast that comes for a player.
    /// </summary>
    /// <param name="biome">Biome the player is in.</param>
    /// <param name="atSea">Whether the player is on a ship.</param>
    /// <returns>The beast, or null if none comes there.</returns>
    public static BeastDefinition Resolve(Heightmap.Biome biome, bool atSea)
    {
        if (atSea)
        {
            return All.FirstOrDefault(beast => beast.Sea && beast.Biome == (biome == Heightmap.Biome.AshLands ? Heightmap.Biome.AshLands : Heightmap.Biome.Ocean));
        }

        return All.FirstOrDefault(beast => !beast.Sea && beast.Biome == biome);
    }

    /// <summary>
    /// Finds a beast by its key or by the name of its biome.
    /// </summary>
    /// <param name="text">Key such as BlackTroll, or a biome such as Swamp.</param>
    /// <returns>The beast, or null.</returns>
    public static BeastDefinition Find(string text)
    {
        return All.FirstOrDefault(beast => string.Equals(beast.Key, text, StringComparison.OrdinalIgnoreCase))
            ?? All.FirstOrDefault(beast => !beast.Sea && string.Equals(beast.Biome.ToString(), text, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Finds a beast by its creature prefab name.
    /// </summary>
    /// <param name="prefabName">Prefab name.</param>
    /// <returns>The beast, or null if the prefab is no beast.</returns>
    public static BeastDefinition ByPrefab(string prefabName)
    {
        return All.FirstOrDefault(beast => beast.PrefabName == prefabName);
    }
}
