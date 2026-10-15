using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// The view closing in: a dark (or sandy, or snow-white) ring from the edges of the screen, a wash of the storm's colour
/// over it, and the white flash of lightning. Under the HUD, so the bars and the hotbar stay readable.
/// </summary>
public static class StormVision
{
    private const int TextureSize = 256;
    private const float FlashFade = 3.5f;

    private static RectTransform root;
    private static Image ring;
    private static Image wash;
    private static Image flash;
    private static Sprite ringSprite;
    private static float flashAlpha;

    /// <summary>
    /// Shows how far the view has closed in.
    /// </summary>
    /// <param name="vision">0 for a clear view, 1 for the worst.</param>
    /// <param name="tint">The storm's colour.</param>
    public static void Show(float vision, Color tint)
    {
        flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, Time.deltaTime * FlashFade);
        if ((vision <= 0.001f && flashAlpha <= 0.001f) || !Ensure())
        {
            if (root != null && root.gameObject.activeSelf)
            {
                root.gameObject.SetActive(false);
            }

            return;
        }

        if (!root.gameObject.activeSelf)
        {
            root.gameObject.SetActive(true);
        }

        // The ring is scaled up when the view is still wide, so its dark edge lies off the screen.
        float scale = Mathf.Lerp(2.4f, 1f, vision);
        ring.rectTransform.localScale = new Vector3(scale, scale, 1f);
        ring.color = new Color(tint.r, tint.g, tint.b, Mathf.Clamp01(vision * 1.1f));
        wash.color = new Color(tint.r, tint.g, tint.b, vision * 0.3f);
        flash.color = new Color(1f, 1f, 1f, flashAlpha);
    }

    /// <summary>
    /// A flash of lightning.
    /// </summary>
    /// <param name="strength">0 to 1.</param>
    public static void Flash(float strength)
    {
        flashAlpha = Mathf.Max(flashAlpha, Mathf.Clamp01(strength) * 0.45f);
    }

    private static bool Ensure()
    {
        if (root != null)
        {
            return true;
        }

        Transform parent = Hud.instance != null && Hud.instance.m_rootObject != null ? Hud.instance.m_rootObject.transform : null;
        if (parent == null)
        {
            return false;
        }

        GameObject holder = new("WhiteHiltStormVision", typeof(RectTransform));
        root = (RectTransform)holder.transform;
        root.SetParent(parent, false);
        root.SetAsFirstSibling();
        Stretch(root);
        wash = Layer("Wash", null);
        ring = Layer("Ring", RingSprite());
        flash = Layer("Flash", null);
        return true;
    }

    private static Image Layer(string name, Sprite sprite)
    {
        GameObject layer = new(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)layer.transform;
        rect.SetParent(root, false);
        Stretch(rect);
        Image image = layer.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        image.color = Color.clear;
        return image;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // Clear in the middle, opaque at the corners.
    private static Sprite RingSprite()
    {
        if (ringSprite != null)
        {
            return ringSprite;
        }

        Texture2D texture = new(TextureSize, TextureSize, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        Color[] pixels = new Color[TextureSize * TextureSize];
        float half = (TextureSize - 1) / 2f;
        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float distance = new Vector2((x - half) / half, (y - half) / half).magnitude / Mathf.Sqrt(2f);
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 0.85f, distance));
                pixels[y * TextureSize + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        ringSprite = Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize), new Vector2(0.5f, 0.5f));
        return ringSprite;
    }
}
