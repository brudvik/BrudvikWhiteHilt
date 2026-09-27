using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;

namespace BrudvikWhiteHilt.Items.Ammunition;

/// <summary>
/// This class defines the base for all White Hilt ammunition items.
/// </summary>
public abstract class WhiteHiltAmmunitionBase : IWhiteHiltCustomItem
{
    /// <summary>
    /// The base name of the ammunition item.
    /// </summary>
    protected abstract string BaseName { get; }

    /// <summary>
    /// The full name of the ammunition item.
    /// </summary>
    protected abstract string FullName { get; }

    /// <summary>
    /// The description of the ammunition item.
    /// </summary>
    protected abstract string Description { get; }

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected abstract string CopyFrom { get; }

    /// <summary>
    /// The amount of ammunition crafted per recipe.
    /// </summary>
    protected virtual int CraftAmount => 200;

    /// <summary>
    /// The requirements for crafting the ammunition item.
    /// </summary>
    protected abstract RequirementConfig[] Requirements { get; }

    /// <summary>
    /// Indicates whether the ammunition is enabled or not.
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
    /// Constructor for the WhiteHiltAmmunitionBase class.
    /// </summary>
    /// <param name="instance"></param>
    protected WhiteHiltAmmunitionBase(ItemManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(BaseName), FullName, Description);
    }

    /// <summary>
    /// Adds the ammunition item to the game.
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
                Amount = CraftAmount,
                Requirements = Requirements
            };

            CustomItem item = new(BaseName, CopyFrom, config);
            
            // Configure enhanced damage
            var itemData = item.ItemDrop.m_itemData.m_shared;
            itemData.m_damages.m_pierce *= 2f;
            itemData.m_damages.m_fire += 30f;
            itemData.m_damages.m_spirit += 20f;
            
            instance.AddItem(item);

            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }
}
