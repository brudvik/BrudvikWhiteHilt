using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Navigation.Waypoints;

/// <summary>
/// The target on the maps: a ruby-red ring with a dot over the pins, named on the large map, and an arrow on the
/// minimap's edge while the target is beyond it. Not a vanilla pin, so it is never saved, shared or removed by a click.
/// </summary>
public static class WaypointMap
{
    private const int RingTexture = 64;
    private const float RingOuter = 0.48f;
    private const float RingInner = 0.34f;
    private const float DotRadius = 0.14f;
    private const float LabelWidth = 200f;
    private const float LabelHeight = 24f;
    private const int FontSize = 14;

    private static readonly Color markerColor = new(0.9f, 0.12f, 0.16f, 1f);
    private static readonly MinimapEdgeArrow edgeArrow = new("WhiteHiltWaypointEdge", markerColor);

    private static Marker large;
    private static Marker small;
    private static Sprite ring;

    /// <summary>
    /// Hides the markers and the edge arrow.
    /// </summary>
    public static void Hide()
    {
        large?.SetActive(false);
        small?.SetActive(false);
        edgeArrow.Hide();
    }

    /// <summary>
    /// Places the marker on the open map, and the edge arrow on the minimap when the target is beyond it.
    /// </summary>
    /// <param name="map">The map.</param>
    /// <param name="here">The player's position.</param>
    /// <param name="target">The target.</param>
    public static void Update(Minimap map, Vector3 here, Vector3 target)
    {
        if (map.m_mapImageLarge == null || map.m_mapImageSmall == null)
        {
            return;
        }

        bool largeOpen = map.m_mode == Minimap.MapMode.Large;
        bool smallOpen = map.m_mode == Minimap.MapMode.Small && map.m_smallRoot != null && map.m_smallRoot.activeInHierarchy;
        Place(ref large, map, map.m_pinRootLarge, map.m_mapImageLarge, map.m_pinSizeLarge, largeOpen, target, true);
        bool onMinimap = Place(ref small, map, map.m_pinRootSmall, map.m_mapImageSmall, map.m_pinSizeSmall, smallOpen, target, false);
        if (smallOpen && !onMinimap)
        {
            edgeArrow.Show(map, here, target);
        }
        else
        {
            edgeArrow.Hide();
        }
    }

    // Returns true when the marker is shown, i.e. the map is open and the target is within its view.
    private static bool Place(ref Marker marker, Minimap map, RectTransform pinRoot, RawImage image, float size, bool open, Vector3 target, bool named)
    {
        if (!open || pinRoot == null)
        {
            marker?.SetActive(false);
            return false;
        }

        map.WorldToMapPoint(target, out float mx, out float my);
        Rect uv = image.uvRect;
        if (mx <= uv.xMin || mx >= uv.xMax || my <= uv.yMin || my >= uv.yMax)
        {
            marker?.SetActive(false);
            return false;
        }

        if (marker == null || !marker.IsUnder(pinRoot))
        {
            marker = new Marker(pinRoot, named);
        }

        marker.SetActive(true);
        Rect rect = image.rectTransform.rect;
        Vector2 local = new(rect.xMin + (mx - uv.xMin) / uv.width * rect.width, rect.yMin + (my - uv.yMin) / uv.height * rect.height);
        marker.Place(image.rectTransform.TransformPoint(local), size, WaypointTarget.Name);
        return true;
    }

    // The waypoint's map marker, a ring with a dot in it with a smooth edge, made once.
    private static Sprite Ring()
    {
        if (ring != null)
        {
            return ring;
        }

        Texture2D texture = new(RingTexture, RingTexture, TextureFormat.RGBA32, false) { name = "WhiteHiltWaypointRing", wrapMode = TextureWrapMode.Clamp };
        Color32[] pixels = new Color32[RingTexture * RingTexture];
        float centre = (RingTexture - 1) / 2f;
        for (int y = 0; y < RingTexture; y++)
        {
            for (int x = 0; x < RingTexture; x++)
            {
                float distance = Mathf.Sqrt((x - centre) * (x - centre) + (y - centre) * (y - centre)) / RingTexture;
                float edge = RingTexture * Mathf.Min(RingOuter - distance, Mathf.Max(distance - RingInner, DotRadius - distance));
                pixels[y * RingTexture + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(edge + 0.5f) * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        ring = Sprite.Create(texture, new Rect(0f, 0f, RingTexture, RingTexture), new Vector2(0.5f, 0.5f));
        return ring;
    }

    private sealed class Marker
    {
        private readonly RectTransform rect;
        private readonly Text label;

        public Marker(RectTransform pinRoot, bool named)
        {
            Vector2 centre = new(0.5f, 0.5f);
            GameObject go = new("WhiteHiltWaypointMarker", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(pinRoot, false);
            rect = (RectTransform)go.transform;
            rect.anchorMin = centre;
            rect.anchorMax = centre;
            Image image = go.GetComponent<Image>();
            image.sprite = Ring();
            image.color = markerColor;
            image.raycastTarget = false;
            if (named)
            {
                label = GUIManager.Instance.CreateText(string.Empty, rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero,
                    GUIManager.Instance.AveriaSerifBold, FontSize, Color.white, true, Color.black, LabelWidth, LabelHeight, false).GetComponent<Text>();
                label.alignment = TextAnchor.UpperCenter;
                label.raycastTarget = false;
                label.rectTransform.pivot = new Vector2(0.5f, 1f);
            }
        }

        public bool IsUnder(RectTransform pinRoot)
        {
            return rect != null && rect.parent == pinRoot;
        }

        public void SetActive(bool active)
        {
            if (rect != null && rect.gameObject.activeSelf != active)
            {
                rect.gameObject.SetActive(active);
            }
        }

        public void Place(Vector3 position, float size, string name)
        {
            rect.position = position;
            rect.sizeDelta = new Vector2(size, size);
            // Pins are added after the marker, so it is moved back on top of them.
            if (rect.GetSiblingIndex() != rect.parent.childCount - 1)
            {
                rect.SetAsLastSibling();
            }

            if (label != null && label.text != name)
            {
                label.text = name;
            }
        }
    }
}
