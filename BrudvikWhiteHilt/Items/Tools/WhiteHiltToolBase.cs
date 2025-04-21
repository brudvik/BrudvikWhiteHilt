using BrudvikWhiteHilt.Items.Indestructible;
using Jotunn.Configs;
using Jotunn.Managers;
using System;

namespace BrudvikWhiteHilt.Items.Tools;

/// <summary>
/// Base class for White Hilt tools.
/// </summary>
public abstract class WhiteHiltToolBase : IWhiteHiltCustomItem
{
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

    private readonly ItemManager instance;

    /// <summary>
    /// Constructor for the WhiteHiltToolBase class.
    /// </summary>
    /// <param name="instance"></param>
    protected WhiteHiltToolBase(ItemManager instance)
    {
        this.instance = instance;
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
                Name = FullName,
                Description = Description,
                CraftingStation = CraftingStations.Workbench,
                Requirements = Requirements
            };

            IndestructibleItem item = new(BaseName, CopyFrom, itemConfig);

            // Set the item to use light stamina.
            item.ItemData.m_homeItemsStaminaModifier -= 1.0f;

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
