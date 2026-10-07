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
/// Base class for the White Hilt weapons and shields. A weapon is a clone of a vanilla one (<see cref="CopyFrom"/>), so
/// it keeps that weapon's attacks, animations, sounds and effects; this class then makes it indestructible, can give it
/// the damage of another vanilla weapon (<see cref="StatsFrom"/>), the config's damage bonuses and its own model from
/// the White Hilt asset bundle (<see cref="ModelName"/>), and registers its upgrades through the biomes. A subclass
/// only states these facts. The plugin finds every subclass by reflection and calls <see cref="Add"/>, so adding a
/// weapon means adding a class.
/// </summary>
public abstract class WhiteHiltWeaponBase : IWhiteHiltCustomItem, IWhiteHiltConfigurable
{
    private const string Section = "Gear.Weapons";

    private static ConfigEntry<float> damageMultiplierBonus;
    private static ConfigEntry<float> bonusDamagePerLevel;

    private IndestructibleItem added;
    private float baseDamageMultiplier;
    private HitData.DamageTypes baseDamages;
    private HitData.DamageTypes baseDamagesPerLevel;
    private GearKind? upgradeKind;
    private GameObject thrownProjectile;

    /// <summary>
    /// Prefab name of the weapon. It is also the key of its name and description in the translation files
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
    /// Vanilla item the weapon is cloned from. It decides how the weapon behaves (attacks, animations, skill, sounds),
    /// not only how it looks, so pick the vanilla weapon that should be swung.
    /// </summary>
    protected abstract string CopyFrom { get; }

    /// <summary>
    /// Vanilla item whose damage, blocking and attack costs replace those of <see cref="CopyFrom"/>, or null to keep
    /// them.
    /// Lets a weapon keep a later biome's look and effects with stats from its own tier.
    /// </summary>
    protected virtual string StatsFrom => null;

    /// <summary>
    /// Fire damage added on top of the weapon's own damage.
    /// </summary>
    protected virtual float BonusFireDamage => 0f;

    /// <summary>
    /// Spirit damage added on top of the weapon's own damage.
    /// </summary>
    protected virtual float BonusSpiritDamage => 0f;

    /// <summary>
    /// Ingredients of the recipe. The progression tier may add its own cost on top (see ProgressionManager).
    /// </summary>
    protected virtual RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "FineWood", Amount = 10, Recover = false }
    };

    /// <summary>
    /// Whether the weapon is added to the game at all. A disabled one is skipped when the plugin starts, so it never
    /// reaches ObjectDB or a recipe.
    /// </summary>
    public abstract bool Enabled { get; }

    /// <summary>
    /// The crafting station the weapon is made at.
    /// </summary>
    protected virtual string Station => CraftingStations.Forge;

    /// <summary>
    /// The level the crafting station must have.
    /// </summary>
    protected virtual int StationLevel => 2;

    /// <summary>
    /// Name of the mesh in the White Hilt asset bundle that replaces the vanilla look (texture
    /// <c>&lt;name&gt;_albedo</c>),
    /// or null to keep the vanilla look.
    /// </summary>
    protected virtual string ModelName => null;

    /// <summary>
    /// What sets the weapon apart when it hits (bleeding, hooking shields), or null for nothing beyond its damage.
    /// </summary>
    protected virtual WeaponTrait Trait => null;

    /// <summary>
    /// Whether a thrown weapon flies as its own model (<see cref="ModelName"/>) instead of the vanilla projectile's look.
    /// </summary>
    protected virtual bool ThrownAsModel => false;

    /// <summary>
    /// How many turns per second a weapon thrown as its own model spins end over end; 0 flies straight like a spear.
    /// </summary>
    protected virtual float ThrownSpin => 0f;

    /// <summary>
    /// The colour the model always glows in where its emission map (<c>&lt;name&gt;_emission</c>) is lit, or null for
    /// none. A Glow Rune etched into the weapon shines over it, and it comes back when that glow is put out.
    /// </summary>
    protected virtual Color? ModelGlow => null;

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
                CraftingStation = Station,
                MinStationLevel = StationLevel,
                Requirements = Requirements
            };

            IndestructibleItem item = new(BaseName, CopyFrom, itemConfig);
            if (StatsFrom != null)
            {
                CopyStats(item.ItemData, StatsFrom);
            }

            AdjustStats(item.ItemData);
            if (ThrownAsModel)
            {
                thrownProjectile = ThrownWeapon.CreateProjectile(BaseName, item.ItemData.m_secondaryAttack);
            }

            baseDamageMultiplier = item.ItemData.m_attack.m_damageMultiplier;
            baseDamages = item.ItemData.m_damages.Clone();
            baseDamagesPerLevel = item.ItemData.m_damagesPerLevel.Clone();
            added = item;
            upgradeKind = UpgradeKind(item.ItemData);
            if (upgradeKind.HasValue)
            {
                GearUpgrades.Register(BaseName, NameToken, upgradeKind.Value);
            }

            FreezeUpgradeGlow(item.ItemPrefab);
            Binding.GearBinding.Register(NameToken, item.ItemData.m_itemType == ItemDrop.ItemData.ItemType.Shield);
            WeaponTraits.Register(item.ItemData.m_name, Trait);

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
        damages.m_fire += BonusFireDamage;
        damages.m_spirit += BonusSpiritDamage;
        shared.m_damages = damages;

        // Only the types the weapon already deals grow with its level, so a sword gets no pierce.
        float perLevelBonus = bonusDamagePerLevel.Value;
        HitData.DamageTypes perLevel = baseDamagesPerLevel.Clone();
        perLevel.m_damage += baseDamages.m_damage > 0f ? perLevelBonus : 0f;
        perLevel.m_blunt += baseDamages.m_blunt > 0f ? perLevelBonus : 0f;
        perLevel.m_slash += baseDamages.m_slash > 0f ? perLevelBonus : 0f;
        perLevel.m_pierce += baseDamages.m_pierce > 0f ? perLevelBonus : 0f;
        perLevel.m_fire += baseDamages.m_fire > 0f ? perLevelBonus : 0f;
        perLevel.m_frost += baseDamages.m_frost > 0f ? perLevelBonus : 0f;
        perLevel.m_lightning += baseDamages.m_lightning > 0f ? perLevelBonus : 0f;
        perLevel.m_poison += baseDamages.m_poison > 0f ? perLevelBonus : 0f;
        perLevel.m_spirit += baseDamages.m_spirit > 0f ? perLevelBonus : 0f;
        shared.m_damagesPerLevel = perLevel;
        if (upgradeKind.HasValue)
        {
            shared.m_maxQuality = GearUpgrades.MaxQuality(upgradeKind.Value);
        }

        added.ApplyConfig();
    }

    /// <summary>
    /// Called after the model from <see cref="ModelName"/> is in place, to add behaviour to it.
    /// </summary>
    /// <param name="model">The object that shows the model, under the item's attach child.</param>
    protected virtual void OnModelApplied(GameObject model)
    {
    }

    /// <summary>
    /// Called after the clone and <see cref="StatsFrom"/>, before the config's bonuses are added, to give the weapon its
    /// own damage and attacks. The config's bonuses then build on what this leaves.
    /// </summary>
    /// <param name="shared">The weapon's shared data.</param>
    protected virtual void AdjustStats(ItemDrop.ItemData.SharedData shared)
    {
    }

    // Copies the combat stats of another weapon onto this one, so the White Hilt weapon can look like one item and
    // fight like another.
    private static void CopyStats(ItemDrop.ItemData.SharedData target, string sourceName)
    {
        ItemDrop source = PrefabManager.Cache.GetPrefab<ItemDrop>(sourceName);
        if (source == null)
        {
            Jotunn.Logger.LogWarning($"Stats source {sourceName} not found; keeping the cloned stats.");
            return;
        }

        ItemDrop.ItemData.SharedData stats = source.m_itemData.m_shared;
        target.m_damages = stats.m_damages.Clone();
        target.m_damagesPerLevel = stats.m_damagesPerLevel.Clone();
        target.m_maxQuality = stats.m_maxQuality;
        target.m_toolTier = stats.m_toolTier;
        target.m_attackForce = stats.m_attackForce;
        target.m_backstabBonus = stats.m_backstabBonus;
        target.m_blockPower = stats.m_blockPower;
        target.m_blockPowerPerLevel = stats.m_blockPowerPerLevel;
        target.m_deflectionForce = stats.m_deflectionForce;
        target.m_deflectionForcePerLevel = stats.m_deflectionForcePerLevel;
        target.m_timedBlockBonus = stats.m_timedBlockBonus;
        target.m_movementModifier = stats.m_movementModifier;
        target.m_attackStatusEffect = stats.m_attackStatusEffect;
        target.m_attackStatusEffectChance = stats.m_attackStatusEffectChance;
        CopyAttackStats(target.m_attack, stats.m_attack);
        CopyAttackStats(target.m_secondaryAttack, stats.m_secondaryAttack);
    }

    private static void CopyAttackStats(Attack target, Attack source)
    {
        if (target == null || source == null)
        {
            return;
        }

        target.m_attackStamina = source.m_attackStamina;
        target.m_attackEitr = source.m_attackEitr;
        target.m_damageMultiplier = source.m_damageMultiplier;
    }

    // The staffs already have the Mistlands staffs' strength, so they stay at quality 4.
    private static GearKind? UpgradeKind(ItemDrop.ItemData.SharedData shared)
    {
        if (shared.m_itemType == ItemDrop.ItemData.ItemType.Shield)
        {
            return GearKind.Shield;
        }

        return shared.m_skillType is Skills.SkillType.ElementalMagic or Skills.SkillType.BloodMagic ? null : GearKind.Weapon;
    }

    private static void BindConfig()
    {
        if (damageMultiplierBonus != null)
        {
            return;
        }

        damageMultiplierBonus = WhiteHiltConfig.BindAdminOnly(Section, "DamageMultiplierBonus", 0.1f,
            "Added to the primary attack's damage multiplier of every White Hilt weapon and shield (0.1 = 10% more damage).", new AcceptableValueRange<float>(0f, 5f));
        bonusDamagePerLevel = WhiteHiltConfig.BindAdminOnly(Section, "BonusDamagePerLevel", 2f,
            "Added per quality level to each damage type the weapon already deals.", new AcceptableValueRange<float>(0f, 500f));
        GearUpgrades.BindConfig();
    }

    // Replaces the cloned weapon's look with the White Hilt model and renders an icon from it. A broken look only logs
    // a warning: the item must stay, or players would lose it from their inventories.
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
            if (ModelGlow.HasValue)
            {
                ApplyModelGlow(model, ModelGlow.Value);
            }

            OnModelApplied(model);
            if (thrownProjectile != null)
            {
                ThrownWeapon.ApplyLook(thrownProjectile, model, ThrownSpin);
            }

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

    // Vanilla upgrade sparkles (ParticleIntensityScaler) light up above quality 4 and grow and change colour with every
    // level after that. White Hilt gear goes far past 4, so they grew too bright; without the per-level growth they keep
    // the look of the first level above 4.
    private static void FreezeUpgradeGlow(GameObject itemPrefab)
    {
        foreach (ParticleIntensityScaler scaler in itemPrefab.GetComponentsInChildren<ParticleIntensityScaler>(true))
        {
            scaler.itemDropLevelMultiplier = 0f;
        }
    }

    // Lights the model with its emission map (<ModelName>_emission). Without the map it keeps its model, only unlit.
    private void ApplyModelGlow(GameObject model, Color glow)
    {
        Material material = model.GetComponent<MeshRenderer>().sharedMaterial;
        if (!material.HasProperty("_EmissionMap"))
        {
            return;
        }

        Texture2D emission;
        try
        {
            emission = ForagingAssets.LoadTexture($"{ModelName}_emission");
        }
        catch (InvalidOperationException ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: no glow: {ex.Message}");
            return;
        }

        material.EnableKeyword("_EMISSION");
        material.SetTexture("_EmissionMap", emission);
        material.SetColor("_EmissionColor", glow);
    }
}
