using BrudvikWhiteHilt.Clock;
using BrudvikWhiteHilt.Pieces.Navigation;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Navigation.Weather;

/// <summary>
/// The weather forecast at the top right of the large map: now and the coming periods, each with its icon, weather and
/// wind. It shows near a map table or a Cartographer's Desk, and aboard a ship with a Navigator's Table.
/// </summary>
public static class ForecastPanel
{
    private const float Right = 30f;
    private const float Top = 110f;
    private const float Width = 320f;
    private const float Padding = 14f;
    private const float TitleHeight = 28f;
    private const float RowHeight = 44f;
    private const float IconSize = 32f;
    private const float NoteHeight = 34f;
    private const float RefreshInterval = 1f;

    private static readonly List<Piece> nearbyPieces = new();
    private static readonly List<Row> rows = new();

    private static GameObject panel;
    private static RectTransform content;
    private static Text title;
    private static Text note;
    private static float nextRefresh;
    private static bool available;

    /// <summary>
    /// Shows or hides the panel with the large map and keeps it up to date. Called after the map's own update.
    /// </summary>
    /// <param name="map">The map.</param>
    public static void Update(Minimap map)
    {
        Player player = Player.m_localPlayer;
        bool open = map.m_mode == Minimap.MapMode.Large && player != null && ForecastSettings.Forecast.Value && GUIManager.CustomGUIFront != null;
        if (open && Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + RefreshInterval;
            available = IsAvailable(player);
            if (available)
            {
                Refresh(player);
            }
        }

        bool visible = open && available;
        if (panel != null && panel.activeSelf != visible)
        {
            panel.SetActive(visible);
        }

        if (!open)
        {
            nextRefresh = 0f;
        }
    }

    // Near a map table or a Cartographer's Desk, or aboard a ship with a Navigator's Table.
    private static bool IsAvailable(Player player)
    {
        if (ShipChartTable.IsAboardWithTable(player))
        {
            return true;
        }

        nearbyPieces.Clear();
        Piece.GetAllPiecesInRadius(player.transform.position, ForecastSettings.Range.Value, nearbyPieces);
        return nearbyPieces.Exists(piece => piece != null
            && (piece.GetComponent<MapTable>() != null || Utils.GetPrefabName(piece.gameObject) == CartographerDesk.StationPrefabName));
    }

    private static void Refresh(Player player)
    {
        if (panel == null)
        {
            Create();
        }

        List<WeatherForecast.Period> periods = WeatherForecast.Foretell(ForecastSettings.PeriodsAhead(ExplorationSkill.GetLevel(player)));
        BiomeSector sector = EnvMan.instance.GetBiome();
        string biome = Localization.instance.Localize("$biome_" + sector.Biome.ToString().ToLowerInvariant());
        title.text = string.Format(Localization.instance.Localize("$whitehilt_forecast_title"), biome);

        while (rows.Count < periods.Count)
        {
            rows.Add(CreateRow(rows.Count));
        }

        for (int i = 0; i < rows.Count; i++)
        {
            bool shown = i < periods.Count;
            rows[i].Root.SetActive(shown);
            if (!shown)
            {
                continue;
            }

            WeatherForecast.Period period = periods[i];
            string when = i == 0
                ? Localization.instance.Localize("$whitehilt_forecast_now")
                : string.Format(Localization.instance.Localize("$whitehilt_forecast_in"), Mathf.Max(1, Mathf.CeilToInt((float)period.StartsIn / 60f)));
            rows[i].Icon.sprite = GameClock.LoadIcon(GameClock.WeatherKey(period.Env, period.Night));
            rows[i].Text.text = $"<b>{when}</b>  {WeatherForecast.Describe(period)}\n{WeatherForecast.DescribeWind(period)}";
        }

        string noteText = WeatherForecast.Overridden()
            ? Localization.instance.Localize("$whitehilt_forecast_forced")
            : ExplorationSkill.GetLevel(player) < 100 && ForecastSettings.PeriodsAtLevel100.Value > ForecastSettings.PeriodsAtLevelZero.Value
                ? Localization.instance.Localize("$whitehilt_forecast_more")
                : string.Empty;
        note.text = noteText;
        float y = Padding + TitleHeight + periods.Count * RowHeight;
        note.rectTransform.anchoredPosition = new Vector2(Padding, -y);
        if (noteText.Length > 0)
        {
            y += NoteHeight;
        }

        ((RectTransform)panel.transform).sizeDelta = new Vector2(Width, y + Padding);
    }

    private static void Create()
    {
        Vector2 topRight = new(1f, 1f);
        panel = GUIManager.Instance.CreateWoodpanel(GUIManager.CustomGUIFront.transform, topRight, topRight, Vector2.zero, Width, 100f, false);
        panel.name = "WhiteHiltForecast";
        RectTransform rect = (RectTransform)panel.transform;
        rect.pivot = topRight;
        rect.anchoredPosition = new Vector2(-Right, -Top);
        content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(panel.transform, false);
        content.anchorMin = Vector2.zero;
        content.anchorMax = Vector2.one;
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;
        title = CreateText(content, 18, GUIManager.Instance.ValheimOrange, TextAnchor.MiddleCenter, Padding, Padding, Width - Padding * 2f, TitleHeight);
        note = CreateText(content, 13, Color.white, TextAnchor.MiddleCenter, Padding, Padding, Width - Padding * 2f, NoteHeight);
        rows.Clear();
    }

    private static Row CreateRow(int index)
    {
        GameObject root = new($"Row{index}", typeof(RectTransform));
        RectTransform rect = (RectTransform)root.transform;
        Place(rect, content, Padding, Padding + TitleHeight + index * RowHeight, Width - Padding * 2f, RowHeight);

        GameObject iconObject = new("Icon", typeof(RectTransform), typeof(Image));
        Place((RectTransform)iconObject.transform, rect, 0f, (RowHeight - IconSize) / 2f, IconSize, IconSize);
        Image icon = iconObject.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        Text text = CreateText(rect, 15, Color.white, TextAnchor.MiddleLeft, IconSize + 8f, 0f, Width - Padding * 2f - IconSize - 8f, RowHeight);
        return new Row { Root = root, Icon = icon, Text = text };
    }

    private static Text CreateText(RectTransform parent, int size, Color colour, TextAnchor alignment, float x, float y, float width, float height)
    {
        GameObject label = GUIManager.Instance.CreateText(string.Empty, parent, Vector2.zero, Vector2.zero, Vector2.zero, GUIManager.Instance.AveriaSerifBold, size,
            colour, true, Color.black, width, height, false);
        Text text = label.GetComponent<Text>();
        Place(text.rectTransform, parent, x, y, width, height);
        text.alignment = alignment;
        text.raycastTarget = false;
        text.supportRichText = true;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    // Laid out from the parent's top-left corner, y growing downwards.
    private static void Place(RectTransform rect, RectTransform parent, float x, float y, float width, float height)
    {
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private sealed class Row
    {
        public GameObject Root;
        public Image Icon;
        public Text Text;
    }
}
