using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Summoning;

/// <summary>A black-trimmed horn that calls the sea or the biome's black beasts.</summary>
public class SummoningHorn : IWhiteHiltCustomItem
{
    private readonly ItemManager manager;

    /// <inheritdoc/>
    public bool Enabled => true;
    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Start;
    /// <inheritdoc/>
    public string Id => SummoningHornService.HornName;
    /// <inheritdoc/>
    public string DisplayName => "Horn of the Deep";
    /// <inheritdoc/>
    public string NameToken => Translations.Token(Translations.ItemKey(Id));
    /// <inheritdoc/>
    public string GatedPrefabName => Id;

    /// <summary>Registers the horn's text for item discovery.</summary>
    /// <param name="manager">The item manager.</param>
    public SummoningHorn(ItemManager manager)
    {
        this.manager = manager;
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(Id), DisplayName,
            "A horn bound in black iron. Its deep note calls Kraken from deep ocean beside your ship, or a black beast on land. Their guardian boss must first be defeated.");
        Translations.AddEnglish("whitehilt_horn_wait", "The horn must rest before another call.");
        Translations.AddEnglish("whitehilt_horn_nearby", "A beast already answers nearby.");
        Translations.AddEnglish("whitehilt_horn_ship", "Board a ship over deep ocean to call Kraken.");
        Translations.AddEnglish("whitehilt_horn_locked", "Defeat this creature's guardian boss first.");
        Translations.AddEnglish("whitehilt_horn_empty", "Nothing answers the horn in this biome.");
        Translations.AddEnglish("whitehilt_horn_answer", "Something answers the horn.");
        Translations.AddEnglish("whitehilt_horn_space", "The creature cannot rise here. Find more open ground or deeper water.");
    }

    /// <summary>Clones the celebration horn and registers its forge recipe.</summary>
    public void Add()
    {
        try
        {
            CustomItem item = new(Id, "TankardAnniversary", new ItemConfig
            {
                Name = NameToken,
                Description = Translations.Token(Translations.ItemKey(Id) + "_description"),
                CraftingStation = CraftingStations.Forge,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "Iron", Amount = 4 },
                    new() { Item = "FineWood", Amount = 6 },
                    new() { Item = "Coal", Amount = 4 },
                    new() { Item = "TrophyDeer", Amount = 1 }
                }
            });
            var shared = item.ItemDrop.m_itemData.m_shared;
            shared.m_ammoType = "";
            shared.m_useDurability = false;
            shared.m_attack = new Attack();
            shared.m_secondaryAttack = new Attack();
            shared.m_startEffect = new EffectList();
            shared.m_triggerEffect = new EffectList();
            if (!VisualHelper.IsHeadless)
            {
                try
                {
                    VisualHelper.Recolor(item.ItemPrefab, BlackTrim);
                    Sprite icon = VisualHelper.RenderIcon(item.ItemPrefab);
                    if (icon != null)
                        shared.m_icons = Enumerable.Repeat(icon, shared.m_icons.Length).ToArray();
                    HornCaller.LoadSound();
                }
                catch (Exception exception)
                {
                    Jotunn.Logger.LogWarning($"Horn appearance or audio: {exception.Message}");
                }
            }
            manager.AddItem(item);
            Jotunn.Logger.LogInfo($"{DisplayName} added!");
        }
        catch (Exception exception)
        {
            Jotunn.Logger.LogError($"{DisplayName} failed to load!");
            Jotunn.Logger.LogError(exception);
        }
    }

    private static Color32 BlackTrim(Color32 pixel)
    {
        Color colour = pixel;
        Color.RGBToHSV(colour, out float hue, out float saturation, out float value);
        if (saturation > 0.5f && value > 0.2f)
        {
            byte shade = (byte)(20f + value * 25f);
            return new Color32(shade, shade, shade, pixel.a);
        }
        return pixel;
    }
}