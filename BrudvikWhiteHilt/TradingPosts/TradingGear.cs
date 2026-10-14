using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// What kind of gear a piece is, and what a post is known for when most of its gear is of one kind.
/// </summary>
public enum GearKind
{
    /// <summary>Swords, axes, maces, spears, knives, atgeirs, staves.</summary>
    Weapons,

    /// <summary>Bows and crossbows.</summary>
    Bows,

    /// <summary>Helmets, chests, legs and capes.</summary>
    Armour,

    /// <summary>Shields.</summary>
    Shields,

    /// <summary>Pickaxes, hammers, hoes and the like.</summary>
    Tools
}

/// <summary>
/// One piece of gear a biome's posts can sell: found from the game's recipe, with the biome of its latest material and
/// its price.
/// </summary>
public sealed class GearEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GearEntry"/> class.
    /// </summary>
    /// <param name="prefab">The item's prefab name.</param>
    /// <param name="kind">What kind of gear it is.</param>
    /// <param name="biome">The biome index of its latest material.</param>
    /// <param name="price">Its price in coins, before any discount.</param>
    public GearEntry(string prefab, GearKind kind, int biome, int price)
    {
        Prefab = prefab;
        Kind = kind;
        Biome = biome;
        Price = price;
    }

    /// <summary>Gets the item's prefab name.</summary>
    public string Prefab { get; }

    /// <summary>Gets what kind of gear it is.</summary>
    public GearKind Kind { get; }

    /// <summary>Gets the biome index (<see cref="TradingPostGoods.Biomes"/>) of its latest material.</summary>
    public int Biome { get; }

    /// <summary>Gets its price in coins, before any discount.</summary>
    public int Price { get; }
}

/// <summary>
/// The rules of the Trading skill and of the gear each post sells (docs/plans/trading-skill.md): which biome a recipe
/// belongs to and what it costs, how a biome's gear is shared out among its posts so that each piece is sold at one
/// post only, the skill level that opens each piece, and what the skill does to prices. Pure numbers, so the rules can
/// be tested without the game.
/// </summary>
public static class TradingGear
{
    // The posts' biome worth again (TradingPostGoods keeps it private): coins a plain material of each biome is worth.
    private static readonly float[] biomeWorth = { 1f, 2f, 3f, 4f, 5f, 6f, 8f, 10f };

    // A refined metal is worth this many plain materials of its biome; a boss's or a rare creature's part this many.
    private const float Refined = 5f;
    private const float Rare = 5f;
    private const float Part = 1.5f;

    // Materials gear is made of that the posts do not sell: the biome (index into TradingPostGoods.Biomes) and their
    // worth in plain materials of it. The ocean's count as the Swamp's, as the fish do.
    private static readonly Dictionary<string, (int Biome, float Worth)> otherMaterials = new()
    {
        { "TrophyDeer", (0, Part) }, { "TrophyBoar", (0, Part) }, { "TrophyNeck", (0, Part) }, { "HardAntler", (0, Rare) },
        { "Copper", (1, Refined) }, { "Tin", (1, Refined) }, { "Bronze", (1, Refined) }, { "BronzeNails", (1, 1f) },
        { "BoneFragments", (1, 1f) }, { "SurtlingCore", (1, Rare) }, { "Coal", (1, 0.5f) }, { "GreydwarfEye", (1, 1f) },
        { "TrophyGreydwarf", (1, Part) }, { "TrophyFrostTroll", (1, Rare) }, { "CryptKey", (1, Rare) }, { "TrophyTheElder", (1, Rare) },
        { "Iron", (2, Refined) }, { "IronNails", (2, 1f) }, { "Ooze", (2, 1f) }, { "TrophyDraugr", (2, Part) }, { "TrophyBlob", (2, Part) },
        { "TrophyLeech", (2, Part) }, { "TrophyAbomination", (2, Rare) }, { "Chitin", (2, Part) }, { "SerpentScale", (2, Part) },
        { "TrophySerpent", (2, Rare) }, { "Wishbone", (2, Rare) },
        { "Silver", (3, Refined) }, { "WolfFang", (3, Part) }, { "WolfClaw", (3, Part) }, { "DragonTear", (3, Rare) },
        { "TrophyHatchling", (3, Part) }, { "TrophySGolem", (3, Rare) }, { "TrophyCultist", (3, Part) }, { "TrophyUlv", (3, Part) },
        { "FenrisHair", (3, Part) }, { "FenrisClaw", (3, Part) }, { "WolfHairBundle", (3, Part) }, { "TrophyWolf", (3, Part) },
        { "BlackMetal", (4, Refined) }, { "Needle", (4, 1f) }, { "TrophyGoblin", (4, Part) }, { "TrophyDeathsquito", (4, Part) },
        { "GoblinTotem", (4, Part) }, { "YagluthDrop", (4, Rare) }, { "TrophyLox", (4, Part) }, { "TrophyGrowth", (4, Part) },
        { "Eitr", (5, Refined) }, { "BlackCore", (5, Rare) }, { "Mandible", (5, Part) }, { "ScaleHide", (5, Part) }, { "Bilebag", (5, Part) },
        { "JuteRed", (5, 1f) }, { "JuteBlue", (5, 1f) }, { "Wisp", (5, 1f) }, { "Softtissue", (5, 1f) }, { "QueenDrop", (5, Rare) },
        { "TrophySeeker", (5, Part) }, { "TrophySeekerBrute", (5, Rare) }, { "TrophyGjall", (5, Rare) }, { "TrophyTick", (5, Part) },
        { "TrophyHare", (5, Part) }, { "TrophyDvergr", (5, Part) }, { "DvergrNeedle", (5, Part) }, { "DvergrKeyFragment", (5, Rare) },
        { "FlametalNew", (6, Refined) }, { "AsksvinHide", (6, Part) }, { "CharredBone", (6, 1f) }, { "CelestialFeather", (6, Rare) },
        { "MorgenSinew", (6, Part) }, { "MorgenHeart", (6, Rare) }, { "MoltenCore", (6, Rare) }, { "CharredCogwheel", (6, Part) },
        { "Charredskull", (6, Part) }, { "BellFragment", (6, Part) }, { "ProustitePowder", (6, 1f) }, { "SulfurStone", (6, 1f) },
        { "Iolite", (6, Rare) }, { "GemstoneRed", (6, Rare) }, { "GemstoneBlue", (6, Rare) }, { "GemstoneGreen", (6, Rare) },
        { "TrophyCharredMelee", (6, Part) }, { "TrophyCharredArcher", (6, Part) }, { "TrophyCharredMage", (6, Part) },
        { "TrophyFallenValkyrie", (6, Rare) }, { "TrophyAsksvin", (6, Part) }, { "TrophyVolture", (6, Part) }, { "FaderDrop", (6, Rare) },
        { "BonemawSerpentTooth", (6, Rare) }, { "TrophyBonemawSerpent", (6, Rare) }
    };

    /// <summary>
    /// The biome a material belongs to, from the posts' goods or the table of other materials; -1 when unknown.
    /// </summary>
    /// <param name="prefab">The material's prefab name.</param>
    /// <returns>The biome index.</returns>
    public static int MaterialBiome(string prefab)
    {
        int biome = TradingPostGoods.BiomeOf(prefab);
        if (biome >= 0)
        {
            return biome;
        }

        return prefab != null && otherMaterials.TryGetValue(prefab, out (int Biome, float Worth) other) ? other.Biome : -1;
    }

    /// <summary>
    /// What one piece of a material is worth: the posts' asking price for their own goods, else its worth from the table
    /// of other materials; 0 when unknown.
    /// </summary>
    /// <param name="prefab">The material's prefab name.</param>
    /// <param name="prices">Configured asking prices, or null.</param>
    /// <returns>Coins per piece.</returns>
    public static float MaterialPrice(string prefab, IReadOnlyDictionary<string, float> prices = null)
    {
        if (prefab != null && prices != null && prices.TryGetValue(prefab, out float configured))
        {
            return configured;
        }

        float price = TradingPostGoods.UnitPrice(prefab);
        if (price > 0f)
        {
            return price;
        }

        return prefab != null && otherMaterials.TryGetValue(prefab, out (int Biome, float Worth) other) ? biomeWorth[other.Biome] * other.Worth : 0f;
    }

    /// <summary>
    /// The biome a recipe belongs to: that of its latest material. -1 when it has no materials or one is unknown, so
    /// nothing is sold in a biome too early.
    /// </summary>
    /// <param name="resources">The recipe's materials and amounts.</param>
    /// <returns>The biome index.</returns>
    public static int RecipeBiome(IEnumerable<(string Prefab, int Amount)> resources)
    {
        int latest = -1;
        foreach ((string prefab, int amount) in resources)
        {
            if (amount <= 0)
            {
                continue;
            }

            int biome = MaterialBiome(prefab);
            if (biome < 0)
            {
                return -1;
            }

            latest = Mathf.Max(latest, biome);
        }

        return latest;
    }

    /// <summary>
    /// What a post asks for a piece of gear: its recipe's materials at their worth, times the gear factor.
    /// </summary>
    /// <param name="resources">The recipe's materials and amounts.</param>
    /// <param name="factor">The gear price factor.</param>
    /// <param name="prices">Configured asking prices, or null.</param>
    /// <returns>Coins, at least 1.</returns>
    public static int RecipePrice(IEnumerable<(string Prefab, int Amount)> resources, float factor, IReadOnlyDictionary<string, float> prices = null)
    {
        float sum = resources.Where(resource => resource.Amount > 0).Sum(resource => MaterialPrice(resource.Prefab, prices) * resource.Amount);
        return Mathf.Max(1, Mathf.RoundToInt(sum * Mathf.Max(0f, factor)));
    }

    /// <summary>
    /// Orders a biome's gear for sharing out: cheapest first, then by name, so every machine gets the same order.
    /// </summary>
    /// <param name="pool">The biome's gear.</param>
    /// <returns>The gear in order.</returns>
    public static List<GearEntry> Ordered(IEnumerable<GearEntry> pool)
    {
        return pool.OrderBy(entry => entry.Price).ThenBy(entry => entry.Prefab, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// The gear one post sells: of the biome's gear in order, every <paramref name="count"/>th from
    /// <paramref name="slot"/>, so each piece is sold at one post only and each post gets cheap and dear pieces alike.
    /// </summary>
    /// <param name="ordered">The biome's gear, from <see cref="Ordered"/>.</param>
    /// <param name="slot">The post's place among the biome's posts, from 0.</param>
    /// <param name="count">How many posts the biome has.</param>
    /// <returns>The post's gear, cheapest first.</returns>
    public static List<GearEntry> ForSlot(IReadOnlyList<GearEntry> ordered, int slot, int count)
    {
        if (count <= 0 || slot < 0 || slot >= count)
        {
            return new List<GearEntry>();
        }

        return ordered.Where((_, index) => index % count == slot).ToList();
    }

    /// <summary>
    /// A post's place among its biome's posts: the posts ordered by id, so the server and every reader agree.
    /// </summary>
    /// <param name="postIds">The ids of the biome's posts.</param>
    /// <param name="id">The post's id.</param>
    /// <returns>Its place from 0, or -1 when it is not among them.</returns>
    public static int SlotOf(IEnumerable<string> postIds, string id)
    {
        return postIds.Distinct().OrderBy(postId => postId, StringComparer.Ordinal).ToList().IndexOf(id);
    }

    /// <summary>
    /// The Trading level a post's piece of gear needs: the post's gear, cheapest first, is split into as many equal
    /// steps as there are levels, the cheapest step opening at the first.
    /// </summary>
    /// <param name="rank">The piece's place in the post's gear, cheapest first, from 0.</param>
    /// <param name="count">How many pieces the post sells.</param>
    /// <param name="levels">The levels that open each step, lowest first.</param>
    /// <returns>The level needed; 0 with no levels.</returns>
    public static int UnlockLevel(int rank, int count, IReadOnlyList<int> levels)
    {
        if (levels == null || levels.Count == 0 || count <= 0)
        {
            return 0;
        }

        int step = Mathf.Clamp(rank * levels.Count / count, 0, levels.Count - 1);
        return levels[step];
    }

    /// <summary>
    /// Reads the unlock levels: whole numbers from 0 to 100, comma separated; bad entries are skipped, the rest sorted.
    /// </summary>
    /// <param name="text">The setting's text.</param>
    /// <returns>The levels, lowest first.</returns>
    public static List<int> ParseLevels(string text)
    {
        List<int> levels = new();
        foreach (string part in (text ?? string.Empty).Split(','))
        {
            if (int.TryParse(part.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int level) && level >= 0 && level <= 100)
            {
                levels.Add(level);
            }
        }

        levels.Sort();
        return levels;
    }

    /// <summary>
    /// The share of its asking price a post pays a player: from <paramref name="share"/> at level 0 to
    /// <paramref name="maxShare"/> at 100, never below the first.
    /// </summary>
    /// <param name="level">The player's Trading, 0 to 100.</param>
    /// <param name="share">The share at level 0.</param>
    /// <param name="maxShare">The share at level 100.</param>
    /// <returns>The share.</returns>
    public static float BuyShare(float level, float share, float maxShare)
    {
        return share + Mathf.Max(0f, maxShare - share) * Mathf.Clamp01(level / 100f);
    }

    /// <summary>
    /// What a player pays for something a post sells: the price less the skill's share of the discount, at least 1.
    /// </summary>
    /// <param name="price">The asking price, in coins.</param>
    /// <param name="level">The player's Trading, 0 to 100.</param>
    /// <param name="maxDiscount">The discount at level 100 (0.1 = a tenth off).</param>
    /// <returns>Coins.</returns>
    public static int Discounted(int price, float level, float maxDiscount)
    {
        float off = Mathf.Clamp01(maxDiscount) * Mathf.Clamp01(level / 100f);
        return Mathf.Max(1, Mathf.RoundToInt(price * (1f - off)));
    }

    /// <summary>
    /// The skill a trade gives: one point per <paramref name="coinsPerPoint"/> coins.
    /// </summary>
    /// <param name="coins">The coins paid or received.</param>
    /// <param name="coinsPerPoint">Coins per point; 0 or less for none.</param>
    /// <returns>Points.</returns>
    public static float Experience(int coins, float coinsPerPoint)
    {
        return coins <= 0 || coinsPerPoint <= 0f ? 0f : coins / coinsPerPoint;
    }

    /// <summary>
    /// What a post is known for: the kind most of its gear is, the earlier kind on a tie; null with no gear.
    /// </summary>
    /// <param name="kinds">The kinds of the post's gear.</param>
    /// <returns>The kind, or null.</returns>
    public static GearKind? Specialty(IEnumerable<GearKind> kinds)
    {
        List<IGrouping<GearKind, GearKind>> groups = kinds.GroupBy(kind => kind).ToList();
        if (groups.Count == 0)
        {
            return null;
        }

        return groups.OrderByDescending(group => group.Count()).ThenBy(group => group.Key).First().Key;
    }
}
