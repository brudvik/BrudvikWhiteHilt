using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Backpack;

/// <summary>
/// Equipment slots for helmet, chest, legs, cape, trinket and the first accessory. What lies in a slot is worn, and what
/// is worn lies in its slot: putting an item in a slot puts it on, equipping it from the grid moves it into the slot, and
/// right-clicking it or dragging it out takes it off. The slots are hidden rows of the player's inventory, shown in a panel.
/// </summary>
public static class GearSlots
{
    private const float RetrySeconds = 2f;

    private static readonly Vector2i[] slots =
    {
        new(0, BackpackLayout.GearRow),
        new(1, BackpackLayout.GearRow),
        new(2, BackpackLayout.GearRow),
        new(3, BackpackLayout.GearRow),
        new(4, BackpackLayout.GearRow),
        new(0, BackpackLayout.UtilityRow)
    };

    private static readonly Dictionary<Vector2i, ItemDrop.ItemData> lastTried = new();
    private static float nextRetry;

    /// <summary>
    /// True while the inventory window handles a click or drop; it unequips and re-equips items on its own then.
    /// </summary>
    public static bool Dropping { get; set; }

    /// <summary>
    /// True if the item lies in one of the equipment slots.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>True for items in a slot.</returns>
    public static bool InSlot(ItemDrop.ItemData item)
    {
        return BackpackLayout.TryGetGearSlot(item, out Vector2i slot) && item.m_gridPos == slot;
    }

    /// <summary>
    /// Moves an item that was just equipped into its slot, and what lay there to where the item came from.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="item">The equipped item.</param>
    public static void OnEquipped(Player player, ItemDrop.ItemData item)
    {
        if (Dropping || player != Player.m_localPlayer || !BackpackLayout.IsLocalInventory(player.m_inventory)
            || !BackpackLayout.TryGetGearSlot(item, out Vector2i slot) || item.m_gridPos == slot)
        {
            return;
        }

        MoveIntoSlot(player.m_inventory, item, slot);
    }

    /// <summary>
    /// Moves worn items that lie in the grid into their slots, e.g. the first time the mod runs.
    /// </summary>
    /// <param name="player">The player.</param>
    public static void MoveWornIntoSlots(Player player)
    {
        foreach (ItemDrop.ItemData item in new List<ItemDrop.ItemData>(player.m_inventory.GetAllItems()))
        {
            if (player.IsItemEquiped(item) && BackpackLayout.TryGetGearSlot(item, out Vector2i slot) && item.m_gridPos != slot)
            {
                MoveIntoSlot(player.m_inventory, item, slot);
            }
        }
    }

    /// <summary>
    /// Takes an item out of its slot into the grid. The caller unequips it.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="item">The item in a slot.</param>
    /// <returns>False when the grid is full; the item then stays.</returns>
    public static bool MoveToGrid(Player player, ItemDrop.ItemData item)
    {
        Vector2i free = BackpackLayout.FindGridSlot(player.m_inventory, BackpackLayout.VisibleRows(player), false);
        if (free.x < 0)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_noroom");
            return false;
        }

        item.m_gridPos = free;
        player.m_inventory.Changed();
        return true;
    }

    /// <summary>
    /// After the inventory window moved items: worn items that left their slot are taken off.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="items">The items the window moved.</param>
    public static void AfterDrop(Player player, params ItemDrop.ItemData[] items)
    {
        foreach (ItemDrop.ItemData item in items)
        {
            if (item != null && player.m_inventory.ContainsItem(item) && player.IsItemEquiped(item)
                && BackpackLayout.TryGetGearSlot(item, out Vector2i slot) && item.m_gridPos != slot)
            {
                player.UnequipItem(item, false);
            }
        }
    }

    /// <summary>
    /// Puts on what lies in the slots. Called every frame for the local player; a failed attempt is retried after a while.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Tick(Player player)
    {
        if (player.IsDead() || player.IsTeleporting() || player.InAttack() || !BackpackLayout.IsLocalInventory(player.m_inventory))
        {
            return;
        }

        bool retry = Time.time >= nextRetry;
        if (retry)
        {
            nextRetry = Time.time + RetrySeconds;
        }

        foreach (Vector2i slot in slots)
        {
            ItemDrop.ItemData item = player.m_inventory.GetItemAt(slot.x, slot.y);
            if (item == null || player.IsItemEquiped(item) || player.IsEquipActionQueued(item))
            {
                lastTried.Remove(slot);
                continue;
            }

            if (retry || !lastTried.TryGetValue(slot, out ItemDrop.ItemData tried) || tried != item)
            {
                lastTried[slot] = item;
                player.EquipItem(item);
            }
        }
    }

    private static void MoveIntoSlot(Inventory inventory, ItemDrop.ItemData item, Vector2i slot)
    {
        ItemDrop.ItemData occupant = inventory.GetItemAt(slot.x, slot.y);
        Vector2i from = item.m_gridPos;
        item.m_gridPos = slot;
        if (occupant != null && occupant != item)
        {
            occupant.m_gridPos = from;
        }

        inventory.Changed();
    }
}
