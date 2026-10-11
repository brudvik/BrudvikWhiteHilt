using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Biome = Heightmap.Biome;

namespace BrudvikWhiteHilt.School;

/// <summary>
/// Tells whether a reader may learn a material at the School Bench under the biome rule: they must have been to a
/// biome where it can be found (dropped by a creature that spawns there, picked, chopped or mined from what grows
/// there), or be able to learn everything it is made from (a recipe, a smelter, a kiln, a fermenter, a cooking
/// station or a beehive). The sources are read from the game's own spawn and vegetation lists, so it also covers items
/// added by other mods. An item found nowhere in them (a boss drop, a trader's ware, a dungeon find) is not taught: the
/// reader has to find it themselves.
/// </summary>
public static class LearnableItems
{
    // Drops are followed this many steps (tree, log, sub-log, item) and no further.
    private const int MaxDepth = 6;

    private static Dictionary<string, Biome> sources;
    private static Dictionary<string, List<string[]>> makers;
    private static bool unreadable;

    /// <summary>
    /// Forgets what was read, e.g. when another world is loaded with other mods' vegetation.
    /// </summary>
    public static void Reset()
    {
        sources = null;
        makers = null;
        unreadable = false;
    }

    /// <summary>
    /// The materials of a list a reader may learn.
    /// </summary>
    /// <param name="materials">Shared item names (<c>$item_...</c>).</param>
    /// <param name="known">The biomes the reader has been to.</param>
    /// <returns>Those the reader may learn.</returns>
    public static HashSet<string> Filter(IEnumerable<string> materials, Biome known)
    {
        Prepare();
        if (unreadable)
        {
            return new HashSet<string>(materials, StringComparer.Ordinal);
        }

        Dictionary<string, bool> memo = new(StringComparer.Ordinal);
        HashSet<string> visiting = new(StringComparer.Ordinal);
        return new HashSet<string>(materials.Where(material => CanLearn(material, known, memo, visiting)), StringComparer.Ordinal);
    }

    private static bool CanLearn(string item, Biome known, Dictionary<string, bool> memo, HashSet<string> visiting)
    {
        if (memo.TryGetValue(item, out bool cached))
        {
            return cached;
        }

        // A loop of recipes (an item made from what is made from it) does not count as a way to learn it.
        if (!visiting.Add(item))
        {
            return false;
        }

        bool result = sources.TryGetValue(item, out Biome found) && (found & known) != Biome.None;
        if (!result && makers.TryGetValue(item, out List<string[]> ways))
        {
            result = ways.Any(ingredients => ingredients.All(ingredient => CanLearn(ingredient, known, memo, visiting)));
        }

        visiting.Remove(item);
        memo[item] = result;
        return result;
    }

    // Reads once where every item can be found and how it can be made.
    private static void Prepare()
    {
        if (sources != null || unreadable)
        {
            return;
        }

        sources = new Dictionary<string, Biome>(StringComparer.Ordinal);
        makers = new Dictionary<string, List<string[]>>(StringComparer.Ordinal);
        try
        {
            ReadSpawns();
            ReadVegetation();
            ReadRecipes();
            ReadProducers();
            Jotunn.Logger.LogInfo($"School: {sources.Count} items found in the world, {makers.Count} that can be made");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"School: could not read where items are found, so materials are taught without the biome rule: {ex.Message}");
            unreadable = true;
        }
    }

    private static void ReadSpawns()
    {
        HashSet<SpawnSystemList> lists = new();
        GameObject zoneCtrl = ZoneSystem.instance != null ? ZoneSystem.instance.m_zoneCtrlPrefab : null;
        if (zoneCtrl != null)
        {
            lists.UnionWith(zoneCtrl.GetComponentsInChildren<SpawnSystemList>(true));
            SpawnSystem system = zoneCtrl.GetComponent<SpawnSystem>();
            if (system != null)
            {
                lists.UnionWith(system.m_spawnLists.Where(list => list != null));
            }
        }

        foreach (SpawnSystem system in SpawnSystem.m_instances.Where(system => system != null))
        {
            lists.UnionWith(system.m_spawnLists.Where(list => list != null));
        }

        foreach (SpawnSystem.SpawnData spawn in lists.SelectMany(list => list.m_spawners))
        {
            if (spawn != null && spawn.m_enabled && spawn.m_prefab != null)
            {
                Collect(spawn.m_prefab, spawn.m_biome, 0);
            }
        }
    }

    private static void ReadVegetation()
    {
        if (ZoneSystem.instance == null)
        {
            return;
        }

        foreach (ZoneSystem.ZoneVegetation vegetation in ZoneSystem.instance.m_vegetation)
        {
            if (vegetation != null && vegetation.m_enable && vegetation.m_prefab != null)
            {
                Collect(vegetation.m_prefab, vegetation.m_biome, 0);
            }
        }
    }

    // Each recipe is a way to make its item from its ingredients; upgrade-only requirements (amount 0) are left out.
    private static void ReadRecipes()
    {
        if (ObjectDB.instance == null)
        {
            return;
        }

        foreach (Recipe recipe in ObjectDB.instance.m_recipes)
        {
            if (recipe == null || recipe.m_item == null || recipe.m_resources == null)
            {
                continue;
            }

            string[] ingredients = recipe.m_resources
                .Where(requirement => requirement?.m_resItem != null && requirement.m_amount > 0)
                .Select(requirement => requirement.m_resItem.m_itemData.m_shared.m_name)
                .ToArray();
            if (ingredients.Length > 0)
            {
                AddMaker(recipe.m_item.m_itemData.m_shared.m_name, ingredients);
            }
        }
    }

    // Smelters, kilns, fermenters and cooking stations turn one item into another; a beehive or sap collector gives
    // its product to whoever can build it.
    private static void ReadProducers()
    {
        if (ZNetScene.instance == null)
        {
            return;
        }

        foreach (GameObject prefab in ZNetScene.instance.m_prefabs.Where(prefab => prefab != null))
        {
            foreach (Smelter.ItemConversion conversion in prefab.GetComponent<Smelter>()?.m_conversion ?? new List<Smelter.ItemConversion>())
            {
                AddConversion(conversion?.m_from, conversion?.m_to);
            }

            foreach (Fermenter.ItemConversion conversion in prefab.GetComponent<Fermenter>()?.m_conversion ?? new List<Fermenter.ItemConversion>())
            {
                AddConversion(conversion?.m_from, conversion?.m_to);
            }

            foreach (CookingStation.ItemConversion conversion in prefab.GetComponent<CookingStation>()?.m_conversion ?? new List<CookingStation.ItemConversion>())
            {
                AddConversion(conversion?.m_from, conversion?.m_to);
            }

            ItemDrop product = prefab.GetComponent<Beehive>()?.m_honeyItem ?? prefab.GetComponent<SapCollector>()?.m_spawnItem;
            Piece piece = prefab.GetComponent<Piece>();
            if (product != null && piece != null && piece.m_resources != null)
            {
                string[] ingredients = piece.m_resources.Where(requirement => requirement?.m_resItem != null)
                    .Select(requirement => requirement.m_resItem.m_itemData.m_shared.m_name).ToArray();
                if (ingredients.Length > 0)
                {
                    AddMaker(product.m_itemData.m_shared.m_name, ingredients);
                }
            }
        }
    }

    private static void AddConversion(ItemDrop from, ItemDrop to)
    {
        if (from != null && to != null)
        {
            AddMaker(to.m_itemData.m_shared.m_name, new[] { from.m_itemData.m_shared.m_name });
        }
    }

    private static void AddMaker(string item, string[] ingredients)
    {
        if (!makers.TryGetValue(item, out List<string[]> ways))
        {
            ways = new List<string[]>();
            makers[item] = ways;
        }

        ways.Add(ingredients);
    }

    // Notes the items a spawned or growing thing gives, following what it leaves behind: a creature's drops, a
    // pickable's item, a tree's log and the logs it splits into, a rock's pieces, a nest's creatures.
    private static void Collect(GameObject prefab, Biome biome, int depth)
    {
        if (prefab == null || depth > MaxDepth || biome == Biome.None)
        {
            return;
        }

        ItemDrop item = prefab.GetComponent<ItemDrop>();
        if (item != null)
        {
            string name = item.m_itemData.m_shared.m_name;
            sources.TryGetValue(name, out Biome found);
            sources[name] = found | biome;
        }

        CharacterDrop characterDrop = prefab.GetComponent<CharacterDrop>();
        if (characterDrop != null)
        {
            foreach (CharacterDrop.Drop drop in characterDrop.m_drops)
            {
                Collect(drop?.m_prefab, biome, depth + 1);
            }
        }

        Pickable pickable = prefab.GetComponent<Pickable>();
        if (pickable != null)
        {
            Collect(pickable.m_itemPrefab, biome, depth + 1);
            CollectTable(pickable.m_extraDrops, biome, depth);
        }

        CollectTable(prefab.GetComponent<DropOnDestroyed>()?.m_dropWhenDestroyed, biome, depth);
        CollectTable(prefab.GetComponent<MineRock>()?.m_dropItems, biome, depth);
        CollectTable(prefab.GetComponent<MineRock5>()?.m_dropItems, biome, depth);
        foreach (MineRock5 rock in prefab.GetComponentsInChildren<MineRock5>(true))
        {
            CollectTable(rock.m_dropItems, biome, depth);
        }

        TreeBase tree = prefab.GetComponent<TreeBase>();
        if (tree != null)
        {
            CollectTable(tree.m_dropWhenDestroyed, biome, depth);
            Collect(tree.m_logPrefab, biome, depth + 1);
        }

        TreeLog log = prefab.GetComponent<TreeLog>();
        if (log != null)
        {
            CollectTable(log.m_dropWhenDestroyed, biome, depth);
            Collect(log.m_subLogPrefab, biome, depth + 1);
        }

        Destructible destructible = prefab.GetComponent<Destructible>();
        if (destructible != null)
        {
            Collect(destructible.m_spawnWhenDestroyed, biome, depth + 1);
        }

        SpawnArea nest = prefab.GetComponent<SpawnArea>();
        if (nest != null)
        {
            foreach (SpawnArea.SpawnData spawn in nest.m_prefabs)
            {
                Collect(spawn?.m_prefab, biome, depth + 1);
            }
        }
    }

    private static void CollectTable(DropTable table, Biome biome, int depth)
    {
        if (table?.m_drops == null)
        {
            return;
        }

        foreach (DropTable.DropData drop in table.m_drops)
        {
            Collect(drop.m_item, biome, depth + 1);
        }
    }
}
