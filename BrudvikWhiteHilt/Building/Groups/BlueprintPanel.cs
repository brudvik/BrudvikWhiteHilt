using BrudvikWhiteHilt.Building.Media;
using BrudvikWhiteHilt.School;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Building.Groups;

/// <summary>
/// The blueprint panel on the right, with two tabs. Mine: the saved blueprints with search, what the chosen one costs
/// against what you have, and buttons to place, rename, delete or share it. Shared: the blueprints shared in this world
/// on the server, to fetch a copy of or, for your own, to unshare. The hammer opens it; a School Bench opens it on the
/// shared tab without one.
/// </summary>
public class BlueprintPanel : MonoBehaviour
{
    private const float PanelWidth = 440f;
    private const float Padding = 14f;
    private const float RowHeight = 30f;
    private const float RowGap = 4f;
    private const float ListHeight = 300f;
    private const float DetailsHeight = 150f;
    private const float ConfirmSeconds = 3f;
    private const float BenchLeaveMargin = 2f;
    private const int FontSize = 14;

    private static readonly Color selectedColor = new(1f, 0.8f, 0.4f);
    private static readonly Color lackingColor = new(1f, 0.45f, 0.35f);
    private static readonly Color mineColor = new(0.6f, 0.9f, 0.6f);

    private static BlueprintPanel instance;

    private readonly List<GameObject> rows = new();

    private GameObject panel;
    private InputField search;
    private RectTransform listContent;
    private Text details;
    private Text mineTab;
    private Text sharedTab;
    private Text firstLabel;
    private Text secondLabel;
    private Text thirdLabel;
    private GameObject secondButton;
    private GameObject thirdButton;
    private GameObject shareButton;
    private List<Blueprint> blueprints;
    private Blueprint selected;
    private SharedBlueprints.Entry selectedShared;
    private SchoolBench openedAt;
    private bool shared;
    private float confirmUntil;
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
        instance.openedAt = null;
        instance.panel.SetActive(true);
        instance.SetTab(instance.shared);
    }

    /// <summary>
    /// Opens the panel on the shared tab from a School Bench; it stays open without a build tool while you are there.
    /// </summary>
    /// <param name="bench">The bench.</param>
    public static void OpenShared(SchoolBench bench)
    {
        Ensure();
        if (instance == null)
        {
            return;
        }

        MediaPanel.Close();
        instance.openedAt = bench;
        instance.panel.SetActive(true);
        instance.SetTab(true);
        instance.SetInputBlocked(true);
    }

    /// <summary>
    /// Closes the panel.
    /// </summary>
    public static void Close()
    {
        if (instance != null)
        {
            instance.panel.SetActive(false);
            instance.openedAt = null;
            instance.SetInputBlocked(false);
        }
    }

    /// <summary>
    /// Reads the blueprint folder again, e.g. after saving or fetching.
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

    // Closes the panel when the build tool is put away (unless it was opened at a School Bench and you are still
    // there) or the screen is cleared for a photo, keeps the game from reading keys while the search field has focus,
    // and puts a confirming button back after its time.
    private void Update()
    {
        if (!IsOpen)
        {
            return;
        }

        Player player = Player.m_localPlayer;
        bool atBench = openedAt != null
            && Vector3.Distance(player != null ? player.transform.position : Vector3.zero, openedAt.transform.position) <= SchoolSettings.BlueprintRange.Value + BenchLeaveMargin;
        if (player == null || MediaMode.HideUi || (!player.InPlaceMode() && !atBench) || (openedAt != null && Input.GetKeyDown(KeyCode.Escape)))
        {
            Close();
            return;
        }

        SetInputBlocked(search.isFocused || openedAt != null);
        if (confirmUntil > 0f && Time.unscaledTime > confirmUntil)
        {
            confirmUntil = 0f;
            UpdateButtons();
        }

        // The answer from the server, or its silence, changes what the shared tab says.
        if (shared && Time.frameCount % 30 == 0 && !SharedBlueprints.Answered)
        {
            RefreshDetails();
        }
    }

    private void OnDestroy()
    {
        SharedBlueprints.Changed -= OnSharedChanged;
        SetInputBlocked(false);
        if (instance == this)
        {
            instance = null;
        }
    }

    // Builds the panel once: tabs, search field, list, the chosen blueprint's details and the buttons. The three
    // buttons of the first row change with the tab.
    private void Build()
    {
        panel = GUIManager.Instance.CreateWoodpanel(transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, PanelWidth, 760f, false);
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

        mineTab = AddButton(0f, half, () => SetTab(false)).Label;
        mineTab.text = Localization.instance.Localize("$whitehilt_blueprint_mine");
        sharedTab = AddButton(half + RowGap, half, () => SetTab(true)).Label;
        sharedTab.text = Localization.instance.Localize("$whitehilt_blueprint_shared");
        NextRow(RowHeight);

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

        firstLabel = AddButton(0f, third, OnFirst).Label;
        (secondButton, secondLabel) = AddButton(third + RowGap, third, OnSecond);
        (thirdButton, thirdLabel) = AddButton((third + RowGap) * 2f, third, OnThird);
        NextRow(RowHeight);
        Text shareLabel;
        (shareButton, shareLabel) = AddButton(0f, half, OnShare);
        shareLabel.text = Localization.instance.Localize("$whitehilt_blueprint_share");
        AddButton(half + RowGap, half, Close).Label.text = Localization.instance.Localize("$whitehilt_group_close");
        NextRow(RowHeight);

        rect.sizeDelta = new Vector2(PanelWidth, nextRow - RowGap + Padding);
        SharedBlueprints.Changed += OnSharedChanged;
        panel.SetActive(false);
    }

    // Switches between your own blueprints and the shared ones; the shared list is asked for anew each time.
    private void SetTab(bool showShared)
    {
        shared = showShared;
        confirmUntil = 0f;
        mineTab.color = shared ? Color.white : selectedColor;
        sharedTab.color = shared ? selectedColor : Color.white;
        if (shared)
        {
            selectedShared = null;
            SharedBlueprints.RequestIndex();
            RefreshList();
        }
        else
        {
            Reload();
        }
    }

    private void OnSharedChanged()
    {
        if (IsOpen && shared)
        {
            selectedShared = SharedBlueprints.Index.FirstOrDefault(entry => entry.Id == selectedShared?.Id);
            RefreshList();
        }
    }

    // Place a blueprint of your own, or fetch a shared one.
    private void OnFirst()
    {
        Player player = Player.m_localPlayer;
        if (shared)
        {
            if (selectedShared != null && CanUseServer(player))
            {
                SharedBlueprints.Fetch(selectedShared);
            }

            return;
        }

        if (selected == null || player == null)
        {
            return;
        }

        if (!player.InPlaceMode())
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_blueprint_hammer");
            return;
        }

        GroupTools.StartPaste(selected, null);
        Close();
    }

    // Rename a blueprint of your own, or unshare one you shared (on the second click within a few seconds).
    private void OnSecond()
    {
        if (shared)
        {
            if (selectedShared == null || !selectedShared.Mine)
            {
                return;
            }

            if (!Confirm())
            {
                return;
            }

            SharedBlueprints.Unshare(selectedShared);
            selectedShared = null;
            return;
        }

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

    // Deletes the chosen blueprint on the second click within a few seconds, so a blueprint is not lost to a slip of
    // the mouse.
    private void OnThird()
    {
        if (shared || selected == null || !Confirm())
        {
            return;
        }

        BlueprintStore.Delete(selected);
        selected = null;
        Reload();
    }

    private void OnShare()
    {
        if (!shared && selected != null && CanUseServer(Player.m_localPlayer))
        {
            SharedBlueprints.Share(selected);
        }
    }

    // True on the second click of a button that asks to be sure.
    private bool Confirm()
    {
        if (Time.unscaledTime > confirmUntil)
        {
            confirmUntil = Time.unscaledTime + ConfirmSeconds;
            UpdateButtons();
            return false;
        }

        confirmUntil = 0f;
        UpdateButtons();
        return true;
    }

    // Sharing and fetching must be switched on, and may need a School Bench nearby.
    private static bool CanUseServer(Player player)
    {
        if (player == null)
        {
            return false;
        }

        if (!SharedBlueprintSettings.Enabled.Value)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_blueprint_disabled");
            return false;
        }

        if (SharedBlueprintSettings.NeedSchoolBench.Value && !SchoolBench.IsNear(player.transform.position))
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_blueprint_needbench");
            return false;
        }

        return true;
    }

    private void UpdateButtons()
    {
        bool confirming = confirmUntil > 0f;
        string confirm = Localization.instance.Localize("$whitehilt_media_confirm");
        if (shared)
        {
            firstLabel.text = Localization.instance.Localize("$whitehilt_blueprint_fetch");
            secondLabel.text = confirming ? confirm : Localization.instance.Localize("$whitehilt_blueprint_unshare");
            secondButton.SetActive(selectedShared != null && selectedShared.Mine);
            thirdButton.SetActive(false);
            shareButton.SetActive(false);
            return;
        }

        firstLabel.text = Localization.instance.Localize("$whitehilt_group_use");
        secondLabel.text = Localization.instance.Localize("$whitehilt_group_rename");
        thirdLabel.text = confirming ? confirm : Localization.instance.Localize("$whitehilt_media_delete");
        secondButton.SetActive(true);
        thirdButton.SetActive(true);
        shareButton.SetActive(SharedBlueprintSettings.Enabled.Value);
    }

    // Lists the blueprints of the tab that match the search; clicking one selects it.
    private void RefreshList()
    {
        rows.ForEach(Destroy);
        rows.Clear();
        float width = PanelWidth - Padding * 2f - 20f;
        string filter = search.text.Trim();
        bool Matches(string name) => filter.Length == 0 || name.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0;

        if (shared)
        {
            List<SharedBlueprints.Entry> visible = SharedBlueprints.Index.Where(entry => Matches(entry.Name)).ToList();
            if (visible.Count == 0)
            {
                AddEmptyRow(SharedBlueprints.Answered ? "$whitehilt_blueprint_none" : string.Empty, width);
            }

            foreach (SharedBlueprints.Entry entry in visible)
            {
                string text = $"{entry.Name}   {string.Format(Localization.instance.Localize("$whitehilt_group_pieces"), entry.Pieces)}   {entry.Creator}";
                SharedBlueprints.Entry chosen = entry;
                AddRow(text, width, entry == selectedShared ? selectedColor : entry.Mine ? mineColor : Color.white, () => selectedShared = chosen);
            }
        }
        else
        {
            List<Blueprint> visible = (blueprints ?? new List<Blueprint>()).Where(blueprint => Matches(blueprint.Name)).ToList();
            if (visible.Count == 0)
            {
                AddEmptyRow("$whitehilt_group_none", width);
            }

            foreach (Blueprint blueprint in visible)
            {
                string text = blueprint.Name + "   " + string.Format(Localization.instance.Localize("$whitehilt_group_pieces"), blueprint.Pieces.Count);
                Blueprint chosen = blueprint;
                AddRow(text, width, blueprint == selected ? selectedColor : Color.white, () => selected = chosen);
            }
        }

        UpdateButtons();
        RefreshDetails();
    }

    private void AddEmptyRow(string token, float width)
    {
        GameObject empty = GUIManager.Instance.CreateText(token.Length > 0 ? Localization.instance.Localize(token) : string.Empty, listContent, Vector2.zero,
            Vector2.zero, Vector2.zero, GUIManager.Instance.AveriaSerif, FontSize, Color.white, true, Color.black, width, 80f, false);
        empty.AddComponent<LayoutElement>().preferredHeight = 80f;
        rows.Add(empty);
    }

    private void AddRow(string text, float width, Color color, Action select)
    {
        GameObject row = GUIManager.Instance.CreateButton(text, listContent, Vector2.zero, Vector2.zero, Vector2.zero, width, RowHeight);
        row.AddComponent<LayoutElement>().preferredHeight = RowHeight;
        Text label = row.GetComponentInChildren<Text>();
        label.alignment = TextAnchor.MiddleLeft;
        label.fontSize = FontSize;
        label.color = color;
        label.rectTransform.offsetMin = new Vector2(10f, 0f);
        row.GetComponent<Button>().onClick.AddListener(() =>
        {
            confirmUntil = 0f;
            select();
            RefreshList();
        });
        rows.Add(row);
    }

    // Shows the chosen blueprint. Your own: its name and maker, what it costs against what the player and the chests
    // nearby have, and how many of its pieces this game does not know (from a mod not installed), which are skipped
    // when placed. A shared one: who shared it and how many pieces it has, or what the server said.
    private void RefreshDetails()
    {
        Player player = Player.m_localPlayer;
        if (shared)
        {
            details.text = SharedDetails();
            return;
        }

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

    private string SharedDetails()
    {
        if (!SharedBlueprints.Answered)
        {
            return Localization.instance.Localize(SharedBlueprints.Waiting ? "$whitehilt_blueprint_waiting" : "$whitehilt_blueprint_noserver");
        }

        StringBuilder text = new();
        if (selectedShared != null)
        {
            text.Append("<b>").Append(selectedShared.Name).Append("</b>");
            if (selectedShared.Mine)
            {
                text.Append("  ").Append(Localization.instance.Localize("$whitehilt_blueprint_yours"));
            }

            text.AppendLine();
            text.AppendLine(string.Format(Localization.instance.Localize("$whitehilt_blueprint_by"), selectedShared.Creator));
            text.AppendLine(string.Format(Localization.instance.Localize("$whitehilt_group_pieces"), selectedShared.Pieces));
            text.AppendLine();
        }

        text.Append(Localization.instance.Localize("$whitehilt_blueprint_fetch_hint"));
        return text.ToString();
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

    private (GameObject Button, Text Label) AddButton(float x, float width, Action onClick)
    {
        GameObject button = GUIManager.Instance.CreateButton(string.Empty, panel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(Padding + x + width / 2f, -(nextRow + RowHeight / 2f)), width, RowHeight);
        button.GetComponent<Button>().onClick.AddListener(() => onClick());
        Text label = button.GetComponentInChildren<Text>();
        label.fontSize = FontSize;
        return (button, label);
    }

    private void NextRow(float height)
    {
        nextRow += height + RowGap;
    }
}
