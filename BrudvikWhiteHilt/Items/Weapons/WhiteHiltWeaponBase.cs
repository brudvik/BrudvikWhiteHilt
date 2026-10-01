using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Indestructible;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Weapons;

/// <summary>
/// Base class for White Hilt weapons.
/// </summary>
public abstract class WhiteHiltWeaponBase : IWhiteHiltCustomItem, IWhiteHiltConfigurable
{
    private const string Section = "Gear.Weapons";

    private static ConfigEntry<float> damageMultiplierBonus;
    private static ConfigEntry<float> bonusDamage;
    private static ConfigEntry<float> bonusDamagePerLevel;

    private IndestructibleItem added;
    private float baseDamageMultiplier;
    private HitData.DamageTypes baseDamages;
    private HitData.DamageTypes baseDamagesPerLevel;

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
        BindConfig();
        IndestructibleItem.BindConfig();
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

            baseDamageMultiplier = item.ItemData.m_attack.m_damageMultiplier;
            baseDamages = item.ItemData.m_damages.Clone();
            baseDamagesPerLevel = item.ItemData.m_damagesPerLevel.Clone();
            added = item;
            ApplyConfig();

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
    /// Applies the configured damage bonuses on top of the cloned vanilla values.
    /// </summary>
    public void ApplyConfig()
    {
        if (added == null)
        {
            return;
        }

        ItemDrop.ItemData.SharedData shared = added.ItemData;
        shared.m_attack.m_damageMultiplier = baseDamageMultiplier + damageMultiplierBonus.Value;

        HitData.DamageTypes damages = baseDamages.Clone();
        damages.m_damage += bonusDamage.Value;
        damages.m_fire += bonusDamage.Value;
        damages.m_pierce += bonusDamage.Value;
        shared.m_damages = damages;

        HitData.DamageTypes perLevel = baseDamagesPerLevel.Clone();
        perLevel.m_damage += bonusDamagePerLevel.Value;
        perLevel.m_fire += bonusDamagePerLevel.Value;
        perLevel.m_pierce += bonusDamagePerLevel.Value;
        perLevel.m_slash += bonusDamagePerLevel.Value;
        shared.m_damagesPerLevel = perLevel;

        added.ApplyConfig();
    }

    /// <summary>
    /// Called after the model from <see cref="ModelName"/> is in place, to add behaviour to it.
    /// </summary>
    /// <param name="model">The object that shows the model, under the item's attach child.</param>
    protected virtual void OnModelApplied(GameObject model)
    {
    }

    private static void BindConfig()
    {
        if (damageMultiplierBonus != null)
        {
            return;
        }

        damageMultiplierBonus = WhiteHiltConfig.BindAdminOnly(Section, "DamageMultiplierBonus", 0.5f,
            "Added to the primary attack's damage multiplier of every White Hilt weapon and shield.", new AcceptableValueRange<float>(0f, 5f));
        bonusDamage = WhiteHiltConfig.BindAdminOnly(Section, "BonusDamage", 10f,
            "Added to the plain, fire and pierce damage.", new AcceptableValueRange<float>(0f, 500f));
        bonusDamagePerLevel = WhiteHiltConfig.BindAdminOnly(Section, "BonusDamagePerLevel", 10f,
            "Added per quality level to the plain, fire, pierce and slash damage.", new AcceptableValueRange<float>(0f, 500f));
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
                // Keep one entry per vanilla variant: GetIcon indexes m_icons by the saved m_variant.
                item.ItemData.m_icons = Enumerable.Repeat(icon, Math.Max(1, item.ItemData.m_icons?.Length ?? 0)).ToArray();
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: keeping the vanilla look: {ex.Message}");
        }
    }
}
