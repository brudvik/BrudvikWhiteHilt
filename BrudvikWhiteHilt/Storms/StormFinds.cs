using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// What a storm leaves behind: when it passes, things lie about near each player who was out in it, from the storm's
/// own biome: amber and resin after the forest storm, pearls and scales after the sea storm, coins and rubies the sand
/// laid bare, crystals and frost glands after the blizzard. Lightning leaves a thunderstone now and then, and Ragnarok
/// leaves rare things among the rest.
/// </summary>
public static class StormFinds
{
    private const float Nearest = 5f;
    private const float Farthest = 22f;
    private const int Attempts = 6;
    private const string Thunderstone = "Thunderstone";
    private const float RareShare = 0.4f;

    // What each storm leaves: prefab, least and most, weight. Prefabs this game does not know are left out.
    private static readonly Dictionary<StormKind, Find[]> finds = new()
    {
        [StormKind.Tempest] = new Find[] { new("Resin", 3, 6, 4), new("Amber", 1, 2, 3), new("Feathers", 2, 4, 2), new("AmberPearl", 1, 1, 1), new(Thunderstone, 1, 1, 1) },
        [StormKind.Sea] = new Find[] { new("AmberPearl", 1, 2, 3), new("Amber", 1, 2, 2), new("Chitin", 1, 3, 2), new("SerpentScale", 1, 2, 1) },
        [StormKind.Sand] = new Find[] { new("Coins", 10, 30, 4), new("Amber", 1, 2, 2), new("Ruby", 1, 1, 2), new("SilverNecklace", 1, 1, 1) },
        [StormKind.Blizzard] = new Find[] { new("Crystal", 1, 3, 3), new("FreezeGland", 1, 2, 3), new("Obsidian", 2, 4, 2) },
        [StormKind.Mist] = new Find[] { new("Softtissue", 1, 3, 3), new("BlackMarble", 2, 4, 2), new("Carapace", 1, 2, 2), new("Wisp", 1, 1, 1) },
        [StormKind.Fire] = new Find[] { new("SulfurStone", 2, 4, 3), new("Grausten", 3, 6, 3), new("Coal", 5, 10, 2), new("MoltenCore", 1, 1, 1) },
        [StormKind.Arctic] = new Find[] { new("FreezeGland", 1, 3, 3), new("Crystal", 1, 3, 3), new("Obsidian", 2, 4, 2) }
    };

    // What Ragnarok leaves besides the storm's own finds.
    private static readonly Find[] rare =
    {
        new(Thunderstone, 1, 2, 3), new("Ruby", 1, 3, 3), new("AmberPearl", 2, 4, 3), new("SilverNecklace", 1, 2, 2),
        new("SurtlingCore", 2, 4, 2), new("YmirRemains", 1, 1, 1)
    };

    private static readonly HashSet<string> reported = new();

    /// <summary>
    /// Leaves the storm's finds around the player.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="kind">The storm they were out in.</param>
    /// <param name="count">How many finds.</param>
    /// <param name="ragnarok">After Ragnarok, some of the finds are rare.</param>
    /// <returns>How many were left.</returns>
    public static int Leave(Player player, StormKind kind, int count, bool ragnarok = false)
    {
        if (player == null || ObjectDB.instance == null || !finds.TryGetValue(kind, out Find[] table))
        {
            return 0;
        }

        List<Find> known = table.Where(find => Prefab(find.Prefab) != null).ToList();
        List<Find> rareKnown = ragnarok ? rare.Where(find => Prefab(find.Prefab) != null).ToList() : new List<Find>();
        int left = 0;
        for (int i = 0; i < count && known.Count > 0; i++)
        {
            Find find = rareKnown.Count > 0 && Random.value < RareShare ? Pick(rareKnown) : Pick(known);
            if (Drop(find.Prefab, Random.Range(find.Least, find.Most + 1), Around(player.transform.position)))
            {
                left++;
            }
        }

        return left;
    }

    /// <summary>
    /// Now and then a thunderstone where lightning struck.
    /// </summary>
    /// <param name="point">Where it struck.</param>
    /// <param name="chance">The chance, 0 to 1.</param>
    public static void AfterStrike(Vector3 point, float chance)
    {
        if (Random.value < chance)
        {
            Drop(Thunderstone, 1, point);
        }
    }

    private static Find Pick(List<Find> known)
    {
        float roll = Random.Range(0f, known.Sum(find => find.Weight));
        foreach (Find find in known)
        {
            roll -= find.Weight;
            if (roll <= 0f)
            {
                return find;
            }
        }

        return known[known.Count - 1];
    }

    // A dry spot on the ground near the player; at sea, the deck they stand on.
    private static Vector3? Around(Vector3 centre)
    {
        if (ZoneSystem.instance == null)
        {
            return null;
        }

        for (int attempt = 0; attempt < Attempts; attempt++)
        {
            Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(Nearest, Farthest);
            Vector3 point = centre + new Vector3(offset.x, 0f, offset.y);
            if (ZoneSystem.instance.GetGroundHeight(point, out float ground) && ground > ZoneSystem.instance.m_waterLevel + 0.2f)
            {
                return new Vector3(point.x, ground + 0.4f, point.z);
            }
        }

        return Ship.GetLocalShip() != null ? centre + Vector3.up : null;
    }

    private static bool Drop(string prefabName, int amount, Vector3? point)
    {
        ItemDrop prefab = Prefab(prefabName);
        if (prefab == null || point == null)
        {
            return false;
        }

        // The prefab's own item data has no drop prefab (the game sets it on items made from it), and DropItem needs one.
        ItemDrop.ItemData item = prefab.m_itemData.Clone();
        item.m_dropPrefab = prefab.gameObject;
        ItemDrop.DropItem(item, amount, point.Value + Vector3.up * 0.3f, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        return true;
    }

    private static ItemDrop Prefab(string name)
    {
        ItemDrop prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(name)?.GetComponent<ItemDrop>() : null;
        if (prefab == null && reported.Add(name))
        {
            Jotunn.Logger.LogWarning($"Storms: no item {name} to leave behind");
        }

        return prefab;
    }

    private readonly struct Find
    {
        public Find(string prefab, int least, int most, float weight)
        {
            Prefab = prefab;
            Least = least;
            Most = most;
            Weight = weight;
        }

        public string Prefab { get; }

        public int Least { get; }

        public int Most { get; }

        public float Weight { get; }
    }
}
