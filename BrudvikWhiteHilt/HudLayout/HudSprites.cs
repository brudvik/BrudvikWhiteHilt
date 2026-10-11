using UnityEngine;

namespace BrudvikWhiteHilt.HudLayout;

/// <summary>
/// The round shapes the console draws with, made once in code: a soft-edged disc behind icons, and a ring that fills
/// round as a countdown.
/// </summary>
internal static class HudSprites
{
    private const int Size = 128;
    private const float RingWidth = 9f;

    private static Sprite disc;
    private static Sprite ring;
    private static Sprite roundedBox;
    private static Sprite wideRing;

    /// <summary>Pixels of a corner of <see cref="RoundedBox"/>, its border for slicing.</summary>
    internal const int RoundedBoxCorner = 32;

    /// <summary>A white box with round corners, for an image of the Sliced type.</summary>
    internal static Sprite RoundedBox => roundedBox != null ? roundedBox : roundedBox = MakeRoundedBox();

    /// <summary>A filled disc.</summary>
    internal static Sprite Disc => disc != null ? disc : disc = Make(0f);

    /// <summary>A wide ring, its hole 84 % of its width, for the round minimap's compass ring.</summary>
    internal static Sprite WideRing => wideRing != null ? wideRing : wideRing = Make(Size / 2f * 0.84f);

    /// <summary>A ring, for an image of the Filled type.</summary>
    internal static Sprite Ring => ring != null ? ring : ring = Make(Size / 2f - RingWidth);

    // A box twice the corner across, each corner a quarter circle, sliced so only the corners keep their size.
    private static Sprite MakeRoundedBox()
    {
        int size = RoundedBoxCorner * 2 + 2;
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        float corner = RoundedBoxCorner;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(0f, Mathf.Max(corner - (x + 0.5f), x + 0.5f - (size - corner)));
                float dy = Mathf.Max(0f, Mathf.Max(corner - (y + 0.5f), y + 0.5f - (size - corner)));
                float alpha = Mathf.Clamp01(corner - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
            new Vector4(corner, corner, corner, corner));
    }

    // White with an antialiased edge, transparent inside the inner radius.
    private static Sprite Make(float inner)
    {
        Texture2D texture = new(Size, Size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        float outer = Size / 2f;
        Vector2 centre = new(outer, outer);
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centre);
                float alpha = Mathf.Clamp01(outer - distance) * (inner > 0f ? Mathf.Clamp01(distance - inner) : 1f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f));
    }
}
