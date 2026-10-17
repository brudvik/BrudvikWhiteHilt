using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Armors.WhiteHiltUniform;
using BrudvikWhiteHilt.Monsters;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Armors.LavaWormArmor;

/// <summary>
/// A piece of Lava Worm armour: a copy of the vanilla Ask armour, charred black with glowing seams, made from Lava Worm
/// Scales at the Black Forge. Each piece keeps the lava's heat off (the game adds the pieces' heat resistance up), and
/// the whole set makes fire hurt much less.
/// </summary>
public abstract class LavaWormArmorBase : IWhiteHiltCustomItem, IWhiteHiltConfigurable
{
    private const string SetKey = "whitehilt_lavawormarmor";

    // The set's bonus, made by the first piece that is added and shared by all three.
    private static SE_Stats setEffect;

    private ItemDrop.ItemData.SharedData shared;

    /// <summary>
    /// Registers the English texts of the piece, and once of the set.
    /// </summary>
    protected LavaWormArmorBase()
    {
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(Id), FullName, Description);
        Translations.AddEnglish(SetKey, "Lava Worm Armour");
        Translations.AddEnglish($"{SetKey}_tooltip", "The whole set of Lava Worm armour. Fire hurts you much less.");
    }

    /// <summary>Prefab name of the piece.</summary>
    public abstract string Id { get; }

    /// <summary>The vanilla piece it is copied from.</summary>
    protected abstract string CopyFrom { get; }

    /// <summary>Name in English.</summary>
    protected abstract string FullName { get; }

    /// <summary>Description in English.</summary>
    protected abstract string Description { get; }

    /// <summary>Lava Worm Scales it takes, and half as many more for each upgrade.</summary>
    protected abstract int Scales { get; }

    /// <summary>Whether it is the piece whose icon the set's bonus shows.</summary>
    protected virtual bool ShowsSet => false;

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Ashlands;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(Translations.ItemKey(Id));

    /// <inheritdoc/>
    public string GatedPrefabName => Id;

    /// <summary>
    /// Adds the piece and its recipe at the Black Forge.
    /// </summary>
    public void Add()
    {
        try
        {
            CustomItem item = new(Id, CopyFrom, new ItemConfig
            {
                Name = NameToken,
                Description = Translations.Token($"{Translations.ItemKey(Id)}_description"),
                CraftingStation = CraftingStations.BlackForge,
                MinStationLevel = 1,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = LavaWormRegistry.ScaleName, Amount = Scales, AmountPerLevel = Scales / 2 },
                    new() { Item = "AskHide", Amount = 4, AmountPerLevel = 2 },
                    new() { Item = "FlametalNew", Amount = 4, AmountPerLevel = 2 }
                }
            });

            shared = item.ItemDrop.m_itemData.m_shared;
            shared.m_setName = SetKey;
            shared.m_setSize = 3;
            shared.m_setStatusEffect = setEffect ??= CreateSetEffect();
            ApplyConfig();
            if (!VisualHelper.IsHeadless)
            {
                try
                {
                    UniformLook.Recolor(item.ItemPrefab, shared, Ember);
                    UniformLook.RenderIcon(item.ItemPrefab, shared);
                }
                catch (Exception ex)
                {
                    Jotunn.Logger.LogWarning($"{FullName}: keeping the Ask armour's look: {ex.Message}");
                }
            }

            if (ShowsSet && shared.m_icons != null && shared.m_icons.Length > 0)
            {
                setEffect.m_icon = shared.m_icons[0];
            }

            ItemManager.Instance.AddItem(item);
            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    /// <summary>
    /// Applies changed or server-synced heat and fire resistance. The game counts a piece's heat resistance when it is
    /// put on.
    /// </summary>
    public void ApplyConfig()
    {
        if (shared != null)
        {
            shared.m_heatResistanceModifier = LavaWormSettings.ArmorHeatResistance.Value;
        }

        if (setEffect != null)
        {
            setEffect.m_mods = new List<HitData.DamageModPair>
            {
                new() { m_type = HitData.DamageType.Fire, m_modifier = LavaWormSettings.ArmorSetFire.Value }
            };
        }
    }

    private static SE_Stats CreateSetEffect()
    {
        SE_Stats effect = ScriptableObject.CreateInstance<SE_Stats>();
        effect.name = "SE_WhiteHilt_LavaWormArmor";
        effect.m_name = Translations.Token(SetKey);
        effect.m_tooltip = Translations.Token($"{SetKey}_tooltip");
        ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(effect, fixReference: false));
        return effect;
    }

    // Charcoal, with the brightest parts of the leather and metal turned to embers, like the worm's own hide.
    private static Color32 Ember(Color32 pixel)
    {
        Color.RGBToHSV(pixel, out _, out _, out float value);
        float heat = Mathf.SmoothStep(0f, 1f, (value - 0.6f) / 0.4f);
        Color charred = new(value * 0.32f, value * 0.27f, value * 0.25f);
        Color32 result = Color.Lerp(charred, new Color(1f, 0.42f, 0.08f), heat);
        result.a = pixel.a;
        return result;
    }
}
