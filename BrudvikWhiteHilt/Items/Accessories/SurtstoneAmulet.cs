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
/// An amulet of Surtstone worn in an accessory slot: fire hurts less, and the lava's heat builds up slower (the game
/// adds its heat resistance to the armour's). A clone of the Wishbone, with its finder effect swapped for a plain stats
/// effect.
/// </summary>
public class SurtstoneAmulet : IWhiteHiltCustomItem, IWhiteHiltConfigurable
{
    private const string FullName = "Surtstone Amulet";
    private const string PrefabName = "WhiteHilt_SurtstoneAmulet";
    private const string EffectKey = "se_whitehilt_surtstoneamulet";

    private readonly ItemManager instance;
    private SE_Stats effect;
    private ItemDrop.ItemData.SharedData shared;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run. Registers the English texts.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public SurtstoneAmulet(ItemManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(PrefabName), FullName,
            "Two Surtstones in a ring of flametal. The fire of Muspelheim knows its own, and spares the one who wears it.");
        Translations.AddEnglish(EffectKey, FullName);
        Translations.AddEnglish($"{EffectKey}_tooltip", "Resists fire, and the lava's heat builds up {0}% slower");
        Translations.AddDynamic($"{EffectKey}_tooltip", () => new object[] { Translations.Number(MuspellSettings.AmuletHeatResistance.Value * 100f) });
    }

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Ashlands;

    /// <inheritdoc/>
    public string Id => PrefabName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(Translations.ItemKey(PrefabName));

    /// <inheritdoc/>
    public string GatedPrefabName => PrefabName;

    /// <summary>
    /// Adds the amulet and its recipe at the Galdr Table.
    /// </summary>
    public void Add()
    {
        try
        {
            CustomItem amulet = new(PrefabName, "Wishbone", new ItemConfig
            {
                Name = NameToken,
                Description = Translations.Token($"{Translations.ItemKey(PrefabName)}_description"),
                CraftingStation = "piece_magetable",
                MinStationLevel = 1,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = MuspellRegistry.SurtstoneName, Amount = 2 },
                    new() { Item = "FlametalNew", Amount = 4 },
                    new() { Item = "Eitr", Amount = 4 }
                }
            });

            shared = amulet.ItemDrop.m_itemData.m_shared;
            effect = ScriptableObject.CreateInstance<SE_Stats>();
            effect.name = "SE_WhiteHiltSurtstoneAmulet";
            effect.m_name = Translations.Token(EffectKey);
            effect.m_tooltip = Translations.Token($"{EffectKey}_tooltip");
            shared.m_equipStatusEffect = effect;
            ApplyConfig();
            ApplyVisual(amulet);
            effect.m_icon = shared.m_icons.FirstOrDefault();
            instance.AddStatusEffect(new CustomStatusEffect(effect, fixReference: false));
            instance.AddItem(amulet);
            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    /// <summary>
    /// Applies changed or server-synced fire and heat resistance. An amulet already worn keeps the old values until it
    /// is put on again.
    /// </summary>
    public void ApplyConfig()
    {
        if (shared != null)
        {
            shared.m_heatResistanceModifier = MuspellSettings.AmuletHeatResistance.Value;
        }

        if (effect != null)
        {
            effect.m_mods = new() { new HitData.DamageModPair { m_type = HitData.DamageType.Fire, m_modifier = MuspellSettings.AmuletFire.Value } };
        }
    }

    // Tints the Wishbone a glowing ember orange and renders an icon from it; a failure keeps the Wishbone's look.
    private static void ApplyVisual(CustomItem amulet)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            VisualHelper.Tint(amulet.ItemPrefab, new Color(1f, 0.5f, 0.2f));
            Sprite icon = VisualHelper.RenderIcon(amulet.ItemPrefab);
            ItemDrop.ItemData.SharedData shared = amulet.ItemDrop.m_itemData.m_shared;
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
