using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Indestructible;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System;

namespace BrudvikWhiteHilt.Items.Armors;

/// <summary>
/// This class defines the base for all White Hilt armor items.
/// </summary>
public abstract class WhiteHiltArmorBase : IWhiteHiltCustomItem, IWhiteHiltConfigurable
{
    private const string Section = "Gear.Armor";

    private static ConfigEntry<float> armorPerLevelBonus;
    private static ConfigEntry<float> movementBonus;

    private IndestructibleItem added;
    private float baseArmorPerLevel;
    private float baseMovementModifier;

    /// <summary>
    /// The base name of the armor item.
    /// </summary>
    protected abstract string BaseName { get; }

    /// <summary>
    /// The full name of the armor item.
    /// </summary>
    protected abstract string FullName { get; }

    /// <summary>
    /// The description of the armor item.
    /// </summary>
    protected abstract string Description { get; }

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected abstract string CopyFrom { get; }

    /// <summary>
    /// The requirements for crafting the armor item.
    /// </summary>
    protected virtual RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Feathers", Amount = 30, Recover = false }
    };

    /// <summary>
    /// Indicates whether the armor is enabled or not.
    /// </summary>
    public abstract bool Enabled { get; }

    /// <inheritdoc/>
    public virtual ProgressionTier DefaultTier => ProgressionTier.Swamp;

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
    /// Constructor for the WhiteHiltArmorBase class.
    /// </summary>
    /// <param name="instance"></param>
    protected WhiteHiltArmorBase(ItemManager instance)
    {
        this.instance = instance;
        BindConfig();
        IndestructibleItem.BindConfig();
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(BaseName), FullName, Description);
    }

    /// <summary>
    /// Adds the armor item to the game.
    /// </summary>
    public void Add()
    {
        try
        {
            ItemConfig weaponConfig = new()
            {
                Name = Translations.Token(Translations.ItemKey(BaseName)),
                Description = Translations.Token($"{Translations.ItemKey(BaseName)}_description"),
                CraftingStation = CraftingStations.Forge,
                MinStationLevel = 3,
                Requirements = Requirements
            };

            IndestructibleItem item = new(BaseName, CopyFrom, weaponConfig);
            baseArmorPerLevel = item.ItemData.m_armorPerLevel;
            baseMovementModifier = item.ItemData.m_movementModifier;
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
    /// Applies the configured armor and movement bonuses on top of the cloned vanilla values.
    /// </summary>
    public void ApplyConfig()
    {
        if (added == null)
        {
            return;
        }

        added.ItemData.m_armorPerLevel = baseArmorPerLevel + armorPerLevelBonus.Value;
        added.ItemData.m_movementModifier = baseMovementModifier + movementBonus.Value;
        added.ApplyConfig();
    }

    private static void BindConfig()
    {
        if (armorPerLevelBonus != null)
        {
            return;
        }

        armorPerLevelBonus = WhiteHiltConfig.BindAdminOnly(Section, "ArmorPerLevelBonus", 10f,
            "Added to the armor gained per quality level of every White Hilt armor piece.", new AcceptableValueRange<float>(0f, 200f));
        movementBonus = WhiteHiltConfig.BindAdminOnly(Section, "MovementBonus", 0.05f,
            "Added to the movement speed modifier of every White Hilt armor piece (0.05 = 5% faster).", new AcceptableValueRange<float>(-0.5f, 0.5f));
    }
}
