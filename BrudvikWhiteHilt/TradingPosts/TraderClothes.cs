using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Armors.WhiteHiltUniform;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// The traders' clothes: for each biome a copy of a vanilla tunic, the troll leather trousers and sometimes a cape, dyed
/// in the biome's colours. Only traders wear them; they have no recipe, nobody sells them and no chest holds them, so
/// they exist only for the game's equipment display to find by name.
/// </summary>
public static class TraderClothes
{
    /// <summary>
    /// Registers the item names. Call from the plugin's Awake, before the game loads its languages.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglishNameAndDescription("item_whitehilt_tradertunic", "Trader's Tunic", "The tunic of a trader by the sea.");
        Translations.AddEnglishNameAndDescription("item_whitehilt_tradertrousers", "Trader's Trousers", "The trousers of a trader by the sea.");
        Translations.AddEnglishNameAndDescription("item_whitehilt_tradercape", "Trader's Cape", "The cape of a trader by the sea.");
    }

    /// <summary>
    /// Makes every biome's clothes. Call when the vanilla prefabs are available.
    /// </summary>
    public static void Create()
    {
        foreach (TraderLook look in TraderLooks.All)
        {
            Color main = TraderLook.ToColour(look.main);
            Color accent = TraderLook.ToColour(look.accent);
            Add(look.TunicPrefab, look.tunic, "item_whitehilt_tradertunic", Dye(main, accent, keepLeather: true));
            Add(look.TrousersPrefab, look.trousers, "item_whitehilt_tradertrousers", Dye(TraderLook.ToColour(look.legs), null, keepLeather: true));
            if (look.CapePrefab != null)
            {
                Add(look.CapePrefab, look.cape, "item_whitehilt_tradercape", Dye(TraderLook.ToColour(look.capeColour), accent, keepLeather: false));
            }
        }
    }

    /// <summary>
    /// Returns a dyeing of cloth: its colour becomes <paramref name="cloth"/>, keeping the grain's light and shade;
    /// yellow and orange trim becomes <paramref name="trim"/> (or stays); white embroidery stays, and with
    /// <paramref name="keepLeather"/> so do brown leather belts. The same rules as the preview's recolour.
    /// </summary>
    /// <param name="cloth">The cloth's new colour.</param>
    /// <param name="trim">The trim's new colour, or null to keep it.</param>
    /// <param name="keepLeather">Whether dark brown leather keeps its colour.</param>
    /// <returns>The dyeing.</returns>
    public static Func<Color32, Color32> Dye(Color cloth, Color? trim, bool keepLeather)
    {
        return pixel =>
        {
            Color.RGBToHSV(pixel, out float hue, out float saturation, out float value);
            Color result;
            if (hue > 0.07f && hue < 0.17f && saturation > 0.45f && value > 0.45f)
            {
                result = trim.HasValue ? trim.Value * Mathf.Clamp(value / 0.7f, 0.6f, 1.4f) : (Color)pixel;
            }
            else if (saturation < 0.18f && value > 0.7f)
            {
                result = pixel;
            }
            else if (keepLeather && hue > 0.02f && hue < 0.11f && value < 0.45f && saturation > 0.3f)
            {
                result = pixel;
            }
            else
            {
                result = cloth * Mathf.Clamp(value / 0.5f, 0.45f, 1.6f);
            }

            result.a = pixel.a / 255f;
            return result;
        };
    }

    // A copy of a vanilla piece of clothing under its own name, dyed. On a dedicated server, which draws nothing, the
    // copy keeps the vanilla colours; a failing dye keeps them too.
    private static void Add(string prefabName, string source, string nameKey, Func<Color32, Color32> dye)
    {
        try
        {
            ItemConfig config = new()
            {
                Name = Translations.Token(nameKey),
                Description = Translations.Token(nameKey + "_description")
            };
            CustomItem item = new(prefabName, source, config);
            ItemDrop.ItemData.SharedData shared = item.ItemDrop.m_itemData.m_shared;
            shared.m_value = 0;
            if (!VisualHelper.IsHeadless)
            {
                try
                {
                    UniformLook.Recolor(item.ItemPrefab, shared, dye);
                }
                catch (Exception ex)
                {
                    Jotunn.Logger.LogWarning($"{prefabName}: not dyed: {ex.Message}");
                }
            }

            ItemManager.Instance.AddItem(item);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{prefabName} failed to load: {ex.Message}");
        }
    }
}
