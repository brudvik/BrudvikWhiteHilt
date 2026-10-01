using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Navigation.Areas;

/// <summary>
/// Draws the areas and wards from <see cref="MapAreaService"/> on the large map and the minimap. Each area gets its own
/// small texture at 2 m per pixel (the map itself is about 12 m per pixel), filled in the builder's colour and bordered
/// by kind; wards get a dashed ring. Only areas where the player has uncovered the map are drawn.
/// </summary>
public static class MapAreaOverlay
{
    private const string RootName = "WhiteHiltAreas";
    private const float MetresPerPixel = 2f;
    private const float Margin = 6f;
    private const int BorderPixels = 2;
    private const int MaxTexture = 512;
    private const float VisibilityInterval = 3f;
    private const float LabelZoom = 0.3f;
    private const int RingTexture = 128;
    private const int Dashes = 24;

    private static readonly Color fieldFill = new(0.45f, 0.75f, 0.3f, 0.3f);
    private static readonly Color fieldBorder = new(0.3f, 0.6f, 0.2f, 0.9f);
    private static readonly Color pastureFill = new(0.8f, 0.6f, 0.35f, 0.3f);
    private static readonly Color pastureBorder = new(0.6f, 0.42f, 0.2f, 0.9f);
    private static readonly Color baseBorder = new(1f, 0.82f, 0.35f, 0.95f);
    private static readonly Color outpostBorder = new(0.55f, 0.8f, 1f, 0.95f);
    private static readonly Color buildingBorder = new(0.9f, 0.9f, 0.9f, 0.9f);
    private static readonly Color wardOn = new(0.5f, 1f, 0.6f, 0.85f);
    private static readonly Color wardOff = new(0.7f, 0.7f, 0.7f, 0.6f);

    private static readonly List<Visual> visuals = new();

    private static List<MapArea> areas = new();
    private static List<MapWard> wards = new();
    private static Minimap boundMap;
    private static RectTransform largeRoot;
    private static RectTransform smallRoot;
    private static RectTransform labelRoot;
    private static Texture2D ring;
    private static string visibleKey;
    private static bool dirty = true;
    private static float nextVisibilityCheck;

    /// <summary>
    /// Replaces the areas and wards to draw.
    /// </summary>
    /// <param name="newAreas">The areas.</param>
    /// <param name="newWards">The wards.</param>
    public static void SetData(List<MapArea> newAreas, List<MapWard> newWards)
    {
        areas = newAreas ?? new List<MapArea>();
        wards = newWards ?? new List<MapWard>();
        MarkDirty();
    }

    /// <summary>
    /// Draws everything again on the next map update.
    /// </summary>
    public static void MarkDirty()
    {
        dirty = true;
    }

    /// <summary>
    /// Keeps the drawings in place over the map. Called after the map's own update.
    /// </summary>
    /// <param name="map">The map.</param>
    public static void Update(Minimap map)
    {
        if (map.m_mapImageLarge == null || map.m_mapImageSmall == null || map.m_explored == null)
        {
            return;
        }

        if (map != boundMap || largeRoot == null || smallRoot == null || labelRoot == null)
        {
            Bind(map);
        }

        if (Time.unscaledTime >= nextVisibilityCheck)
        {
            nextVisibilityCheck = Time.unscaledTime + VisibilityInterval;
            string key = VisibleKey(map);
            if (key != visibleKey)
            {
                visibleKey = key;
                dirty = true;
            }
        }

        if (dirty)
        {
            dirty = false;
            Rebuild(map);
        }

        bool large = map.m_mode == Minimap.MapMode.Large;
        bool small = map.m_mode == Minimap.MapMode.Small;
        largeRoot.gameObject.SetActive(large);
        smallRoot.gameObject.SetActive(small);
        if (large || small)
        {
            RawImage image = large ? map.m_mapImageLarge : map.m_mapImageSmall;
            bool labels = large && MapAreaSettings.ShowLabels.Value && map.LargeZoom < LabelZoom;
            Place(map, image.uvRect, (large ? largeRoot : smallRoot).rect, large, labels);
        }
    }

    private static void Bind(Minimap map)
    {
        Clear();
        boundMap = map;
        largeRoot = CreateRoot(map.m_mapImageLarge.rectTransform);
        smallRoot = CreateRoot(map.m_mapImageSmall.rectTransform);
        labelRoot = new GameObject("Labels", typeof(RectTransform)).GetComponent<RectTransform>();
        labelRoot.SetParent(largeRoot, false);
        labelRoot.anchorMin = Vector2.zero;
        labelRoot.anchorMax = Vector2.one;
        labelRoot.offsetMin = Vector2.zero;
        labelRoot.offsetMax = Vector2.zero;
        dirty = true;
    }

    private static RectTransform CreateRoot(RectTransform map)
    {
        Transform old = map.Find(RootName);
        if (old != null)
        {
            Object.Destroy(old.gameObject);
        }

        GameObject root = new(RootName, typeof(RectTransform), typeof(RectMask2D));
        RectTransform rect = (RectTransform)root.transform;
        rect.SetParent(map, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static void Clear()
    {
        foreach (Visual visual in visuals)
        {
            visual.Destroy();
        }

        visuals.Clear();
    }

    private static bool Wanted(MapAreaKind kind)
    {
        return kind switch
        {
            MapAreaKind.Field => MapAreaSettings.ShowFields.Value,
            MapAreaKind.Pasture => MapAreaSettings.ShowPastures.Value,
            _ => MapAreaSettings.ShowBuildings.Value,
        };
    }

    private static bool Explored(Minimap map, Vector3 position)
    {
        map.WorldToPixel(position, out int x, out int y);
        int size = map.m_textureSize;
        if (x < 0 || y < 0 || x >= size || y >= size)
        {
            return false;
        }

        int index = y * size + x;
        return map.m_explored[index] || (map.m_showSharedMapData && map.m_exploredOthers != null && map.m_exploredOthers[index]);
    }

    private static IEnumerable<MapArea> VisibleAreas(Minimap map)
    {
        return areas.Where(area => Wanted(area.Kind)
            && area.Cells.Any(cell => Explored(map, new Vector3((cell.x + 0.5f) * MapArea.CellSize, 0f, (cell.y + 0.5f) * MapArea.CellSize))));
    }

    private static IEnumerable<MapWard> VisibleWards(Minimap map)
    {
        return MapAreaSettings.ShowWards.Value ? wards.Where(ward => Explored(map, ward.Position)) : Enumerable.Empty<MapWard>();
    }

    private static string VisibleKey(Minimap map)
    {
        return string.Join(",", VisibleAreas(map).Select(area => areas.IndexOf(area))) + "|" + VisibleWards(map).Count();
    }

    private static void Rebuild(Minimap map)
    {
        Clear();

        // Fields and pastures first, so buildings are drawn over them.
        foreach (MapArea area in VisibleAreas(map).OrderBy(area => area.Kind == MapAreaKind.Field || area.Kind == MapAreaKind.Pasture ? 0 : 1))
        {
            visuals.Add(AreaVisual(area));
        }

        foreach (MapWard ward in VisibleWards(map))
        {
            visuals.Add(WardVisual(ward));
        }

        labelRoot.SetAsLastSibling();
    }

    private static Visual AreaVisual(MapArea area)
    {
        float radius = MapArea.CellSize * 0.75f + Margin;
        Vector2 min = new(area.Cells.Min(cell => cell.x), area.Cells.Min(cell => cell.y));
        Vector2 max = new(area.Cells.Max(cell => cell.x) + 1, area.Cells.Max(cell => cell.y) + 1);
        Vector3 worldMin = new(min.x * MapArea.CellSize - radius, 0f, min.y * MapArea.CellSize - radius);
        Vector3 worldMax = new(max.x * MapArea.CellSize + radius, 0f, max.y * MapArea.CellSize + radius);
        float scale = Mathf.Max(MetresPerPixel, Mathf.Max(worldMax.x - worldMin.x, worldMax.z - worldMin.z) / MaxTexture);
        int width = Mathf.CeilToInt((worldMax.x - worldMin.x) / scale);
        int height = Mathf.CeilToInt((worldMax.z - worldMin.z) / scale);

        bool[] mask = new bool[width * height];
        float r = radius / scale;
        foreach (Vector2Int cell in area.Cells)
        {
            float cx = ((cell.x + 0.5f) * MapArea.CellSize - worldMin.x) / scale;
            float cy = ((cell.y + 0.5f) * MapArea.CellSize - worldMin.z) / scale;
            for (int y = Mathf.Max(0, Mathf.FloorToInt(cy - r)); y <= Mathf.Min(height - 1, Mathf.CeilToInt(cy + r)); y++)
            {
                for (int x = Mathf.Max(0, Mathf.FloorToInt(cx - r)); x <= Mathf.Min(width - 1, Mathf.CeilToInt(cx + r)); x++)
                {
                    float dx = x + 0.5f - cx;
                    float dy = y + 0.5f - cy;
                    if (dx * dx + dy * dy <= r * r)
                    {
                        mask[y * width + x] = true;
                    }
                }
            }
        }

        (Color fill, Color border) = Colours(area);
        Color32[] pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (mask[y * width + x])
                {
                    pixels[y * width + x] = NearEdge(mask, width, height, x, y) ? border : fill;
                }
            }
        }

        Texture2D texture = new(width, height, TextureFormat.RGBA32, true)
        {
            name = "WhiteHiltArea",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Trilinear,
        };
        texture.SetPixels32(pixels);
        texture.Apply(true, true);
        return new Visual(texture, true, Color.white, worldMin, worldMax, Label(area), area.Centre());
    }

    private static bool NearEdge(bool[] mask, int width, int height, int x, int y)
    {
        for (int dy = -BorderPixels; dy <= BorderPixels; dy++)
        {
            for (int dx = -BorderPixels; dx <= BorderPixels; dx++)
            {
                int nx = x + dx;
                int ny = y + dy;
                if (dx * dx + dy * dy <= BorderPixels * BorderPixels && (nx < 0 || ny < 0 || nx >= width || ny >= height || !mask[ny * width + nx]))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static (Color Fill, Color Border) Colours(MapArea area)
    {
        switch (area.Kind)
        {
            case MapAreaKind.Field:
                return (fieldFill, fieldBorder);
            case MapAreaKind.Pasture:
                return (pastureFill, pastureBorder);
        }

        Color fill = BuilderColour(area.Creator);
        Color border = area.Kind == MapAreaKind.Base ? baseBorder : area.Kind == MapAreaKind.Outpost ? outpostBorder : buildingBorder;
        return (fill, border);
    }

    // Each builder keeps the same colour on every client, picked from the player ID.
    private static Color BuilderColour(long creator)
    {
        if (creator == 0L)
        {
            return new Color(0.6f, 0.6f, 0.6f, 0.3f);
        }

        float hue = (uint)(creator ^ (creator >> 32)) % 360u / 360f;
        Color colour = Color.HSVToRGB(hue, 0.6f, 0.9f);
        colour.a = 0.3f;
        return colour;
    }

    private static string Label(MapArea area)
    {
        string kind = Localization.instance.Localize("$whitehilt_area_" + area.Kind.ToString().ToLowerInvariant());
        string title = string.IsNullOrEmpty(area.Name) ? kind : area.Name;
        string detail = area.Kind switch
        {
            MapAreaKind.Field => Localization.instance.Localize("$whitehilt_area_plants", area.Count.ToString()),
            MapAreaKind.Pasture => Localization.instance.Localize("$whitehilt_area_animals", area.Count.ToString()),
            _ => string.IsNullOrEmpty(area.Name) ? area.Builder : string.IsNullOrEmpty(area.Builder) ? kind : $"{kind} · {area.Builder}",
        };
        return string.IsNullOrEmpty(detail) ? $"<b>{title}</b>" : $"<b>{title}</b>\n<size=11>{detail}</size>";
    }

    private static Visual WardVisual(MapWard ward)
    {
        Vector3 reach = new(ward.Radius, 0f, ward.Radius);
        return new Visual(Ring(), false, ward.Enabled ? wardOn : wardOff, ward.Position - reach, ward.Position + reach, null, ward.Position);
    }

    private static Texture2D Ring()
    {
        if (ring != null)
        {
            return ring;
        }

        ring = new Texture2D(RingTexture, RingTexture, TextureFormat.RGBA32, true)
        {
            name = "WhiteHiltWardRing",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Trilinear,
        };
        Color32[] pixels = new Color32[RingTexture * RingTexture];
        float centre = RingTexture / 2f;
        float radius = centre - 3f;
        for (int y = 0; y < RingTexture; y++)
        {
            for (int x = 0; x < RingTexture; x++)
            {
                float dx = x + 0.5f - centre;
                float dy = y + 0.5f - centre;
                float edge = Mathf.Clamp01(1.5f - Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - radius));
                float angle = (Mathf.Atan2(dy, dx) / (2f * Mathf.PI) + 1f) % 1f;
                bool dash = Mathf.FloorToInt(angle * Dashes * 2f) % 2 == 0;
                pixels[y * RingTexture + x] = new Color32(255, 255, 255, (byte)(dash ? edge * 255f : 0f));
            }
        }

        ring.SetPixels32(pixels);
        ring.Apply(true, true);
        return ring;
    }

    private static void Place(Minimap map, Rect uv, Rect rect, bool large, bool labels)
    {
        foreach (Visual visual in visuals)
        {
            map.WorldToMapPoint(visual.Min, out float minX, out float minY);
            map.WorldToMapPoint(visual.Max, out float maxX, out float maxY);
            bool inView = maxX > uv.xMin && minX < uv.xMax && maxY > uv.yMin && minY < uv.yMax;
            RawImage image = large ? visual.Large : visual.Small;
            image.gameObject.SetActive(inView);
            if (inView)
            {
                Vector2 from = ToLocal(minX, minY, uv, rect);
                Vector2 to = ToLocal(maxX, maxY, uv, rect);
                image.rectTransform.anchoredPosition = from;
                image.rectTransform.sizeDelta = to - from;
            }

            if (visual.Label == null)
            {
                continue;
            }

            map.WorldToMapPoint(visual.Centre, out float cx, out float cy);
            bool show = labels && cx > uv.xMin && cx < uv.xMax && cy > uv.yMin && cy < uv.yMax;
            visual.Label.gameObject.SetActive(show);
            if (show)
            {
                visual.Label.rectTransform.anchoredPosition = ToLocal(cx, cy, uv, rect);
            }
        }
    }

    private static Vector2 ToLocal(float mx, float my, Rect uv, Rect rect)
    {
        return new Vector2((mx - uv.xMin) / uv.width * rect.width, (my - uv.yMin) / uv.height * rect.height);
    }

    /// <summary>
    /// One area or ward: an image on each map, sharing a texture, and a name on the large map.
    /// </summary>
    private sealed class Visual
    {
        private readonly Texture2D owned;

        public Visual(Texture2D texture, bool ownsTexture, Color tint, Vector3 min, Vector3 max, string label, Vector3 centre)
        {
            owned = ownsTexture ? texture : null;
            Min = min;
            Max = max;
            Centre = centre;
            Large = CreateImage(largeRoot, texture, tint);
            Small = CreateImage(smallRoot, texture, tint);
            if (!string.IsNullOrEmpty(label))
            {
                Label = CreateLabel(labelRoot, label);
            }
        }

        public Vector3 Min { get; }

        public Vector3 Max { get; }

        public Vector3 Centre { get; }

        public RawImage Large { get; }

        public RawImage Small { get; }

        public Text Label { get; }

        public void Destroy()
        {
            if (Large != null)
            {
                Object.Destroy(Large.gameObject);
            }

            if (Small != null)
            {
                Object.Destroy(Small.gameObject);
            }

            if (Label != null)
            {
                Object.Destroy(Label.gameObject);
            }

            if (owned != null)
            {
                Object.Destroy(owned);
            }
        }

        private static RawImage CreateImage(RectTransform root, Texture2D texture, Color tint)
        {
            GameObject image = new("Area", typeof(RectTransform), typeof(RawImage));
            RectTransform rect = (RectTransform)image.transform;
            rect.SetParent(root, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            RawImage raw = image.GetComponent<RawImage>();
            raw.texture = texture;
            raw.color = tint;
            raw.raycastTarget = false;
            return raw;
        }

        private static Text CreateLabel(RectTransform root, string text)
        {
            GameObject label = GUIManager.Instance.CreateText(text, root, Vector2.zero, Vector2.zero, Vector2.zero,
                GUIManager.Instance.AveriaSerifBold, 14, Color.white, true, new Color(0f, 0f, 0f, 0.8f), 220f, 40f, false);
            label.name = "AreaLabel";
            Text component = label.GetComponent<Text>();
            component.alignment = TextAnchor.MiddleCenter;
            component.supportRichText = true;
            component.horizontalOverflow = HorizontalWrapMode.Overflow;
            component.verticalOverflow = VerticalWrapMode.Overflow;
            component.raycastTarget = false;
            component.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            label.SetActive(false);
            return component;
        }
    }
}
