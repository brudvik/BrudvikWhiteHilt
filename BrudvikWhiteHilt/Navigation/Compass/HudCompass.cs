using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Navigation.Compass;

/// <summary>A camera-relative compass tape under the game's HUD root.</summary>
public static class HudCompass
{
    private static readonly string[] directions = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
    private static readonly Color ink = new(0.93f, 0.87f, 0.69f, 1f);
    private static readonly List<Tick> ticks = new();
    private static RectTransform root;
    private static RectTransform tickContainer;
    private static Image background;
    private static Image indicator;
    private static CanvasGroup group;
    private static int tickInterval;
    private static int mediumInterval;
    private static int numberInterval;
    private static int majorInterval;
    private static bool showDegrees;
    private static bool showDirections;

    /// <summary>Space reserved above other top-centre HUD elements when the compass is visible.</summary>
    public static float ReservedHeight => ShouldShow()
        ? HudCompassSettings.VerticalPosition.Value + HudCompassSettings.CompassHeight.Value * HudCompassSettings.Scale.Value + HudCompassSettings.LayoutGap.Value
        : 0f;

    /// <summary>Whether the compass should be visible in the current game state.</summary>
    /// <returns>True for a living local player with an unobstructed, visible HUD.</returns>
    public static bool ShouldShow()
    {
        Player player = Player.m_localPlayer;
        return HudCompassSettings.Enabled != null && HudCompassSettings.Enabled.Value && HudCompassSettings.Opacity.Value > 0f
            && player != null && !player.IsDead() && Hud.instance != null && Hud.instance.m_rootObject != null
            && Hud.instance.m_rootObject.activeInHierarchy && !Hud.IsUserHidden() && Utils.GetMainCamera() != null
            && !InventoryGui.IsVisible() && !Menu.IsVisible() && !TextInput.IsVisible() && !StoreGui.IsVisible()
            && (Minimap.instance == null || Minimap.instance.m_mode != Minimap.MapMode.Large);
    }

    /// <summary>Updates the tape after the game camera has finished its late update.</summary>
    public static void Update()
    {
        bool visible = ShouldShow();
        if (!visible)
        {
            if (root != null && root.gameObject.activeSelf) root.gameObject.SetActive(false);
            HudCompassLayout.Update();
            return;
        }

        Ensure();
        if (!root.gameObject.activeSelf) root.gameObject.SetActive(true);
        float scale = HudCompassSettings.Scale.Value;
        float available = ((RectTransform)root.parent).rect.width;
        float width = Mathf.Min(HudCompassSettings.CompassWidth.Value, Mathf.Max(1f, available - 24f) / scale);
        float height = HudCompassSettings.CompassHeight.Value;
        float offsetLimit = Mathf.Max(0f, (available - width * scale) / 2f - 12f);
        root.sizeDelta = new Vector2(width, height);
        root.localScale = Vector3.one * scale;
        root.anchoredPosition = new Vector2(Mathf.Clamp(HudCompassSettings.HorizontalPosition.Value, -offsetLimit, offsetLimit), -HudCompassSettings.VerticalPosition.Value);
        group.alpha = HudCompassSettings.Opacity.Value;
        background.color = new Color(0.08f, 0.07f, 0.05f, HudCompassSettings.BackgroundOpacity.Value);
        RebuildTicks();

        float heading = CompassGeometry.Normalize(Utils.GetMainCamera().transform.eulerAngles.y);
        float degrees = HudCompassSettings.VisibleDegrees.Value;
        float compact = Mathf.Min(1f, height / 52f);
        indicator.rectTransform.sizeDelta = new Vector2(5f, 5f) * compact;
        indicator.rectTransform.anchoredPosition = new Vector2(0f, -3f * compact);
        foreach (Tick tick in ticks)
        {
            bool inside = CompassGeometry.Project(heading, tick.Heading, width, degrees, out float position);
            if (tick.Rect.gameObject.activeSelf != inside) tick.Rect.gameObject.SetActive(inside);
            if (!inside) continue;
            tick.Rect.anchoredPosition = new Vector2(position, 0f);
            tick.Group.alpha = Fade(position, width);
            float length = tick.Heading % majorInterval == 0 ? 6f : tick.Heading % mediumInterval == 0 ? 4f : 2f;
            tick.Line.rectTransform.sizeDelta = new Vector2(1f, length * compact);
            tick.Line.rectTransform.anchoredPosition = new Vector2(0f, -height * 0.56f);
            tick.Label.rectTransform.anchoredPosition = new Vector2(0f, -height * 0.35f);
            tick.Label.rectTransform.sizeDelta = new Vector2(50f, 24f * compact);
            tick.Label.fontSize = Mathf.RoundToInt(tick.FontSize * compact);
        }

        HudCompassMarkers.Update(Player.m_localPlayer.transform.position, heading, width, height);
        HudCompassLayout.Update();
    }

    internal static float Fade(float position, float width)
    {
        float fade = HudCompassSettings.EdgeFade.Value;
        return fade <= 0f ? 1f : Mathf.Clamp01((1f - Mathf.Abs(position) / (width / 2f)) / fade);
    }

    internal static RectTransform CreateRect(Transform parent, string name)
    {
        RectTransform rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
        return rect;
    }

    internal static Image CreateImage(Transform parent, string name)
    {
        Image image = CreateRect(parent, name).gameObject.AddComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    private static void Ensure()
    {
        Transform parent = Hud.instance.m_rootObject.transform;
        if (root != null && root.parent == parent) return;
        if (root != null) Object.Destroy(root.gameObject);
        ticks.Clear();
        tickInterval = 0;
        root = CreateRect(parent, "WhiteHiltHudCompass");
        root.gameObject.AddComponent<RectMask2D>();
        group = root.gameObject.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        background = CreateImage(root, "Background");
        background.rectTransform.anchorMin = Vector2.zero;
        background.rectTransform.anchorMax = Vector2.one;
        background.rectTransform.sizeDelta = Vector2.zero;
        tickContainer = CreateRect(root, "TickContainer");
        tickContainer.sizeDelta = Vector2.zero;
        HudCompassMarkers.Ensure(root);
        indicator = CreateImage(root, "CenterIndicator");
        indicator.color = ink;
        indicator.sprite = Minimap.instance != null && Minimap.instance.m_smallMarker != null
            ? Minimap.instance.m_smallMarker.GetComponent<Image>()?.sprite : null;
        indicator.preserveAspect = true;
        indicator.rectTransform.sizeDelta = new Vector2(7f, 7f);
        indicator.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        indicator.rectTransform.anchoredPosition = new Vector2(0f, -5f);
        indicator.rectTransform.localRotation = Quaternion.Euler(0f, 0f, indicator.sprite != null ? 180f : 45f);
    }

    private static void RebuildTicks()
    {
        if (tickInterval == HudCompassSettings.TickInterval.Value && mediumInterval == HudCompassSettings.MediumTickInterval.Value
            && numberInterval == HudCompassSettings.NumberInterval.Value && majorInterval == HudCompassSettings.MajorTickInterval.Value
            && showDegrees == HudCompassSettings.ShowDegrees.Value && showDirections == HudCompassSettings.ShowCardinalDirections.Value) return;
        foreach (Tick tick in ticks) Object.Destroy(tick.Rect.gameObject);
        ticks.Clear();
        tickInterval = HudCompassSettings.TickInterval.Value;
        mediumInterval = HudCompassSettings.MediumTickInterval.Value;
        numberInterval = HudCompassSettings.NumberInterval.Value;
        majorInterval = HudCompassSettings.MajorTickInterval.Value;
        showDegrees = HudCompassSettings.ShowDegrees.Value;
        showDirections = HudCompassSettings.ShowCardinalDirections.Value;
        for (int bearing = 0; bearing < 360; bearing++)
        {
            bool direction = showDirections && bearing % 45 == 0;
            bool number = showDegrees && bearing % numberInterval == 0;
            if (bearing % tickInterval != 0 && bearing % mediumInterval != 0 && bearing % majorInterval != 0 && !direction && !number) continue;
            RectTransform rect = CreateRect(tickContainer, "Tick" + bearing);
            rect.sizeDelta = Vector2.zero;
            CanvasGroup alpha = rect.gameObject.AddComponent<CanvasGroup>();
            Image line = CreateImage(rect, "Line");
            line.color = ink;
            string text = direction ? directions[bearing / 45] : number ? bearing.ToString("000") : string.Empty;
            int font = direction ? bearing % 90 == 0 ? 20 : 16 : 12;
            Text label = GUIManager.Instance.CreateText(text, rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero,
                GUIManager.Instance.AveriaSerifBold, font, ink, true, Color.black, 50f, 24f, false).GetComponent<Text>();
            label.name = "Label";
            label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            ticks.Add(new Tick { Heading = bearing, Rect = rect, Group = alpha, Line = line, Label = label, FontSize = font });
        }
    }

    private sealed class Tick
    {
        internal int Heading;
        internal RectTransform Rect;
        internal CanvasGroup Group;
        internal Image Line;
        internal Text Label;
        internal int FontSize;
    }
}