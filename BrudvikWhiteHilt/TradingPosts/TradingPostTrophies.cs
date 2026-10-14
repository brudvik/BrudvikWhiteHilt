using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// What the hunter who now and then sits at a trading post sells: the trophies of the creatures of the post's biome,
/// the game's and White Hilt's own, never a boss's (those open the Forsaken powers). The trophies of the sea's
/// creatures are sold where they are first met: the Serpent's by the Swamp, the Kraken's and the Greenland Shark's by
/// the Mountains (both come after Bonemass), the Draugr Jarl's by the Plains. The Deep North's Elaking is left out, in
/// case it is the land's boss. A trophy costs the plain material of its
/// biome times <see cref="TradingPostSettings.TrophyWorth"/>, three times that for the great beasts. Pure numbers, so
/// the rules can be tested without the game.
/// </summary>
public static class TradingPostTrophies
{
    // Each biome's trophies, in the order of TradingPostGoods.Biomes. A name the game does not know is left out of the
    // store, so a creature an update renames costs nothing but its entry.
    private static readonly string[][] trophies =
    {
        new[] { "TrophyBoar", "TrophyDeer", "TrophyNeck" },
        new[]
        {
            "TrophyGreydwarf", "TrophyGreydwarfBrute", "TrophyGreydwarfShaman", "TrophySkeleton", "TrophyGhost", "TrophyFrostTroll", "TrophyBjorn",
            "WhiteHilt_TrophyGiantSpider", "WhiteHilt_TrophyLindorm"
        },
        new[]
        {
            "TrophyBlob", "TrophyDraugr", "TrophyDraugrElite", "TrophyLeech", "TrophySurtling", "TrophyWraith", "TrophyAbomination",
            "TrophySkeletonPoison", "TrophyKvastur", "TrophySerpent"
        },
        new[]
        {
            "TrophyWolf", "TrophyFenring", "TrophyHatchling", "TrophyCultist", "TrophySGolem", "TrophyUlv", "WhiteHilt_TrophyKraken",
            "WhiteHilt_TrophyGreenlandShark"
        },
        new[]
        {
            "TrophyDeathsquito", "TrophyGoblin", "TrophyGoblinBrute", "TrophyGoblinShaman", "TrophyLox", "TrophyGrowth", "TrophyBjornUndead",
            "WhiteHilt_TrophyDesertDragon", "WhiteHilt_TrophyDraugrJarl"
        },
        new[]
        {
            "TrophySeeker", "TrophySeekerBrute", "TrophyTick", "TrophyGjall", "TrophyDvergr", "TrophyHare", "WhiteHilt_TrophyMistStag",
            "WhiteHilt_TrophyNidhoggSpawn", "WhiteHilt_TrophyYggdrasilWarden"
        },
        new[]
        {
            "TrophyCharredMelee", "TrophyCharredArcher", "TrophyCharredMage", "TrophyAsksvin", "TrophyVolture", "TrophyFallenValkyrie",
            "TrophyMorgen", "TrophyBonemawSerpent", "TrophyBlob_Lava", "WhiteHilt_TrophyGarmHound"
        },
        new[]
        {
            "TrophyMoose", "TrophyDeerWhite", "TrophySeal", "TrophyMole", "TrophyBarka", "TrophyBlob_Frost", "TrophyWrithan",
            "TrophyJotunWarrior", "TrophyJotunWitch"
        }
    };

    // The great beasts, whose trophies cost three times as much.
    private static readonly HashSet<string> great = new()
    {
        "TrophyAbomination", "TrophySerpent", "TrophySGolem", "TrophyGjall", "TrophyFallenValkyrie", "TrophyMorgen", "TrophyBonemawSerpent",
        "WhiteHilt_TrophyLindorm", "WhiteHilt_TrophyKraken", "WhiteHilt_TrophyDesertDragon", "WhiteHilt_TrophyDraugrJarl",
        "WhiteHilt_TrophyYggdrasilWarden", "TrophyJotunWarrior", "TrophyJotunWitch"
    };

    // How much more a great beast's trophy costs.
    private const float GreatFactor = 3f;

    /// <summary>
    /// The trophies a biome's hunter sells.
    /// </summary>
    /// <param name="biome">The biome index (<see cref="TradingPostGoods.Biomes"/>).</param>
    /// <returns>The trophies' prefab names; none for a biome without.</returns>
    public static IReadOnlyList<string> Of(int biome)
    {
        return biome >= 0 && biome < trophies.Length ? trophies[biome] : new string[0];
    }

    /// <summary>
    /// Whether a biome's hunter has anything to sell, so a hunter only comes to such posts.
    /// </summary>
    /// <param name="biome">The biome index.</param>
    /// <returns>True if the biome has trophies.</returns>
    public static bool HasAny(int biome)
    {
        return Of(biome).Count > 0;
    }

    /// <summary>
    /// What a trophy costs at its biome's post, before the player's Trading.
    /// </summary>
    /// <param name="biome">The biome index.</param>
    /// <param name="prefab">The trophy's prefab name.</param>
    /// <param name="worth">Coins per plain material of the biome (<see cref="TradingPostSettings.TrophyWorth"/>).</param>
    /// <returns>Coins, at least 1.</returns>
    public static int Price(int biome, string prefab, float worth)
    {
        float price = TradingPostGoods.PlainWorth(biome) * worth * (great.Contains(prefab) ? GreatFactor : 1f);
        return Mathf.Max(1, Mathf.RoundToInt(price));
    }

    /// <summary>
    /// The hunter's store: one of each trophy of the biome the game knows, behind the world key that opens the biome,
    /// at the player's prices.
    /// </summary>
    /// <param name="biome">The biome index.</param>
    /// <param name="player">The player who trades, or null for the prices at level 0.</param>
    /// <returns>The store's list.</returns>
    public static List<Trader.TradeItem> BuildItems(int biome, Player player = null)
    {
        List<Trader.TradeItem> items = new();
        if (ObjectDB.instance == null)
        {
            return items;
        }

        foreach (string prefab in Of(biome))
        {
            ItemDrop drop = ObjectDB.instance.GetItemPrefab(prefab)?.GetComponent<ItemDrop>();
            if (drop == null)
            {
                continue;
            }

            items.Add(new Trader.TradeItem
            {
                m_prefab = drop,
                m_stack = 1,
                m_tooltip = string.Empty,
                m_price = TradingSkill.Price(Price(biome, prefab, TradingPostSettings.TrophyWorth.Value), player),
                m_requiredGlobalKey = TradingPostGoods.OpenKey(biome),
                // The store window plays these after every purchase without checking for none.
                m_buyPlayerEffects = new EffectList()
            });
        }

        return items;
    }
}
