using BrudvikWhiteHilt.Backpack;
using BrudvikWhiteHilt.Building.Media;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Production;

/// <summary>
/// A list of the stations near the player at the right side of the screen, opened with a key: what has stopped first,
/// then what needs the player soon, what is ready, and what is working, soonest done first. It does not take the
/// mouse or keys, so the player can walk around with it open.
/// </summary>
public static class ProductionOverview
{
    private const float RefreshInterval = 1f;
    private const int MaxRows = 18;
    private const float Width = 440f;
    private const float Padding = 16f;
    private const float TitleHeight = 30f;
    private const float RowHeight = 20f;
    private const int FontSize = 15;

    private static readonly List<Component> nearby = new();
    private static readonly List<Row> rows = new();
    private static readonly StringBuilder builder = new();

    private static bool open;
    private static GameObject panel;
    private static Text body;
    private static float nextRefresh;

    /// <summary>
    /// Opens or closes the overview on its key and keeps it up to date. Called after the HUD update.
    /// </summary>
    /// <param name="hud">The HUD.</param>
    /// <param name="player">The local player.</param>
    public static void Update(Hud hud, Player player)
    {
        if (!BackpackInput.Typing() && !player.InPlaceMode() && BackpackInput.Pressed(ProductionSettings.KeyOverview))
        {
            open = !open;
            nextRefresh = 0f;
        }

        bool show = open && !MediaMode.HideUi && !InventoryGui.IsVisible() && !Minimap.IsOpen() && !Menu.IsVisible() && !player.IsDead();
        if (!show)
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }

            return;
        }

        if (!Ensure(hud))
        {
            return;
        }

        panel.SetActive(true);
        if (Time.unscaledTime < nextRefresh)
        {
            return;
        }

        nextRefresh = Time.unscaledTime + RefreshInterval;
        Refresh(player);
    }

    // Lists what is working near the player: name, status and distance, sorted.
    private static void Refresh(Player player)
    {
        Vector3 position = player.transform.position;
        ProductionRegistry.Near(position, ProductionSettings.OverviewRange.Value, nearby);
        rows.Clear();
        foreach (Component source in nearby)
        {
            ProductionStatus status = new();
            if (ProductionReader.Read(source, status, false) && ProductionLabels.Wanted(status) && !string.IsNullOrEmpty(status.Summary))
            {
                rows.Add(new Row(status, Vector3.Distance(position, source.transform.position)));
            }
        }

        rows.Sort(Compare);
        builder.Clear();
        int shown = Mathf.Min(rows.Count, MaxRows);
        for (int i = 0; i < shown; i++)
        {
            Row row = rows[i];
            if (i > 0)
            {
                builder.Append('\n');
            }

            builder.Append(Localization.instance.Localize(row.Status.Name)).Append("   ").Append(row.Status.Summary)
                .Append("   <color=#BBBBBB>").Append(Mathf.RoundToInt(row.Distance)).Append(" m</color>");
        }

        if (rows.Count > shown)
        {
            builder.Append("\n<color=#BBBBBB>+").Append(rows.Count - shown).Append("</color>");
            shown++;
        }

        if (shown == 0)
        {
            builder.Append(Localization.instance.Localize("$whitehilt_prod_overview_empty"));
            shown = 1;
        }

        body.text = builder.ToString();
        RectTransform rect = (RectTransform)panel.transform;
        float height = Padding * 2f + TitleHeight + shown * RowHeight;
        rect.sizeDelta = new Vector2(Width, height);
        body.rectTransform.sizeDelta = new Vector2(Width - Padding * 2f, shown * RowHeight + 4f);
    }

    private static int Compare(Row a, Row b)
    {
        int order = Rank(a.Status).CompareTo(Rank(b.Status));
        if (order != 0)
        {
            return order;
        }

        order = a.Status.Seconds.CompareTo(b.Status.Seconds);
        return order != 0 ? order : a.Distance.CompareTo(b.Distance);
    }

    private static int Rank(ProductionStatus status)
    {
        if (status.State == ProductionState.Stopped)
        {
            return 0;
        }

        if (status.Attention)
        {
            return 1;
        }

        return status.State == ProductionState.Ready ? 2 : 3;
    }

    // Under the HUD root, so it goes away with the HUD.
    private static bool Ensure(Hud hud)
    {
        if (panel != null && panel.transform.parent == hud.m_rootObject.transform)
        {
            return true;
        }

        if (GUIManager.Instance == null || GUIManager.Instance.AveriaSerifBold == null)
        {
            return false;
        }

        panel = GUIManager.Instance.CreateWoodpanel(hud.m_rootObject.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero,
            Width, 200f, false);
        panel.name = "WhiteHiltProductionOverview";
        RectTransform rect = (RectTransform)panel.transform;
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(-20f, 60f);
        foreach (Graphic graphic in panel.GetComponentsInChildren<Graphic>())
        {
            graphic.raycastTarget = false;
        }

        Text title = GUIManager.Instance.CreateText(Localization.instance.Localize("$whitehilt_prod_overview"), panel.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -Padding - TitleHeight / 2f), GUIManager.Instance.AveriaSerifBold, 20,
            GUIManager.Instance.ValheimOrange, true, Color.black, Width - Padding * 2f, TitleHeight, false).GetComponent<Text>();
        title.alignment = TextAnchor.MiddleCenter;
        title.raycastTarget = false;

        body = GUIManager.Instance.CreateText(string.Empty, panel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero,
            GUIManager.Instance.AveriaSerifBold, FontSize, Color.white, true, Color.black, Width - Padding * 2f, RowHeight, false).GetComponent<Text>();
        body.alignment = TextAnchor.UpperLeft;
        body.supportRichText = true;
        body.raycastTarget = false;
        body.horizontalOverflow = HorizontalWrapMode.Overflow;
        body.rectTransform.pivot = new Vector2(0f, 1f);
        body.rectTransform.anchoredPosition = new Vector2(Padding, -Padding - TitleHeight);
        nextRefresh = 0f;
        return true;
    }

    private readonly struct Row
    {
        public readonly ProductionStatus Status;
        public readonly float Distance;

        public Row(ProductionStatus status, float distance)
        {
            Status = status;
            Distance = distance;
        }
    }
}
