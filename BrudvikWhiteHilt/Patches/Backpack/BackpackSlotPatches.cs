using BrudvikWhiteHilt.Backpack;
using HarmonyLib;
using System;

namespace BrudvikWhiteHilt.Patches.Backpack;

/// <summary>
/// Keeps worn items in their equipment slots: equipping moves an item into its slot, right-clicking an item in a slot
/// takes it off, and the inventory window may only drop items where they may lie. The extra accessory slots add their
/// items' effects, stats and weight.
/// </summary>
[HarmonyPatch]
public static class BackpackSlotPatches
{
    /// <summary>
    /// Sends an accessory to a free extra slot when the game's own slot is taken, and refuses a second of the same kind.
    /// </summary>
    /// <param name="__instance">The character.</param>
    /// <param name="item">The item.</param>
    /// <param name="__result">False when handled here.</param>
    /// <returns>False to skip the game's equipping.</returns>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
    [HarmonyPrefix]
    public static bool EquipItemPrefix(Humanoid __instance, ItemDrop.ItemData item, ref bool __result)
    {
        if (__instance is not Player player || player != Player.m_localPlayer || GearSlots.Dropping
            || item?.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Utility || player.IsItemEquiped(item)
            || !BackpackLayout.IsLocalInventory(player.m_inventory) || UtilitySlots.BeforeEquip(player, item))
        {
            return true;
        }

        __result = false;
        return false;
    }

    /// <summary>
    /// Moves a newly equipped helmet, armor, cape or trinket into its slot, and takes up the shield and ammo from their
    /// slots with a weapon.
    /// </summary>
    /// <param name="__instance">The character.</param>
    /// <param name="item">The item.</param>
    /// <param name="__result">Whether it was equipped.</param>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
    [HarmonyPostfix]
    public static void EquipItem(Humanoid __instance, ItemDrop.ItemData item, bool __result)
    {
        if (__result && __instance is Player player)
        {
            GearSlots.OnEquipped(player, item);
            HandSlots.OnEquipped(player, item);
        }
    }

    /// <summary>
    /// Notes a one-handed weapon put away, so the shield from its slot follows.
    /// </summary>
    /// <param name="__instance">The character.</param>
    /// <param name="item">The item.</param>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
    [HarmonyPostfix]
    public static void UnequipItem(Humanoid __instance, ItemDrop.ItemData item)
    {
        if (__instance is Player player)
        {
            HandSlots.OnUnequipped(player, item);
        }
    }

    /// <summary>
    /// Leaves what lies in the accessory slots out of item counts, so recipes, building and fuel do not count on it.
    /// </summary>
    /// <param name="__instance">The inventory.</param>
    /// <param name="name">Shared item name.</param>
    /// <param name="quality">Quality, or -1 for any.</param>
    /// <param name="matchWorldLevel">Vanilla's world level check.</param>
    /// <param name="__result">The count.</param>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.CountItems))]
    [HarmonyPostfix]
    public static void CountItems(Inventory __instance, string name, int quality, bool matchWorldLevel, ref int __result)
    {
        if (name != null && __result > 0 && BackpackLayout.IsLocalInventory(__instance))
        {
            __result = UnityEngine.Mathf.Max(0, __result - UtilitySlots.CountKept(__instance, name, quality, matchWorldLevel));
        }
    }

    /// <summary>
    /// Never takes items from the accessory slots when the game uses items by name: crafting, building, fuel and the like.
    /// </summary>
    /// <param name="__instance">The inventory.</param>
    /// <param name="name">Shared item name.</param>
    /// <param name="amount">How many.</param>
    /// <param name="itemQuality">Quality, or -1 for any.</param>
    /// <param name="worldLevelBased">Vanilla's world level check.</param>
    /// <returns>False when handled here.</returns>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveItem), typeof(string), typeof(int), typeof(int), typeof(bool))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool RemoveItemByName(Inventory __instance, string name, int amount, int itemQuality, bool worldLevelBased)
    {
        if (!BackpackLayout.IsLocalInventory(__instance) || UtilitySlots.CountKept(__instance, name, itemQuality, worldLevelBased) == 0)
        {
            return true;
        }

        UtilitySlots.RemoveUnkept(__instance, name, amount, itemQuality, worldLevelBased);
        return false;
    }

    /// <summary>
    /// Ammo in the ammo slot is used before any other of its kind.
    /// </summary>
    /// <param name="__instance">The inventory.</param>
    /// <param name="ammoName">The ammo type asked for.</param>
    /// <param name="matchPrefabName">A particular ammo asked for, or null.</param>
    /// <param name="__result">The ammo found.</param>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetAmmoItem))]
    [HarmonyPostfix]
    public static void GetAmmoItem(Inventory __instance, string ammoName, string matchPrefabName, ref ItemDrop.ItemData __result)
    {
        if (matchPrefabName == null && BackpackLayout.IsLocalInventory(__instance)
            && HandSlots.SlotAmmo(__instance, ammoName) is ItemDrop.ItemData ammo)
        {
            __result = ammo;
        }
    }

    /// <summary>
    /// Right-clicking an item in a slot takes it out into the grid, and off.
    /// </summary>
    /// <param name="__instance">The player.</param>
    /// <param name="item">The item.</param>
    /// <param name="__result">True when handled.</param>
    /// <returns>False when the item is not worn, or the grid is full.</returns>
    [HarmonyPatch(typeof(Player), nameof(Player.ToggleEquipped))]
    [HarmonyPrefix]
    public static bool ToggleEquipped(Player __instance, ItemDrop.ItemData item, ref bool __result)
    {
        if (__instance != Player.m_localPlayer || __instance.InAttack() || !BackpackLayout.IsLocalInventory(__instance.m_inventory)
            || (!GearSlots.InSlot(item) && !UtilitySlots.IsExtraSlot(item.m_gridPos)))
        {
            return true;
        }

        if (GearSlots.MoveToGrid(__instance, item) && __instance.IsItemEquiped(item))
        {
            return true;
        }

        __result = true;
        return false;
    }

    /// <summary>
    /// Marks that the inventory window moves items, so its own unequipping and re-equipping is left alone.
    /// </summary>
    /// <param name="__instance">The inventory window.</param>
    /// <param name="item">The item clicked on.</param>
    /// <param name="__state">The dragged item and the item clicked on.</param>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnSelectedItem))]
    [HarmonyPrefix]
    public static void OnSelectedItem(InventoryGui __instance, ItemDrop.ItemData item, out ItemDrop.ItemData[] __state)
    {
        __state = new[] { __instance.m_dragGo != null ? __instance.m_dragItem : null, item };
        GearSlots.Dropping = true;
    }

    /// <summary>
    /// Takes off worn items that were dragged out of their slot.
    /// </summary>
    /// <param name="__state">The dragged item and the item clicked on.</param>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnSelectedItem))]
    [HarmonyPostfix]
    public static void OnSelectedItemPostfix(ItemDrop.ItemData[] __state)
    {
        GearSlots.Dropping = false;
        Player player = Player.m_localPlayer;
        if (player != null && BackpackLayout.IsLocalInventory(player.m_inventory))
        {
            GearSlots.AfterDrop(player, __state);
        }
    }

    /// <summary>
    /// Always clears the drop mark.
    /// </summary>
    /// <param name="__exception">An exception thrown by the method, passed on.</param>
    /// <returns>The same exception.</returns>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnSelectedItem))]
    [HarmonyFinalizer]
    public static Exception OnSelectedItemFinalizer(Exception __exception)
    {
        GearSlots.Dropping = false;
        return __exception;
    }

    /// <summary>
    /// Refuses a drop that would put an item, or the item it swaps with, where it may not lie. Checked up front,
    /// since vanilla removes the dragged item before it moves the other one.
    /// </summary>
    /// <param name="__instance">The grid dropped on.</param>
    /// <param name="fromInventory">The inventory the item comes from.</param>
    /// <param name="item">The dragged item.</param>
    /// <param name="amount">How many are dragged.</param>
    /// <param name="pos">The position dropped on.</param>
    /// <param name="__result">False when refused.</param>
    /// <returns>False to refuse.</returns>
    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.DropItem))]
    [HarmonyPrefix]
    public static bool DropItem(InventoryGrid __instance, Inventory fromInventory, ItemDrop.ItemData item, int amount, Vector2i pos,
        ref bool __result)
    {
        Player player = Player.m_localPlayer;
        Inventory target = __instance.m_inventory;
        bool toPlayer = BackpackLayout.IsLocalInventory(target);
        bool fromPlayer = BackpackLayout.IsLocalInventory(fromInventory);
        if (!toPlayer && !fromPlayer)
        {
            return true;
        }

        ItemDrop.ItemData itemAt = target.GetItemAt(pos.x, pos.y);
        if (itemAt == item)
        {
            return true;
        }

        bool swap = itemAt != null && item.m_stack == amount && (itemAt.m_shared.m_name != item.m_shared.m_name
            || (item.m_shared.m_maxQuality > 1 && itemAt.m_quality != item.m_quality) || itemAt.m_shared.m_maxStackSize == 1);
        bool allowed = (!toPlayer || BackpackLayout.CanHold(player, item, pos))
            && (!swap || !fromPlayer || BackpackLayout.CanHold(player, itemAt, item.m_gridPos));
        if (allowed)
        {
            return true;
        }

        if (toPlayer && BackpackLayout.KindAt(pos, BackpackLayout.VisibleRows(player)) == SlotKind.Utility
            && UtilitySlots.Fits(item, pos))
        {
            player.Message(MessageHud.MessageType.Center, "$whitehilt_backpack_duplicate");
        }

        __result = false;
        return false;
    }

    /// <summary>
    /// Puts on what lies in the equipment slots and eats from the food slots.
    /// </summary>
    /// <param name="__instance">The player.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    [HarmonyPostfix]
    public static void PlayerUpdate(Player __instance)
    {
        if (__instance == Player.m_localPlayer)
        {
            GearSlots.Tick(__instance);
            FoodSlots.Tick(__instance);
            UtilitySlots.Tick(__instance);
            HandSlots.Tick(__instance);
        }
    }

    /// <summary>
    /// Adds the extra accessories to the summed equipment modifiers (movement, stamina use, resistances and so on).
    /// </summary>
    /// <param name="__instance">The player.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdateModifiers))]
    [HarmonyPostfix]
    public static void UpdateModifiers(Player __instance)
    {
        if (__instance == Player.m_localPlayer)
        {
            UtilitySlots.AddModifiers(__instance.m_equipmentModifierValues);
        }
    }

    /// <summary>
    /// Adds the extra accessories' eitr regeneration.
    /// </summary>
    /// <param name="__instance">The player.</param>
    /// <param name="__result">The equipment's eitr regeneration modifier.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentEitrRegenModifier))]
    [HarmonyPostfix]
    public static void GetEquipmentEitrRegenModifier(Player __instance, ref float __result)
    {
        if (__instance == Player.m_localPlayer)
        {
            __result += UtilitySlots.EitrRegen;
        }
    }

    /// <summary>
    /// Counts the extra accessories as worn equipment weight.
    /// </summary>
    /// <param name="__instance">The character.</param>
    /// <param name="__result">The weight of the worn equipment.</param>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.GetEquipmentWeight))]
    [HarmonyPostfix]
    public static void GetEquipmentWeight(Humanoid __instance, ref float __result)
    {
        if (__instance == Player.m_localPlayer)
        {
            __result += UtilitySlots.Weight;
        }
    }

    /// <summary>
    /// Leaves the hotbar alone while a food key with the same number is pressed.
    /// </summary>
    /// <param name="__instance">The player.</param>
    /// <param name="index">The hotbar slot, 1 to 8.</param>
    /// <returns>False to skip the hotbar.</returns>
    [HarmonyPatch(typeof(Player), nameof(Player.UseHotbarItem))]
    [HarmonyPrefix]
    public static bool UseHotbarItem(Player __instance, int index)
    {
        return __instance != Player.m_localPlayer || !FoodSlots.BlocksHotbar(index);
    }
}
