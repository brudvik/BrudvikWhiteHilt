using BrudvikWhiteHilt.Navigation.Discoveries;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Navigation.Overview;

/// <summary>
/// The exploration overview under the large map, at its left edge or right of Munin's memory: a header that opens a
/// list of the biomes upwards over the map, each with a bar for the share uncovered and the area, the whole world below,
/// and what Munin's Perch has found. Below the Exploration level from the settings it only says what level is needed.
/// </summary>
public static class OverviewPanel
{
    private const float Spacing = 8f;
    private const float Padding = 14f;
    private const float Gap = 8f;
    private const float NameWidth = 120f;
    private const float BarWidth = 100f;
    private const float BarHeight = 10f;
    private const float ValueWidth = 130f;
    private const float Width = Padding * 2f + NameWidth + BarWidth + ValueWidth + Gap * 2f;
    private const float HeaderHeight = 28f;
    private const float RowHeight = 22f;
    private const float FootHeight = 40f;
    private const float RefreshInterval = 2f;
    private const float CollapsedHeight = Padding * 2f + HeaderHeight;

    private static readonly Color barBack = new(0f, 0f, 0f, 0.5f);
    private static readonly Dictionary<Heightmap.Biome, Color> biomeColours = new()
    {
        [Heightmap.Biome.Meadows] = new Color(0.55f, 0.8f, 0.35f),
        [Heightmap.Biome.BlackForest] = new Color(0.3f, 0.55f, 0.3f),
        [Heightmap.Biome.Swamp] = new Color(0.6f, 0.45f, 0.3f),
        [Heightmap.Biome.Mountain] = new Color(0.88f, 0.88f, 0.92f),
        [Heightmap.Biome.Plains] = new Color(0.9f, 0.78f, 0.35f),
        [Heightmap.Biome.Mistlands] = new Color(0.6f, 0.5f, 0.7f),
        [Heightmap.Biome.AshLands] = new Color(0.85f, 0.35f, 0.25f),
        [Heightmap.Biome.DeepNorth] = new Color(0.7f, 0.85f, 0.95f),
        [Heightmap.Biome.Ocean] = new Color(0.3f, 0.5f, 0.85f)
    };

    private static readonly List<Row> rows = new();

    private static GameObject panel;
    private static RectTransform content;
    private static RectTransform headerButton;
    private static Text header;
    private static Text foot;
    private static Row worldRow;
    private static bool expanded;
    private static float nextRefresh;

    /// <summary>
    /// Shows or hides the panel with the large map and keeps it up to date. Called after the map's own update.
    /// </summary>
    /// <param name="map">The map.</param>
    public static void Update(Minimap map)
    {
        Player player = Player.m_localPlayer;
        bool visible = map.m_mode == Minimap.MapMode.Large && player != null && OverviewSettings.Overview.Value && GUIManager.CustomGUIFront != null;
        if (!visible)
        {
            if (panel != null && panel.activeSelf)
            {
                panel.SetActive(false);
            }

            nextRefresh = 0f;
            return;
        }

        if (panel == null)
        {
            Create();
        }

        if (!panel.activeSelf)
        {
            panel.SetActive(true);
        }

        RectTransform discoveries = DiscoveryPanel.Shown;
        LargeMapFrame.PlaceBelow(map, (RectTransform)panel.transform, discoveries != null ? discoveries.sizeDelta.x + Spacing : 0f, CollapsedHeight);

        bool locked = ExplorationSkill.GetLevel(player) < OverviewSettings.Level.Value;
        if (!locked && expanded)
        {
            ExplorationOverview.Build(map);
        }

        if (Time.unscaledTime >= nextRefresh || (expanded && !ExplorationOverview.Ready))
        {
            nextRefresh = Time.unscaledTime + RefreshInterval;
            Refresh(map, locked);
        }
    }

    private static void Refresh(Minimap map, bool locked)
    {
        string title = Localization.instance.Localize("$whitehilt_overview_title");
        header.text = locked ? title : $"{title}  {(expanded ? "-" : "+")}";
        // Laid out top-down with the header last, so it stays at the bottom while the list opens upwards.
        float y = Padding;
        bool counted = false;
        int[] explored = null;
        int[] all = null;
        if (!locked && expanded)
        {
            counted = ExplorationOverview.Count(map, out explored, out all);
        }

        float area = ExplorationOverview.SampleArea(map);
        int shownTotal = 0;
        int shownExplored = 0;
        for (int i = 0; i < rows.Count; i++)
        {
            bool shown = counted && all[i] > 0;
            rows[i].Root.SetActive(shown);
            if (!shown)
            {
                continue;
            }

            Place(rows[i].Root.GetComponent<RectTransform>(), Padding, y, Width - Padding * 2f, RowHeight);
            SetRow(rows[i], explored[i], all[i], area);
            shownTotal += all[i];
            shownExplored += explored[i];
            y += RowHeight;
        }

        worldRow.Root.SetActive(counted);
        if (counted)
        {
            y += Gap / 2f;
            Place(worldRow.Root.GetComponent<RectTransform>(), Padding, y, Width - Padding * 2f, RowHeight);
            SetRow(worldRow, shownExplored, shownTotal, area);
            y += RowHeight;
        }

        string footText = locked
            ? string.Format(Localization.instance.Localize("$whitehilt_overview_locked"), OverviewSettings.Level.Value)
            : !expanded ? string.Empty
            : !counted ? string.Format(Localization.instance.Localize("$whitehilt_overview_measuring"), Mathf.RoundToInt(ExplorationOverview.Progress * 100f))
            : Finds();
        foot.text = footText;
        foot.gameObject.SetActive(footText.Length > 0);
        if (footText.Length > 0)
        {
            Place(foot.rectTransform, Padding, y + Gap / 2f, Width - Padding * 2f, FootHeight);
            y += Gap / 2f + FootHeight;
        }

        if (y > Padding)
        {
            y += Gap / 2f;
        }

        Place(headerButton, Padding, y, Width - Padding * 2f, HeaderHeight);
        y += HeaderHeight;
        ((RectTransform)panel.transform).sizeDelta = new Vector2(Width, y + Padding);
    }

    // What Munin's Perch has found, by group, e.g. "Found: Dungeons 12 · Settlements 3".
    private static string Finds()
    {
        if (DiscoveryOverlay.Kinds.Count == 0)
        {
            return string.Empty;
        }

        IEnumerable<string> groups = DiscoveryOverlay.Kinds.GroupBy(kind => kind.Group).OrderBy(group => group.Key)
            .Select(group => $"{DiscoveryCatalog.GetGroupLabel(group.Key)} {group.Sum(kind => kind.Total)}");
        return $"{Localization.instance.Localize("$whitehilt_overview_finds")}: {string.Join(" · ", groups)}";
    }

    private static void SetRow(Row row, int explored, int all, float area)
    {
        float share = all > 0 ? (float)explored / all : 0f;
        row.Fill.rectTransform.sizeDelta = new Vector2(BarWidth * share, BarHeight);
        row.Value.text = $"{Mathf.FloorToInt(share * 100f)} %  ({Mathf.RoundToInt(explored * area)} km²)";
    }

    private static void Create()
    {
        Vector2 bottomLeft = Vector2.zero;
        panel = GUIManager.Instance.CreateWoodpanel(GUIManager.CustomGUIFront.transform, bottomLeft, bottomLeft, Vector2.zero, Width, 100f, false);
        panel.name = "WhiteHiltOverview";
        content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(panel.transform, false);
        content.anchorMin = Vector2.zero;
        content.anchorMax = Vector2.one;
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;

        GameObject button = new("Header", typeof(RectTransform), typeof(Image), typeof(Button));
        headerButton = (RectTransform)button.transform;
        Place(headerButton, Padding, Padding, Width - Padding * 2f, HeaderHeight);
        button.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
        Button component = button.GetComponent<Button>();
        component.transition = Selectable.Transition.None;
        component.onClick.AddListener(() =>
        {
            expanded = !expanded;
            nextRefresh = 0f;
        });
        header = CreateText((RectTransform)button.transform, 18, GUIManager.Instance.ValheimOrange, TextAnchor.MiddleCenter);
        Stretch(header.rectTransform);

        rows.Clear();
        foreach (Heightmap.Biome biome in ExplorationOverview.Biomes)
        {
            rows.Add(CreateRow(Localization.instance.Localize("$biome_" + biome.ToString().ToLowerInvariant()), biomeColours[biome]));
        }

        worldRow = CreateRow(Localization.instance.Localize("$whitehilt_overview_world"), GUIManager.Instance.ValheimOrange);
        foot = CreateText(content, 13, Color.white, TextAnchor.UpperLeft);
    }

    private static Row CreateRow(string name, Color colour)
    {
        GameObject root = new("Row", typeof(RectTransform));
        RectTransform rect = (RectTransform)root.transform;
        Place(rect, 0f, 0f, Width - Padding * 2f, RowHeight);

        Text label = CreateText(rect, 14, Color.white, TextAnchor.MiddleLeft);
        Place(label.rectTransform, 0f, 0f, NameWidth, RowHeight, rect);
        label.text = name;

        Image back = new GameObject("Bar", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        Place(back.rectTransform, NameWidth + Gap, (RowHeight - BarHeight) / 2f, BarWidth, BarHeight, rect);
        back.color = barBack;
        back.raycastTarget = false;

        Image fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        Place(fill.rectTransform, 0f, 0f, 0f, BarHeight, back.rectTransform);
        fill.color = colour;
        fill.raycastTarget = false;

        Text value = CreateText(rect, 13, Color.white, TextAnchor.MiddleRight);
        Place(value.rectTransform, NameWidth + Gap * 2f + BarWidth, 0f, ValueWidth, RowHeight, rect);
        root.SetActive(false);
        return new Row { Root = root, Fill = fill, Value = value };
    }

    private static Text CreateText(RectTransform parent, int size, Color colour, TextAnchor alignment)
    {
        GameObject label = GUIManager.Instance.CreateText(string.Empty, parent, Vector2.zero, Vector2.zero, Vector2.zero, GUIManager.Instance.AveriaSerifBold, size,
            colour, true, Color.black, 0f, 0f, false);
        Text text = label.GetComponent<Text>();
        text.alignment = alignment;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // Laid out from the parent's top-left corner (the panel's content by default), y growing downwards.
    private static void Place(RectTransform rect, float x, float y, float width, float height, RectTransform parent = null)
    {
        rect.SetParent(parent ?? content, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private sealed class Row
    {
        public GameObject Root;
        public Image Fill;
        public Text Value;
    }
}
