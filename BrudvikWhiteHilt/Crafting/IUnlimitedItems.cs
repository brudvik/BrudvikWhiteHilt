namespace BrudvikWhiteHilt.Crafting;

/// <summary>
/// Tells the crafting panel which items the restocking chests keep without limit. Set by the chest module.
/// </summary>
public interface IUnlimitedItems
{
    /// <summary>
    /// Checks whether a container keeps a stack full without limit.
    /// </summary>
    /// <param name="container">The container.</param>
    /// <param name="item">A stack in the container.</param>
    /// <returns>True if the container refills the stack.</returns>
    bool IsUnlimitedIn(Container container, ItemDrop.ItemData item);

    /// <summary>
    /// Gets the name of the chest that keeps an item unlimited.
    /// </summary>
    /// <param name="prefabName">The item prefab name.</param>
    /// <param name="shared">The item's shared data.</param>
    /// <returns>The translated chest name, or null if no chest keeps the item unlimited.</returns>
    string GetUnlimitedChest(string prefabName, ItemDrop.ItemData.SharedData shared);

    /// <summary>
    /// Gets how close the best chest is to making an item unlimited.
    /// </summary>
    /// <param name="prefabName">The item prefab name.</param>
    /// <param name="shared">The item's shared data.</param>
    /// <param name="stored">The amount in the chest closest to unlocking the item.</param>
    /// <param name="required">The amount that unlocks the item.</param>
    /// <returns>True if the item can still become unlimited.</returns>
    bool TryGetUnlockProgress(string prefabName, ItemDrop.ItemData.SharedData shared, out int stored, out int required);
}
