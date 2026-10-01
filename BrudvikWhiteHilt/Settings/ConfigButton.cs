using Jotunn.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Settings;

/// <summary>
/// A small panel in the bottom left corner while the inventory is open, right of the health and food bars, with a
/// short explanation and a button that opens the settings window.
/// </summary>
public class ConfigButton : MonoBehaviour
{
    private const float PanelWidth = 290f;
    private const float PanelHeight = 150f;
    private const float Left = 150f;
    private const float Bottom = 16f;
    private const float Padding = 14f;
    private const float ButtonHeight = 46f;

    private static ConfigButton instance;

    private GameObject panel;
    private Text title;
    private Text text;
    private TMP_Text buttonTmp;
    private Text buttonText;

    /// <summary>
    /// Creates the panel once the game's GUI exists. Cheap to call every frame.
    /// </summary>
    public static void EnsureCreated()
    {
        if (instance != null || InventoryGui.instance == null)
        {
            return;
        }

        Canvas canvas = InventoryGui.instance.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        // Full screen on the game's GUI canvas, so the corner and the scaling match the HUD.
        GameObject root = new("WhiteHiltSettingsPanel", typeof(RectTransform));
        RectTransform rect = (RectTransform)root.transform;
        rect.SetParent(canvas.rootCanvas.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        instance = root.AddComponent<ConfigButton>();
        instance.Build();
    }

    private void Build()
    {
        Vector2 corner = Vector2.zero;
        panel = GUIManager.Instance.CreateWoodpanel(transform, corner, corner, Vector2.zero, PanelWidth, PanelHeight, false);
        panel.name = "Panel";
        RectTransform panelRect = (RectTransform)panel.transform;
        panelRect.pivot = corner;
        panelRect.anchoredPosition = new Vector2(Left, Bottom);

        Vector2 top = new(0.5f, 1f);
        title = GUIManager.Instance.CreateText(string.Empty, panelRect, top, top, new Vector2(0f, -Padding - 12f), GUIManager.Instance.AveriaSerifBold, 20,
            GUIManager.Instance.ValheimOrange, true, Color.black, PanelWidth - Padding * 2f, 24f, false).GetComponent<Text>();
        title.alignment = TextAnchor.MiddleCenter;

        text = GUIManager.Instance.CreateText(string.Empty, panelRect, top, top, new Vector2(0f, -Padding - 24f - 22f), GUIManager.Instance.AveriaSerif, 14,
            new Color(0.9f, 0.88f, 0.8f), true, Color.black, PanelWidth - Padding * 2f, 40f, false).GetComponent<Text>();
        text.alignment = TextAnchor.MiddleCenter;

        CreateButton(panelRect);
        panel.SetActive(false);
    }

    // A copy of the vanilla Craft button, so it looks and sounds the same.
    private void CreateButton(RectTransform parent)
    {
        Button source = InventoryGui.instance.m_craftButton;
        GameObject copy = source != null
            ? Instantiate(source.gameObject, parent, false)
            : GUIManager.Instance.CreateButton(string.Empty, parent, Vector2.zero, Vector2.zero, Vector2.zero, 200f, ButtonHeight);
        copy.name = "SettingsButton";
        copy.SetActive(true);

        // The copy must not answer the gamepad's craft key.
        foreach (UIGamePad pad in copy.GetComponentsInChildren<UIGamePad>(true))
        {
            if (pad.m_hint != null)
            {
                Destroy(pad.m_hint);
            }

            Destroy(pad);
        }

        // The craft tooltip lists recipe requirements, and a Localize component would reset the label to "Craft".
        foreach (MonoBehaviour behaviour in copy.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour is UITooltip || behaviour.GetType().Name == "Localize")
            {
                Destroy(behaviour);
            }
        }

        RectTransform rect = (RectTransform)copy.transform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.offsetMin = new Vector2(Padding, Padding);
        rect.offsetMax = new Vector2(-Padding, Padding + ButtonHeight);

        Button button = copy.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(ConfigWindow.Toggle);
        button.interactable = true;

        buttonTmp = copy.GetComponentInChildren<TMP_Text>(true);
        buttonText = buttonTmp == null ? copy.GetComponentInChildren<Text>(true) : null;
    }

    private void Update()
    {
        bool show = InventoryGui.IsVisible() && !ConfigWindow.IsOpen;
        if (panel.activeSelf == show)
        {
            return;
        }

        if (show)
        {
            Refresh();
        }

        panel.SetActive(show);
    }

    // Texts follow the language and the chosen key, so they are set each time the panel appears.
    private void Refresh()
    {
        title.text = Localization.instance.Localize("$whitehilt_settings_panel_title");
        string key = ConfigWindow.OpenKeyText;
        text.text = string.IsNullOrEmpty(key)
            ? Localization.instance.Localize("$whitehilt_settings_panel_text")
            : string.Format(Localization.instance.Localize("$whitehilt_settings_panel_text_key"), key);

        string label = Localization.instance.Localize("$whitehilt_settings_button");
        if (buttonTmp != null)
        {
            buttonTmp.text = label;
        }
        else if (buttonText != null)
        {
            buttonText.text = label;
        }
    }
}
