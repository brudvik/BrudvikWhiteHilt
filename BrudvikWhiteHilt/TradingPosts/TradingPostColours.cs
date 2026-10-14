using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// Gives a trading post's market stalls and pennant the colours of its biome, as the preview pictures show: the
/// awning's red stripes become the biome trader's cloth colour, the white stays. The stalls are the Decor Hammer's, so
/// their prefab is shared with stalls players build; only the post's own get the biome materials, on each machine
/// that draws them.
/// </summary>
public static class TradingPostColours
{
    /// <summary>Prefab name of the market stall.</summary>
    public const string StallPrefab = "piece_whitehilt_decor_market_stall";

    /// <summary>Prefab name of the flag pole.</summary>
    public const string FlagPrefab = "piece_whitehilt_flaggstang";

    // The awning takes up the right quarter of the stall's texture (see build_traders.py awning_texture).
    private const float AwningStart = 0.75f;

    private static readonly Dictionary<(Material, string), Material> awnings = new();
    private static readonly Dictionary<string, Texture2D> pennants = new();

    /// <summary>
    /// Colours one piece of a post, if it is a stall or the flag pole.
    /// </summary>
    /// <param name="piece">The piece.</param>
    /// <param name="look">The post's trader, whose cloth colour is used.</param>
    public static void Apply(ZNetView piece, TraderLook look)
    {
        if (VisualHelper.IsHeadless || piece == null)
        {
            return;
        }

        string prefab = global::Utils.GetPrefabName(piece.gameObject);
        if (prefab == StallPrefab)
        {
            foreach (Renderer renderer in piece.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.sharedMaterial != null && renderer.sharedMaterial.mainTexture != null && !renderer.sharedMaterial.name.EndsWith(look.biome))
                {
                    renderer.sharedMaterial = Awning(renderer.sharedMaterial, look);
                }
            }
        }
        else if (prefab == FlagPrefab)
        {
            foreach (Renderer renderer in piece.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.gameObject.name == "pennant")
                {
                    renderer.material.mainTexture = Pennant(look);
                }
            }
        }
    }

    // A copy of the stall's material whose awning stripes are dyed, made once per biome.
    private static Material Awning(Material source, TraderLook look)
    {
        if (awnings.TryGetValue((source, look.biome), out Material cached))
        {
            return cached;
        }

        Texture texture = source.mainTexture;
        Color32[] pixels = VisualHelper.ReadPixels(texture, texture.width, texture.height);
        Color cloth = TraderLook.ToColour(look.main);
        int start = Mathf.FloorToInt(texture.width * AwningStart);
        for (int i = 0; i < pixels.Length; i++)
        {
            if (i % texture.width < start)
            {
                continue;
            }

            Color.RGBToHSV(pixels[i], out float hue, out float saturation, out float value);
            if ((hue < 0.06f || hue > 0.94f) && saturation > 0.3f)
            {
                Color dyed = cloth * Mathf.Clamp(value / 0.55f, 0.5f, 1.5f);
                pixels[i] = new Color(dyed.r, dyed.g, dyed.b, pixels[i].a / 255f);
            }
        }

        Material material = new(source)
        {
            name = $"{source.name}_{look.biome}",
            mainTexture = VisualHelper.CreateTexture($"{texture.name}_{look.biome}", texture.width, texture.height, pixels, texture)
        };
        awnings[(source, look.biome)] = material;
        return material;
    }

    private static Texture2D Pennant(TraderLook look)
    {
        if (!pennants.TryGetValue(look.biome, out Texture2D texture))
        {
            texture = TradingPostPennant.CreateTexture(TraderLook.ToColour(look.main));
            pennants[look.biome] = texture;
        }

        return texture;
    }
}
