using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Ships;
using BrudvikWhiteHilt.Pieces.Ships.Ballista;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.ShipUpgrades;

/// <summary>
/// The Ship Bolt, ammunition for the Ship Ballista. It is a clone of the vanilla turret bolt, so it brings the
/// projectile and its flight, but it has an ammo type of its own, so the Mistlands turret can not shoot it and the
/// ballista can not shoot turret bolts. Damage, speed, spread and the stack made at once come from
/// <see cref="BallistaSettings"/>.
/// </summary>
public class ShipBolts : IWhiteHiltCustomItem, IWhiteHiltConfigurable
{
    /// <summary>
    /// Prefab name of the bolt, and the key of its name and description in the translation files.
    /// </summary>
    public const string PrefabName = "WhiteHiltShipBolt";

    /// <summary>
    /// Ammo type of the bolt. The ballista takes bolts of this type from the gunner's inventory.
    /// </summary>
    public const string AmmoType = "$ammo_whitehilt_shipbolt";

    private const string FullName = "Ship Bolt";
    private const string Description = "A heavy iron-tipped bolt for the Ship Ballista.";

    // The vanilla turret bolt: Turret_projectile, pierce damage, and the ammo item type the inventory looks for.
    private const string CopyFrom = "TurretBoltWood";

    // Steel grey over the wooden stake of the vanilla bolt.
    private static readonly Color steel = new(0.62f, 0.68f, 0.78f);

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
    /// the result only once Add has run. Registers the English text.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public ShipBolts(ItemManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(PrefabName), FullName, Description);
    }

    /// <summary>
    /// Adds the bolt and its Shipwright's Bench recipe to the game.
    /// </summary>
    public void Add()
    {
        try
        {
            string key = Translations.ItemKey(PrefabName);
            CustomItem bolt = new(PrefabName, CopyFrom, new ItemConfig
            {
                Name = Translations.Token(key),
                Description = Translations.Token($"{key}_description"),
                CraftingStation = ShipwrightBench.StationPrefabName,
                Amount = BallistaSettings.BoltsPerCraft.Value,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "Wood", Amount = 4, Recover = false },
                    new() { Item = "Iron", Amount = 1, Recover = false },
                    new() { Item = "Feathers", Amount = 2, Recover = false }
                }
            });

            shared = bolt.ItemDrop.m_itemData.m_shared;
            shared.m_ammoType = AmmoType;
            recipe = bolt.Recipe?.Recipe;
            ApplyConfig();
            TryApplyLook(bolt, shared);

            instance.AddItem(bolt);
            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    /// <summary>
    /// Applies the configured damage, speed, spread and stack size. Does nothing before the bolt is added.
    /// </summary>
    public void ApplyConfig()
    {
        if (shared == null)
        {
            return;
        }

        shared.m_damages.m_pierce = BallistaSettings.Damage.Value;
        shared.m_attack.m_projectileVel = BallistaSettings.BoltSpeed.Value;
        shared.m_attack.m_projectileAccuracy = BallistaSettings.Accuracy.Value;
        shared.m_attack.m_projectileAccuracyMin = BallistaSettings.Accuracy.Value;
        if (recipe != null)
        {
            recipe.m_amount = BallistaSettings.BoltsPerCraft.Value;
        }
    }

    // Steel grey and an icon of its own. A dedicated server draws nothing; a failure keeps the vanilla look.
    private static void TryApplyLook(CustomItem bolt, ItemDrop.ItemData.SharedData itemShared)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            VisualHelper.Tint(bolt.ItemPrefab, steel);
            Sprite icon = VisualHelper.RenderIcon(bolt.ItemPrefab);
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
}
