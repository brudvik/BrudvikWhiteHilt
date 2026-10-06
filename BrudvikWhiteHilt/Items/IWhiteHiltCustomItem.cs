using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Items;

/// <summary>
/// An item the mod adds. The plugin finds every non-abstract class implementing this by reflection when it starts
/// (BrudvikWhiteHilt.DiscoverCustomEntries), creates it with Jotunn's <c>ItemManager</c>, registers it for progression
/// and calls <see cref="Add"/> if it is enabled: adding an item to the mod means adding a class, nothing else.
/// </summary>
public interface IWhiteHiltCustomItem : IWhiteHiltProgressionEntry
{
    /// <summary>
    /// Whether the item is added to the game at all.
    /// </summary>
    bool Enabled { get; }

    /// <summary>
    /// Clones the item's vanilla prefab, changes it and hands it to Jotunn, which adds it to ObjectDB and its recipe
    /// to the crafting station. Called once, when Jotunn's vanilla prefabs are available.
    /// </summary>
    void Add();
}
