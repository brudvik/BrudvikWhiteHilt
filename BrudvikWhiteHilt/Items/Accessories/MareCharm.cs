using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Monsters;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Accessories;

/// <summary>
/// A charm braided of Mare Hair, worn in an accessory slot: the Mare's Nightmare cannot take hold of the one who wears
/// it, from her grip or from her ride on a sleeper (<see cref="MareSpirit.Warded"/>). A clone of the Wishbone, with its
/// finder effect swapped for a plain effect that marks the wearer.
/// </summary>
public class MareCharm : IWhiteHiltCustomItem
{
    /// <summary>Prefab name of the charm.</summary>
    public const string PrefabName = "WhiteHilt_MareCharm";

    /// <summary>Hash of the charm's effect while it is worn.</summary>
    public static readonly int EffectHash = "SE_WhiteHiltMareCharm".GetStableHashCode();

    private const string FullName = "Mare Charm";
    private const string EffectKey = "se_whitehilt_marecharm";

    private readonly ItemManager instance;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run. Registers the English texts.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public MareCharm(ItemManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(PrefabName), FullName,
            "A lock of a Mare's own black hair, braided round silver. She will not ride the one who wears it.");
        Translations.AddEnglish(EffectKey, FullName);
        Translations.AddEnglish($"{EffectKey}_tooltip", "The Mare's Nightmare cannot take hold of you");
    }

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Mistlands;

    /// <inheritdoc/>
    public string Id => PrefabName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(Translations.ItemKey(PrefabName));

    /// <inheritdoc/>
    public string GatedPrefabName => PrefabName;

    /// <summary>
    /// Adds the charm and its recipe at the Galdr Table.
    /// </summary>
    public void Add()
    {
        try
        {
            CustomItem charm = new(PrefabName, "Wishbone", new ItemConfig
            {
                Name = NameToken,
                Description = Translations.Token($"{Translations.ItemKey(PrefabName)}_description"),
                CraftingStation = "piece_magetable",
                MinStationLevel = 1,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = WildMonsters.MareHair, Amount = 3 },
                    new() { Item = "Silver", Amount = 2 },
                    new() { Item = "Eitr", Amount = 2 }
                }
            });

            ItemDrop.ItemData.SharedData shared = charm.ItemDrop.m_itemData.m_shared;
            SE_Stats effect = ScriptableObject.CreateInstance<SE_Stats>();
            effect.name = "SE_WhiteHiltMareCharm";
            effect.m_name = Translations.Token(EffectKey);
            effect.m_tooltip = Translations.Token($"{EffectKey}_tooltip");
            shared.m_equipStatusEffect = effect;
            ApplyVisual(charm);
            effect.m_icon = shared.m_icons.FirstOrDefault();
            instance.AddStatusEffect(new CustomStatusEffect(effect, fixReference: false));
            instance.AddItem(charm);
            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    // Tints the Wishbone a cold, pale grey and renders an icon from it; a failure keeps the Wishbone's look.
    private static void ApplyVisual(CustomItem charm)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            VisualHelper.Tint(charm.ItemPrefab, new Color(0.55f, 0.6f, 0.7f));
            Sprite icon = VisualHelper.RenderIcon(charm.ItemPrefab);
            ItemDrop.ItemData.SharedData shared = charm.ItemDrop.m_itemData.m_shared;
            if (icon != null)
            {
                shared.m_icons = Enumerable.Repeat(icon, Math.Max(1, shared.m_icons.Length)).ToArray();
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: keeping the Wishbone's look: {ex.Message}");
        }
    }
}
