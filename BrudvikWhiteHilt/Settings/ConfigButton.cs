using BrudvikWhiteHilt.Helpers;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Settings;

/// <summary>
/// A small round settings button in the bottom left corner while the inventory is open, right of the health and food
/// bars, with two short lines of text beside it. It stays low and narrow, so tall chests and big inventories do not
/// end up behind it.
/// </summary>
public class ConfigButton : MonoBehaviour
{
    private const float Left = 150f;
    private const float Bottom = 16f;
    private const float Diameter = 44f;
    private const float TextGap = 8f;
    private const float TextWidth = 170f;
    private const int IconSize = 64;

    private static ConfigButton instance;

    private GameObject panel;
    private Text title;
    private Text text;

    /// <summary>
    /// Creates the button once the game's GUI exists. Cheap to call every frame.
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

    // Builds the corner once: the round button and the two lines beside it. It starts hidden and is shown in the
    // inventory only.
    private void Build()
    {
        panel = new GameObject("Panel", typeof(RectTransform));
        RectTransform panelRect = (RectTransform)panel.transform;
        panelRect.SetParent(transform, false);
        panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = Vector2.zero;
        panelRect.sizeDelta = new Vector2(Diameter + TextGap + TextWidth, Diameter);
        panelRect.anchoredPosition = new Vector2(Left, Bottom);

        CreateButton(panelRect);

        Vector2 left = new(0f, 0.5f);
        float textX = Diameter + TextGap + TextWidth / 2f;
        title = GUIManager.Instance.CreateText(string.Empty, panelRect, left, left, new Vector2(textX, 9f), GUIManager.Instance.AveriaSerifBold, 15,
            GUIManager.Instance.ValheimOrange, true, Color.black, TextWidth, 20f, false).GetComponent<Text>();
        title.alignment = TextAnchor.MiddleLeft;
        title.raycastTarget = false;

        text = GUIManager.Instance.CreateText(string.Empty, panelRect, left, left, new Vector2(textX, -9f), GUIManager.Instance.AveriaSerif, 12,
            new Color(0.9f, 0.88f, 0.8f), true, Color.black, TextWidth, 18f, false).GetComponent<Text>();
        text.alignment = TextAnchor.MiddleLeft;
        text.raycastTarget = false;

        panel.SetActive(false);
    }

    // A copy of the vanilla Craft button, so it sounds the same, dressed as a round gear.
    private void CreateButton(RectTransform parent)
    {
        Button source = InventoryGui.instance.m_craftButton;
        GameObject copy = source != null
            ? Instantiate(source.gameObject, parent, false)
            : GUIManager.Instance.CreateButton(string.Empty, parent, Vector2.zero, Vector2.zero, Vector2.zero, Diameter, Diameter);
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

        // Only the button's own picture stays: the label and any decoration under it are hidden.
        foreach (Transform child in copy.transform)
        {
            child.gameObject.SetActive(false);
        }

        RectTransform rect = (RectTransform)copy.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(Diameter, Diameter);
        rect.anchoredPosition = Vector2.zero;

        Image image = copy.GetComponent<Image>();
        if (image == null)
        {
            image = copy.AddComponent<Image>();
        }

        image.sprite = GearSprite();
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;

        Button button = copy.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(ConfigWindow.Toggle);
        button.interactable = true;
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = new Color(0.9f, 0.9f, 0.9f);
        colors.highlightedColor = Color.white;
        colors.selectedColor = new Color(0.9f, 0.9f, 0.9f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        button.colors = colors;
    }

    // A dark round plate with an orange rim and a pale gear, drawn once in code.
    private static Sprite GearSprite()
    {
        if (VisualHelper.IsHeadless)
        {
            return null;
        }

        Color rim = GUIManager.Instance.ValheimOrange;
        Color plate = new(0.16f, 0.11f, 0.07f, 0.92f);
        Color gear = new(0.93f, 0.89f, 0.8f);
        const float half = IconSize / 2f;
        const float plateRadius = half - 1f;
        const float rimWidth = 3f;
        const float hole = 5f;
        const int teeth = 8;

        Color[] pixels = new Color[IconSize * IconSize];
        for (int y = 0; y < IconSize; y++)
        {
            for (int x = 0; x < IconSize; x++)
            {
                float dx = x + 0.5f - half;
                float dy = y + 0.5f - half;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                // Rounded teeth: a squared-off cosine around the rim of the wheel.
                float tooth = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.25f, 0.25f, Mathf.Cos(Mathf.Atan2(dy, dx) * teeth)));
                float gearRadius = 14f + 5f * tooth;

                Color pixel = Color.Lerp(plate, rim, Coverage(distance - (plateRadius - rimWidth)));
                float inGear = Coverage(gearRadius - distance) * Coverage(distance - hole);
                pixel = Color.Lerp(pixel, gear, inGear);
                pixel.a *= Coverage(plateRadius - distance);
                pixels[y * IconSize + x] = pixel;
            }
        }

        Texture2D texture = new(IconSize, IconSize, TextureFormat.RGBA32, false)
        {
            name = "White Hilt settings gear",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, IconSize, IconSize), new Vector2(0.5f, 0.5f));
    }

    // How much of a pixel lies inside an edge, from its signed distance in pixels, for soft edges.
    private static float Coverage(float inside) => Mathf.Clamp01(inside + 0.5f);

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

    // Texts follow the language and the chosen key, so they are set each time the button appears.
    private void Refresh()
    {
        title.text = Localization.instance.Localize("$whitehilt_settings_panel_title");
        string key = ConfigWindow.OpenKeyText;
        text.text = string.IsNullOrEmpty(key)
            ? Localization.instance.Localize("$whitehilt_settings_button")
            : string.Format(Localization.instance.Localize("$whitehilt_settings_button_key"), key);
    }
}
