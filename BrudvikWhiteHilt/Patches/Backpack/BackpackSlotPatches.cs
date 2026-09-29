using BrudvikWhiteHilt.Backpack;
using HarmonyLib;
using System;

namespace BrudvikWhiteHilt.Patches.Backpack;

/// <summary>
/// Keeps worn items in their equipment slots: equipping moves an item into its slot, right-clicking an item in a slot
/// takes it off, and the inventory window may only drop items where they may lie.
/// </summary>
[HarmonyPatch]
public static class BackpackSlotPatches
{
    /// <summary>
    /// Moves a newly equipped helmet, armor, cape or trinket into its slot.
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
            || !GearSlots.InSlot(item))
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

        __result = false;
        return false;
    }

    /// <summary>
    /// Puts on what lies in the equipment slots.
    /// </summary>
    /// <param name="__instance">The player.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    [HarmonyPostfix]
    public static void PlayerUpdate(Player __instance)
    {
        if (__instance == Player.m_localPlayer)
        {
            GearSlots.Tick(__instance);
        }
    }
}
