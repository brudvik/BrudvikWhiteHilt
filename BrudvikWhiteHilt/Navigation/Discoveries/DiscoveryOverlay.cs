using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Navigation.Discoveries;

/// <summary>
/// Holds the discoveries the server sent and draws the kinds the player has switched on, as the kind's icon on a disc
/// rimmed in the colour of its group. Markers of one kind that would overlap on screen merge into one with their
/// number. The large map needs <see cref="DiscoverySettings.LargeMapLevel"/> in Exploration, the minimap
/// <see cref="DiscoverySettings.MinimapLevel"/>.
/// </summary>
public static class DiscoveryOverlay
{
    private const string LayerName = "WhiteHiltDiscoveries";
    private const int DiscTexture = 64;
    private const float FillShare = 0.84f;
    private const float IconShare = 0.68f;

    private static readonly Color fill = new(0.08f, 0.08f, 0.1f, 0.82f);
    private static readonly Dictionary<(int Kind, int X, int Y), Merge> merges = new();
    private static readonly List<Merge> mergeList = new();
    private static readonly List<Drawn> drawn = new();

    private static List<DiscoveryKind> kinds = new();
    private static List<DiscoveryEntry> entries = new();
    private static int dataVersion;
    private static bool[] shownKinds = new bool[0];
    private static int shownVersion = -1;
    private static int shownDataVersion = -1;
    private static Minimap boundMap;
    private static Layer largeLayer;
    private static Layer smallLayer;
    private static Sprite disc;

    /// <summary>
    /// The kinds the server sent, in its order.
    /// </summary>
    public static IReadOnlyList<DiscoveryKind> Kinds => kinds;

    /// <summary>
    /// Goes up every time new data arrives.
    /// </summary>
    public static int DataVersion => dataVersion;

    /// <summary>
    /// Replaces the discoveries to draw.
    /// </summary>
    /// <param name="newKinds">The kinds.</param>
    /// <param name="newEntries">The markers, pointing into <paramref name="newKinds"/>.</param>
    public static void SetData(List<DiscoveryKind> newKinds, List<DiscoveryEntry> newEntries)
    {
        kinds = newKinds ?? new List<DiscoveryKind>();
        entries = newEntries ?? new List<DiscoveryEntry>();
        dataVersion++;
    }

    /// <summary>
    /// The colour of a group's rim.
    /// </summary>
    /// <param name="group">The group.</param>
    /// <returns>The colour.</returns>
    public static Color GroupColour(DiscoveryGroup group)
    {
        return group switch
        {
            DiscoveryGroup.Dungeons => new Color(0.86f, 0.36f, 0.3f),
            DiscoveryGroup.Settlements => new Color(0.95f, 0.66f, 0.3f),
            DiscoveryGroup.Plants => new Color(0.45f, 0.8f, 0.4f),
            DiscoveryGroup.Resources => new Color(0.6f, 0.75f, 0.92f),
            _ => new Color(0.76f, 0.56f, 0.96f),
        };
    }

    /// <summary>
    /// A round white sprite, tinted for discs and rims.
    /// </summary>
    /// <returns>The sprite.</returns>
    public static Sprite Disc()
    {
        if (disc != null)
        {
            return disc;
        }

        Texture2D texture = new(DiscTexture, DiscTexture, TextureFormat.RGBA32, false) { name = "WhiteHiltDiscoveryDisc", wrapMode = TextureWrapMode.Clamp };
        Color32[] pixels = new Color32[DiscTexture * DiscTexture];
        float centre = (DiscTexture - 1) / 2f;
        for (int y = 0; y < DiscTexture; y++)
        {
            for (int x = 0; x < DiscTexture; x++)
            {
                float distance = Mathf.Sqrt((x - centre) * (x - centre) + (y - centre) * (y - centre));
                pixels[y * DiscTexture + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(DiscTexture / 2f - distance - 0.5f) * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        disc = Sprite.Create(texture, new Rect(0f, 0f, DiscTexture, DiscTexture), new Vector2(0.5f, 0.5f));
        return disc;
    }

    /// <summary>
    /// Whether the local player may see discoveries on the large map.
    /// </summary>
    /// <returns>True from the Exploration level in the settings.</returns>
    public static bool LargeMapUnlocked()
    {
        return ExplorationSkill.GetLevel(Player.m_localPlayer) >= DiscoverySettings.LargeMapLevel.Value;
    }

    /// <summary>
    /// Keeps the markers in place over the map. Called after the map's own update.
    /// </summary>
    /// <param name="map">The map.</param>
    public static void Update(Minimap map)
    {
        if (map.m_mapImageLarge == null || map.m_mapImageSmall == null)
        {
            return;
        }

        if (map != boundMap || largeLayer == null || !largeLayer.Valid || smallLayer == null || !smallLayer.Valid)
        {
            boundMap = map;
            largeLayer = new Layer(map.m_mapImageLarge.rectTransform, map.m_pinRootLarge);
            smallLayer = new Layer(map.m_mapImageSmall.rectTransform, map.m_pinRootSmall);
        }

        int level = ExplorationSkill.GetLevel(Player.m_localPlayer);
        bool large = map.m_mode == Minimap.MapMode.Large && level >= DiscoverySettings.LargeMapLevel.Value;
        bool small = map.m_mode == Minimap.MapMode.Small && DiscoverySettings.ShowOnMinimap.Value && level >= DiscoverySettings.MinimapLevel.Value;
        largeLayer.SetActive(large);
        smallLayer.SetActive(small);
        if (!large)
        {
            drawn.Clear();
        }

        if (!large && !small)
        {
            return;
        }

        UpdateShownKinds();
        Layer layer = large ? largeLayer : smallLayer;
        RawImage image = large ? map.m_mapImageLarge : map.m_mapImageSmall;
        float size = large ? DiscoverySettings.LargeIconSize.Value : DiscoverySettings.SmallIconSize.Value;
        layer.KeepUnderPins();
        if (layer.NeedsLayout(image.uvRect, layer.Root.rect.size, size, dataVersion, shownVersion))
        {
            Layout(map, layer, image.uvRect, size, large);
        }
    }

    /// <summary>
    /// Text for the marker under the cursor on the large map, or null if there is none.
    /// </summary>
    /// <param name="worldPosition">World position under the cursor.</param>
    /// <param name="radius">How close the cursor must be, in metres.</param>
    /// <returns>The kind, its number and how many can be picked, localized.</returns>
    public static string GetHoverText(Vector3 worldPosition, float radius)
    {
        Drawn best = null;
        float bestDistance = radius;
        foreach (Drawn marker in drawn)
        {
            float distance = Utils.DistanceXZ(worldPosition, marker.World);
            if (distance < bestDistance)
            {
                best = marker;
                bestDistance = distance;
            }
        }

        if (best == null || best.Kind >= kinds.Count)
        {
            return null;
        }

        string text = DiscoveryCatalog.GetLabel(kinds[best.Kind].Key);
        if (best.Count > 1)
        {
            text += $" ×{best.Count}";
        }

        if (best.Ripe >= 0)
        {
            text += "\n" + string.Format(Localization.instance.Localize("$whitehilt_disc_ripe"), best.Ripe);
        }

        return text;
    }

    private static void UpdateShownKinds()
    {
        int version = DiscoveryFilter.Version;
        if (version == shownVersion && dataVersion == shownDataVersion)
        {
            return;
        }

        shownVersion = version;
        shownDataVersion = dataVersion;
        shownKinds = new bool[kinds.Count];
        for (int i = 0; i < kinds.Count; i++)
        {
            shownKinds[i] = DiscoveryFilter.IsShown(kinds[i].Key);
        }
    }

    // Places the discovery markers on the map: those of one kind that would overlap at this zoom are merged into one
    // marker with a count, so the map does not drown in icons.
    private static void Layout(Minimap map, Layer layer, Rect uv, float size, bool large)
    {
        Vector2 rect = layer.Root.rect.size;
        merges.Clear();
        mergeList.Clear();
        float cell = Mathf.Max(4f, size * 0.9f);
        foreach (DiscoveryEntry entry in entries)
        {
            if (entry.Kind >= shownKinds.Length || !shownKinds[entry.Kind])
            {
                continue;
            }

            map.WorldToMapPoint(entry.Position, out float mx, out float my);
            if (mx < uv.xMin || mx > uv.xMax || my < uv.yMin || my > uv.yMax)
            {
                continue;
            }

            Vector2 local = new((mx - uv.xMin) / uv.width * rect.x, (my - uv.yMin) / uv.height * rect.y);
            (int, int, int) key = (entry.Kind, Mathf.FloorToInt(local.x / cell), Mathf.FloorToInt(local.y / cell));
            if (!merges.TryGetValue(key, out Merge merge))
            {
                merge = new Merge { Kind = entry.Kind, Ripe = -1 };
                merges[key] = merge;
                mergeList.Add(merge);
            }

            merge.Local += local * entry.Count;
            merge.World += entry.Position * entry.Count;
            merge.Count += entry.Count;
            if (entry.Ripe >= 0)
            {
                merge.Ripe = Mathf.Max(merge.Ripe, 0) + entry.Ripe;
            }
        }

        if (large)
        {
            drawn.Clear();
        }

        int limit = Mathf.Min(mergeList.Count, DiscoverySettings.MaxMarkers.Value);
        for (int i = 0; i < limit; i++)
        {
            Merge merge = mergeList[i];
            DiscoveryKind kind = kinds[merge.Kind];
            Vector2 position = merge.Local / merge.Count;
            layer.Show(i, position, size, DiscoveryCatalog.GetIcon(kind.Key), GroupColour(kind.Group), large && merge.Count > 1 ? merge.Count : 0);
            if (large)
            {
                drawn.Add(new Drawn { Kind = merge.Kind, World = merge.World / merge.Count, Count = merge.Count, Ripe = merge.Ripe });
            }
        }

        layer.HideFrom(limit);
    }

    private sealed class Merge
    {
        public int Kind;
        public Vector2 Local;
        public Vector3 World;
        public int Count;
        public int Ripe;
    }

    private sealed class Drawn
    {
        public int Kind;
        public Vector3 World;
        public int Count;
        public int Ripe;
    }

    /// <summary>
    /// The markers on one of the two maps, kept in a pool.
    /// </summary>
    private sealed class Layer
    {
        private readonly RectTransform pinRoot;
        private readonly List<Marker> pool = new();
        private Rect lastUv;
        private Vector2 lastSize;
        private float lastIconSize;
        private int lastData = -1;
        private int lastShown = -1;

        public Layer(RectTransform map, RectTransform pins)
        {
            pinRoot = pins;
            Transform old = map.Find(LayerName);
            if (old != null)
            {
                Object.Destroy(old.gameObject);
            }

            GameObject root = new(LayerName, typeof(RectTransform), typeof(RectMask2D));
            Root = (RectTransform)root.transform;
            Root.SetParent(map, false);
            Root.anchorMin = Vector2.zero;
            Root.anchorMax = Vector2.one;
            Root.offsetMin = Vector2.zero;
            Root.offsetMax = Vector2.zero;
        }

        public RectTransform Root { get; }

        public bool Valid => Root != null;

        public void SetActive(bool active)
        {
            if (Root.gameObject.activeSelf != active)
            {
                Root.gameObject.SetActive(active);
                lastData = -1;
            }
        }

        // Over the map and its area drawings, under the pins, so portals, ships and players stay on top.
        public void KeepUnderPins()
        {
            if (pinRoot == null || pinRoot.parent != Root.parent)
            {
                return;
            }

            int pins = pinRoot.GetSiblingIndex();
            if (Root.GetSiblingIndex() > pins)
            {
                Root.SetSiblingIndex(pins);
            }
        }

        public bool NeedsLayout(Rect uv, Vector2 size, float iconSize, int data, int shown)
        {
            if (uv == lastUv && size == lastSize && Mathf.Approximately(iconSize, lastIconSize) && data == lastData && shown == lastShown)
            {
                return false;
            }

            lastUv = uv;
            lastSize = size;
            lastIconSize = iconSize;
            lastData = data;
            lastShown = shown;
            return true;
        }

        public void Show(int index, Vector2 position, float size, Sprite icon, Color rim, int count)
        {
            while (pool.Count <= index)
            {
                pool.Add(new Marker(Root));
            }

            pool[index].Show(position, size, icon, rim, count);
        }

        public void HideFrom(int index)
        {
            for (int i = index; i < pool.Count; i++)
            {
                pool[i].Hide();
            }
        }
    }

    /// <summary>
    /// One marker: a rim, a dark disc, the icon and a number.
    /// </summary>
    private sealed class Marker
    {
        private readonly GameObject root;
        private readonly RectTransform rect;
        private readonly Image rim;
        private readonly Image icon;
        private readonly Text count;

        public Marker(RectTransform parent)
        {
            root = new GameObject("Discovery", typeof(RectTransform));
            rect = (RectTransform)root.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);

            rim = AddImage(rect, "Rim", 1f);
            rim.sprite = Disc();
            Image inner = AddImage(rect, "Fill", FillShare);
            inner.sprite = Disc();
            inner.color = fill;
            icon = AddImage(rect, "Icon", IconShare);
            icon.preserveAspect = true;

            GameObject label = new("Count", typeof(RectTransform), typeof(Text), typeof(Outline));
            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.sizeDelta = new Vector2(40f, 16f);
            count = label.GetComponent<Text>();
            count.font = GUIManager.Instance.AveriaSerifBold;
            count.fontSize = 12;
            count.alignment = TextAnchor.MiddleCenter;
            count.color = Color.white;
            count.raycastTarget = false;
            count.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.GetComponent<Outline>().effectColor = Color.black;
        }

        /// <summary>
        /// Shows one marker: its icon, a coloured rim, and a count when it stands for more than one.
        /// </summary>
        public void Show(Vector2 position, float size, Sprite sprite, Color rimColour, int number)
        {
            if (!root.activeSelf)
            {
                root.SetActive(true);
            }

            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(size, size);
            rim.color = rimColour;
            icon.sprite = sprite;
            icon.enabled = sprite != null;
            count.gameObject.SetActive(number > 1);
            if (number > 1)
            {
                count.text = number.ToString();
            }
        }

        public void Hide()
        {
            if (root.activeSelf)
            {
                root.SetActive(false);
            }
        }

        private static Image AddImage(RectTransform parent, string name, float share)
        {
            GameObject image = new(name, typeof(RectTransform), typeof(Image));
            RectTransform imageRect = (RectTransform)image.transform;
            imageRect.SetParent(parent, false);
            imageRect.anchorMin = new Vector2((1f - share) / 2f, (1f - share) / 2f);
            imageRect.anchorMax = new Vector2((1f + share) / 2f, (1f + share) / 2f);
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            Image component = image.GetComponent<Image>();
            component.raycastTarget = false;
            return component;
        }
    }
}
