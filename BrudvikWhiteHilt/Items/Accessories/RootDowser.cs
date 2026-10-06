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

/// <summary>A craftable Wishbone-style accessory that finds root-bearing plants.</summary>
public class RootDowser : IWhiteHiltCustomItem
{
    /// <summary>Prefab name of the Root Dowser.</summary>
    public const string PrefabName = "WhiteHiltRootDowser";
    private const string FullName = "Root Dowser";
    private const string EffectKey = "se_whitehiltrootdowser";
    private static readonly MethodInfo memberwiseClone = AccessTools.Method(typeof(object), "MemberwiseClone");
    private readonly ItemManager instance;

    /// <inheritdoc/>
    public bool Enabled => true;
    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Start;
    /// <inheritdoc/>
    public string Id => PrefabName;
    /// <inheritdoc/>
    public string DisplayName => FullName;
    /// <inheritdoc/>
    public string NameToken => Translations.Token(Translations.ItemKey(PrefabName));
    /// <inheritdoc/>
    public string GatedPrefabName => PrefabName;

    /// <summary>Registers the accessory's English texts.</summary>
    /// <param name="instance">Item manager used for registration.</param>
    public RootDowser(ItemManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(PrefabName), FullName,
            "A bone charm bound with resin and a greydwarf eye. Worn as an accessory, it pulses toward root-bearing plants, faster as you approach. Nearby roots glow green.");
        Translations.AddEnglish(EffectKey, FullName);
        Translations.AddEnglish($"{EffectKey}_tooltip", "Pulses toward the nearest unpicked root-bearing plant. Nearby roots glow green.");
    }

    /// <summary>Registers the accessory, equip effect and level-two workbench recipe.</summary>
    public void Add()
    {
        try
        {
            CustomItem dowser = new(PrefabName, "Wishbone", new ItemConfig
            {
                Name = NameToken,
                Description = Translations.Token($"{Translations.ItemKey(PrefabName)}_description"),
                CraftingStation = CraftingStations.Workbench,
                MinStationLevel = 2,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "BoneFragments", Amount = 10 },
                    new() { Item = "Wood", Amount = 5 },
                    new() { Item = "Resin", Amount = 5 },
                    new() { Item = "GreydwarfEye", Amount = 2 }
                }
            });

            ItemDrop.ItemData.SharedData shared = dowser.ItemDrop.m_itemData.m_shared;
            RootDowsingEffect effect = ScriptableObject.CreateInstance<RootDowsingEffect>();
            effect.name = "SE_WhiteHiltRootDowser";
            effect.m_name = Translations.Token(EffectKey);
            effect.m_tooltip = Translations.Token($"{EffectKey}_tooltip");
            if (shared.m_equipStatusEffect is SE_Finder finder)
            {
                effect.Ping = CreateSignal(finder.m_pingEffectNear);
                effect.m_icon = finder.m_icon;
            }
            else
            {
                Jotunn.Logger.LogWarning($"{FullName}: Wishbone finder effect missing; proximity sound unavailable");
            }

            shared.m_equipStatusEffect = effect;
            ApplyVisual(dowser);
            effect.m_icon = shared.m_icons.FirstOrDefault() ?? effect.m_icon;
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

    // A copy of the Wishbone's ping effects with their own prefabs: higher pitched and green, so the root dowser is
    // told apart from the Wishbone by ear and eye. The cloned prefabs are made once and reused.
    private static EffectList CreateSignal(EffectList source)
    {
        return new EffectList
        {
            m_effectPrefabs = (source?.m_effectPrefabs ?? Array.Empty<EffectList.EffectData>()).Select(data =>
            {
                EffectList.EffectData copy = (EffectList.EffectData)memberwiseClone.Invoke(data, null);
                if (data.m_prefab == null || VisualHelper.IsHeadless)
                {
                    return copy;
                }

                string name = $"{data.m_prefab.name}_whitehilt_root";
                GameObject signal = PrefabManager.Instance.GetPrefab(name);
                if (signal == null)
                {
                    signal = PrefabManager.Instance.CreateClonedPrefab(name, data.m_prefab);
                    foreach (ZSFX sound in signal.GetComponentsInChildren<ZSFX>(true))
                    {
                        sound.m_minPitch *= RootDowsingSettings.PingPitch.Value;
                        sound.m_maxPitch *= RootDowsingSettings.PingPitch.Value;
                    }

                    foreach (ParticleSystem particles in signal.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        ParticleSystem.MainModule main = particles.main;
                        main.startColor = Color.green;
                    }
                }

                copy.m_prefab = signal;
                return copy;
            }).ToArray()
        };
    }

    // Turns the cloned Wishbone green and renders an icon from it.
    private static void ApplyVisual(CustomItem dowser)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            VisualHelper.Recolor(dowser.ItemPrefab, pixel =>
                new Color32((byte)(pixel.r * 0.55f), pixel.g, (byte)(pixel.b * 0.55f), pixel.a));
            Sprite icon = VisualHelper.RenderIcon(dowser.ItemPrefab);
            ItemDrop.ItemData.SharedData shared = dowser.ItemDrop.m_itemData.m_shared;
            if (icon != null)
            {
                shared.m_icons = Enumerable.Repeat(icon, Mathf.Max(1, shared.m_icons.Length)).ToArray();
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: keeping the Wishbone's look: {ex.Message}");
        }
    }
}