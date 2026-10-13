using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Ships;
using BrudvikWhiteHilt.Pieces.Ships.Ballista;
using BrudvikWhiteHilt.Progression;
using BrudvikWhiteHilt.SeaBattles.Weapons;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.ShipUpgrades;

/// <summary>
/// Ammunition for the Ship Ballista that does something besides hurt: the Chain Shot and the Grapnel Bolt. Like the Ship Bolt
/// it is a clone of the vanilla turret bolt with the ballista's ammo type, so the ballista fires it and the Mistlands turret
/// cannot. Its projectile is a clone of the bolt's own with a <see cref="ShotHits"/> on it, which tells the ship that was hit
/// what happened. Speed and spread come from <see cref="BallistaSettings"/> as for the Ship Bolt; damage, the stack made at
/// once and the recipe are the subclass's. The names and descriptions are registered by <see cref="WeaponSettings"/>.
/// </summary>
public abstract class ShipAmmoBase : IWhiteHiltCustomItem, IWhiteHiltConfigurable
{
    // The vanilla turret bolt and its projectile; the ammo type is the Ship Bolt's.
    private const string CopyFrom = "TurretBoltWood";
    private const string CopyProjectile = "Turret_projectile";

    private readonly ItemManager instance;
    private ItemDrop.ItemData.SharedData shared;
    private Recipe recipe;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees the
    /// result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    protected ShipAmmoBase(ItemManager instance)
    {
        this.instance = instance;
    }

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Swamp;

    /// <inheritdoc/>
    public abstract string Id { get; }

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(Translations.ItemKey(Id));

    /// <inheritdoc/>
    public string GatedPrefabName => Id;

    /// <summary>The name used in the log.</summary>
    protected abstract string FullName { get; }

    /// <summary>Prefab name of the ammunition's projectile.</summary>
    protected abstract string ProjectileName { get; }

    /// <summary>What the projectile does when it hits.</summary>
    protected abstract ShotKind Kind { get; }

    /// <summary>Colour the bolt and its projectile are tinted.</summary>
    protected abstract Color Tint { get; }

    /// <summary>What the ammunition costs at the Shipwright's Bench.</summary>
    protected abstract RequirementConfig[] Requirements { get; }

    /// <summary>The pierce damage of a hit, from the settings.</summary>
    protected abstract float Damage { get; }

    /// <summary>How many are made at once, from the settings.</summary>
    protected abstract int AmountPerCraft { get; }

    /// <summary>
    /// Adds the ammunition, its projectile and its Shipwright's Bench recipe to the game.
    /// </summary>
    public void Add()
    {
        try
        {
            string key = Translations.ItemKey(Id);
            CustomItem ammo = new(Id, CopyFrom, new ItemConfig
            {
                Name = Translations.Token(key),
                Description = Translations.Token($"{key}_description"),
                CraftingStation = ShipwrightBench.StationPrefabName,
                Amount = AmountPerCraft,
                Requirements = Requirements
            });

            shared = ammo.ItemDrop.m_itemData.m_shared;
            shared.m_ammoType = ShipBolts.AmmoType;
            shared.m_attack.m_attackProjectile = AddProjectile();
            recipe = ammo.Recipe?.Recipe;
            ApplyConfig();
            TryApplyLook(ammo);

            instance.AddItem(ammo);
            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    /// <summary>
    /// Applies the configured damage, speed, spread and stack size. Does nothing before the item is added.
    /// </summary>
    public void ApplyConfig()
    {
        if (shared == null)
        {
            return;
        }

        shared.m_damages.m_pierce = Damage;
        shared.m_attack.m_projectileVel = BallistaSettings.BoltSpeed.Value;
        shared.m_attack.m_projectileAccuracy = BallistaSettings.Accuracy.Value;
        shared.m_attack.m_projectileAccuracyMin = BallistaSettings.Accuracy.Value;
        if (recipe != null)
        {
            recipe.m_amount = AmountPerCraft;
        }
    }

    // The ammunition's own projectile: the bolt's, with a hit handler that does what the ammunition does. It is made on servers
    // too, since the projectile is a network prefab.
    private GameObject AddProjectile()
    {
        GameObject projectile = PrefabManager.Instance.CreateClonedPrefab(ProjectileName, CopyProjectile);
        projectile.AddComponent<ShotHits>().m_kind = Kind;
        if (!VisualHelper.IsHeadless)
        {
            try
            {
                VisualHelper.Tint(projectile, Tint);
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogWarning($"{FullName} projectile keeps the vanilla look: {ex.Message}");
            }
        }

        PrefabManager.Instance.AddPrefab(new CustomPrefab(projectile, false));
        return projectile;
    }

    // A tint and an icon of its own. A dedicated server draws nothing; a failure keeps the vanilla look.
    private void TryApplyLook(CustomItem ammo)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            VisualHelper.Tint(ammo.ItemPrefab, Tint);
            Sprite icon = VisualHelper.RenderIcon(ammo.ItemPrefab);
            if (icon != null)
            {
                shared.m_icons = Enumerable.Repeat(icon, Math.Max(1, shared.m_icons.Length)).ToArray();
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName} keeps the vanilla look: {ex.Message}");
        }
    }
}
