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

namespace BrudvikWhiteHilt.Items.Accessories;

/// <summary>
/// A charm of Brood Scale worn in an accessory slot: it adds armor and cuts the poison damage taken, the way the
/// Nidhogg Spawn's own hide does. A clone of the Wishbone, with its finder effect swapped for a plain stats effect.
/// </summary>
public class BroodCharm : IWhiteHiltCustomItem, IWhiteHiltConfigurable
{
    private const string FullName = "Brood Scale Charm";
    private const string EffectKey = "se_whitehilt_broodcharm";

    private readonly ItemManager instance;
    private SE_Stats effect;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run. Registers the English texts.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public BroodCharm(ItemManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(WildMonsters.BroodCharm), FullName,
            "Four black scales bound with eitr wire. The brood's venom no longer bites the one who wears it.");
        Translations.AddEnglish(EffectKey, FullName);
        Translations.AddEnglish($"{EffectKey}_tooltip", "Adds {0} armor and resists poison");
        Translations.AddDynamic($"{EffectKey}_tooltip", () => new object[] { Translations.Number(WildLootSettings.CharmArmor.Value) });
    }

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Mistlands;

    /// <inheritdoc/>
    public string Id => WildMonsters.BroodCharm;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(Translations.ItemKey(WildMonsters.BroodCharm));

    /// <inheritdoc/>
    public string GatedPrefabName => WildMonsters.BroodCharm;

    /// <summary>
    /// Adds the charm and its recipe at the Galdr Table.
    /// </summary>
    public void Add()
    {
        try
        {
            CustomItem charm = new(WildMonsters.BroodCharm, "Wishbone", new ItemConfig
            {
                Name = NameToken,
                Description = Translations.Token($"{Translations.ItemKey(WildMonsters.BroodCharm)}_description"),
                CraftingStation = "piece_magetable",
                MinStationLevel = 1,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = WildMonsters.BroodScale, Amount = 4 },
                    new() { Item = "BlackMarble", Amount = 2 },
                    new() { Item = "Eitr", Amount = 2 }
                }
            });

            ItemDrop.ItemData.SharedData shared = charm.ItemDrop.m_itemData.m_shared;
            effect = ScriptableObject.CreateInstance<SE_Stats>();
            effect.name = "SE_WhiteHiltBroodCharm";
            effect.m_name = Translations.Token(EffectKey);
            effect.m_tooltip = Translations.Token($"{EffectKey}_tooltip");
            shared.m_equipStatusEffect = effect;
            ApplyConfig();
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

    /// <summary>
    /// Applies changed or server-synced armor and poison resistance. A charm already worn keeps the old values until it
    /// is put on again.
    /// </summary>
    public void ApplyConfig()
    {
        if (effect == null)
        {
            return;
        }

        effect.m_addArmor = WildLootSettings.CharmArmor.Value;
        effect.m_mods = new() { new HitData.DamageModPair { m_type = HitData.DamageType.Poison, m_modifier = WildLootSettings.CharmPoison.Value } };
    }

    // Tints the Wishbone dark red and renders an icon from it; a failure keeps the Wishbone's look.
    private static void ApplyVisual(CustomItem charm)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            VisualHelper.Tint(charm.ItemPrefab, new Color(0.75f, 0.35f, 0.3f));
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
