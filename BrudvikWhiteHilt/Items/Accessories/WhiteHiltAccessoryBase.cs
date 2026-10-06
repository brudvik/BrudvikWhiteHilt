using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Indestructible;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System;

namespace BrudvikWhiteHilt.Items.Accessories;

/// <summary>
/// Base class for the White Hilt accessories worn in the utility slot, such as the belt pouch and the dowsers. Each is
/// a clone of a vanilla utility item (<see cref="CopyFrom"/>), made indestructible; a subclass sets what it does in
/// <see cref="ConfigureStats"/>, usually an equip effect. The plugin finds every subclass by reflection and calls
/// <see cref="Add"/>.
/// </summary>
public abstract class WhiteHiltAccessoryBase : IWhiteHiltCustomItem, IWhiteHiltConfigurable
{
    private IndestructibleItem added;

    /// <summary>
    /// Prefab name of the accessory. It is also the key of its name and description in the translation files
    /// (<c>$item_&lt;name&gt;</c>), and saved worlds and inventories refer to the item by it, so it must never change
    /// once released.
    /// </summary>
    protected abstract string BaseName { get; }

    /// <summary>
    /// Name shown to players, in English. Other languages come from the embedded translation files
    /// (Translations/*.json), keyed by the prefab name.
    /// </summary>
    protected abstract string FullName { get; }

    /// <summary>
    /// Item description, in English; translated the same way as the name.
    /// </summary>
    protected abstract string Description { get; }

    /// <summary>
    /// Vanilla utility item the accessory is cloned from, e.g. Megingjord: it brings the slot and the model the look is
    /// built on.
    /// </summary>
    protected abstract string CopyFrom { get; }

    /// <summary>
    /// Ingredients of the recipe. The progression tier may add its own cost on top (see ProgressionManager).
    /// </summary>
    protected abstract RequirementConfig[] Requirements { get; }

    /// <summary>
    /// Whether the accessory is added to the game at all. A disabled one is skipped when the plugin starts, so it never
    /// reaches ObjectDB or a recipe.
    /// </summary>
    public abstract bool Enabled { get; }

    /// <inheritdoc/>
    public virtual ProgressionTier DefaultTier => ProgressionTier.Mountain;

    /// <inheritdoc/>
    public string Id => BaseName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(Translations.ItemKey(BaseName));

    /// <inheritdoc/>
    public string GatedPrefabName => BaseName;

    private readonly ItemManager instance;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    protected WhiteHiltAccessoryBase(ItemManager instance)
    {
        this.instance = instance;
        IndestructibleItem.BindConfig();
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(BaseName), FullName, Description);
    }

    /// <summary>
    /// Configures the accessory stats. Override in derived classes to customize.
    /// </summary>
    /// <param name="item">The item to configure.</param>
    protected virtual void ConfigureStats(IndestructibleItem item) { }

    /// <summary>
    /// Adds the accessory item to the game.
    /// </summary>
    public void Add()
    {
        try
        {
            ItemConfig config = new()
            {
                Name = Translations.Token(Translations.ItemKey(BaseName)),
                Description = Translations.Token($"{Translations.ItemKey(BaseName)}_description"),
                CraftingStation = CraftingStations.Forge,
                MinStationLevel = 2,
                Requirements = Requirements
            };

            IndestructibleItem item = new(BaseName, CopyFrom, config);
            ConfigureStats(item);
            added = item;
            instance.AddItem(item);

            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    /// <summary>
    /// Applies the shared indestructible item config.
    /// </summary>
    public void ApplyConfig()
    {
        added?.ApplyConfig();
    }
}
