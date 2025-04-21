using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;

namespace BrudvikWhiteHilt.Items.Potions;

/// <summary>
/// This class defines the base for all potions.
/// </summary>
public abstract class PotionBase : IWhiteHiltCustomItem
{
    /// <summary>
    /// The base name of the potion.
    /// </summary>
    protected abstract string BaseName { get; }

    /// <summary>
    /// The full name of the potion.
    /// </summary>
    protected abstract string FullName { get; }

    /// <summary>
    /// The description of the potion.
    /// </summary>
    protected abstract string Description { get; }

    /// <summary>
    /// The path to the icon of the potion.
    /// </summary>
    protected abstract string IconPath { get; }

    /// <summary>
    /// The requirements for crafting the potion.
    /// </summary>
    protected abstract RequirementConfig[] MeadBaseRequirements { get; }

    private readonly ItemManager instance;

    /// <summary>
    /// Constructor for the PotionBase class.
    /// </summary>
    /// <param name="instance"></param>
    protected PotionBase(ItemManager instance)
    {
        this.instance = instance;
    }

    /// <summary>
    /// Adds the potion to the game.
    /// </summary>
    public void Add()
    {
        try
        {
            // Create Mead Base
            ItemConfig meadBaseConfig = new()
            {
                Name = $"Mead Base: {FullName}",
                Description = "Needs to be fermented",
                CraftingStation = CraftingStations.Cauldron,
                Requirements = MeadBaseRequirements
            };

            CustomItem meadBase = new($"{BaseName}MeadBase", "MeadBaseHealthMinor", meadBaseConfig);
            instance.AddItem(meadBase);

            // Create Mead
            ItemConfig meadConfig = new()
            {
                Name = FullName,
                Description = Description
            };

            CustomItem mead = new($"{BaseName}Mead", "MeadHealthMinor", meadConfig);
            mead.ItemDrop.m_itemData.m_shared.m_description = Description;
            mead.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtilsExtended.LoadTextureFromEmbeddedResource(IconPath).ConvertToSprite();

            var effect = CreateEffect();
            var customEffect = new CustomStatusEffect(effect, fixReference: false);
            instance.AddStatusEffect(customEffect);
            mead.ItemDrop.m_itemData.m_shared.m_consumeStatusEffect = customEffect.StatusEffect;

            instance.AddItem(mead);

            // Add Fermenter Conversion
            var fermentConfig = new FermenterConversionConfig
            {
                FromItem = $"{BaseName}MeadBase",
                ToItem = $"{BaseName}Mead",
                Station = Fermenters.Fermenter,
                ProducedItems = 1
            };
            instance.AddItemConversion(new CustomItemConversion(fermentConfig));

            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    /// <summary>
    /// Creates the effect for the potion.
    /// </summary>
    /// <returns></returns>
    protected abstract StatusEffect CreateEffect();
}
