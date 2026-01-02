using BrudvikWhiteHilt.Items.Indestructible;
using Jotunn.Configs;
using Jotunn.Managers;
using System;

namespace BrudvikWhiteHilt.Items.Accessories;

/// <summary>
/// This class defines the base for all White Hilt accessory items.
/// </summary>
public abstract class WhiteHiltAccessoryBase : IWhiteHiltCustomItem
{
    /// <summary>
    /// The base name of the accessory item.
    /// </summary>
    protected abstract string BaseName { get; }

    /// <summary>
    /// The full name of the accessory item.
    /// </summary>
    protected abstract string FullName { get; }

    /// <summary>
    /// The description of the accessory item.
    /// </summary>
    protected abstract string Description { get; }

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected abstract string CopyFrom { get; }

    /// <summary>
    /// The requirements for crafting the accessory item.
    /// </summary>
    protected abstract RequirementConfig[] Requirements { get; }

    /// <summary>
    /// Indicates whether the accessory is enabled or not.
    /// </summary>
    public abstract bool Enabled { get; }

    private readonly ItemManager instance;

    /// <summary>
    /// Constructor for the WhiteHiltAccessoryBase class.
    /// </summary>
    /// <param name="instance"></param>
    protected WhiteHiltAccessoryBase(ItemManager instance)
    {
        this.instance = instance;
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
                Name = FullName,
                Description = Description,
                CraftingStation = CraftingStations.Forge,
                MinStationLevel = 2,
                Requirements = Requirements
            };

            IndestructibleItem item = new(BaseName, CopyFrom, config);
            ConfigureStats(item);
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
