using BrudvikWhiteHilt.Clock;
using BrudvikWhiteHilt.Navigation.Compass;
using BrudvikWhiteHilt.Navigation.Waypoints;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.HudLayout;

/// <summary>
/// What sits behind the top of the White Hilt HUD, so the compass, clock and waypoint read against a bright sky: a
/// half dark band with rounded corners round the three of them, as the panel behind the console, or a soft darkening
/// from the top edge of the screen down, or nothing.
/// </summary>
public enum TopBackground
{
    /// <summary>One rounded band behind the clock, the compass and the waypoint.</summary>
    Band,

    /// <summary>The top of the screen darkened, fading out downwards.</summary>
    Fade,

    /// <summary>Nothing behind them.</summary>
    None,
}

/// <summary>
/// Draws the background chosen in <see cref="HudSettings.Top"/> behind the top of the White Hilt HUD.
/// </summary>
public static class HudTop
{
    private const float Padding = 8f;
    private const int FadeTexels = 64;

    private static readonly Color shade = new(0.03f, 0.04f, 0.04f, 1f);

    private static RectTransform root;
    private static Image band;
    private static Image fade;
    private static Sprite fadeSprite;

    /// <summary>
    /// Whether the band is behind the compass now, so the compass leaves out its own faint background.
    /// </summary>
    public static bool BandShown => band != null && band.gameObject.activeInHierarchy;

    /// <summary>
    /// Places the background. Called after the HUD's late update.
    /// </summary>
    /// <param name="hud">The HUD.</param>
    public static void Update(Hud hud)
    {
        TopBackground kind = HudSettings.WhiteHilt && Player.m_localPlayer != null && hud.m_rootObject != null
            && (Minimap.instance == null || Minimap.instance.m_mode != Minimap.MapMode.Large)
            ? HudSettings.Top.Value
            : TopBackground.None;
        if (kind == TopBackground.None && root == null)
        {
            return;
        }

        Ensure(hud);
        float opacity = Mathf.Clamp01(HudSettings.TopOpacity.Value);
        SetActive(band, kind == TopBackground.Band && PlaceBand());
        SetActive(fade, kind == TopBackground.Fade);
        band.color = new Color(shade.r, shade.g, shade.b, opacity);
        fade.color = new Color(shade.r, shade.g, shade.b, opacity);
        fade.rectTransform.sizeDelta = new Vector2(0f, Mathf.Max(20f, HudSettings.TopFadeHeight.Value));
    }

    // Round whatever of the compass, clock and waypoint shows; false when none does.
    private static bool PlaceBand()
    {
        // Without the compass the clock and waypoint go back to their own corners, too far apart for one band.
        if (HudCompass.Tape == null)
        {
            return false;
        }

        bool any = false;
        Rect around = default;
        Include(HudCompass.Tape, ref around, ref any);
        Include(GameClock.Shown, ref around, ref any);
        if (WaypointHud.Bounds(root, out Rect waypoint))
        {
            around = any ? Rect.MinMaxRect(Mathf.Min(around.xMin, waypoint.xMin), Mathf.Min(around.yMin, waypoint.yMin),
                Mathf.Max(around.xMax, waypoint.xMax), Mathf.Max(around.yMax, waypoint.yMax)) : waypoint;
            any = true;
        }

        if (!any)
        {
            return false;
        }

        float radius = Mathf.Max(0f, HudSettings.PanelRadius.Value);
        band.rectTransform.anchoredPosition = new Vector2(around.xMin - Padding, around.yMin - Padding);
        band.rectTransform.sizeDelta = new Vector2(around.width + 2f * Padding, around.height + 2f * Padding);
        band.pixelsPerUnitMultiplier = HudSprites.RoundedBoxCorner / Mathf.Max(1f, radius);
        return true;
    }

    private static void Include(RectTransform rect, ref Rect around, ref bool any)
    {
        if (rect == null || !VanillaPlacement.Bounds(root, new[] { rect }, out Rect box))
        {
            return;
        }

        around = any ? Rect.MinMaxRect(Mathf.Min(around.xMin, box.xMin), Mathf.Min(around.yMin, box.yMin),
            Mathf.Max(around.xMax, box.xMax), Mathf.Max(around.yMax, box.yMax)) : box;
        any = true;
    }

    private static void SetActive(Image image, bool active)
    {
        if (image.gameObject.activeSelf != active)
        {
            image.gameObject.SetActive(active);
        }
    }

    // Under the HUD root, behind everything on it, so it goes away with the HUD; measured from the lower left corner.
    private static void Ensure(Hud hud)
    {
        if (root != null && root.parent == hud.m_rootObject.transform)
        {
            return;
        }

        root = (RectTransform)new GameObject("WhiteHiltTopBackground", typeof(RectTransform)).transform;
        root.SetParent(hud.m_rootObject.transform, false);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.pivot = Vector2.zero;
        root.sizeDelta = Vector2.zero;
        root.SetAsFirstSibling();

        band = new GameObject("Band", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        band.rectTransform.SetParent(root, false);
        band.rectTransform.anchorMin = band.rectTransform.anchorMax = band.rectTransform.pivot = Vector2.zero;
        band.sprite = HudSprites.RoundedBox;
        band.type = Image.Type.Sliced;
        band.raycastTarget = false;

        fade = new GameObject("Fade", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        fade.rectTransform.SetParent(root, false);
        fade.rectTransform.anchorMin = new Vector2(0f, 1f);
        fade.rectTransform.anchorMax = Vector2.one;
        fade.rectTransform.pivot = new Vector2(0.5f, 1f);
        fade.rectTransform.anchoredPosition = Vector2.zero;
        fade.sprite = FadeSprite();
        fade.raycastTarget = false;
    }

    // Full at the top edge, still well over half at half the height, then out to nothing.
    private static Sprite FadeSprite()
    {
        if (fadeSprite != null)
        {
            return fadeSprite;
        }

        Texture2D texture = new(1, FadeTexels, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < FadeTexels; y++)
        {
            float down = 1f - (y + 0.5f) / FadeTexels;
            float alpha = down < 0.55f ? Mathf.Lerp(1f, 0.6f, down / 0.55f) : Mathf.Lerp(0.6f, 0f, (down - 0.55f) / 0.45f);
            texture.SetPixel(0, y, new Color(1f, 1f, 1f, alpha));
        }

        texture.Apply();
        fadeSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, FadeTexels), new Vector2(0.5f, 0.5f));
        return fadeSprite;
    }
}
