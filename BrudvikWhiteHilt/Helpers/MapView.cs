namespace BrudvikWhiteHilt.Helpers;

/// <summary>
/// Opening the large map from something read in the inventory.
/// </summary>
public static class MapView
{
    /// <summary>
    /// Closes the inventory before the large map opens. The game takes no map input while the inventory shows, so with
    /// both open neither could be closed.
    /// </summary>
    public static void CloseInventory()
    {
        if (InventoryGui.instance != null && InventoryGui.IsVisible())
        {
            InventoryGui.instance.Hide();
        }
    }
}
