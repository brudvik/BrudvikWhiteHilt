using BrudvikWhiteHilt.Mastery;
using HarmonyLib;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Mastery;

/// <summary>
/// Makes items with stars work: they count for recipes, never stack with other stars, show their stars and give more
/// when eaten.
/// </summary>
[HarmonyPatch]
public static class StarPatches
{
    private static string craftPrefab;
    private static int craftStars = -1;

    /// <summary>
    /// Forgets the cached item lists when a new ObjectDB starts.
    /// </summary>
    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
    [HarmonyPostfix]
    private static void ClearItemCache()
    {
        Stars.ClearCache();
    }

    /// <summary>
    /// Forgets the cached plant lists when a new ZNetScene starts.
    /// </summary>
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    [HarmonyPostfix]
    private static void ClearPlantCache()
    {
        Stars.ClearCache();
        Gathering.ClearCache();
    }

    /// <summary>
    /// Lets recipes count starrable items of every star. Vanilla only counts items of one quality at a time.
    /// </summary>
    /// <param name="name">Shared item name.</param>
    /// <param name="quality">Quality asked for.</param>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.CountItems))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static void CountEveryStar(string name, ref int quality)
    {
        if (quality == 1 && Stars.CanHaveStars(name))
        {
            quality = -1;
        }
    }

    /// <summary>
    /// Takes starrable items the most stars first, so starred ingredients go into what is made.
    /// </summary>
    /// <param name="__instance">The inventory.</param>
    /// <param name="name">Shared item name.</param>
    /// <param name="amount">How many.</param>
    /// <param name="itemQuality">Quality asked for, or -1.</param>
    /// <param name="worldLevelBased">Vanilla's world level check.</param>
    /// <returns>False when handled here.</returns>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveItem), typeof(string), typeof(int), typeof(int), typeof(bool))]
    [HarmonyPrefix]
    private static bool RemoveBestFirst(Inventory __instance, string name, int amount, int itemQuality, bool worldLevelBased)
    {
        if (itemQuality > 1 || !Stars.CanHaveStars(name))
        {
            return true;
        }

        Stars.RemoveBestFirst(__instance, name, amount, worldLevelBased);
        return false;
    }

    /// <summary>
    /// Keeps items with different stars apart. Vanilla ignores quality for items that have none of their own.
    /// </summary>
    /// <param name="__instance">The item.</param>
    /// <param name="other">The other item.</param>
    /// <param name="__result">Whether they stack.</param>
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.IsSameType))]
    [HarmonyPostfix]
    private static void KeepStarsApart(ItemDrop.ItemData __instance, ItemDrop.ItemData other, ref bool __result)
    {
        if (__result && __instance.m_quality != other.m_quality && Stars.CanHaveStars(__instance))
        {
            __result = false;
        }
    }

    /// <summary>
    /// Swaps two stacks with different stars when one is dropped on the other, as vanilla does for different items.
    /// </summary>
    /// <param name="__instance">The grid dropped on.</param>
    /// <param name="fromInventory">Where the item comes from.</param>
    /// <param name="item">The item dragged.</param>
    /// <param name="amount">How many.</param>
    /// <param name="pos">Grid position.</param>
    /// <param name="__result">True when done.</param>
    /// <returns>False when handled here.</returns>
    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.DropItem))]
    [HarmonyPrefix]
    private static bool SwapStars(InventoryGrid __instance, Inventory fromInventory, ItemDrop.ItemData item, int amount, Vector2i pos, ref bool __result)
    {
        ItemDrop.ItemData target = __instance.m_inventory.GetItemAt(pos.x, pos.y);
        if (target == null || target == item || item.m_stack != amount || target.m_shared.m_name != item.m_shared.m_name
            || target.m_quality == item.m_quality || !Stars.CanHaveStars(item))
        {
            return true;
        }

        fromInventory.RemoveItem(item);
        fromInventory.MoveItemToThis(__instance.m_inventory, target, target.m_stack, item.m_gridPos.x, item.m_gridPos.y);
        __instance.m_inventory.MoveItemToThis(fromInventory, item, amount, pos.x, pos.y);
        __result = true;
        return false;
    }

    /// <summary>
    /// Shows stars in the corner of inventory slots, where vanilla shows quality.
    /// </summary>
    /// <param name="__instance">The grid.</param>
    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
    [HarmonyPostfix]
    private static void ShowStars(InventoryGrid __instance)
    {
        Inventory inventory = __instance.m_inventory;
        if (inventory == null)
        {
            return;
        }

        int width = inventory.GetWidth();
        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
        {
            int stars = Stars.Of(item);
            InventoryElement element = stars > 0 ? __instance.GetElement(item.m_gridPos.x, item.m_gridPos.y, width) : null;
            if (element != null && element.m_quality != null)
            {
                element.m_quality.enabled = true;
                element.m_quality.text = Stars.Text(stars);
            }
        }
    }

    /// <summary>
    /// Adds the stars and what they give to the tooltip.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="crafting">True in the crafting list.</param>
    /// <param name="__result">The tooltip.</param>
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool))]
    [HarmonyPostfix]
    private static void TooltipStars(ItemDrop.ItemData item, bool crafting, ref string __result)
    {
        if (!crafting && item != null)
        {
            __result += Stars.Tooltip(item);
        }
    }

    /// <summary>
    /// Shows stars instead of vanilla's quality number on items lying on the ground.
    /// </summary>
    /// <param name="__instance">The item drop.</param>
    /// <param name="__result">Hover text.</param>
    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.GetHoverText))]
    [HarmonyPostfix]
    private static void HoverStars(ItemDrop __instance, ref string __result)
    {
        int stars = Stars.Of(__instance.m_itemData);
        if (stars > 0 && __result != null)
        {
            __result = __result.Replace($"[{__instance.m_itemData.m_quality}] ", " " + Stars.Text(stars));
        }
    }

    /// <summary>
    /// Gives new drops the stars of the current context, and lets gathering see what is dropped.
    /// </summary>
    /// <param name="item">The new item drop.</param>
    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.OnCreateNew), typeof(ItemDrop), typeof(bool))]
    [HarmonyPrefix]
    private static void StarNewDrop(ItemDrop item)
    {
        if (item == null)
        {
            return;
        }

        if (Stars.DropStars != null && Stars.CanHaveStars(item.m_itemData))
        {
            Stars.Set(item.m_itemData, Stars.DropStars(item));
        }

        Gathering.Observe(item);
    }

    /// <summary>
    /// Notes the stars of what is eaten.
    /// </summary>
    /// <param name="item">The food.</param>
    /// <param name="__state">Its stars.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.EatFood))]
    [HarmonyPrefix]
    private static void BeforeEat(ItemDrop.ItemData item, out int __state)
    {
        __state = Stars.Of(item);
    }

    /// <summary>
    /// Remembers the stars of what was eaten and lets it last longer.
    /// </summary>
    /// <param name="__instance">The player.</param>
    /// <param name="item">The food.</param>
    /// <param name="__result">True if eaten.</param>
    /// <param name="__state">Its stars.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.EatFood))]
    [HarmonyPostfix]
    private static void AfterEat(Player __instance, ItemDrop.ItemData item, bool __result, int __state)
    {
        if (!__result || item?.m_dropPrefab == null)
        {
            return;
        }

        Player.Food food = __instance.m_foods.Find(eaten => eaten.m_name == item.m_dropPrefab.name);
        if (food != null)
        {
            Stars.RememberFood(__instance, food, __state);
        }
    }

    /// <summary>
    /// Adds what the stars of eaten food give.
    /// </summary>
    /// <param name="__instance">The player.</param>
    /// <param name="hp">Max health.</param>
    /// <param name="stamina">Max stamina.</param>
    /// <param name="eitr">Max eitr.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.GetTotalFoodValue))]
    [HarmonyPostfix]
    private static void AddStarFood(Player __instance, ref float hp, ref float stamina, ref float eitr)
    {
        foreach (Player.Food food in __instance.m_foods)
        {
            float bonus = Stars.FoodBonus(Stars.OfFood(__instance, food));
            if (bonus > 0f)
            {
                hp += food.m_health * bonus;
                stamina += food.m_stamina * bonus;
                eitr += food.m_eitr * bonus;
            }
        }
    }

    /// <summary>
    /// Works out the stars of a dish before it is made, since vanilla adds it before taking the ingredients.
    /// </summary>
    /// <param name="__instance">The inventory screen.</param>
    /// <param name="player">The crafter.</param>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
    [HarmonyPrefix]
    private static void BeforeCrafting(InventoryGui __instance, Player player)
    {
        craftStars = -1;
        Recipe recipe = __instance.m_craftRecipe;
        if (recipe == null || recipe.m_item == null || __instance.m_craftUpgradeItem != null || !Stars.CanHaveStars(recipe.m_item.m_itemData))
        {
            return;
        }

        int multiplier = __instance.m_multiCrafting ? __instance.m_multiCraftAmount : 1;
        int ingredients = Stars.PredictIngredients(player.GetInventory(), recipe, multiplier);
        craftStars = Mathf.Min(Stars.Max, ingredients + CookingStars.Roll(player.GetSkillLevel(Skills.SkillType.Cooking)));
        craftPrefab = recipe.m_item.gameObject.name;
    }

    /// <summary>
    /// Clears the dish's stars after crafting.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
    [HarmonyFinalizer]
    private static void AfterCrafting()
    {
        craftStars = -1;
        craftPrefab = null;
    }

    /// <summary>
    /// Gives the dish being crafted its stars.
    /// </summary>
    /// <param name="name">Prefab name.</param>
    /// <param name="quality">Quality, changed to carry the stars.</param>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(string), typeof(int), typeof(int), typeof(int), typeof(long), typeof(string), typeof(Vector2i), typeof(bool), typeof(bool), typeof(bool))]
    [HarmonyPrefix]
    private static void StarCraftedDish(string name, ref int quality)
    {
        if (craftStars > 0 && quality == 1 && name == craftPrefab)
        {
            quality = 1 + craftStars;
        }
    }
}
