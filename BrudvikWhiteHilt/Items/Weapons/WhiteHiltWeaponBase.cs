using BrudvikWhiteHilt.Items.Indestructible;
using Jotunn.Configs;
using Jotunn.Managers;
using System;

namespace BrudvikWhiteHilt.Items.Weapons;

/// <summary>
/// Base class for White Hilt weapons.
/// </summary>
public abstract class WhiteHiltWeaponBase : IWhiteHiltCustomItem
{
    /// <summary>
    /// The base name of the weapon.
    /// </summary>
    protected abstract string BaseName { get; }

    /// <summary>
    /// The full name of the weapon.
    /// </summary>
    protected abstract string FullName { get; }

    /// <summary>
    /// The description of the weapon.
    /// </summary>
    protected abstract string Description { get; }

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected abstract string CopyFrom { get; }

    /// <summary>
    /// The requirements for crafting the weapon.
    /// </summary>
    protected virtual RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "FineWood", Amount = 10, Recover = false }
    };

    private readonly ItemManager instance;

    /// <summary>
    /// Constructor for the WhiteHiltWeaponBase class.
    /// </summary>
    /// <param name="instance"></param>
    protected WhiteHiltWeaponBase(ItemManager instance)
    {
        this.instance = instance;
    }

    /// <summary>
    /// Adds the weapon to the game.
    /// </summary>
    public void Add()
    {
        try
        {
            ItemConfig itemConfig = new()
            {
                Name = FullName,
                Description = Description,
                CraftingStation = CraftingStations.Forge,
                MinStationLevel = 2,
                Requirements = Requirements
            };

            IndestructibleItem item = new(BaseName, CopyFrom, itemConfig);

            // Set the item damage and other properties.
            item.ItemData.m_attack.m_damageMultiplier += 0.5f;
            item.ItemData.m_damagesPerLevel.m_damage += 10;
            item.ItemData.m_damagesPerLevel.m_fire += 10;
            item.ItemData.m_damagesPerLevel.m_pierce += 10;
            item.ItemData.m_damagesPerLevel.m_slash += 10;
            item.ItemData.m_damages.m_damage += 10;
            item.ItemData.m_damages.m_fire += 10;
            item.ItemData.m_damages.m_pierce += 10;

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
