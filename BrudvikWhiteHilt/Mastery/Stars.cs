using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Mastery;

/// <summary>
/// Stars on food, wild picks, crops and seeds. A star is a quality step above 1 on an item that has no quality of its
/// own, so items with different stars never stack, and every container, drop and save keeps them.
/// </summary>
public static class Stars
{
    /// <summary>Most stars an item can have.</summary>
    public const int Max = 3;

    private const string FoodStarsPrefix = "whitehilt_foodstars_";
    private const string StarText = "★";
    private const string StarColor = "#FFD24A";

    private static HashSet<string> seedNames;
    private static Dictionary<string, ItemDrop.ItemData.SharedData> itemsByName;
    private static int ledgerDepth;
    private static int ledgerStars;
    private static int ledgerCount;

    /// <summary>
    /// Stars for the items created by the current drop context, or a roll for each; null when no context is active.
    /// </summary>
    public static System.Func<ItemDrop, int> DropStars { get; set; }

    /// <summary>
    /// Registers the English text. Call from the plugin's Awake.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("whitehilt_stars", "Stars");
        Translations.AddEnglish("whitehilt_stars_food", "+{0}% health, stamina, eitr and time");
        Translations.AddEnglish("whitehilt_stars_seed", "Grows crops with more stars");
    }

    /// <summary>
    /// True if the item can have stars: something to eat, or a seed or crop that is planted.
    /// </summary>
    /// <param name="shared">Shared data of the item.</param>
    /// <returns>True if it can have stars.</returns>
    public static bool CanHaveStars(ItemDrop.ItemData.SharedData shared)
    {
        if (shared == null || !MasterySettings.Stars.Value || shared.m_maxQuality > 1 || shared.m_maxStackSize <= 1)
        {
            return false;
        }

        return IsEdible(shared) || IsSeed(shared.m_name);
    }

    /// <summary>
    /// True if the item can have stars.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>True if it can have stars.</returns>
    public static bool CanHaveStars(ItemDrop.ItemData item)
    {
        return item != null && CanHaveStars(item.m_shared);
    }

    /// <summary>
    /// True if items with this shared name can have stars.
    /// </summary>
    /// <param name="sharedName">Shared item name, e.g. <c>$item_raspberries</c>.</param>
    /// <returns>True if it can have stars.</returns>
    public static bool CanHaveStars(string sharedName)
    {
        if (string.IsNullOrEmpty(sharedName) || ObjectDB.instance == null)
        {
            return false;
        }

        return ItemsByName().TryGetValue(sharedName, out ItemDrop.ItemData.SharedData shared) && CanHaveStars(shared);
    }

    /// <summary>
    /// Stars of an item, 0 to <see cref="Max"/>.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>Its stars.</returns>
    public static int Of(ItemDrop.ItemData item)
    {
        return CanHaveStars(item) ? Mathf.Clamp(item.m_quality - 1, 0, Max) : 0;
    }

    /// <summary>
    /// Gives an item stars.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="stars">Stars, 0 to <see cref="Max"/>.</param>
    public static void Set(ItemDrop.ItemData item, int stars)
    {
        if (CanHaveStars(item))
        {
            item.m_quality = 1 + Mathf.Clamp(stars, 0, Max);
        }
    }

    /// <summary>
    /// The stars as text, e.g. three gold stars.
    /// </summary>
    /// <param name="stars">Stars.</param>
    /// <returns>Rich text, empty for none.</returns>
    public static string Text(int stars)
    {
        return stars <= 0 ? string.Empty : $"<color={StarColor}>{string.Concat(Enumerable.Repeat(StarText, stars))}</color>";
    }

    /// <summary>
    /// The most stars a skill level allows: one from 25, two from 50, three from 100.
    /// </summary>
    /// <param name="level">Skill level.</param>
    /// <param name="first">Milestone that gives the first star.</param>
    /// <param name="last">Milestone that gives the third star.</param>
    /// <returns>Most stars.</returns>
    public static int MaxAt(float level, Perk first, Perk last)
    {
        if (!MasterySettings.Stars.Value || !first.ReachedAt(level))
        {
            return 0;
        }

        return last.ReachedAt(level) ? 3 : level >= 50f ? 2 : 1;
    }

    /// <summary>
    /// Rolls stars: the first is likely with skill, each next one less so.
    /// </summary>
    /// <param name="level">Skill level.</param>
    /// <param name="maxStars">Most stars allowed.</param>
    /// <param name="chanceScale">Multiplier on the chance of the first star, e.g. 2 at the best picking time.</param>
    /// <param name="bonus">Added to every chance, e.g. from seeds with stars.</param>
    /// <returns>Stars rolled.</returns>
    public static int Roll(float level, int maxStars, float chanceScale = 1f, float bonus = 0f)
    {
        float factor = Perks.Factor(level);
        float[] chances = { 0.6f * factor * chanceScale, 0.35f, 0.25f };
        int stars = 0;
        while (stars < maxStars && Random.value < chances[stars] + bonus)
        {
            stars++;
        }

        return stars;
    }

    /// <summary>
    /// Starts counting the stars of starrable items taken out of inventories, e.g. while a piece is paid for.
    /// </summary>
    public static void BeginLedger()
    {
        if (ledgerDepth++ == 0)
        {
            ledgerStars = 0;
            ledgerCount = 0;
        }
    }

    /// <summary>
    /// Stops counting.
    /// </summary>
    public static void EndLedger()
    {
        ledgerDepth = Mathf.Max(0, ledgerDepth - 1);
    }

    /// <summary>
    /// Counts starrable items taken out while the ledger is open.
    /// </summary>
    /// <param name="item">The item taken.</param>
    /// <param name="amount">How many.</param>
    public static void Record(ItemDrop.ItemData item, int amount)
    {
        if (ledgerDepth > 0 && amount > 0)
        {
            ledgerStars += Of(item) * amount;
            ledgerCount += amount;
        }
    }

    /// <summary>
    /// Average stars of what was taken since the ledger opened, rounded down.
    /// </summary>
    /// <returns>Stars, 0 to <see cref="Max"/>.</returns>
    public static int LedgerAverage()
    {
        return ledgerCount == 0 ? 0 : Mathf.Clamp(ledgerStars / ledgerCount, 0, Max);
    }

    /// <summary>
    /// Takes starrable items out of an inventory, the most stars first, and counts them in the ledger.
    /// </summary>
    /// <param name="inventory">The inventory.</param>
    /// <param name="name">Shared item name.</param>
    /// <param name="amount">How many to take.</param>
    /// <param name="worldLevelBased">Vanilla's world level check.</param>
    public static void RemoveBestFirst(Inventory inventory, string name, int amount, bool worldLevelBased)
    {
        List<ItemDrop.ItemData> items = inventory.m_inventory
            .Where(item => item.m_shared.m_name == name && (!worldLevelBased || item.m_worldLevel >= Game.m_worldLevel))
            .OrderByDescending(item => item.m_quality)
            .ToList();
        foreach (ItemDrop.ItemData item in items)
        {
            int take = Mathf.Min(item.m_stack, amount);
            item.m_stack -= take;
            amount -= take;
            Record(item, take);
            if (amount <= 0)
            {
                break;
            }
        }

        inventory.m_inventory.RemoveAll(item => item.m_stack <= 0);
        inventory.Changed();
    }

    /// <summary>
    /// The stars a recipe's ingredients would give, from the starrable items in the inventory, the most stars first.
    /// Ingredients without stars count as none.
    /// </summary>
    /// <param name="inventory">The crafter's inventory.</param>
    /// <param name="recipe">The recipe.</param>
    /// <param name="multiplier">How many are crafted at once.</param>
    /// <returns>Average stars, rounded down.</returns>
    public static int PredictIngredients(Inventory inventory, Recipe recipe, int multiplier)
    {
        int stars = 0;
        int count = 0;
        foreach (Piece.Requirement requirement in recipe.m_resources.Where(requirement => requirement.m_resItem != null))
        {
            ItemDrop.ItemData.SharedData shared = requirement.m_resItem.m_itemData.m_shared;
            if (!CanHaveStars(shared))
            {
                continue;
            }

            int need = requirement.GetAmount(1) * Mathf.Max(1, multiplier);
            count += need;
            foreach (ItemDrop.ItemData item in inventory.m_inventory.Where(item => item.m_shared.m_name == shared.m_name).OrderByDescending(item => item.m_quality))
            {
                int take = Mathf.Min(item.m_stack, need);
                stars += Of(item) * take;
                need -= take;
                if (need <= 0)
                {
                    break;
                }
            }
        }

        return count == 0 ? 0 : Mathf.Clamp(stars / count, 0, Max);
    }

    /// <summary>
    /// Remembers the stars of a food the player just ate. The game saves only the food's name, so the stars live in the
    /// player's custom data.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="food">The food eaten.</param>
    /// <param name="stars">Its stars.</param>
    public static void RememberFood(Player player, Player.Food food, int stars)
    {
        string key = FoodStarsPrefix + food.m_name;
        if (stars > 0)
        {
            player.m_customData[key] = stars.ToString();
            food.m_time = food.m_item.m_shared.m_foodBurnTime * (1f + FoodBonus(stars));
        }
        else
        {
            player.m_customData.Remove(key);
        }
    }

    /// <summary>
    /// Stars of a food the player has eaten.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="food">The food.</param>
    /// <returns>Its stars.</returns>
    public static int OfFood(Player player, Player.Food food)
    {
        return player.m_customData.TryGetValue(FoodStarsPrefix + food.m_name, out string text) && int.TryParse(text, out int stars)
            ? Mathf.Clamp(stars, 0, Max)
            : 0;
    }

    /// <summary>
    /// Share added to a food's health, stamina, eitr and time by its stars.
    /// </summary>
    /// <param name="stars">Stars.</param>
    /// <returns>The share.</returns>
    public static float FoodBonus(int stars)
    {
        return stars * MasterySettings.StarFoodBonus.Value;
    }

    /// <summary>
    /// Lines for an item's tooltip.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>Tooltip text, empty without stars.</returns>
    public static string Tooltip(ItemDrop.ItemData item)
    {
        int stars = Of(item);
        if (stars == 0)
        {
            return string.Empty;
        }

        string text = $"\n$whitehilt_stars: {Text(stars)}";
        if (IsEdible(item.m_shared))
        {
            text += "\n" + Localization.instance.Localize("$whitehilt_stars_food", Mathf.RoundToInt(FoodBonus(stars) * 100f).ToString());
        }
        else if (IsSeed(item.m_shared.m_name))
        {
            text += "\n$whitehilt_stars_seed";
        }

        return text;
    }

    /// <summary>
    /// Forgets the cached item lists, e.g. when a new ObjectDB is loaded.
    /// </summary>
    public static void ClearCache()
    {
        seedNames = null;
        itemsByName = null;
    }

    private static Dictionary<string, ItemDrop.ItemData.SharedData> ItemsByName()
    {
        if (itemsByName == null || itemsByName.Count == 0)
        {
            itemsByName = new Dictionary<string, ItemDrop.ItemData.SharedData>();
            foreach (GameObject prefab in ObjectDB.instance.m_items)
            {
                ItemDrop drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                if (drop != null && !itemsByName.ContainsKey(drop.m_itemData.m_shared.m_name))
                {
                    itemsByName[drop.m_itemData.m_shared.m_name] = drop.m_itemData.m_shared;
                }
            }
        }

        return itemsByName;
    }

    private static bool IsEdible(ItemDrop.ItemData.SharedData shared)
    {
        return shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable && (shared.m_food > 0f || shared.m_foodStamina > 0f || shared.m_foodEitr > 0f);
    }

    // What is paid to plant a crop: carrot, turnip and onion seeds, barley and flax.
    private static bool IsSeed(string sharedName)
    {
        if (seedNames == null || seedNames.Count == 0)
        {
            seedNames = new HashSet<string>();
            if (ZNetScene.instance == null)
            {
                return false;
            }

            foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
            {
                Plant plant = prefab != null ? prefab.GetComponent<Plant>() : null;
                Piece piece = plant != null ? prefab.GetComponent<Piece>() : null;
                bool crop = plant != null && plant.m_grownPrefabs.Any(grown => grown != null && grown.GetComponent<Pickable>()?.m_pickRaiseSkill == Skills.SkillType.Farming);
                if (!crop || piece == null)
                {
                    continue;
                }

                foreach (Piece.Requirement requirement in piece.m_resources.Where(requirement => requirement.m_resItem != null))
                {
                    seedNames.Add(requirement.m_resItem.m_itemData.m_shared.m_name);
                }
            }
        }

        return seedNames.Contains(sharedName);
    }
}
