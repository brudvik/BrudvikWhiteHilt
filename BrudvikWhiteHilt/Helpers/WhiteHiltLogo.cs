using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Helpers;

/// <summary>
/// The Brudvik White Hilt logo, embedded in the DLL, for painting on textures and showing in the UI.
/// </summary>
public static class WhiteHiltLogo
{
    private const string ResourceName = "BrudvikWhiteHilt.Assets.Logo.WhiteHiltLogo.png";

    private static Texture2D texture;
    private static Sprite sprite;

    /// <summary>
    /// The logo as a CPU-readable texture with a transparent background.
    /// </summary>
    public static Texture2D Texture
    {
        get
        {
            if (texture == null)
            {
                texture = AssetUtilsExtended.LoadTextureFromEmbeddedResource(ResourceName);
                texture.name = "whitehilt_logo";
                texture.wrapMode = TextureWrapMode.Clamp;
            }

            return texture;
        }
    }

    /// <summary>
    /// The logo as a UI sprite.
    /// </summary>
    public static Sprite Sprite
    {
        get
        {
            if (sprite == null)
            {
                Texture2D logo = Texture;
                sprite = Sprite.Create(logo, new Rect(0, 0, logo.width, logo.height), new Vector2(0.5f, 0.5f));
                sprite.name = "whitehilt_logo";
            }

            return sprite;
        }
    }

    /// <summary>
    /// Paints the logo into an ellipse of a pixel array, over what is there.
    /// </summary>
    /// <param name="pixels">Pixels, <paramref name="width"/> per row, bottom to top.</param>
    /// <param name="width">Width of the pixel array.</param>
    /// <param name="height">Height of the pixel array.</param>
    /// <param name="center">Centre of the logo in texture coordinates (0 to 1).</param>
    /// <param name="radius">Half the logo's width and height in texture coordinates.</param>
    /// <param name="shade">Multiplier for a logo pixel from the colour below it, to keep folds and grain; null for none.</param>
    public static void Paint(Color32[] pixels, int width, int height, Vector2 center, Vector2 radius, Func<Color32, float> shade = null)
    {
        Texture2D logo = Texture;
        int minX = Mathf.Max(0, Mathf.FloorToInt((center.x - radius.x) * width));
        int maxX = Mathf.Min(width - 1, Mathf.CeilToInt((center.x + radius.x) * width));
        int minY = Mathf.Max(0, Mathf.FloorToInt((center.y - radius.y) * height));
        int maxY = Mathf.Min(height - 1, Mathf.CeilToInt((center.y + radius.y) * height));
        for (int y = minY; y <= maxY; y++)
        {
            float logoY = 0.5f + ((y + 0.5f) / height - center.y) / (2f * radius.y);
            for (int x = minX; x <= maxX; x++)
            {
                float logoX = 0.5f + ((x + 0.5f) / width - center.x) / (2f * radius.x);
                if (logoX < 0f || logoX > 1f || logoY < 0f || logoY > 1f)
                {
                    continue;
                }

                Color color = logo.GetPixelBilinear(logoX, logoY);
                if (color.a <= 0f)
                {
                    continue;
                }

                int i = y * width + x;
                Color below = pixels[i];
                Color painted = shade != null ? color * shade(pixels[i]) : color;
                Color result = Color.Lerp(below, painted, color.a);
                result.a = below.a;
                pixels[i] = result;
            }
        }
    }
}
