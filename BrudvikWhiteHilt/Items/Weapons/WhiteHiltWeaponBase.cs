using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Indestructible;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System;
using UnityEngine;

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

    /// <summary>
    /// Indicates whether the weapon is enabled or not.
    /// </summary>
    public abstract bool Enabled { get; }

    /// <summary>
    /// Name of the mesh in the White Hilt asset bundle that replaces the vanilla look (texture <c>&lt;name&gt;_albedo</c>),
    /// or null to keep the vanilla look.
    /// </summary>
    protected virtual string ModelName => null;

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
    /// Constructor for the WhiteHiltWeaponBase class.
    /// </summary>
    /// <param name="instance"></param>
    protected WhiteHiltWeaponBase(ItemManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(BaseName), FullName, Description);
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
                Name = Translations.Token(Translations.ItemKey(BaseName)),
                Description = Translations.Token($"{Translations.ItemKey(BaseName)}_description"),
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

            TryApplyModel(item);

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
    /// Called after the model from <see cref="ModelName"/> is in place, to add behaviour to it.
    /// </summary>
    /// <param name="model">The object that shows the model, under the item's attach child.</param>
    protected virtual void OnModelApplied(GameObject model)
    {
    }

    private void TryApplyModel(IndestructibleItem item)
    {
        if (ModelName == null || VisualHelper.IsHeadless)
        {
            return;
        }

        // A broken look must not remove the item, or players would lose it from their inventories.
        try
        {
            GameObject model = VisualHelper.ReplaceWeaponMesh(item.ItemPrefab, ForagingAssets.LoadMesh(ModelName), ForagingAssets.LoadTexture($"{ModelName}_albedo"));
            OnModelApplied(model);

            Sprite icon = VisualHelper.RenderIcon(item.ItemPrefab);
            if (icon != null)
            {
                item.ItemData.m_icons = new[] { icon };
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: keeping the vanilla look: {ex.Message}");
        }
    }
}
