using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Roofing;
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
/// The Pitch Pot: a clay pot of hot pine tar that sets a deck alight where it lands. It is a clone of the vanilla Ooze
/// Bomb, thrown the same way, but it flies as a projectile of its own that does fire damage instead of spawning the
/// poison cloud, and reports where it lands to <see cref="ShotHits"/>, which lights the fire. The Nail Ships' crews throw
/// the same pot. Damage and the stack made at once come from <see cref="WeaponSettings"/>; its names are registered there.
/// </summary>
public class PitchPot : IWhiteHiltCustomItem, IWhiteHiltConfigurable
{
    /// <summary>
    /// Prefab name of the pot, and the key of its name and description in the translation files.
    /// </summary>
    public const string PrefabName = WeaponSettings.PitchPotName;

    /// <summary>
    /// Prefab name of the pot's projectile.
    /// </summary>
    public const string ProjectileName = "WhiteHilt_PitchPot_projectile";

    private const string FullName = "Pitch Pot";

    // The vanilla Ooze Bomb: thrown with the bomb animation, its projectile and its stack of 50.
    private const string CopyFrom = "BombOoze";
    private const string CopyProjectile = "oozebomb_projectile";

    // Dark brown, the colour of pine tar.
    private static readonly Color pitch = new(0.35f, 0.3f, 0.27f);

    private readonly ItemManager instance;
    private ItemDrop.ItemData.SharedData shared;
    private Recipe recipe;

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Swamp;

    /// <inheritdoc/>
    public string Id => PrefabName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(Translations.ItemKey(PrefabName));

    /// <inheritdoc/>
    public string GatedPrefabName => PrefabName;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run. The English text is registered by <see cref="WeaponSettings"/>.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public PitchPot(ItemManager instance)
    {
        this.instance = instance;
    }

    /// <summary>
    /// Adds the pot, its projectile and its Workbench recipe to the game.
    /// </summary>
    public void Add()
    {
        try
        {
            string key = Translations.ItemKey(PrefabName);
            CustomItem pot = new(PrefabName, CopyFrom, new ItemConfig
            {
                Name = Translations.Token(key),
                Description = Translations.Token($"{key}_description"),
                CraftingStation = CraftingStations.Workbench,
                MinStationLevel = 1,
                Amount = WeaponSettings.PitchPotsPerCraft.Value,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = RoofMaterials.PineTar, Amount = 1, Recover = false },
                    new() { Item = "Resin", Amount = 2, Recover = false },
                    new() { Item = "LeatherScraps", Amount = 1, Recover = false }
                }
            });

            shared = pot.ItemDrop.m_itemData.m_shared;
            recipe = pot.Recipe?.Recipe;
            shared.m_attack.m_attackProjectile = AddProjectile();
            ApplyConfig();
            TryApplyLook(pot, shared);

            instance.AddItem(pot);
            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    /// <summary>
    /// Applies the configured damage and stack size. Does nothing before the pot is added.
    /// </summary>
    public void ApplyConfig()
    {
        if (shared == null)
        {
            return;
        }

        shared.m_damages.m_fire = WeaponSettings.PitchPotDamage.Value;
        if (recipe != null)
        {
            recipe.m_amount = WeaponSettings.PitchPotsPerCraft.Value;
        }
    }

    // The pot's own projectile: the Ooze Bomb's, which falls on the same arc, without the poison cloud it spawns where it
    // lands, and with a hit handler that lights the fire. It is made on servers too, since the projectile is a network prefab.
    private static GameObject AddProjectile()
    {
        GameObject projectile = PrefabManager.Instance.CreateClonedPrefab(ProjectileName, CopyProjectile);
        Projectile flight = projectile.GetComponent<Projectile>();
        flight.m_spawnOnHit = null;
        flight.m_canHitWater = true;
        projectile.AddComponent<ShotHits>().m_kind = ShotKind.PitchPot;
        if (!VisualHelper.IsHeadless)
        {
            try
            {
                StopOoze(projectile);
                VisualHelper.Tint(projectile, pitch);
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogWarning($"{FullName} projectile keeps the vanilla look: {ex.Message}");
            }
        }

        PrefabManager.Instance.AddPrefab(new CustomPrefab(projectile, false));
        return projectile;
    }

    // Dark clay and an icon of its own, without the green ooze. A dedicated server draws nothing; a failure keeps the vanilla look.
    private static void TryApplyLook(CustomItem pot, ItemDrop.ItemData.SharedData itemShared)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            StopOoze(pot.ItemPrefab);
            VisualHelper.Tint(pot.ItemPrefab, pitch);
            Sprite icon = VisualHelper.RenderIcon(pot.ItemPrefab);
            if (icon != null)
            {
                itemShared.m_icons = Enumerable.Repeat(icon, Math.Max(1, itemShared.m_icons.Length)).ToArray();
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName} keeps the vanilla look: {ex.Message}");
        }
    }

    // The vanilla bomb drips ooze; a pot of tar does not.
    private static void StopOoze(GameObject root)
    {
        foreach (ParticleSystem particles in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = false;
        }
    }
}
