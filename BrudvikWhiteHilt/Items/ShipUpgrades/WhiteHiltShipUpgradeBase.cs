using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.ShipUpgrades;

/// <summary>
/// Base class for the White Hilt Ship upgrades. Used on the ship's mast, an upgrade switches on a part of the ship.
/// </summary>
public abstract class WhiteHiltShipUpgradeBase : IWhiteHiltCustomItem
{
    /// <summary>
    /// Number of upgrades.
    /// </summary>
    public const int Count = 8;

    private static readonly WhiteHiltShipUpgradeBase[] all = new WhiteHiltShipUpgradeBase[Count];

    private readonly ItemManager instance;

    /// <summary>
    /// Bit of the upgrade in the ship's upgrade mask, from 0 to <see cref="Count"/> - 1.
    /// </summary>
    public abstract int Index { get; }

    /// <summary>
    /// Prefab name of the upgrade item.
    /// </summary>
    protected abstract string BaseName { get; }

    /// <summary>
    /// Name shown to players, in English. Other languages come from the embedded translation files.
    /// </summary>
    protected abstract string FullName { get; }

    /// <summary>
    /// Item description, in English.
    /// </summary>
    protected abstract string Description { get; }

    /// <summary>
    /// Vanilla item to clone the look from.
    /// </summary>
    protected abstract string CopyFrom { get; }

    /// <summary>
    /// Crafting requirements. Only materials up to and including the Swamp.
    /// </summary>
    protected abstract RequirementConfig[] Requirements { get; }

    /// <summary>
    /// Indicates whether the upgrade is enabled.
    /// </summary>
    public abstract bool Enabled { get; }

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Swamp;

    /// <inheritdoc/>
    public string Id => BaseName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(NameKey);

    /// <inheritdoc/>
    public string GatedPrefabName => BaseName;

    /// <summary>
    /// Prefab name of the upgrade item.
    /// </summary>
    public string PrefabName => BaseName;

    private string NameKey => Translations.ItemKey(BaseName);

    /// <summary>
    /// Constructor for the WhiteHiltShipUpgradeBase class. Registers the upgrade and its English text.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    protected WhiteHiltShipUpgradeBase(ItemManager instance)
    {
        this.instance = instance;
        all[Index] = this;
        Translations.AddEnglishNameAndDescription(NameKey, FullName, Description);
    }

    /// <summary>
    /// Returns the upgrade with the given bit.
    /// </summary>
    /// <param name="index">Bit, from 0 to <see cref="Count"/> - 1.</param>
    /// <returns>The upgrade, or null if it is disabled.</returns>
    public static WhiteHiltShipUpgradeBase Get(int index)
    {
        return index >= 0 && index < Count ? all[index] : null;
    }

    /// <summary>
    /// Returns the upgrade an inventory item is, if any.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The upgrade, or null if the item is not one.</returns>
    public static WhiteHiltShipUpgradeBase FromItem(ItemDrop.ItemData item)
    {
        return all.FirstOrDefault(upgrade => upgrade != null && item?.m_shared.m_name == upgrade.NameToken);
    }

    /// <summary>
    /// Adds the upgrade item and its workbench recipe to the game.
    /// </summary>
    public void Add()
    {
        try
        {
            CustomItem upgrade = new(BaseName, CopyFrom, new ItemConfig
            {
                Name = Translations.Token(NameKey),
                Description = Translations.Token($"{NameKey}_description"),
                CraftingStation = CraftingStations.Workbench,
                Requirements = Requirements
            });

            // A plain material, so a cloned lantern can not be equipped as a light.
            ItemDrop.ItemData.SharedData shared = upgrade.ItemDrop.m_itemData.m_shared;
            shared.m_itemType = ItemDrop.ItemData.ItemType.Material;
            shared.m_maxStackSize = 1;
            shared.m_weight = 5f;
            shared.m_value = 0;
            shared.m_teleportable = true;
            shared.m_equipStatusEffect = null;

            Sprite icon = VisualHelper.RenderIcon(upgrade.ItemPrefab);
            if (icon != null)
            {
                shared.m_icons = new[] { icon };
            }

            instance.AddItem(upgrade);
            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }
}
