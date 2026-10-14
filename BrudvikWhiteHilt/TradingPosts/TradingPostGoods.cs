using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// What the trading posts sell and buy, and for how much, as agreed in docs/plans/trading-posts.md section 4: each post
/// sells the materials of its own biome, once the world has reached that biome; it buys the goods of the next biome
/// (the Deep North's buy the Meadows') and every fish. A post pays a share (<see cref="TradingPostSettings.BuyShare"/>)
/// of what it asks. Pure numbers, so the rules can be tested without the game.
/// </summary>
public static class TradingPostGoods
{
    /// <summary>The biomes in the order of the game, as the posts know them.</summary>
    public static readonly string[] Biomes = { "Meadows", "BlackForest", "Swamp", "Mountain", "Plains", "Mistlands", "AshLands", "DeepNorth" };

    // Coins a plain material of each biome is worth, in the order of Biomes.
    private static readonly float[] biomeWorth = { 1f, 2f, 3f, 4f, 5f, 6f, 8f, 10f };

    // The world key that opens each biome's posts: the boss slain before it (the Meadows' are open at once).
    private static readonly string[] biomeKeys =
    {
        "", "defeated_eikthyr", "defeated_gdking", "defeated_bonemass", "defeated_dragon", "defeated_goblinking", "defeated_queen", "defeated_fader"
    };

    // Each biome's materials, from the biome rule in .github/copilot-instructions.md and the plan's table.
    private static readonly string[][] materials =
    {
        new[] { "Wood", "Stone", "Flint", "Resin", "LeatherScraps", "DeerHide", "Raspberry", "Honey", "Feathers" },
        new[] { "RoundLog", "FineWood", "CopperOre", "TinOre", "TrollHide", "Blueberries", "CarrotSeeds", "Thistle" },
        new[] { "IronScrap", "ElderBark", "Guck", "Root", "Bloodbag", "TurnipSeeds", "Entrails", "Chain", "WitheredBone" },
        new[] { "SilverOre", "Obsidian", "FreezeGland", "WolfPelt", "OnionSeeds", "Crystal" },
        new[] { "BlackMetalScrap", "Flax", "Barley", "LinenThread", "Cloudberry", "LoxMeat", "LoxPelt", "Tar" },
        new[] { "BlackMarble", "YggdrasilWood", "Carapace", "Sap", "RoyalJelly" },
        new[] { "FlametalOreNew", "Blackwood", "Grausten", "CharcoalResin", "VineberrySeeds" },
        new[] { "GoldOre", "Gold", "MooseHide", "MooseMeat", "MooseSinew", "SealHide", "ElakingHairBundle", "Leatherstraps", "OatSeeds", "PoteitrSeeds" }
    };

    // Kinds of material worth more or less than a plain one: bulk building stuff, ore and metal, hides and pelts.
    private static readonly HashSet<string> bulk = new()
    {
        "Wood", "Stone", "Flint", "Resin", "RoundLog", "FineWood", "ElderBark", "Guck", "Root", "Flax", "Barley", "BlackMarble", "YggdrasilWood", "Blackwood", "Grausten"
    };

    private static readonly HashSet<string> metal = new()
    {
        "CopperOre", "TinOre", "IronScrap", "SilverOre", "BlackMetalScrap", "FlametalOreNew", "GoldOre", "Gold", "Chain"
    };

    private static readonly HashSet<string> hides = new() { "DeerHide", "TrollHide", "WolfPelt", "LoxPelt", "MooseHide", "SealHide" };

    // The fish, with the biome whose waters they come from (index into Biomes); the ocean's count as the Swamp's.
    private static readonly Dictionary<string, int> fish = new()
    {
        { "Fish1", 0 }, { "Fish2", 0 }, { "Fish5", 1 }, { "Fish3", 2 }, { "Fish6", 2 }, { "Fish8", 2 }, { "Fish4_cave", 3 },
        { "Fish7", 4 }, { "Fish9", 5 }, { "Fish12", 5 }, { "Fish11", 6 }, { "Fish10", 7 }
    };

    // Fish meat, raw and cooked, which every post also buys.
    private static readonly Dictionary<string, float> fishMeat = new() { { "FishRaw", 2f }, { "FishCooked", 3f } };

    // A fish is worth this many plain materials of its biome, and each star of quality adds half again.
    private const float FishWorth = 4f;
    private const float FishQualityStep = 0.5f;

    /// <summary>
    /// The index of a biome in <see cref="Biomes"/>, or -1 for one without posts. Case aside, so the game's
    /// <c>AshLands</c> and the plan's <c>Ashlands</c> are the same.
    /// </summary>
    /// <param name="biome">The biome's name.</param>
    /// <returns>The index.</returns>
    public static int IndexOf(string biome)
    {
        return Array.FindIndex(Biomes, name => string.Equals(name, biome, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The biome whose goods a post buys: the next one, and the Meadows' for the Deep North.
    /// </summary>
    /// <param name="biome">The post's biome index.</param>
    /// <returns>The linked biome's index.</returns>
    public static int Linked(int biome) => (biome + 1) % Biomes.Length;

    /// <summary>
    /// The world key that opens a biome's posts; empty for always.
    /// </summary>
    /// <param name="biome">The biome index.</param>
    /// <returns>The key.</returns>
    public static string OpenKey(int biome) => biome >= 0 && biome < biomeKeys.Length ? biomeKeys[biome] : string.Empty;

    /// <summary>
    /// The materials a biome's posts sell.
    /// </summary>
    /// <param name="biome">The biome index.</param>
    /// <returns>Prefab names.</returns>
    public static IReadOnlyList<string> Materials(int biome) => biome >= 0 && biome < materials.Length ? materials[biome] : Array.Empty<string>();

    /// <summary>
    /// The biome a material belongs to, or -1.
    /// </summary>
    /// <param name="prefab">The prefab name.</param>
    /// <returns>The biome index.</returns>
    public static int BiomeOf(string prefab) => Array.FindIndex(materials, list => list.Contains(prefab));

    /// <summary>
    /// What one of a material costs at a post, before any configured price.
    /// </summary>
    /// <param name="prefab">The prefab name.</param>
    /// <returns>Coins per piece, or 0 for something that is not a post material.</returns>
    public static float UnitPrice(string prefab)
    {
        int biome = BiomeOf(prefab);
        if (biome < 0)
        {
            return 0f;
        }

        float factor = bulk.Contains(prefab) ? 0.5f : metal.Contains(prefab) ? 2f : hides.Contains(prefab) ? 1.5f : 1f;
        return biomeWorth[biome] * factor;
    }

    /// <summary>
    /// How many of a material a post sells at once: fewer of the dear ones, more of the bulk.
    /// </summary>
    /// <param name="prefab">The prefab name.</param>
    /// <returns>The stack sold.</returns>
    public static int StackSold(string prefab) => metal.Contains(prefab) ? 5 : bulk.Contains(prefab) ? 20 : 10;

    /// <summary>
    /// What a post pays for one piece of something a player sells there: the linked biome's materials at
    /// <paramref name="share"/> of their price, and fish and fish meat from anywhere.
    /// </summary>
    /// <param name="postBiome">The post's biome index.</param>
    /// <param name="prefab">The prefab name.</param>
    /// <param name="quality">The piece's quality (a fish's stars).</param>
    /// <param name="share">The share of the asking price a post pays.</param>
    /// <param name="prices">Configured asking prices that replace the built-in ones.</param>
    /// <returns>Coins per piece, 0 for something the post does not buy.</returns>
    public static float BuyPrice(int postBiome, string prefab, int quality, float share, IReadOnlyDictionary<string, float> prices = null)
    {
        if (fish.TryGetValue(prefab, out int fishBiome))
        {
            return biomeWorth[fishBiome] * FishWorth * (1f + FishQualityStep * (Mathf.Max(1, quality) - 1)) * share;
        }

        if (fishMeat.TryGetValue(prefab, out float meat))
        {
            return meat * share;
        }

        if (postBiome < 0 || BiomeOf(prefab) != Linked(postBiome))
        {
            return 0f;
        }

        return AskingPrice(prefab, prices) * share;
    }

    /// <summary>
    /// The asking price of one piece: the configured one, or the built-in one.
    /// </summary>
    /// <param name="prefab">The prefab name.</param>
    /// <param name="prices">Configured prices, or null.</param>
    /// <returns>Coins per piece.</returns>
    public static float AskingPrice(string prefab, IReadOnlyDictionary<string, float> prices)
    {
        return prices != null && prices.TryGetValue(prefab, out float price) ? price : UnitPrice(prefab);
    }

    /// <summary>
    /// Reads configured prices: <c>Prefab:Price</c>, comma separated, the price in coins per piece. Bad entries are
    /// skipped.
    /// </summary>
    /// <param name="text">The setting's text.</param>
    /// <returns>Price per prefab name.</returns>
    public static Dictionary<string, float> ParsePrices(string text)
    {
        Dictionary<string, float> prices = new();
        foreach (string part in (text ?? string.Empty).Split(','))
        {
            string[] fields = part.Split(':');
            if (fields.Length == 2 && fields[0].Trim().Length > 0
                && float.TryParse(fields[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float price) && price >= 0f)
            {
                prices[fields[0].Trim()] = price;
            }
        }

        return prices;
    }
}
