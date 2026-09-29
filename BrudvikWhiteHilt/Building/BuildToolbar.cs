using BepInEx.Configuration;
using BrudvikWhiteHilt.Building.Groups;
using BrudvikWhiteHilt.Building.Media;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Building;

/// <summary>
/// The build toolbar on the left and the key hint bottom-left, shown while a build tool is held.
/// Holding the cursor key (Left Alt) shows the mouse cursor so the buttons can be clicked; clicking the title folds it.
/// </summary>
public class BuildToolbar : MonoBehaviour
{
    private const float PanelWidth = 320f;
    private const float Padding = 14f;
    private const float RowHeight = 30f;
    private const float RowGap = 4f;
    private const float TitleHeight = 26f;
    private const float ReadoutHeight = 56f;
    private const float FooterHeight = 64f;
    private const int FontSize = 13;
    private const float RefreshInterval = 0.1f;

    private static BuildToolbar instance;

    private GameObject panel;
    private RectTransform panelRect;
    private GameObject buttons;
    private Text title;
    private Text readout;
    private Text hint;
    private Text cameraLabel;
    private Text stepLabel;
    private Text snapLabel;
    private Text gridLabel;
    private Text lightLabel;
    private Text selectLabel;
    private Text lineLabel;
    private Text footer;
    private GameObject tooltipPrefab;
    private float nextRow;
    private float fullHeight;
    private float nextRefresh;

    /// <summary>
    /// True while the cursor key is held with the toolbar showing: the cursor is free, the view and placement stand still.
    /// </summary>
    public static bool CursorMode { get; private set; }

    /// <summary>
    /// Creates the toolbar once the custom GUI exists. Safe to call every frame.
    /// </summary>
    public static void Ensure()
    {
        if (instance != null || GUIManager.CustomGUIFront == null)
        {
            return;
        }

        GameObject root = new("WhiteHiltBuildToolbar", typeof(RectTransform));
        root.transform.SetParent(GUIManager.CustomGUIFront.transform, false);
        RectTransform rect = (RectTransform)root.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        instance = root.AddComponent<BuildToolbar>();
        instance.Build();
    }

    private void Update()
    {
        Player player = Player.m_localPlayer;
        bool building = player != null && player.InPlaceMode() && !player.IsDead() && !InventoryGui.IsVisible()
            && !Hud.IsPieceSelectionVisible() && !Minimap.IsOpen() && !Menu.IsVisible() && !TextInput.IsVisible()
            && (Hud.instance == null || !Hud.instance.m_userHidden);

        bool showPanel = building && BuildToolSettings.ShowToolbar.Value && !MediaMode.HideUi;
        if (panel.activeSelf != showPanel)
        {
            panel.SetActive(showPanel);
        }

        bool showHint = building && BuildToolSettings.ShowHint.Value && !MediaMode.HideUi;
        if (hint.gameObject.activeSelf != showHint)
        {
            hint.gameObject.SetActive(showHint);
        }

        bool typing = (Chat.instance != null && Chat.instance.HasFocus()) || Console.IsVisible();
        CursorMode = !typing && ((showPanel && Input.GetKey(BuildToolSettings.CursorKey.Value))
            || ((MediaPanel.IsOpen || BlueprintPanel.IsOpen) && !MediaMode.HideUi));

        if (building && Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + RefreshInterval;
            Refresh(player);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            CursorMode = false;
        }
    }

    private void Build()
    {
        tooltipPrefab = InventoryGui.instance?.m_playerGrid?.m_elementPrefab?.GetComponent<UITooltip>()?.m_tooltipPrefab;

        panel = GUIManager.Instance.CreateWoodpanel(transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, PanelWidth, 600f, false);
        panel.name = "Panel";
        panelRect = (RectTransform)panel.transform;
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(20f, 410f);

        nextRow = Padding;
        title = AddText(panel.transform, string.Empty, 20, GUIManager.Instance.ValheimOrange, TitleHeight);
        title.font = GUIManager.Instance.AveriaSerifBold;
        title.alignment = TextAnchor.MiddleCenter;
        title.raycastTarget = true;
        title.gameObject.AddComponent<Button>().onClick.AddListener(ToggleCollapsed);
        readout = AddText(panel.transform, string.Empty, FontSize, Color.white, ReadoutHeight);
        readout.alignment = TextAnchor.MiddleCenter;

        buttons = new GameObject("Buttons", typeof(RectTransform));
        buttons.transform.SetParent(panel.transform, false);
        RectTransform buttonsRect = (RectTransform)buttons.transform;
        buttonsRect.anchorMin = Vector2.zero;
        buttonsRect.anchorMax = Vector2.one;
        buttonsRect.offsetMin = Vector2.zero;
        buttonsRect.offsetMax = Vector2.zero;
        Transform parent = buttons.transform;

        float full = PanelWidth - Padding * 2f;
        float half = (full - RowGap) / 2f;
        float third = (full - RowGap * 2f) / 3f;
        float right = half + RowGap;

        cameraLabel = AddButton(parent, 0f, half, BuildTools.ToggleCamera, "camera");
        Label(AddButton(parent, right, half, BuildTools.FlyTo, "flyto"), BuildToolSettings.KeyFlyTo, "$whitehilt_build_flyto");
        NextRow();
        Label(AddButton(parent, 0f, half, p => BuildTools.ChangeSpeed(p, -1), "speed"), BuildToolSettings.KeySpeedDown, "$whitehilt_build_speed_down");
        Label(AddButton(parent, right, half, p => BuildTools.ChangeSpeed(p, 1), "speed"), BuildToolSettings.KeySpeedUp, "$whitehilt_build_speed_up");
        NextRow();
        stepLabel = AddButton(parent, 0f, full, BuildTools.CycleStep, "step");
        NextRow();
        Label(AddButton(parent, 0f, half, p => BuildTools.Tilt(p, 1), "tilt"), BuildToolSettings.KeyTiltForward, "$whitehilt_build_tilt_forward");
        Label(AddButton(parent, right, half, p => BuildTools.Tilt(p, -1), "tilt"), BuildToolSettings.KeyTiltBack, "$whitehilt_build_tilt_back");
        NextRow();
        Label(AddButton(parent, 0f, half, p => BuildTools.Roll(p, -1), "roll"), BuildToolSettings.KeyRollLeft, "$whitehilt_build_roll_left");
        Label(AddButton(parent, right, half, p => BuildTools.Roll(p, 1), "roll"), BuildToolSettings.KeyRollRight, "$whitehilt_build_roll_right");
        NextRow();
        Label(AddButton(parent, 0f, half, BuildTools.QuickTilt, "quick"), BuildToolSettings.KeyQuickTilt, "$whitehilt_build_quick_tilt");
        Label(AddButton(parent, right, half, BuildTools.QuickRoll, "quick"), BuildToolSettings.KeyQuickRoll, "$whitehilt_build_quick_roll");
        NextRow();
        Label(AddButton(parent, 0f, half, BuildTools.Flip, "flip"), BuildToolSettings.KeyFlip, "$whitehilt_build_flip");
        Label(AddButton(parent, right, half, BuildTools.Reset, "reset"), BuildToolSettings.KeyReset, "$whitehilt_build_reset");
        NextRow();
        snapLabel = AddButton(parent, 0f, half, BuildTools.ToggleSnap, "snap");
        AddButton(parent, right, half, BuildTools.NextSnapPoint, "snappoint").text =
            Key(Localization.instance.Localize("$KEY_TabRight")) + Localization.instance.Localize("$whitehilt_build_snappoint");
        NextRow();
        Label(AddButton(parent, 0f, half, BuildTools.Copy, "copy"), BuildToolSettings.KeyCopy, "$whitehilt_build_copy");
        gridLabel = AddButton(parent, right, half, BuildTools.ToggleGrid, "grid");
        NextRow();
        Label(AddButton(parent, 0f, half, BuildTools.Stamp, "stamp"), BuildToolSettings.KeyStamp, "$whitehilt_build_stamp");
        lightLabel = AddButton(parent, right, half, BuildTools.ToggleLight, "light");
        NextRow();
        AddButton(parent, 0f, third, p => BuildTools.Nudge(p, 1, 0, 0), "nudge").text = Localization.instance.Localize("$whitehilt_build_nudge_away");
        AddButton(parent, third + RowGap, third, p => BuildTools.Nudge(p, -1, 0, 0), "nudge").text = Localization.instance.Localize("$whitehilt_build_nudge_closer");
        AddButton(parent, (third + RowGap) * 2f, third, p => BuildTools.Nudge(p, 0, 0, 1), "nudge").text = Localization.instance.Localize("$whitehilt_build_nudge_up");
        NextRow();
        AddButton(parent, 0f, third, p => BuildTools.Nudge(p, 0, -1, 0), "nudge").text = Localization.instance.Localize("$whitehilt_build_nudge_left");
        AddButton(parent, third + RowGap, third, p => BuildTools.Nudge(p, 0, 1, 0), "nudge").text = Localization.instance.Localize("$whitehilt_build_nudge_right");
        AddButton(parent, (third + RowGap) * 2f, third, p => BuildTools.Nudge(p, 0, 0, -1), "nudge").text = Localization.instance.Localize("$whitehilt_build_nudge_down");
        NextRow();
        selectLabel = AddButton(parent, 0f, half, _ => GroupTools.ToggleSelect(), "select");
        Label(AddButton(parent, right, half, _ => BlueprintPanel.Toggle(), "blueprints"), GroupSettings.KeyBlueprints, "$whitehilt_group_blueprints");
        NextRow();
        lineLabel = AddButton(parent, 0f, half, GroupTools.CycleLine, "line");
        Label(AddButton(parent, right, half, GroupTools.Paste, "paste"), GroupSettings.KeyPaste, "$whitehilt_group_paste");
        NextRow();
        Label(AddButton(parent, 0f, half, BuildTools.Undo, "undo"), BuildToolSettings.KeyUndo, "$whitehilt_build_undo");
        Label(AddButton(parent, right, half, BuildTools.Redo, "redo"), BuildToolSettings.KeyRedo, "$whitehilt_build_redo");
        NextRow();
        footer = AddText(parent, string.Empty, 12, new Color(0.85f, 0.85f, 0.85f), FooterHeight);
        footer.alignment = TextAnchor.MiddleCenter;
        fullHeight = nextRow - RowGap + Padding;

        hint = GUIManager.Instance.CreateText(string.Empty, transform, Vector2.zero, Vector2.zero, Vector2.zero,
            GUIManager.Instance.AveriaSerifBold, 16, Color.white, true, Color.black, 1100f, 140f, false).GetComponent<Text>();
        RectTransform hintRect = hint.rectTransform;
        hintRect.pivot = Vector2.zero;
        hintRect.anchoredPosition = new Vector2(360f, 30f);
        hint.alignment = TextAnchor.LowerLeft;

        ApplyCollapsed();
        panel.SetActive(false);
        hint.gameObject.SetActive(false);
    }

    private void ToggleCollapsed()
    {
        BuildToolSettings.ToolbarCollapsed.Value = !BuildToolSettings.ToolbarCollapsed.Value;
        ApplyCollapsed();
    }

    private void ApplyCollapsed()
    {
        bool collapsed = BuildToolSettings.ToolbarCollapsed.Value;
        buttons.SetActive(!collapsed);
        float height = collapsed ? Padding + TitleHeight + RowGap + ReadoutHeight + Padding : fullHeight;
        panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, height);
        title.text = Localization.instance.Localize("$whitehilt_build_toolbar") + (collapsed ? "  [+]" : "  [-]");
    }

    private void Refresh(Player player)
    {
        string on = Localization.instance.Localize("$whitehilt_build_on");
        string off = Localization.instance.Localize("$whitehilt_build_off");
        string none = "-";

        GameObject ghost = player.m_placementGhost;
        bool ghostShown = ghost != null && ghost.activeSelf;
        string height = ghostShown && ZoneSystem.instance != null
            ? BuildTools.FormatLength(ghost.transform.position.y - ZoneSystem.instance.GetGroundHeight(ghost.transform.position))
            : none;
        Piece last = BuildUndo.LastPlaced;
        string fromLast = ghostShown && last != null ? BuildTools.FormatLength(Vector3.Distance(ghost.transform.position, last.transform.position)) : none;

        readout.text = string.Format(Localization.instance.Localize("$whitehilt_build_readout"),
            BuildTools.FormatAngle(BuildRotation.Yaw(player)), BuildTools.FormatAngle(BuildRotation.Pitch),
            BuildTools.FormatAngle(BuildRotation.Roll + (BuildRotation.Flipped ? 180f : 0f)), height, fromLast);
        cameraLabel.text = Key(BuildToolSettings.KeyName(BuildToolSettings.KeyCamera)) + Localization.instance.Localize("$whitehilt_build_camera")
            + ": " + (BuildCamera.Active ? on : off);
        stepLabel.text = Key(BuildToolSettings.KeyName(BuildToolSettings.KeyStep)) + Localization.instance.Localize("$whitehilt_build_step")
            + ": " + BuildTools.FormatAngle(BuildRotation.Step);
        snapLabel.text = Key(BuildToolSettings.KeyName(BuildToolSettings.KeySnap)) + Localization.instance.Localize("$whitehilt_build_snap")
            + ": " + (BuildRotation.SnapOff ? off : on);
        gridLabel.text = Key(BuildToolSettings.KeyName(BuildToolSettings.KeyGrid)) + Localization.instance.Localize("$whitehilt_build_grid")
            + ": " + (BuildRotation.GridOn ? on : off);
        lightLabel.text = Key(BuildToolSettings.KeyName(BuildToolSettings.KeyLight)) + Localization.instance.Localize("$whitehilt_build_light")
            + ": " + (BuildCamera.LightOn ? on : off);
        int selectedCount = BuildSelection.Count;
        selectLabel.text = Key(BuildToolSettings.KeyName(GroupSettings.KeySelect)) + Localization.instance.Localize("$whitehilt_group_select")
            + ": " + (GroupTools.Mode == GroupTools.ToolMode.Select ? on : off) + (selectedCount > 0 ? $" ({selectedCount})" : string.Empty);
        string lineState = GroupTools.Mode == GroupTools.ToolMode.Line ? Localization.instance.Localize("$whitehilt_group_line_line")
            : GroupTools.Mode == GroupTools.ToolMode.Area ? Localization.instance.Localize("$whitehilt_group_line_area") : off;
        lineLabel.text = Key(BuildToolSettings.KeyName(GroupSettings.KeyLine)) + Localization.instance.Localize("$whitehilt_group_line") + ": " + lineState;

        string cursorKey = BuildToolSettings.KeyName(BuildToolSettings.CursorKey.Value);
        string orbitKey = BuildToolSettings.KeyName(BuildToolSettings.OrbitKey.Value);
        string nudgeKeys = BuildToolSettings.KeyName(BuildToolSettings.KeyNudgeForward) + "/" + BuildToolSettings.KeyName(BuildToolSettings.KeyNudgeBack)
            + "/" + BuildToolSettings.KeyName(BuildToolSettings.KeyNudgeLeft) + "/" + BuildToolSettings.KeyName(BuildToolSettings.KeyNudgeRight)
            + "/" + BuildToolSettings.KeyName(BuildToolSettings.KeyNudgeUp) + "/" + BuildToolSettings.KeyName(BuildToolSettings.KeyNudgeDown);
        footer.text = string.Format(Localization.instance.Localize("$whitehilt_build_footer"), cursorKey,
            BuildToolSettings.KeyName(BuildToolSettings.TiltWheelModifier.Value), BuildToolSettings.KeyName(BuildToolSettings.RollWheelModifier.Value),
            orbitKey, nudgeKeys, BuildTools.FormatLength(BuildToolSettings.NudgeStep.Value));

        string cameraKey = BuildToolSettings.KeyName(BuildToolSettings.KeyCamera);
        hint.text = BuildCamera.Active
            ? string.Format(Localization.instance.Localize("$whitehilt_build_hint_active"), cameraKey,
                BuildToolSettings.KeyName(BuildToolSettings.KeyFlyTo), orbitKey,
                BuildToolSettings.KeyName(BuildToolSettings.KeySpeedUp) + "/" + BuildToolSettings.KeyName(BuildToolSettings.KeySpeedDown),
                BuildTools.FormatFactor(BuildCamera.SpeedFactor), BuildToolSettings.KeyName(MediaSettings.KeyPhoto),
                BuildToolSettings.KeyName(MediaSettings.KeyPhotoView), BuildToolSettings.KeyName(MediaSettings.KeyPanel))
            : string.Format(Localization.instance.Localize("$whitehilt_build_hint"), cameraKey, cursorKey);
        string modeHint = string.Join("\n", new[] { GroupTools.HintLine, Terrain.HoeTools.HintLine, Terrain.FarmTools.HintLine, Terrain.RoadBuilder.HintLine }
            .Where(line => !string.IsNullOrEmpty(line)));
        if (modeHint.Length > 0)
        {
            hint.text = modeHint + "\n" + hint.text;
        }
    }

    private Text AddText(Transform parent, string text, int fontSize, Color color, float height)
    {
        Text label = GUIManager.Instance.CreateText(text, parent, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(PanelWidth / 2f, -(nextRow + height / 2f)), GUIManager.Instance.AveriaSerif, fontSize, color, true, Color.black,
            PanelWidth - Padding * 2f, height, false).GetComponent<Text>();
        nextRow += height + RowGap;
        return label;
    }

    private Text AddButton(Transform parent, float x, float width, Action<Player> onClick, string tip)
    {
        GameObject button = GUIManager.Instance.CreateButton(string.Empty, parent, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(Padding + x + width / 2f, -(nextRow + RowHeight / 2f)), width, RowHeight);
        button.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (Player.m_localPlayer != null)
            {
                onClick(Player.m_localPlayer);
            }
        });

        if (tooltipPrefab != null)
        {
            UITooltip tooltip = button.AddComponent<UITooltip>();
            tooltip.m_tooltipPrefab = tooltipPrefab;
            tooltip.m_topic = string.Empty;
            tooltip.m_text = Localization.instance.Localize("$whitehilt_build_tip_" + tip);
        }

        Text label = button.GetComponentInChildren<Text>();
        label.fontSize = FontSize;
        label.resizeTextForBestFit = false;
        label.supportRichText = true;
        return label;
    }

    private void NextRow()
    {
        nextRow += RowHeight + RowGap;
    }

    private static void Label(Text label, ConfigEntry<KeyboardShortcut> key, string token)
    {
        label.text = Key(BuildToolSettings.KeyName(key)) + Localization.instance.Localize(token);
    }

    private static string Key(string key)
    {
        return string.IsNullOrEmpty(key) ? string.Empty : "<color=#E8B04B>[" + key + "]</color> ";
    }
}
