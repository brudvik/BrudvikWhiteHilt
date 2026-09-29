using BrudvikWhiteHilt.Backpack;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrudvikWhiteHilt.Patches.Backpack;

/// <summary>
/// Keeps the hidden rows of the local player's inventory out of the vanilla searches for room: new items only go into
/// the visible grid, and an item may only be put where <see cref="BackpackLayout.CanHold"/> allows.
/// </summary>
[HarmonyPatch]
public static class BackpackInventoryPatches
{
    private static Vector2i? reservedSlot;

    /// <summary>
    /// New items go into the visible grid only.
    /// </summary>
    /// <param name="__instance">The inventory.</param>
    /// <param name="topFirst">Search from the top row down.</param>
    /// <param name="__result">The empty position, or (-1, -1).</param>
    /// <returns>False for the local player's inventory.</returns>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.FindEmptySlot))]
    [HarmonyPrefix]
    public static bool FindEmptySlot(Inventory __instance, bool topFirst, ref Vector2i __result)
    {
        if (!BackpackLayout.IsLocalInventory(__instance))
        {
            return true;
        }

        __result = reservedSlot ?? BackpackLayout.FindGridSlot(__instance, BackpackLayout.VisibleRows(Player.m_localPlayer), topFirst);
        return false;
    }

    /// <summary>
    /// Counts only the empty positions of the visible grid.
    /// </summary>
    /// <param name="__instance">The inventory.</param>
    /// <param name="__result">The number of empty positions.</param>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetEmptySlots))]
    [HarmonyPostfix]
    public static void GetEmptySlots(Inventory __instance, ref int __result)
    {
        if (BackpackLayout.IsLocalInventory(__instance))
        {
            __result = BackpackLayout.EmptyGridSlots(__instance, BackpackLayout.VisibleRows(Player.m_localPlayer));
        }
    }

    /// <summary>
    /// True only while the visible grid has an empty position.
    /// </summary>
    /// <param name="__instance">The inventory.</param>
    /// <param name="__result">Whether there is room.</param>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.HaveEmptySlot))]
    [HarmonyPostfix]
    public static void HaveEmptySlot(Inventory __instance, ref bool __result)
    {
        if (BackpackLayout.IsLocalInventory(__instance))
        {
            __result = BackpackLayout.EmptyGridSlots(__instance, BackpackLayout.VisibleRows(Player.m_localPlayer)) > 0;
        }
    }

    /// <summary>
    /// Room for an item: free space in stacks anywhere, plus the empty positions of the visible grid.
    /// </summary>
    /// <param name="__instance">The inventory.</param>
    /// <param name="item">The item.</param>
    /// <param name="stack">How many, or the item's own stack when not above 0.</param>
    /// <param name="__result">Whether it fits.</param>
    /// <returns>False for the local player's inventory.</returns>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.CanAddItem), new[] { typeof(ItemDrop.ItemData), typeof(int) })]
    [HarmonyPrefix]
    public static bool CanAddItem(Inventory __instance, ItemDrop.ItemData item, int stack, ref bool __result)
    {
        if (!BackpackLayout.IsLocalInventory(__instance))
        {
            return true;
        }

        if (stack <= 0)
        {
            stack = item.m_stack;
        }

        int empty = BackpackLayout.EmptyGridSlots(__instance, BackpackLayout.VisibleRows(Player.m_localPlayer));
        __result = __instance.FindFreeStackSpace(item.m_shared.m_name, item.m_worldLevel) + empty * item.m_shared.m_maxStackSize >= stack;
        return false;
    }

    /// <summary>
    /// Refuses to put an item where it may not lie. This is the path of dragging, "take all" and moving between inventories.
    /// </summary>
    /// <param name="__instance">The inventory.</param>
    /// <param name="item">The item.</param>
    /// <param name="x">Target column.</param>
    /// <param name="y">Target row.</param>
    /// <param name="__result">False when refused.</param>
    /// <returns>False to refuse.</returns>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem),
        new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int), typeof(bool) })]
    [HarmonyPrefix]
    public static bool AddItemAt(Inventory __instance, ItemDrop.ItemData item, int x, int y, ref bool __result)
    {
        if (!BackpackLayout.IsLocalInventory(__instance) || BackpackLayout.CanHold(Player.m_localPlayer, item, new Vector2i(x, y)))
        {
            return true;
        }

        __result = false;
        return false;
    }

    /// <summary>
    /// Lets a crafted or upgraded item go back into the hidden slot it was taken from, even when the grid is full.
    /// </summary>
    /// <param name="__instance">The inventory.</param>
    /// <param name="name">Prefab name of the item.</param>
    /// <param name="position">Where the item should go.</param>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), new[] { typeof(string), typeof(int), typeof(int), typeof(int),
        typeof(long), typeof(string), typeof(Vector2i), typeof(bool), typeof(bool), typeof(bool) })]
    [HarmonyPrefix]
    public static void AddItemByName(Inventory __instance, string name, Vector2i position)
    {
        reservedSlot = null;
        if (!BackpackLayout.IsLocalInventory(__instance) || __instance.GetItemAt(position.x, position.y) != null
            || BackpackLayout.KindAt(position, BackpackLayout.VisibleRows(Player.m_localPlayer)) == SlotKind.Grid)
        {
            return;
        }

        ItemDrop prefab = ObjectDB.instance?.GetItemPrefab(name)?.GetComponent<ItemDrop>();
        if (prefab != null && BackpackLayout.CanHold(Player.m_localPlayer, prefab.m_itemData, position))
        {
            reservedSlot = position;
        }
    }

    /// <summary>
    /// Clears the reserved slot again.
    /// </summary>
    /// <param name="__exception">An exception thrown by the method, passed on.</param>
    /// <returns>The same exception.</returns>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), new[] { typeof(string), typeof(int), typeof(int), typeof(int),
        typeof(long), typeof(string), typeof(Vector2i), typeof(bool), typeof(bool), typeof(bool) })]
    [HarmonyFinalizer]
    public static Exception AddItemByNameFinalizer(Exception __exception)
    {
        reservedSlot = null;
        return __exception;
    }

    /// <summary>
    /// "Place stacks" only takes from the visible grid, not from the stored hotbar or the equipment slots.
    /// </summary>
    /// <param name="__instance">The container's inventory.</param>
    /// <param name="fromInventory">The player's inventory.</param>
    /// <param name="message">Whether to show how many were placed.</param>
    /// <param name="__result">How many were placed.</param>
    /// <returns>False for the local player's inventory.</returns>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.StackAll))]
    [HarmonyPrefix]
    public static bool StackAll(Inventory __instance, Inventory fromInventory, bool message, ref int __result)
    {
        if (!BackpackLayout.IsLocalInventory(fromInventory))
        {
            return true;
        }

        Player player = Player.m_localPlayer;
        int rows = BackpackLayout.VisibleRows(player);
        List<ItemDrop.ItemData> items = fromInventory.GetAllItems().Where(item => item.m_gridPos.y < rows).ToList();
        int before = __instance.CountItems(null);
        foreach (ItemDrop.ItemData item in items)
        {
            if (__instance.ContainsItemByName(item.m_shared.m_name) && !player.IsItemEquiped(item) && __instance.AddItem(item))
            {
                fromInventory.RemoveItem(item);
            }
        }

        __result = __instance.CountItems(null) - before;
        if (message)
        {
            player.Message(MessageHud.MessageType.Center, __result > 0 ? "$msg_stackall " + __result : "$msg_stackall_none");
        }

        __instance.Changed();
        fromInventory.Changed();
        Game.instance.IncrementPlayerStat(PlayerStatType.PlaceStacks);
        return false;
    }

    /// <summary>
    /// A grave is looted in one go when its items fit, counting the hidden slots they came from as room.
    /// </summary>
    /// <param name="__instance">The grave.</param>
    /// <param name="player">The player.</param>
    /// <param name="__result">Whether everything fits.</param>
    [HarmonyPatch(typeof(TombStone), nameof(TombStone.EasyFitInInventory))]
    [HarmonyPostfix]
    public static void EasyFitInInventory(TombStone __instance, Player player, ref bool __result)
    {
        Inventory inventory = player.GetInventory();
        if (__result || !BackpackLayout.IsLocalInventory(inventory))
        {
            return;
        }

        Inventory grave = __instance.m_container.GetInventory();
        int rows = BackpackLayout.VisibleRows(player);
        int needed = 0;
        foreach (ItemDrop.ItemData item in grave.GetAllItems())
        {
            bool ownSlot = BackpackLayout.KindAt(item.m_gridPos, rows) != SlotKind.Grid
                && inventory.GetItemAt(item.m_gridPos.x, item.m_gridPos.y) == null && BackpackLayout.CanHold(player, item, item.m_gridPos);
            if (!ownSlot && inventory.FindFreeStackSpace(item.m_shared.m_name, item.m_worldLevel) < item.m_stack)
            {
                needed++;
            }
        }

        __result = needed <= BackpackLayout.EmptyGridSlots(inventory, rows)
            && inventory.GetTotalWeight() + grave.GetTotalWeight() <= player.GetMaxCarryWeight();
    }
}
