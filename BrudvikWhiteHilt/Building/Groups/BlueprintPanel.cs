using BrudvikWhiteHilt.Building.Media;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Building.Groups;

/// <summary>
/// The blueprint panel on the right: the saved blueprints with search, what the chosen one costs against what you have,
/// and buttons to place, rename or delete it.
/// </summary>
public class BlueprintPanel : MonoBehaviour
{
    private const float PanelWidth = 440f;
    private const float Padding = 14f;
    private const float RowHeight = 30f;
    private const float RowGap = 4f;
    private const float ListHeight = 300f;
    private const float DetailsHeight = 170f;
    private const float ConfirmSeconds = 3f;
    private const int FontSize = 14;

    private static readonly Color selectedColor = new(1f, 0.8f, 0.4f);
    private static readonly Color lackingColor = new(1f, 0.45f, 0.35f);

    private static BlueprintPanel instance;

    private readonly List<GameObject> rows = new();

    private GameObject panel;
    private InputField search;
    private RectTransform listContent;
    private Text details;
    private Text deleteLabel;
    private List<Blueprint> blueprints;
    private Blueprint selected;
    private float deleteArmedUntil;
    private bool inputBlocked;
    private float nextRow;

    /// <summary>True while the panel is open.</summary>
    public static bool IsOpen => instance != null && instance.panel.activeSelf;

    /// <summary>
    /// Creates the panel once the custom GUI exists. Safe to call every frame.
    /// </summary>
    public static void Ensure()
    {
        if (instance != null || GUIManager.CustomGUIFront == null)
        {
            return;
        }

        GameObject root = new("WhiteHiltBlueprintPanel", typeof(RectTransform));
        root.transform.SetParent(GUIManager.CustomGUIFront.transform, false);
        RectTransform rect = (RectTransform)root.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        instance = root.AddComponent<BlueprintPanel>();
        instance.Build();
    }

    /// <summary>
    /// Opens or closes the panel.
    /// </summary>
    public static void Toggle()
    {
        Ensure();
        if (instance == null)
        {
            return;
        }

        if (IsOpen)
        {
            Close();
            return;
        }

        MediaPanel.Close();
        instance.panel.SetActive(true);
        Reload();
    }

    /// <summary>
    /// Closes the panel.
    /// </summary>
    public static void Close()
    {
        if (instance != null)
        {
            instance.panel.SetActive(false);
            instance.SetInputBlocked(false);
        }
    }

    /// <summary>
    /// Reads the blueprint folder again, e.g. after saving.
    /// </summary>
    public static void Reload()
    {
        if (instance == null || !IsOpen)
        {
            return;
        }

        string keep = instance.selected?.File;
        instance.blueprints = BlueprintStore.LoadAll();
        instance.selected = instance.blueprints.FirstOrDefault(blueprint => blueprint.File == keep);
        instance.RefreshList();
    }

    private void Update()
    {
        if (!IsOpen)
        {
            return;
        }

        Player player = Player.m_localPlayer;
        if (player == null || !player.InPlaceMode() || MediaMode.HideUi)
        {
            Close();
            return;
        }

        SetInputBlocked(search.isFocused);
        if (deleteArmedUntil > 0f && Time.unscaledTime > deleteArmedUntil)
        {
            deleteArmedUntil = 0f;
            deleteLabel.text = Localization.instance.Localize("$whitehilt_media_delete");
        }
    }

    private void OnDestroy()
    {
        SetInputBlocked(false);
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Build()
    {
        panel = GUIManager.Instance.CreateWoodpanel(transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, PanelWidth, 700f, false);
        panel.name = "Panel";
        RectTransform rect = (RectTransform)panel.transform;
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(-20f, 0f);

        float full = PanelWidth - Padding * 2f;
        float third = (full - RowGap * 2f) / 3f;
        float half = (full - RowGap) / 2f;

        nextRow = Padding;
        Text title = AddText(Localization.instance.Localize("$whitehilt_group_blueprints"), 22, GUIManager.Instance.ValheimOrange, 26f);
        title.font = GUIManager.Instance.AveriaSerifBold;
        title.alignment = TextAnchor.MiddleCenter;
        NextRow(26f);

        search = GUIManager.Instance.CreateInputField(panel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(Padding + full / 2f, -(nextRow + RowHeight / 2f)), InputField.ContentType.Standard,
            Localization.instance.Localize("$whitehilt_group_search"), FontSize, full, RowHeight).GetComponent<InputField>();
        search.onValueChanged.AddListener(_ => RefreshList());
        NextRow(RowHeight);

        GameObject scroll = GUIManager.Instance.CreateScrollView(panel.transform, false, true, 8f, 4f, GUIManager.Instance.ValheimScrollbarHandleColorBlock,
            new Color(0f, 0f, 0f, 0.35f), full, ListHeight);
        RectTransform scrollRect = (RectTransform)scroll.transform;
        scrollRect.anchorMin = new Vector2(0f, 1f);
        scrollRect.anchorMax = new Vector2(0f, 1f);
        scrollRect.pivot = new Vector2(0f, 1f);
        scrollRect.anchoredPosition = new Vector2(Padding, -nextRow);
        scrollRect.sizeDelta = new Vector2(full, ListHeight);
        listContent = scroll.GetComponentInChildren<ScrollRect>().content;
        VerticalLayoutGroup layout = listContent.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        NextRow(ListHeight);

        details = AddText(string.Empty, 13, Color.white, DetailsHeight);
        details.alignment = TextAnchor.UpperLeft;
        details.supportRichText = true;
        NextRow(DetailsHeight);

        AddButton(0f, third, OnPlace).text = Localization.instance.Localize("$whitehilt_group_use");
        AddButton(third + RowGap, third, OnRename).text = Localization.instance.Localize("$whitehilt_group_rename");
        deleteLabel = AddButton((third + RowGap) * 2f, third, OnDelete);
        deleteLabel.text = Localization.instance.Localize("$whitehilt_media_delete");
        NextRow(RowHeight);
        AddButton((full - half) / 2f, half, Close).text = Localization.instance.Localize("$whitehilt_group_close");
        NextRow(RowHeight);

        rect.sizeDelta = new Vector2(PanelWidth, nextRow - RowGap + Padding);
        panel.SetActive(false);
    }

    private void OnPlace()
    {
        if (selected == null)
        {
            return;
        }

        GroupTools.StartPaste(selected, null);
        Close();
    }

    private void OnRename()
    {
        Blueprint blueprint = selected;
        if (blueprint == null)
        {
            return;
        }

        GroupTools.NameDialog.Ask(blueprint.Name, name =>
        {
            blueprint.Name = name;
            BlueprintStore.Save(blueprint);
            Reload();
        });
    }

    private void OnDelete()
    {
        if (selected == null)
        {
            return;
        }

        if (Time.unscaledTime > deleteArmedUntil)
        {
            deleteArmedUntil = Time.unscaledTime + ConfirmSeconds;
            deleteLabel.text = Localization.instance.Localize("$whitehilt_media_confirm");
            return;
        }

        deleteArmedUntil = 0f;
        deleteLabel.text = Localization.instance.Localize("$whitehilt_media_delete");
        BlueprintStore.Delete(selected);
        selected = null;
        Reload();
    }

    private void RefreshList()
    {
        rows.ForEach(Destroy);
        rows.Clear();
        float width = PanelWidth - Padding * 2f - 20f;
        string filter = search.text.Trim();
        List<Blueprint> visible = (blueprints ?? new List<Blueprint>())
            .Where(blueprint => filter.Length == 0 || blueprint.Name.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0).ToList();
        if (visible.Count == 0)
        {
            GameObject empty = GUIManager.Instance.CreateText(Localization.instance.Localize("$whitehilt_group_none"), listContent, Vector2.zero,
                Vector2.zero, Vector2.zero, GUIManager.Instance.AveriaSerif, FontSize, Color.white, true, Color.black, width, 80f, false);
            empty.AddComponent<LayoutElement>().preferredHeight = 80f;
            rows.Add(empty);
        }

        foreach (Blueprint blueprint in visible)
        {
            string text = blueprint.Name + "   " + string.Format(Localization.instance.Localize("$whitehilt_group_pieces"), blueprint.Pieces.Count);
            GameObject row = GUIManager.Instance.CreateButton(text, listContent, Vector2.zero, Vector2.zero, Vector2.zero, width, RowHeight);
            row.AddComponent<LayoutElement>().preferredHeight = RowHeight;
            Text label = row.GetComponentInChildren<Text>();
            label.alignment = TextAnchor.MiddleLeft;
            label.fontSize = FontSize;
            label.color = blueprint == selected ? selectedColor : Color.white;
            label.rectTransform.offsetMin = new Vector2(10f, 0f);
            Blueprint chosen = blueprint;
            row.GetComponent<Button>().onClick.AddListener(() =>
            {
                selected = chosen;
                RefreshList();
            });
            rows.Add(row);
        }

        RefreshDetails();
    }

    private void RefreshDetails()
    {
        Player player = Player.m_localPlayer;
        if (selected == null || player == null)
        {
            details.text = string.Empty;
            return;
        }

        List<Piece> pieces = new();
        int unknown = 0;
        foreach (PieceSnapshot snapshot in selected.Pieces)
        {
            Piece piece = GroupPlacer.Resolve(snapshot.Prefab);
            if (piece != null)
            {
                pieces.Add(piece);
            }
            else
            {
                unknown++;
            }
        }

        StringBuilder text = new();
        text.Append("<b>").Append(selected.Name).Append("</b>");
        if (!string.IsNullOrEmpty(selected.Creator))
        {
            text.Append("  -  ").Append(selected.Creator);
        }

        text.AppendLine();
        foreach (KeyValuePair<ItemDrop, int> cost in GroupPlacer.Cost(pieces).OrderBy(pair => Localization.instance.Localize(pair.Key.m_itemData.m_shared.m_name)))
        {
            int have = GroupPlacer.Have(player, cost.Key);
            string line = $"{Localization.instance.Localize(cost.Key.m_itemData.m_shared.m_name)}  {have}/{cost.Value}";
            text.AppendLine(have >= cost.Value ? line : $"<color=#{ColorUtility.ToHtmlStringRGB(lackingColor)}>{line}</color>");
        }

        if (unknown > 0)
        {
            text.AppendLine(string.Format(Localization.instance.Localize("$whitehilt_group_unknown"), unknown));
        }

        details.text = text.ToString();
    }

    private void SetInputBlocked(bool block)
    {
        if (block != inputBlocked)
        {
            GUIManager.BlockInput(block);
            inputBlocked = block;
        }
    }

    private Text AddText(string text, int fontSize, Color color, float height)
    {
        float width = PanelWidth - Padding * 2f;
        return GUIManager.Instance.CreateText(text, panel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(Padding + width / 2f, -(nextRow + height / 2f)), GUIManager.Instance.AveriaSerif, fontSize, color, true, Color.black,
            width, height, false).GetComponent<Text>();
    }

    private Text AddButton(float x, float width, Action onClick)
    {
        GameObject button = GUIManager.Instance.CreateButton(string.Empty, panel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(Padding + x + width / 2f, -(nextRow + RowHeight / 2f)), width, RowHeight);
        button.GetComponent<Button>().onClick.AddListener(() => onClick());
        Text label = button.GetComponentInChildren<Text>();
        label.fontSize = FontSize;
        return label;
    }

    private void NextRow(float height)
    {
        nextRow += height + RowGap;
    }
}
