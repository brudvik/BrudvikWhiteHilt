using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Indestructible;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Armors;

/// <summary>
/// Base class for the White Hilt armour, capes and shields worn as gear. Each piece is a clone of a vanilla piece
/// (<see cref="CopyFrom"/>), which decides its slot and how it sits on the body; this class makes it indestructible,
/// can give it another vanilla piece's armour, resistances and set bonus (<see cref="StatsFrom"/>), applies the
/// config's armour and movement bonuses, registers its upgrades through the biomes and lets the subclass change its
/// look (<see cref="ApplyVisual"/>). The plugin finds every subclass by reflection and calls <see cref="Add"/>.
/// </summary>
public abstract class WhiteHiltArmorBase : IWhiteHiltCustomItem, IWhiteHiltConfigurable
{
    private const string Section = "Gear.Armor";

    private static ConfigEntry<float> armorBonus;
    private static ConfigEntry<float> armorPerLevelBonus;
    private static ConfigEntry<float> movementBonus;

    private IndestructibleItem added;
    private float baseArmor;
    private float baseArmorPerLevel;
    private float baseMovementModifier;
    private GearKind? upgradeKind;

    /// <summary>
    /// Prefab name of the armor piece. It is also the key of its name and description in the translation files
    /// (<c>$item_&lt;name&gt;</c>), and saved worlds and inventories refer to the item by it, so it must never change
    /// once released.
    /// </summary>
    protected abstract string BaseName { get; }

    /// <summary>
    /// Name shown to players, in English. Other languages come from the embedded translation files
    /// (Translations/*.json), keyed by the prefab name.
    /// </summary>
    protected abstract string FullName { get; }

    /// <summary>
    /// Item description, in English; translated the same way as the name.
    /// </summary>
    protected abstract string Description { get; }

    /// <summary>
    /// Vanilla piece the armour is cloned from. It decides the slot (helmet, chest, legs, cape, shield) and the model
    /// the look is built on.
    /// </summary>
    protected abstract string CopyFrom { get; }

    /// <summary>
    /// Vanilla item whose armor, resistances and set bonus replace those of <see cref="CopyFrom"/>, or null to keep
    /// them.
    /// Lets a piece keep a later biome's look with stats from its own tier.
    /// </summary>
    protected virtual string StatsFrom => null;

    /// <summary>
    /// Whether the effect worn with <see cref="StatsFrom"/> (e.g. the feather cape's feather fall) is copied along.
    /// </summary>
    protected virtual bool CopyEquipEffect => true;

    /// <summary>
    /// The crafting station of the armor item.
    /// </summary>
    protected virtual string CraftingStation => CraftingStations.Forge;

    /// <summary>
    /// The station level needed to craft the armor item.
    /// </summary>
    protected virtual int MinStationLevel => 3;

    /// <summary>
    /// Ingredients of the recipe. The progression tier may add its own cost on top (see ProgressionManager).
    /// </summary>
    protected virtual RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Feathers", Amount = 30, Recover = false }
    };

    /// <summary>
    /// Whether the armor piece is added to the game at all. A disabled one is skipped when the plugin starts, so it
    /// never reaches ObjectDB or a recipe.
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
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
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
                CraftingStation = CraftingStation,
                MinStationLevel = MinStationLevel,
                Requirements = Requirements
            };

            IndestructibleItem item = new(BaseName, CopyFrom, weaponConfig);
            if (StatsFrom != null)
            {
                CopyStats(item.ItemData, StatsFrom);
                if (!CopyEquipEffect)
                {
                    item.ItemData.m_equipStatusEffect = null;
                }
            }

            baseArmor = item.ItemData.m_armor;
            baseArmorPerLevel = item.ItemData.m_armorPerLevel;
            baseMovementModifier = item.ItemData.m_movementModifier;
            added = item;
            ItemDrop.ItemData.ItemType type = item.ItemData.m_itemType;
            upgradeKind = type switch
            {
                ItemDrop.ItemData.ItemType.Helmet or ItemDrop.ItemData.ItemType.Chest or ItemDrop.ItemData.ItemType.Legs => GearKind.Armor,
                ItemDrop.ItemData.ItemType.Shoulder => GearKind.Cape,
                ItemDrop.ItemData.ItemType.Shield => GearKind.Shield,
                _ => null,
            };
            if (upgradeKind.HasValue)
            {
                GearUpgrades.Register(BaseName, NameToken, upgradeKind.Value);
            }

            if (upgradeKind == GearKind.Shield)
            {
                Binding.GearBinding.Register(NameToken, shield: true);
            }

            ApplyConfig();
            if (!VisualHelper.IsHeadless)
            {
                ApplyVisual(item);
            }

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

        added.ItemData.m_armor = baseArmor + armorBonus.Value;
        added.ItemData.m_armorPerLevel = baseArmorPerLevel + armorPerLevelBonus.Value;
        added.ItemData.m_movementModifier = baseMovementModifier < 0f
            ? Mathf.Min(0f, baseMovementModifier + movementBonus.Value)
            : baseMovementModifier;
        if (upgradeKind.HasValue)
        {
            added.ItemData.m_maxQuality = GearUpgrades.MaxQuality(upgradeKind.Value);
        }

        added.ApplyConfig();
    }

    /// <summary>
    /// Changes the look of the cloned item before it is added. Not called on a dedicated server.
    /// </summary>
    /// <param name="item">The cloned item.</param>
    protected virtual void ApplyVisual(IndestructibleItem item)
    {
    }

    private static void CopyStats(ItemDrop.ItemData.SharedData target, string sourceName)
    {
        ItemDrop source = PrefabManager.Cache.GetPrefab<ItemDrop>(sourceName);
        if (source == null)
        {
            Jotunn.Logger.LogWarning($"Stats source {sourceName} not found; keeping the cloned stats.");
            return;
        }

        ItemDrop.ItemData.SharedData stats = source.m_itemData.m_shared;
        target.m_armor = stats.m_armor;
        target.m_armorPerLevel = stats.m_armorPerLevel;
        target.m_maxQuality = stats.m_maxQuality;
        target.m_damageModifiers = new(stats.m_damageModifiers);
        target.m_setName = stats.m_setName;
        target.m_setSize = stats.m_setSize;
        target.m_setStatusEffect = stats.m_setStatusEffect;
        target.m_equipStatusEffect = stats.m_equipStatusEffect;
        target.m_movementModifier = stats.m_movementModifier;
        target.m_eitrRegenModifier = stats.m_eitrRegenModifier;
        target.m_heatResistanceModifier = stats.m_heatResistanceModifier;
        target.m_homeItemsStaminaModifier = stats.m_homeItemsStaminaModifier;
        target.m_jumpStaminaModifier = stats.m_jumpStaminaModifier;
        target.m_attackStaminaModifier = stats.m_attackStaminaModifier;
        target.m_blockStaminaModifier = stats.m_blockStaminaModifier;
        target.m_dodgeStaminaModifier = stats.m_dodgeStaminaModifier;
        target.m_swimStaminaModifier = stats.m_swimStaminaModifier;
        target.m_sneakStaminaModifier = stats.m_sneakStaminaModifier;
        target.m_runStaminaModifier = stats.m_runStaminaModifier;
    }

    private static void BindConfig()
    {
        if (armorPerLevelBonus != null)
        {
            return;
        }

        armorBonus = WhiteHiltConfig.BindAdminOnly(Section, "ArmorBonus", 4f,
            "Armor added to every White Hilt armor piece.", new AcceptableValueRange<float>(0f, 100f));
        armorPerLevelBonus = WhiteHiltConfig.BindAdminOnly(Section, "ArmorPerLevelBonus", 0f,
            "Added to the armor gained per quality level of every White Hilt armor piece.", new AcceptableValueRange<float>(0f, 200f));
        movementBonus = WhiteHiltConfig.BindAdminOnly(Section, "MovementBonus", 0.05f,
            "Taken off the movement penalty of every White Hilt armor piece that has one (0.05 = 5%). A piece never gets faster than no armor.", new AcceptableValueRange<float>(-0.5f, 0.5f));
        GearUpgrades.BindConfig();
    }
}
