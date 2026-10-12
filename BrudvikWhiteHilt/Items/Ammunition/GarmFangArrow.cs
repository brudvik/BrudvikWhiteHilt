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
/// Arrows tipped with a Garm Fang: the vanilla charred arrow, with a little fire from the fang added to its pierce
/// damage. Both numbers are settings.
/// </summary>
public class GarmFangArrow : IWhiteHiltCustomItem, IWhiteHiltConfigurable
{
    private const string FullName = "Garm Fang Arrow";
    private const string Description = "A charred arrow tipped with a Garm Fang. The tip glows red and sets what it hits alight.";
    private static readonly Color FangTint = new(0.85f, 0.5f, 0.4f);

    private readonly ItemManager instance;
    private ItemDrop.ItemData.SharedData shared;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run. Registers the English text.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public GarmFangArrow(ItemManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(WildMonsters.GarmFangArrow), FullName, Description);
    }

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Ashlands;

    /// <inheritdoc/>
    public string Id => WildMonsters.GarmFangArrow;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(Translations.ItemKey(WildMonsters.GarmFangArrow));

    /// <inheritdoc/>
    public string GatedPrefabName => WildMonsters.GarmFangArrow;

    /// <summary>
    /// Adds the arrow and its recipe at the workbench.
    /// </summary>
    public void Add()
    {
        try
        {
            CustomItem arrow = new(WildMonsters.GarmFangArrow, "ArrowCharred", new ItemConfig
            {
                Name = NameToken,
                Description = Translations.Token($"{Translations.ItemKey(WildMonsters.GarmFangArrow)}_description"),
                CraftingStation = CraftingStations.Workbench,
                MinStationLevel = 2,
                Amount = 20,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "Blackwood", Amount = 8 },
                    new() { Item = WildMonsters.GarmFang, Amount = 2 },
                    new() { Item = "Feathers", Amount = 2 }
                }
            });

            shared = arrow.ItemDrop.m_itemData.m_shared;
            ApplyConfig();
            if (!VisualHelper.IsHeadless)
            {
                VisualHelper.Tint(arrow.ItemPrefab, FangTint);
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
        damages.m_pierce = WildLootSettings.ArrowPierce.Value;
        damages.m_fire = WildLootSettings.ArrowFire.Value;
        shared.m_damages = damages;
    }
}
