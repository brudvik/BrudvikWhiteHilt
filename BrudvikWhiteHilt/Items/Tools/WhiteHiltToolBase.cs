using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Indestructible;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System;

namespace BrudvikWhiteHilt.Items.Tools;

/// <summary>
/// Base class for the White Hilt tools: hammer, hoe, cultivator, axe and pickaxe. Each is a clone of the vanilla tool
/// (<see cref="CopyFrom"/>), which brings what the tool does (the hammer's build table, the hoe's terrain tools); this
/// class makes it indestructible and applies the config's stamina reduction for building, farming and cultivating. The
/// plugin finds every subclass by reflection and calls <see cref="Add"/>.
/// </summary>
public abstract class WhiteHiltToolBase : IWhiteHiltCustomItem, IWhiteHiltConfigurable
{
    private const string Section = "Gear.Tools";

    private static ConfigEntry<float> staminaReduction;

    private IndestructibleItem added;
    private float baseStaminaModifier;

    /// <summary>
    /// Prefab name of the tool. It is also the key of its name and description in the translation files
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
    /// Vanilla tool it is cloned from; it brings what the tool does, such as the hammer's piece table.
    /// </summary>
    protected abstract string CopyFrom { get; }

    /// <summary>
    /// Ingredients of the recipe. The progression tier may add its own cost on top (see ProgressionManager).
    /// </summary>
    protected virtual RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 10, Recover = false },
        new() { Item = "Stone", Amount = 5, Recover = false },
        new() { Item = "Resin", Amount = 5, Recover = false }
    };

    /// <summary>
    /// Whether the tool is added to the game at all. A disabled one is skipped when the plugin starts, so it never
    /// reaches ObjectDB or a recipe.
    /// </summary>
    public abstract bool Enabled { get; }

    /// <inheritdoc/>
    public virtual ProgressionTier DefaultTier => ProgressionTier.Start;

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
    protected WhiteHiltToolBase(ItemManager instance)
    {
        this.instance = instance;
        BindConfig();
        IndestructibleItem.BindConfig();
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(BaseName), FullName, Description);
    }

    /// <summary>
    /// Adds the tool to the game.
    /// </summary>
    public void Add()
    {
        try
        {
            ItemConfig itemConfig = new()
            {
                Name = Translations.Token(Translations.ItemKey(BaseName)),
                Description = Translations.Token($"{Translations.ItemKey(BaseName)}_description"),
                CraftingStation = CraftingStations.Workbench,
                Requirements = Requirements
            };

            IndestructibleItem item = new(BaseName, CopyFrom, itemConfig);
            baseStaminaModifier = item.ItemData.m_homeItemsStaminaModifier;
            added = item;
            ApplyConfig();

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
    /// Applies the configured stamina reduction on top of the cloned vanilla value.
    /// </summary>
    public void ApplyConfig()
    {
        if (added == null)
        {
            return;
        }

        added.ItemData.m_homeItemsStaminaModifier = baseStaminaModifier - staminaReduction.Value;
        added.ApplyConfig();
    }

    private static void BindConfig()
    {
        if (staminaReduction != null)
        {
            return;
        }

        staminaReduction = WhiteHiltConfig.BindAdminOnly(Section, "HomeItemsStaminaReduction", 1f,
            "Taken off the stamina modifier for building, farming and cultivating with a White Hilt tool (1 = 100% less).",
            new AcceptableValueRange<float>(0f, 1f));
    }
}
