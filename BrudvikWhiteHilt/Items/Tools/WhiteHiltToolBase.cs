using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Indestructible;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System;

namespace BrudvikWhiteHilt.Items.Tools;

/// <summary>
/// Base class for White Hilt tools.
/// </summary>
public abstract class WhiteHiltToolBase : IWhiteHiltCustomItem, IWhiteHiltConfigurable
{
    private const string Section = "Gear.Tools";

    private static ConfigEntry<float> staminaReduction;

    private IndestructibleItem added;
    private float baseStaminaModifier;

    /// <summary>
    /// The base name of the tool.
    /// </summary>
    protected abstract string BaseName { get; }

    /// <summary>
    /// The full name of the tool.
    /// </summary>
    protected abstract string FullName { get; }

    /// <summary>
    /// The description of the tool.
    /// </summary>
    protected abstract string Description { get; }

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected abstract string CopyFrom { get; }

    /// <summary>
    /// The requirements for crafting the tool.
    /// </summary>
    protected virtual RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 10, Recover = false },
        new() { Item = "Stone", Amount = 5, Recover = false },
        new() { Item = "Resin", Amount = 5, Recover = false }
    };

    /// <summary>
    /// Indicates whether the tool is enabled or not.
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
    /// Constructor for the WhiteHiltToolBase class.
    /// </summary>
    /// <param name="instance"></param>
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
