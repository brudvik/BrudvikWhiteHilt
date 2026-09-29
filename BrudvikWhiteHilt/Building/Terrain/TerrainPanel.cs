using BepInEx.Configuration;
using Jotunn.Managers;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Building.Terrain;

/// <summary>
/// A panel next to the build toolbar while the White Hilt hoe ("Terrain") or cultivator ("Field") is held, with the
/// tool buttons, their options, the height meter, the brush size and the road's progress.
/// </summary>
public class TerrainPanel : MonoBehaviour
{
    private const float PanelWidth = 300f;
    private const float Padding = 14f;
    private const float RowHeight = 30f;
    private const float RowGap = 4f;
    private const int FontSize = 13;
    private const float RefreshInterval = 0.1f;

    private static TerrainPanel instance;

    private GameObject hoePanel;
    private GameObject farmPanel;
    private Text hoeStatus;
    private Text farmStatus;
    private Text surfaceLabel;
    private Text widthLabel;
    private Text gradeLabel;
    private Text evenLabel;
    private Text pauseLabel;
    private Text autoLabel;
    private Text growthLabel;
    private float nextRow;
    private float nextRefresh;

    /// <summary>
    /// Creates the panel once the custom GUI exists. Safe to call every frame.
    /// </summary>
    public static void Ensure()
    {
        if (instance != null || GUIManager.CustomGUIFront == null)
        {
            return;
        }

        GameObject root = new("WhiteHiltTerrainPanel", typeof(RectTransform));
        root.transform.SetParent(GUIManager.CustomGUIFront.transform, false);
        RectTransform rect = (RectTransform)root.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        instance = root.AddComponent<TerrainPanel>();
        instance.Build();
    }

    private void Update()
    {
        Player player = Player.m_localPlayer;
        bool visible = player != null && player.InPlaceMode() && !player.IsDead() && !InventoryGui.IsVisible() && !Hud.IsPieceSelectionVisible()
            && !Minimap.IsOpen() && !Menu.IsVisible() && !Media.MediaMode.HideUi && (Hud.instance == null || !Hud.instance.m_userHidden)
            && BuildToolSettings.ShowToolbar.Value;
        bool hoe = visible && TerrainSettings.HoldingHoe(player);
        bool farm = visible && TerrainSettings.HoldingCultivator(player);
        if (hoePanel.activeSelf != hoe)
        {
            hoePanel.SetActive(hoe);
        }

        if (farmPanel.activeSelf != farm)
        {
            farmPanel.SetActive(farm);
        }

        if ((hoe || farm) && Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + RefreshInterval;
            Refresh(player, hoe);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Build()
    {
        float full = PanelWidth - Padding * 2f;
        float half = (full - RowGap) / 2f;
        float right = half + RowGap;

        hoePanel = CreatePanel("$whitehilt_terrain", out Transform hoe);
        hoeStatus = AddText(hoe, 56f);
        AddButton(hoe, 0f, half, "$whitehilt_terrain_road", TerrainSettings.KeyRoad, p => { HoeTools.Exit(); RoadBuilder.StartPlanning(p); });
        AddButton(hoe, right, half, "$whitehilt_terrain_level", TerrainSettings.KeyArea, _ => HoeTools.Toggle(HoeTools.ToolMode.Level));
        NextRow();
        AddButton(hoe, 0f, half, "$whitehilt_terrain_ramp", TerrainSettings.KeyShape, _ => HoeTools.Toggle(HoeTools.ToolMode.Ramp));
        AddButton(hoe, right, half, "$whitehilt_terrain_paint", TerrainSettings.KeyPaint, _ => HoeTools.Toggle(HoeTools.ToolMode.Paint));
        NextRow();
        AddButton(hoe, 0f, half, "$whitehilt_terrain_reset", TerrainSettings.KeyReset, _ => HoeTools.Toggle(HoeTools.ToolMode.Reset));
        AddButton(hoe, right, half, "$whitehilt_terrain_reference", TerrainSettings.KeyReference, HoeTools.SetReference);
        NextRow();
        surfaceLabel = AddButton(hoe, 0f, half, null, null, _ => HoeTools.CycleSurface());
        widthLabel = AddButton(hoe, right, half, null, null, _ => HoeTools.CycleWidth());
        NextRow();
        gradeLabel = AddButton(hoe, 0f, half, null, null, _ => HoeTools.CycleGrade());
        evenLabel = AddButton(hoe, right, half, null, null, _ => HoeTools.ToggleEvenRoad());
        NextRow();
        pauseLabel = AddButton(hoe, 0f, half, null, null, _ => RoadBuilder.TogglePause());
        AddButton(hoe, right, half, "$whitehilt_terrain_cancel", null, _ => RoadBuilder.Cancel());
        NextRow();
        Finish(hoePanel);

        farmPanel = CreatePanel("$whitehilt_farm", out Transform farm);
        farmStatus = AddText(farm, 40f);
        AddButton(farm, 0f, half, "$whitehilt_farm_grid", TerrainSettings.KeyShape, _ => FarmTools.Toggle(FarmTools.ToolMode.Grid));
        AddButton(farm, right, half, "$whitehilt_farm_cultivate", TerrainSettings.KeyArea, _ => FarmTools.Toggle(FarmTools.ToolMode.Cultivate));
        NextRow();
        AddButton(farm, 0f, half, "$whitehilt_farm_refill", TerrainSettings.KeyRoad, FarmTools.Refill);
        AddButton(farm, right, half, "$whitehilt_farm_harvest", TerrainSettings.KeyHarvest, _ => FarmTools.Toggle(FarmTools.ToolMode.Harvest));
        NextRow();
        autoLabel = AddButton(farm, 0f, half, null, null, _ => FarmTools.ToggleAutoCultivate());
        growthLabel = AddButton(farm, right, half, null, null, _ => TerrainSettings.ShowGrowth.Value = !TerrainSettings.ShowGrowth.Value);
        NextRow();
        Finish(farmPanel);
    }

    private void Refresh(Player player, bool hoe)
    {
        string on = Localization.instance.Localize("$whitehilt_build_on");
        string off = Localization.instance.Localize("$whitehilt_build_off");
        if (hoe)
        {
            string height = string.Empty;
            if (AreaPicker.Aim(out Vector3 aim))
            {
                height = HoeTools.Reference.HasValue
                    ? string.Format(Localization.instance.Localize("$whitehilt_terrain_height_ref"), HoeTools.Metres(aim.y), Signed(aim.y - HoeTools.Reference.Value))
                    : string.Format(Localization.instance.Localize("$whitehilt_terrain_height"), HoeTools.Metres(aim.y));
            }

            string brush = string.Format(Localization.instance.Localize("$whitehilt_terrain_brush"),
                HoeTools.BrushRadius > 0f ? HoeTools.Metres(HoeTools.BrushRadius) : "-", BuildToolSettings.KeyName(BuildToolSettings.TiltWheelModifier.Value));
            hoeStatus.text = height + "\n" + brush + "\n" + RoadBuilder.Status();
            surfaceLabel.text = Localization.instance.Localize("$whitehilt_terrain_surface") + ": " + HoeTools.SurfaceName(HoeTools.Paving);
            widthLabel.text = Localization.instance.Localize("$whitehilt_terrain_width") + ": " + HoeTools.Width.ToString("0") + " m";
            gradeLabel.text = Localization.instance.Localize("$whitehilt_terrain_grade") + ": "
                + (HoeTools.Grade > 0f ? HoeTools.Grade.ToString("0") + " %" : Localization.instance.Localize("$whitehilt_terrain_grade_free"));
            evenLabel.text = Localization.instance.Localize(HoeTools.EvenRoad ? "$whitehilt_terrain_follow_even" : "$whitehilt_terrain_follow_terrain");
            pauseLabel.text = Localization.instance.Localize(RoadBuilder.Paused ? "$whitehilt_terrain_resume" : "$whitehilt_terrain_pause");
        }
        else
        {
            farmStatus.text = Localization.instance.Localize("$whitehilt_farm_grid") + ": " + FarmTools.GridText;
            autoLabel.text = Localization.instance.Localize("$whitehilt_farm_auto") + ": " + (FarmTools.AutoCultivate ? on : off);
            growthLabel.text = Localization.instance.Localize("$whitehilt_farm_growth") + ": " + (TerrainSettings.ShowGrowth.Value ? on : off);
        }
    }

    private GameObject CreatePanel(string title, out Transform content)
    {
        GameObject panel = GUIManager.Instance.CreateWoodpanel(transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, PanelWidth, 400f, false);
        RectTransform rect = (RectTransform)panel.transform;
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(350f, 410f);
        content = panel.transform;
        nextRow = Padding;
        Text heading = AddText(content, 26f);
        heading.text = Localization.instance.Localize(title);
        heading.font = GUIManager.Instance.AveriaSerifBold;
        heading.fontSize = 20;
        heading.color = GUIManager.Instance.ValheimOrange;
        heading.alignment = TextAnchor.MiddleCenter;
        panel.SetActive(false);
        return panel;
    }

    private void Finish(GameObject panel)
    {
        RectTransform rect = (RectTransform)panel.transform;
        rect.sizeDelta = new Vector2(PanelWidth, nextRow - RowGap + Padding);
    }

    private Text AddText(Transform parent, float height)
    {
        float width = PanelWidth - Padding * 2f;
        Text text = GUIManager.Instance.CreateText(string.Empty, parent, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(Padding + width / 2f, -(nextRow + height / 2f)), GUIManager.Instance.AveriaSerif, FontSize, Color.white, true, Color.black,
            width, height, false).GetComponent<Text>();
        text.alignment = TextAnchor.MiddleCenter;
        nextRow += height + RowGap;
        return text;
    }

    private Text AddButton(Transform parent, float x, float width, string token, ConfigEntry<KeyboardShortcut> key, Action<Player> onClick)
    {
        GameObject button = GUIManager.Instance.CreateButton(string.Empty, parent, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(Padding + x + width / 2f, -(nextRow + RowHeight / 2f)), width, RowHeight);
        button.GetComponent<Button>().onClick.AddListener(() =>
        {
            Player player = Player.m_localPlayer;
            if (player != null)
            {
                onClick(player);
            }
        });

        Text label = button.GetComponentInChildren<Text>();
        label.fontSize = FontSize;
        label.supportRichText = true;
        if (token != null)
        {
            string keyName = key != null ? BuildToolSettings.KeyName(key) : string.Empty;
            label.text = (keyName.Length > 0 ? "<color=#E8B04B>[" + keyName + "]</color> " : string.Empty) + Localization.instance.Localize(token);
        }

        return label;
    }

    private void NextRow()
    {
        nextRow += RowHeight + RowGap;
    }

    private static string Signed(float metres)
    {
        return (metres >= 0f ? "+" : string.Empty) + HoeTools.Metres(metres);
    }
}
