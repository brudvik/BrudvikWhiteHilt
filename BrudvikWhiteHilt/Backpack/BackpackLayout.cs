using UnityEngine;

namespace BrudvikWhiteHilt.Backpack;

/// <summary>
/// The rows of the local player's inventory: the vanilla rows (4, more when bought from a trader) plus the extra rows.
/// </summary>
public static class BackpackLayout
{
    /// <summary>Columns of the player inventory.</summary>
    public const int Width = 8;

    /// <summary>Most rows shown in the inventory grid.</summary>
    public const int MaxVisibleRows = 12;

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
    /// Sizes the player's inventory and the inventory window for the current rows. Items outside the rows are dropped.
    /// </summary>
    /// <param name="player">The player.</param>
    public static void Apply(Player player)
    {
        cachedPlayer = null;
        int rows = VisibleRows(player);
        player.m_inventory.SetHeight(rows);
        if (player == Player.m_localPlayer && InventoryGui.instance != null)
        {
            InventoryGui.instance.SetInventorySize(rows);
        }

        player.DropInvalidItems();
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
}
