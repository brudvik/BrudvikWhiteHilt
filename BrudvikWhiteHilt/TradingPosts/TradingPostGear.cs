using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// The gear the trading posts sell, found from the game's recipes: every weapon, bow, piece of armour, shield and tool
/// whose materials are all known (<see cref="TradingGear"/>) belongs to the biome of its latest material, and is sold at
/// one of that biome's posts, by the post's place among them (asked of the server, <see cref="TradingPostService"/>).
/// White Hilt's own gear is left out. Built again when the world's item list or the prices change.
/// </summary>
public static class TradingPostGear
{
    private const string OwnPrefix = "WhiteHilt";

    private static readonly Dictionary<int, List<GearEntry>> pools = new();
    private static ObjectDB builtFor;
    private static string builtPrices;
    private static float builtFactor;

    /// <summary>
    /// Registers the names of the kinds of gear. Call from the plugin's Awake.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish(KindKey(GearKind.Weapons), "weapons");
        Translations.AddEnglish(KindKey(GearKind.Bows), "bows");
        Translations.AddEnglish(KindKey(GearKind.Armour), "armour");
        Translations.AddEnglish(KindKey(GearKind.Shields), "shields");
        Translations.AddEnglish(KindKey(GearKind.Tools), "tools");
    }

    /// <summary>
    /// The name of a kind of gear in the player's language.
    /// </summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The name.</returns>
    public static string KindName(GearKind kind)
    {
        return Localization.instance.Localize("$" + KindKey(kind));
    }

    /// <summary>
    /// The gear one post sells, cheapest first; empty until the server has told this machine the post's place.
    /// </summary>
    /// <param name="biome">The post's biome index.</param>
    /// <param name="postId">The post's id.</param>
    /// <returns>The gear.</returns>
    public static List<GearEntry> ForPost(int biome, string postId)
    {
        if (string.IsNullOrEmpty(postId) || !TradingPostService.TryGetSlot(postId, out int slot, out int count))
        {
            return new List<GearEntry>();
        }

        return TradingGear.ForSlot(Pool(biome), slot, count);
    }

    /// <summary>
    /// What a post is known for, or null while its gear is unknown.
    /// </summary>
    /// <param name="biome">The post's biome index.</param>
    /// <param name="postId">The post's id.</param>
    /// <returns>The kind most of its gear is.</returns>
    public static GearKind? Specialty(int biome, string postId)
    {
        return TradingGear.Specialty(ForPost(biome, postId).Select(entry => entry.Kind));
    }

    /// <summary>
    /// A biome's gear in the order it is shared out.
    /// </summary>
    /// <param name="biome">The biome index.</param>
    /// <returns>The gear.</returns>
    public static IReadOnlyList<GearEntry> Pool(int biome)
    {
        Refresh();
        return pools.TryGetValue(biome, out List<GearEntry> pool) ? pool : new List<GearEntry>();
    }

    private static string KindKey(GearKind kind) => $"whitehilt_gearkind_{kind.ToString().ToLowerInvariant()}";

    private static void Refresh()
    {
        ObjectDB db = ObjectDB.instance;
        string prices = TradingPostSettings.Prices.Value ?? string.Empty;
        float factor = TradingPostSettings.GearPriceFactor.Value;
        if (db == null || (db == builtFor && prices == builtPrices && factor == builtFactor))
        {
            return;
        }

        builtFor = db;
        builtPrices = prices;
        builtFactor = factor;
        pools.Clear();
        HashSet<string> seen = new();
        List<GearEntry> found = new();
        foreach (Recipe recipe in db.m_recipes)
        {
            ItemDrop item = recipe != null && recipe.m_enabled ? recipe.m_item : null;
            if (item == null || recipe.m_amount != 1 || recipe.m_resources == null)
            {
                continue;
            }

            string name = item.gameObject.name;
            ItemDrop.ItemData.SharedData shared = item.m_itemData.m_shared;
            GearKind? kind = KindOf(shared);
            if (kind == null || name.StartsWith(OwnPrefix) || !string.IsNullOrEmpty(shared.m_dlc) || !seen.Add(name))
            {
                continue;
            }

            List<(string Prefab, int Amount)> resources = recipe.m_resources
                .Where(requirement => requirement?.m_resItem != null)
                .Select(requirement => (requirement.m_resItem.gameObject.name, requirement.m_amount))
                .ToList();
            int biome = TradingGear.RecipeBiome(resources);
            if (biome < 0)
            {
                continue;
            }

            found.Add(new GearEntry(name, kind.Value, biome, TradingGear.RecipePrice(resources, factor, TradingPostSettings.ConfiguredPrices)));
        }

        foreach (IGrouping<int, GearEntry> biome in found.GroupBy(entry => entry.Biome))
        {
            pools[biome.Key] = TradingGear.Ordered(biome);
        }

        Jotunn.Logger.LogInfo($"Trading posts: gear by biome: {string.Join(", ", pools.OrderBy(pool => pool.Key).Select(pool => $"{TradingPostGoods.Biomes[pool.Key]} {pool.Value.Count}"))}.");
    }

    private static GearKind? KindOf(ItemDrop.ItemData.SharedData shared)
    {
        switch (shared.m_itemType)
        {
            case ItemDrop.ItemData.ItemType.Bow:
                return GearKind.Bows;
            case ItemDrop.ItemData.ItemType.OneHandedWeapon:
            case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
            case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
                return shared.m_skillType == Skills.SkillType.Crossbows ? GearKind.Bows : GearKind.Weapons;
            case ItemDrop.ItemData.ItemType.Shield:
                return GearKind.Shields;
            case ItemDrop.ItemData.ItemType.Helmet:
            case ItemDrop.ItemData.ItemType.Chest:
            case ItemDrop.ItemData.ItemType.Legs:
            case ItemDrop.ItemData.ItemType.Shoulder:
                return GearKind.Armour;
            case ItemDrop.ItemData.ItemType.Tool:
                return GearKind.Tools;
            default:
                return null;
        }
    }
}
