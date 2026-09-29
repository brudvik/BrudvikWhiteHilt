using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Backpack;

/// <summary>
/// What a position in the player's inventory is used for.
/// </summary>
public enum SlotKind
{
    /// <summary>Not in use: nothing may lie here.</summary>
    Void,

    /// <summary>The visible inventory grid.</summary>
    Grid,

    /// <summary>The stored hotbar, swapped with the first row.</summary>
    Hotbar
}

/// <summary>
/// The rows of the local player's inventory. The visible rows are the vanilla rows (4, more when bought from a trader)
/// plus the extra rows. Below the most rows that can be shown lie hidden rows at fixed positions for the stored hotbar,
/// so nothing has to move when the visible rows change. Without the mod the game drops what lies there at your feet.
/// </summary>
public static class BackpackLayout
{
    /// <summary>Columns of the player inventory.</summary>
    public const int Width = 8;

    /// <summary>Most rows shown in the inventory grid.</summary>
    public const int MaxVisibleRows = 12;

    /// <summary>Row of the stored hotbar.</summary>
    public const int HotbarRow = MaxVisibleRows;

    /// <summary>Rows of the player inventory, visible and hidden.</summary>
    public const int TotalRows = HotbarRow + 1;

    private const string VanillaRowsKey = "invrows";
    private const int DefaultVanillaRows = 4;
    private const int MaxVanillaRows = 9;

    private static Player cachedPlayer;
    private static int cachedRows;

    /// <summary>
    /// Rows the game itself gives the player, stored in its unique key "invrows".
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The vanilla row count.</returns>
    public static int VanillaRows(Player player)
    {
        return player.TryGetUniqueKeyValue(VanillaRowsKey, out string value) && int.TryParse(value, out int rows)
            ? rows
            : DefaultVanillaRows;
    }

    /// <summary>
    /// Rows shown in the player's inventory grid.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The visible row count.</returns>
    public static int VisibleRows(Player player)
    {
        if (player != cachedPlayer)
        {
            cachedPlayer = player;
            cachedRows = Mathf.Clamp(VanillaRows(player) + BackpackSettings.ExtraRows.Value, 1, MaxVisibleRows);
        }

        return cachedRows;
    }

    /// <summary>
    /// True for the local player's inventory once it has its hidden rows.
    /// </summary>
    /// <param name="inventory">The inventory.</param>
    /// <returns>True if the backpack rules apply.</returns>
    public static bool IsLocalInventory(Inventory inventory)
    {
        Player player = Player.m_localPlayer;
        return player != null && inventory == player.m_inventory && inventory.GetHeight() == TotalRows;
    }

    /// <summary>
    /// What a position is used for.
    /// </summary>
    /// <param name="pos">The grid position.</param>
    /// <param name="visibleRows">Rows shown in the grid.</param>
    /// <returns>The kind of slot.</returns>
    public static SlotKind KindAt(Vector2i pos, int visibleRows)
    {
        if (pos.x < 0 || pos.x >= Width || pos.y < 0)
        {
            return SlotKind.Void;
        }

        if (pos.y < visibleRows)
        {
            return SlotKind.Grid;
        }

        return pos.y == HotbarRow ? SlotKind.Hotbar : SlotKind.Void;
    }

    /// <summary>
    /// Whether an item may lie at a position of the local player's inventory.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="item">The item.</param>
    /// <param name="pos">The grid position.</param>
    /// <returns>True if the item may lie there.</returns>
    public static bool CanHold(Player player, ItemDrop.ItemData item, Vector2i pos)
    {
        return KindAt(pos, VisibleRows(player)) switch
        {
            SlotKind.Grid or SlotKind.Hotbar => true,
            _ => false
        };
    }

    /// <summary>
    /// Finds an empty position in the visible grid.
    /// </summary>
    /// <param name="inventory">The player's inventory.</param>
    /// <param name="visibleRows">Rows shown in the grid.</param>
    /// <param name="topFirst">Search from the top row down, else from the bottom row up.</param>
    /// <returns>The position, or (-1, -1) if the grid is full.</returns>
    public static Vector2i FindGridSlot(Inventory inventory, int visibleRows, bool topFirst)
    {
        for (int i = 0; i < visibleRows; i++)
        {
            int y = topFirst ? i : visibleRows - 1 - i;
            for (int x = 0; x < Width; x++)
            {
                if (inventory.GetItemAt(x, y) == null)
                {
                    return new Vector2i(x, y);
                }
            }
        }

        return new Vector2i(-1, -1);
    }

    /// <summary>
    /// Counts the empty positions in the visible grid.
    /// </summary>
    /// <param name="inventory">The player's inventory.</param>
    /// <param name="visibleRows">Rows shown in the grid.</param>
    /// <returns>The number of empty grid positions.</returns>
    public static int EmptyGridSlots(Inventory inventory, int visibleRows)
    {
        int used = inventory.m_inventory.Count(item => item.m_gridPos.y < visibleRows && item.m_gridPos.y >= 0);
        return Mathf.Max(0, visibleRows * Width - used);
    }

    /// <summary>
    /// Stores the vanilla row count and resizes the inventory. Replaces <see cref="Player.SetInventorySize"/>.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="vanillaRows">Rows the game gives the player.</param>
    public static void SetVanillaRows(Player player, int vanillaRows)
    {
        player.AddUniqueKeyValue(VanillaRowsKey, Mathf.Clamp(vanillaRows, 0, MaxVanillaRows).ToString());
        Apply(player);
    }

    /// <summary>
    /// Gives the player's inventory its hidden rows and sizes the inventory window. Items that may not lie where they are
    /// move to the grid, or are dropped when it is full.
    /// </summary>
    /// <param name="player">The player.</param>
    public static void Apply(Player player)
    {
        cachedPlayer = null;
        int rows = VisibleRows(player);
        player.m_inventory.SetHeight(TotalRows);
        if (player == Player.m_localPlayer && InventoryGui.instance != null)
        {
            BackpackGui.ResizeWindow(InventoryGui.instance, rows);
        }

        MoveMisplacedItems(player, rows);
    }

    /// <summary>
    /// Applies changed or server-synced settings to the local player.
    /// </summary>
    public static void Refresh()
    {
        if (Player.m_localPlayer != null)
        {
            Apply(Player.m_localPlayer);
        }
    }

    private static void MoveMisplacedItems(Player player, int rows)
    {
        Inventory inventory = player.m_inventory;
        bool moved = false;
        foreach (ItemDrop.ItemData item in new List<ItemDrop.ItemData>(inventory.GetAllItems()))
        {
            if (CanHold(player, item, item.m_gridPos))
            {
                continue;
            }

            Vector2i free = FindGridSlot(inventory, rows, true);
            if (free.x >= 0)
            {
                item.m_gridPos = free;
                moved = true;
            }
            else
            {
                player.DropItem(inventory, item, item.m_stack);
            }
        }

        if (moved)
        {
            inventory.Changed();
        }
    }
}
