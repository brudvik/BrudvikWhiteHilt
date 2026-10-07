using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Painting;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Runes;

/// <summary>
/// The runes that are given a colour at the Paint Bench before they are etched: the Glow Rune and the Flame Rune. Each
/// rune keeps its own colour on the item, so they do not stack.
/// </summary>
public static class ColourRunes
{
    // The name it had when only the Glow Rune existed; kept, so runes already coloured keep their colour.
    private const string ColorKey = "whitehilt_glow_rune_color";
    private const float SignIntensity = 1.5f;

    private static readonly Dictionary<Texture2D, Texture2D> greySigns = new();

    /// <summary>
    /// Lights a rune stone's carved sign: its emission map is made grey (the downloaded signs are coloured), so the
    /// sign glows in the given colour.
    /// </summary>
    /// <param name="stone">The stone model.</param>
    /// <param name="sign">The model's emission map.</param>
    /// <param name="color">The colour the sign glows in.</param>
    public static void LightSign(GameObject stone, Texture2D sign, Color color)
    {
        Material material = stone.GetComponent<MeshRenderer>()?.sharedMaterial;
        if (material == null || !material.HasProperty("_EmissionMap"))
        {
            return;
        }

        if (!greySigns.TryGetValue(sign, out Texture2D grey))
        {
            grey = VisualHelper.RecolorTexture(sign, pixel =>
            {
                byte level = (byte)Mathf.Max(pixel.r, pixel.g, pixel.b);
                return new Color32(level, level, level, 255);
            });
            greySigns[sign] = grey;
        }

        material.EnableKeyword("_EMISSION");
        material.SetTexture("_EmissionMap", grey);
        material.SetColor("_EmissionColor", color * SignIntensity);
    }

    /// <summary>
    /// Lights the sign of a coloured rune lying on the ground in its own colour.
    /// </summary>
    /// <param name="item">The dropped item.</param>
    /// <param name="prefabName">The rune's prefab name.</param>
    public static void ApplyDropped(ItemDrop item, string prefabName)
    {
        if (!TryGetColor(item.m_itemData, prefabName, out Color32 color))
        {
            return;
        }

        foreach (MeshRenderer renderer in item.GetComponentsInChildren<MeshRenderer>())
        {
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.IsKeywordEnabled("_EMISSION"))
            {
                renderer.material.SetColor("_EmissionColor", (Color)color * SignIntensity);
            }
        }
    }

    /// <summary>
    /// Registers the English texts.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("whitehilt_glow_rune_colour", "Colour: {0}");
        Translations.AddEnglish("whitehilt_glow_rune_blank", "Not yet coloured: colour it at the Paint Bench");
    }

    /// <summary>
    /// Whether an item is a rune of a kind.
    /// </summary>
    /// <param name="item">The item, or null.</param>
    /// <param name="prefabName">The rune's prefab name.</param>
    /// <returns>True for that rune.</returns>
    public static bool Is(ItemDrop.ItemData item, string prefabName)
    {
        return item?.m_dropPrefab != null && item.m_dropPrefab.name == prefabName;
    }

    /// <summary>
    /// The colour of a colour rune.
    /// </summary>
    /// <param name="item">The rune.</param>
    /// <param name="prefabName">The rune's prefab name.</param>
    /// <param name="color">Its colour.</param>
    /// <returns>False if it is not a coloured rune of that kind.</returns>
    public static bool TryGetColor(ItemDrop.ItemData item, string prefabName, out Color32 color)
    {
        color = default;
        return Is(item, prefabName) && item.m_customData.TryGetValue(ColorKey, out string hex) && PaintColor.TryParseHex(hex, out color);
    }

    /// <summary>
    /// The rune of a kind in the player's inventory to colour next: an uncoloured one first.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="prefabName">The rune's prefab name.</param>
    /// <returns>The rune, or null if the player has none.</returns>
    public static ItemDrop.ItemData FindToColour(Player player, string prefabName)
    {
        ItemDrop.ItemData[] runes = player.GetInventory().GetAllItems().Where(item => Is(item, prefabName)).ToArray();
        return runes.FirstOrDefault(rune => !TryGetColor(rune, prefabName, out _)) ?? runes.FirstOrDefault();
    }

    /// <summary>
    /// Gives a rune a colour.
    /// </summary>
    /// <param name="item">The rune.</param>
    /// <param name="color">The colour.</param>
    public static void SetColor(ItemDrop.ItemData item, Color32 color)
    {
        item.m_customData[ColorKey] = PaintColor.ToHex(color);
    }

    /// <summary>
    /// The tooltip line of a colour rune: its colour, or how to give it one.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="prefabName">The rune's prefab name.</param>
    /// <returns>The line, or nothing for other items.</returns>
    public static string TooltipText(ItemDrop.ItemData item, string prefabName)
    {
        if (!Is(item, prefabName))
        {
            return string.Empty;
        }

        Localization localization = Localization.instance;
        return "\n" + (TryGetColor(item, prefabName, out Color32 color)
            ? string.Format(localization.Localize("$whitehilt_glow_rune_colour"), PaintColor.Swatch(color))
            : "<color=orange>" + localization.Localize("$whitehilt_glow_rune_blank") + "</color>");
    }
}
