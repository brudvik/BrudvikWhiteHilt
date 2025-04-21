using BrudvikWhiteHilt.Items.Indestructible;
using Jotunn.Configs;
using Jotunn.Managers;
using System;

namespace BrudvikWhiteHilt.Items.Armors;

/// <summary>
/// This class defines the base for all White Hilt armor items.
/// </summary>
public abstract class WhiteHiltArmorBase : IWhiteHiltCustomItem
{
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

    private readonly ItemManager instance;

    /// <summary>
    /// Constructor for the WhiteHiltArmorBase class.
    /// </summary>
    /// <param name="instance"></param>
    protected WhiteHiltArmorBase(ItemManager instance)
    {
        this.instance = instance;
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
                Name = FullName,
                Description = Description,
                CraftingStation = CraftingStations.Forge,
                MinStationLevel = 3,
                Requirements = Requirements
            };

            IndestructibleItem item = new(BaseName, CopyFrom, weaponConfig);
            item.ItemData.m_armorPerLevel += 10;
            item.ItemData.m_movementModifier += 0.05f;
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
