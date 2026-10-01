using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Navigation.Dowsing;
using BrudvikWhiteHilt.Progression;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Accessories;

/// <summary>
/// The Stone Dowser: a Wishbone in grey stone, cut at the stonecutter and worn as an accessory. It leads to the nearest
/// clearing that still has rocks for the Mysterious Rock and pings toward each rock nearby (<see cref="StoneDowsingEffect"/>).
/// </summary>
public class StoneDowser : IWhiteHiltCustomItem
{
    /// <summary>
    /// Prefab name of the dowser.
    /// </summary>
    public const string PrefabName = "WhiteHiltStoneDowser";

    private const string FullName = "Stone Dowser";
    private const string Description = "A wishbone cut from grey stone. Worn as an accessory, it tugs toward the nearest clearing where rocks still lie, the kind a Mysterious Rock is made from, and pings ever faster as you come close to one.";
    private const string CopyFrom = "Wishbone";
    private const string EffectKey = "se_whitehiltstonedowser";

    // Lower than the Wishbone's ping, so the two can be told apart when both are worn.
    private const float PingPitch = 0.7f;

    private static readonly MethodInfo memberwiseClone = AccessTools.Method(typeof(object), "MemberwiseClone");

    private readonly ItemManager instance;

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
    /// Constructor for the StoneDowser class. Registers the English text.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public StoneDowser(ItemManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(PrefabName), FullName, Description);
        Translations.AddEnglish(EffectKey, "Stone Dowser");
        Translations.AddEnglish($"{EffectKey}_tooltip", "Leads you to the nearest clearing with rocks, and pings toward each rock nearby.");
    }

    /// <summary>
    /// Adds the dowser and its recipe at the stonecutter.
    /// </summary>
    public void Add()
    {
        try
        {
            CustomItem dowser = new(PrefabName, CopyFrom, new ItemConfig
            {
                Name = NameToken,
                Description = Translations.Token($"{Translations.ItemKey(PrefabName)}_description"),
                CraftingStation = CraftingStations.Stonecutter,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "Stone", Amount = 20 },
                    new() { Item = "Iron", Amount = 2 },
                    new() { Item = "GreydwarfEye", Amount = 5 }
                }
            });

            ItemDrop.ItemData.SharedData shared = dowser.ItemDrop.m_itemData.m_shared;
            StoneDowsingEffect effect = ScriptableObject.CreateInstance<StoneDowsingEffect>();
            effect.name = "SE_WhiteHiltStoneDowser";
            effect.m_name = Translations.Token(EffectKey);
            effect.m_tooltip = Translations.Token($"{EffectKey}_tooltip");
            if (shared.m_equipStatusEffect is SE_Finder finder)
            {
                effect.PingNear = LowerPitch(finder.m_pingEffectNear);
                effect.PingMedium = LowerPitch(finder.m_pingEffectMed);
                effect.PingFar = LowerPitch(finder.m_pingEffectFar);
                effect.CloseInterval = finder.m_closeFrequency;
                effect.DistantInterval = finder.m_distantFrequency;
                effect.m_icon = finder.m_icon;
            }
            else
            {
                Jotunn.Logger.LogWarning($"{FullName}: the {CopyFrom} has no finder effect to copy, the dowser makes no sound");
            }

            shared.m_equipStatusEffect = effect;
            Sprite icon = TryApplyVisual(dowser);
            effect.m_icon = icon ?? effect.m_icon ?? shared.m_icons.FirstOrDefault();

            instance.AddStatusEffect(new CustomStatusEffect(effect, fixReference: false));
            instance.AddItem(dowser);
            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    // A copy of the Wishbone's ping with its sounds pitched down.
    private static EffectList LowerPitch(EffectList source)
    {
        return new EffectList
        {
            m_effectPrefabs = (source?.m_effectPrefabs ?? Array.Empty<EffectList.EffectData>()).Select(LowerPitch).ToArray()
        };
    }

    private static EffectList.EffectData LowerPitch(EffectList.EffectData source)
    {
        EffectList.EffectData copy = (EffectList.EffectData)memberwiseClone.Invoke(source, null);
        if (source.m_prefab == null || source.m_prefab.GetComponentInChildren<ZSFX>(true) == null)
        {
            return copy;
        }

        string name = $"{source.m_prefab.name}_whitehilt_stone";
        GameObject sound = PrefabManager.Instance.GetPrefab(name);
        if (sound == null)
        {
            sound = PrefabManager.Instance.CreateClonedPrefab(name, source.m_prefab);
            foreach (ZSFX sfx in sound.GetComponentsInChildren<ZSFX>(true))
            {
                sfx.m_minPitch *= PingPitch;
                sfx.m_maxPitch *= PingPitch;
            }
        }

        copy.m_prefab = sound;
        return copy;
    }

    // Turns the bone grey like stone, and renders a new icon.
    private static Sprite TryApplyVisual(CustomItem dowser)
    {
        if (VisualHelper.IsHeadless)
        {
            return null;
        }

        try
        {
            VisualHelper.Recolor(dowser.ItemPrefab, pixel =>
            {
                byte grey = (byte)Mathf.Clamp((0.3f * pixel.r + 0.59f * pixel.g + 0.11f * pixel.b) * 0.6f, 0f, 255f);
                return new Color32(grey, grey, (byte)Mathf.Min(255, grey + 6), pixel.a);
            });

            Sprite icon = VisualHelper.RenderIcon(dowser.ItemPrefab);
            ItemDrop.ItemData.SharedData shared = dowser.ItemDrop.m_itemData.m_shared;
            if (icon != null)
            {
                // Same length as before: an item's variant indexes this array.
                shared.m_icons = Enumerable.Repeat(icon, Mathf.Max(1, shared.m_icons.Length)).ToArray();
            }

            return icon;
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: keeping the Wishbone's look: {ex.Message}");
            return null;
        }
    }
}
