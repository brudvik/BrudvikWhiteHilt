using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Monsters;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Ammunition;

/// <summary>
/// Fire arrows tipped with a sliver of Desert Dragon scale: the vanilla fire arrow, with Plains-strength pierce and
/// fire.
/// </summary>
public class DragonfireArrow : IWhiteHiltCustomItem, IWhiteHiltConfigurable
{
    /// <summary>
    /// Prefab name of the arrow.
    /// </summary>
    public const string PrefabName = "WhiteHiltDragonfireArrow";

    private const string FullName = "Dragonfire Arrow";
    private const string Description = "A fire arrow tipped with a sliver of Desert Dragon scale. The scale keeps the flame alive long after the pitch would have burnt out.";
    private const string Section = "Gear.WhiteHiltDragonfireArrow";
    private static readonly Color ScaleTint = new(1f, 0.8f, 0.55f);

    private readonly ItemManager instance;
    private readonly ConfigEntry<float> pierce;
    private readonly ConfigEntry<float> fire;
    private ItemDrop.ItemData.SharedData shared;

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Plains;

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
    /// the result only once Add has run. Binds the config entries and registers the English text.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public DragonfireArrow(ItemManager instance)
    {
        this.instance = instance;
        pierce = WhiteHiltConfig.BindAdminOnly(Section, "Pierce", 30f, "Pierce damage of a Dragonfire Arrow.", new AcceptableValueRange<float>(0f, 500f));
        fire = WhiteHiltConfig.BindAdminOnly(Section, "Fire", 60f, "Fire damage of a Dragonfire Arrow.", new AcceptableValueRange<float>(0f, 500f));
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(PrefabName), FullName, Description);
    }

    /// <summary>
    /// Adds the arrow and its recipe at the workbench.
    /// </summary>
    public void Add()
    {
        try
        {
            CustomItem arrow = new(PrefabName, "ArrowFire", new ItemConfig
            {
                Name = NameToken,
                Description = Translations.Token($"{Translations.ItemKey(PrefabName)}_description"),
                CraftingStation = CraftingStations.Workbench,
                MinStationLevel = 2,
                Amount = 20,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "Wood", Amount = 8 },
                    new() { Item = "Feathers", Amount = 2 },
                    new() { Item = DesertDragonRegistry.ScaleName, Amount = 1 }
                }
            });

            shared = arrow.ItemDrop.m_itemData.m_shared;
            ApplyConfig();
            if (!VisualHelper.IsHeadless)
            {
                VisualHelper.Tint(arrow.ItemPrefab, ScaleTint);
                Sprite icon = VisualHelper.RenderIcon(arrow.ItemPrefab);
                if (icon != null)
                {
                    shared.m_icons = Enumerable.Repeat(icon, Math.Max(1, shared.m_icons.Length)).ToArray();
                }
            }

            instance.AddItem(arrow);
            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    /// <summary>
    /// Applies changed or server-synced damage. Arrows in inventories share this data, so they change too.
    /// </summary>
    public void ApplyConfig()
    {
        if (shared == null)
        {
            return;
        }

        HitData.DamageTypes damages = shared.m_damages;
        damages.m_pierce = pierce.Value;
        damages.m_fire = fire.Value;
        shared.m_damages = damages;
    }
}
