using BrudvikWhiteHilt.Items.Painting;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Painting;

/// <summary>
/// The Paint Bench's colour window: a hue and saturation wheel with a brightness bar, hex and RGB fields, saved colours,
/// and the dyes that come closest to the colour. Mixing takes the dyes and a binder from the inventory and nearby chests
/// and gives a paint pot of the chosen colour.
/// </summary>
public class PaintMixerPanel : MonoBehaviour
{
    private const float PanelWidth = 600f;
    private const float PanelHeight = 700f;
    private const float WheelSize = 240f;
    private const int WheelPixels = 256;
    private const float BarWidth = 26f;
    private const float MaxDistance = 6f;
    private const float SuggestDelay = 0.15f;
    private const int FavouriteCount = 8;

    private static PaintMixerPanel instance;

    private readonly List<Image> favourites = new();

    private CraftingStation bench;
    private float hue;
    private float saturation;
    private float brightness = 1f;
    private float changedAt;
    private bool suggestionDirty;
    private DyeCatalog.Mix suggestion;

    private Texture2D wheelTexture;
    private Texture2D barTexture;
    private RectTransform wheelMarker;
    private RectTransform barMarker;
    private Image swatch;
    private InputField hexField;
    private InputField redField;
    private InputField greenField;
    private InputField blueField;
    private Text suggestionText;
    private Button mixButton;

    /// <summary>True while the window is open.</summary>
    public static bool IsOpen => instance != null && instance.gameObject.activeSelf;

    private Color32 Current => Color.HSVToRGB(hue, saturation, brightness);

    /// <summary>
    /// Opens the window at a Paint Bench.
    /// </summary>
    /// <param name="station">The bench.</param>
    public static void Open(CraftingStation station)
    {
        if (GUIManager.CustomGUIFront == null || Player.m_localPlayer == null)
        {
            return;
        }

        if (instance == null)
        {
            instance = Build();
        }

        instance.bench = station;
        instance.gameObject.SetActive(true);
        GUIManager.BlockInput(true);
        instance.SetColor(instance.Current);
    }

    private static PaintMixerPanel Build()
    {
        Vector2 middle = new(0.5f, 0.5f);
        GameObject panel = GUIManager.Instance.CreateWoodpanel(GUIManager.CustomGUIFront.transform, middle, middle, Vector2.zero, PanelWidth, PanelHeight, false);
        panel.name = "WhiteHiltPaintMixer";
        PaintMixerPanel mixer = panel.AddComponent<PaintMixerPanel>();
        mixer.BuildContent((RectTransform)panel.transform);
        panel.SetActive(false);
        return mixer;
    }

    // Builds the mixer once: a colour wheel with a brightness bar, the colour as a swatch with its hex and RGB fields,
    // favourites, the suggested mix and the buttons.
    private void BuildContent(RectTransform panel)
    {
        Vector2 top = new(0.5f, 1f);
        Vector2 bottom = new(0.5f, 0f);
        Text title = AddText(panel, top, new Vector2(0f, -36f), 26, PanelWidth - 40f, 40f, GUIManager.Instance.ValheimOrange);
        title.text = Localization.instance.Localize("$whitehilt_paint_title");
        Text hint = AddText(panel, top, new Vector2(0f, -70f), 14, PanelWidth - 60f, 36f, new Color(0.85f, 0.82f, 0.75f));
        hint.text = Localization.instance.Localize("$whitehilt_paint_hint");

        // The wheel and the brightness bar.
        wheelTexture = new Texture2D(WheelPixels, WheelPixels, TextureFormat.RGBA32, false) { name = "whitehilt_paint_wheel", wrapMode = TextureWrapMode.Clamp };
        Image wheel = AddImage(panel, top, new Vector2(-150f, -110f - WheelSize / 2f), new Vector2(WheelSize, WheelSize), wheelTexture);
        wheel.gameObject.AddComponent<PaintPicker>().Picked = OnWheel;
        wheelMarker = AddMarker(wheel.rectTransform);

        barTexture = new Texture2D(1, 64, TextureFormat.RGBA32, false) { name = "whitehilt_paint_bar", wrapMode = TextureWrapMode.Clamp };
        float barX = -150f + WheelSize / 2f + 30f;
        Image bar = AddImage(panel, top, new Vector2(barX, -110f - WheelSize / 2f), new Vector2(BarWidth, WheelSize), barTexture);
        bar.gameObject.AddComponent<PaintPicker>().Picked = OnBar;
        barMarker = AddMarker(bar.rectTransform);
        Text brightnessLabel = AddText(panel, top, new Vector2(barX, -110f - WheelSize - 16f), 13, 100f, 22f, new Color(0.85f, 0.82f, 0.75f));
        brightnessLabel.text = Localization.instance.Localize("$whitehilt_paint_brightness");

        // The colour and its fields.
        float right = 150f;
        swatch = AddImage(panel, top, new Vector2(right, -150f), new Vector2(180f, 70f), null);
        hexField = AddField(panel, top, new Vector2(right, -215f), 180f, "#");
        hexField.characterLimit = 7;
        hexField.onEndEdit.AddListener(OnHex);
        redField = AddField(panel, top, new Vector2(right - 62f, -265f), 56f, "R");
        greenField = AddField(panel, top, new Vector2(right, -265f), 56f, "G");
        blueField = AddField(panel, top, new Vector2(right + 62f, -265f), 56f, "B");
        foreach (InputField field in new[] { redField, greenField, blueField })
        {
            field.contentType = InputField.ContentType.IntegerNumber;
            field.characterLimit = 3;
            field.onEndEdit.AddListener(_ => OnRgb());
        }

        // Saved colours.
        float rowY = -390f;
        float step = 44f;
        float start = -(FavouriteCount - 1) * step / 2f - 60f;
        for (int i = 0; i < FavouriteCount; i++)
        {
            int index = i;
            Image slot = AddImage(panel, top, new Vector2(start + i * step, rowY), new Vector2(36f, 36f), null);
            slot.gameObject.AddComponent<Button>().onClick.AddListener(() => OnFavourite(index));
            favourites.Add(slot);
        }

        CreateButton(panel, top, new Vector2(PanelWidth / 2f - 100f, rowY), 150f, OnSave).text =
            Localization.instance.Localize("$whitehilt_paint_save");

        // The dyes.
        suggestionText = AddText(panel, top, new Vector2(0f, -530f), 17, PanelWidth - 60f, 190f, Color.white);
        suggestionText.alignment = TextAnchor.UpperLeft;
        suggestionText.supportRichText = true;

        Text mixLabel = CreateButton(panel, bottom, new Vector2(-100f, 40f), 180f, OnMix);
        mixLabel.text = Localization.instance.Localize("$whitehilt_paint_mix");
        mixButton = mixLabel.GetComponentInParent<Button>();
        CreateButton(panel, bottom, new Vector2(100f, 40f), 180f, Close).text = Localization.instance.Localize("$whitehilt_paint_close");

        DrawWheel();
        ShowFavourites();
    }

    // Closes the panel when the player walks off or presses Escape (not while typing), and works out a new mix a moment
    // after the colour stops changing.
    private void Update()
    {
        Player player = Player.m_localPlayer;
        bool typing = hexField.isFocused || redField.isFocused || greenField.isFocused || blueField.isFocused;
        if (player == null || player.IsDead() || bench == null || Vector3.Distance(player.transform.position, bench.transform.position) > MaxDistance
            || (Input.GetKeyDown(KeyCode.Escape) && !typing))
        {
            Close();
            return;
        }

        if (suggestionDirty && Time.unscaledTime - changedAt >= SuggestDelay)
        {
            suggestionDirty = false;
            suggestion = DyeCatalog.Suggest(player, Current);
            ShowSuggestion();
        }
    }

    private void OnDestroy()
    {
        if (gameObject.activeSelf)
        {
            GUIManager.BlockInput(false);
        }
    }

    private void Close()
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        gameObject.SetActive(false);
        GUIManager.BlockInput(false);
        bench = null;
    }

    private void OnWheel(Vector2 point)
    {
        Vector2 centred = point * 2f - Vector2.one;
        hue = Mathf.Repeat(Mathf.Atan2(centred.y, centred.x) / (2f * Mathf.PI), 1f);
        saturation = Mathf.Clamp01(centred.magnitude);
        Changed(redrawWheel: false);
    }

    private void OnBar(Vector2 point)
    {
        brightness = Mathf.Clamp01(point.y);
        Changed(redrawWheel: true);
    }

    private void OnHex(string text)
    {
        if (PaintColor.TryParseHex(text, out Color32 color))
        {
            SetColor(color);
        }
        else
        {
            ShowColor();
        }
    }

    private void OnRgb()
    {
        byte Read(InputField field) => (byte)Mathf.Clamp(int.TryParse(field.text, out int value) ? value : 0, 0, 255);
        SetColor(new Color32(Read(redField), Read(greenField), Read(blueField), 255));
    }

    private void OnFavourite(int index)
    {
        string[] saved = Saved();
        if (index < saved.Length && PaintColor.TryParseHex(saved[index], out Color32 color))
        {
            SetColor(color);
        }
    }

    // The newest colour goes first; the oldest drops off the end.
    private void OnSave()
    {
        string hex = PaintColor.ToHex(Current);
        List<string> saved = Saved().Where(entry => !string.Equals(entry, hex, StringComparison.OrdinalIgnoreCase)).ToList();
        saved.Insert(0, hex);
        PaintSettings.Favourites.Value = string.Join(",", saved.Take(FavouriteCount));
        ShowFavourites();
    }

    // Mixes a pot of paint from the suggested dyes and a binder.
    private void OnMix()
    {
        Player player = Player.m_localPlayer;
        if (player == null)
        {
            return;
        }

        if (suggestion == null || !suggestion.Available)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_paint_lacking");
            return;
        }

        if (!player.GetInventory().HaveEmptySlot())
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_paint_full");
            return;
        }

        foreach ((DyeCatalog.Dye dye, int amount) in suggestion.Parts)
        {
            DyeCatalog.Take(player, dye.Name, amount);
        }

        DyeCatalog.Take(player, DyeCatalog.BinderName(), 1);
        WhiteHiltPaintPot.Create(player, Current);
        player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_paint_mixed");
        Changed(redrawWheel: false);
    }

    private void SetColor(Color32 color)
    {
        Color.RGBToHSV(color, out hue, out saturation, out brightness);
        Changed(redrawWheel: true);
    }

    private void Changed(bool redrawWheel)
    {
        if (redrawWheel)
        {
            DrawWheel();
        }

        DrawBar();
        ShowColor();
        changedAt = Time.unscaledTime;
        suggestionDirty = true;
    }

    private void ShowColor()
    {
        Color32 color = Current;
        swatch.color = color;
        hexField.SetTextWithoutNotify("#" + PaintColor.ToHex(color));
        redField.SetTextWithoutNotify(color.r.ToString());
        greenField.SetTextWithoutNotify(color.g.ToString());
        blueField.SetTextWithoutNotify(color.b.ToString());

        float angle = hue * 2f * Mathf.PI;
        Vector2 size = ((RectTransform)wheelMarker.parent).rect.size;
        wheelMarker.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * saturation * size / 2f;
        barMarker.anchoredPosition = new Vector2(0f, (brightness - 0.5f) * ((RectTransform)barMarker.parent).rect.height);
    }

    // Shows the suggested mix: how close it comes, its parts and how many of each the player has.
    private void ShowSuggestion()
    {
        Player player = Player.m_localPlayer;
        if (suggestion == null || player == null)
        {
            suggestionText.text = Localization.instance.Localize("$whitehilt_paint_no_dyes");
            mixButton.interactable = false;
            return;
        }

        StringBuilder text = new();
        if (!suggestion.Available)
        {
            text.AppendLine("<color=#FF9A7A>" + Localization.instance.Localize("$whitehilt_paint_missing") + "</color>");
        }

        text.AppendLine(string.Format(Localization.instance.Localize("$whitehilt_paint_match"), Mathf.RoundToInt(suggestion.Match))
            + "   " + PaintColor.Swatch(suggestion.Result));
        foreach ((DyeCatalog.Dye dye, int amount) in suggestion.Parts)
        {
            text.AppendLine(Line(Localization.instance.Localize(dye.Name), amount, DyeCatalog.Have(player, dye.Name)));
        }

        string binder = DyeCatalog.BinderName();
        if (binder != null)
        {
            text.AppendLine(Line(Localization.instance.Localize("$whitehilt_paint_binder"), 1, DyeCatalog.Have(player, binder)));
        }

        suggestionText.text = text.ToString();
        mixButton.interactable = suggestion.Available;
    }

    private static string Line(string name, int amount, int have)
    {
        string color = have >= amount ? "#E8E2D0" : "#FF6A5A";
        return $"<color={color}>{name} ×{amount}   ({string.Format(Localization.instance.Localize("$whitehilt_paint_have"), have)})</color>";
    }

    private void ShowFavourites()
    {
        string[] saved = Saved();
        for (int i = 0; i < favourites.Count; i++)
        {
            favourites[i].color = i < saved.Length && PaintColor.TryParseHex(saved[i], out Color32 color) ? color : new Color(0f, 0f, 0f, 0.3f);
        }
    }

    private static string[] Saved()
    {
        return (PaintSettings.Favourites.Value ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(entry => entry.Trim()).ToArray();
    }

    // Draws the colour wheel at the current brightness, with a smooth edge.
    private void DrawWheel()
    {
        Color32[] pixels = new Color32[WheelPixels * WheelPixels];
        for (int y = 0; y < WheelPixels; y++)
        {
            for (int x = 0; x < WheelPixels; x++)
            {
                float dx = (x + 0.5f) / WheelPixels * 2f - 1f;
                float dy = (y + 0.5f) / WheelPixels * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r > 1f)
                {
                    pixels[y * WheelPixels + x] = new Color32(0, 0, 0, 0);
                    continue;
                }

                float h = Mathf.Repeat(Mathf.Atan2(dy, dx) / (2f * Mathf.PI), 1f);
                Color color = Color.HSVToRGB(h, r, brightness);
                color.a = Mathf.Clamp01((1f - r) * WheelPixels / 2f);
                pixels[y * WheelPixels + x] = color;
            }
        }

        wheelTexture.SetPixels32(pixels);
        wheelTexture.Apply(false);
    }

    private void DrawBar()
    {
        Color32[] pixels = new Color32[barTexture.height];
        for (int y = 0; y < pixels.Length; y++)
        {
            pixels[y] = Color.HSVToRGB(hue, saturation, (y + 0.5f) / pixels.Length);
        }

        barTexture.SetPixels32(pixels);
        barTexture.Apply(false);
    }

    private static Text AddText(Transform parent, Vector2 anchor, Vector2 position, int size, float width, float height, Color color)
    {
        Text text = GUIManager.Instance.CreateText(string.Empty, parent, anchor, anchor, position, GUIManager.Instance.AveriaSerifBold, size,
            color, true, Color.black, width, height, false).GetComponent<Text>();
        text.alignment = TextAnchor.MiddleCenter;
        return text;
    }

    private static Image AddImage(Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Texture2D texture)
    {
        GameObject go = new("image", typeof(RectTransform), typeof(Image));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = go.GetComponent<Image>();
        if (texture != null)
        {
            image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }

        return image;
    }

    // A small white square with a dark edge that shows the picked point.
    private static RectTransform AddMarker(RectTransform parent)
    {
        Image outline = AddImage(parent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14f, 14f), null);
        outline.color = Color.black;
        outline.raycastTarget = false;
        Image inner = AddImage(outline.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8f, 8f), null);
        inner.color = Color.white;
        inner.raycastTarget = false;
        return outline.rectTransform;
    }

    private static InputField AddField(Transform parent, Vector2 anchor, Vector2 position, float width, string placeholder)
    {
        return GUIManager.Instance.CreateInputField(parent, anchor, anchor, position, InputField.ContentType.Standard, placeholder, 18, width, 36f)
            .GetComponent<InputField>();
    }

    private static Text CreateButton(Transform parent, Vector2 anchor, Vector2 position, float width, UnityEngine.Events.UnityAction onClick)
    {
        GameObject button = GUIManager.Instance.CreateButton(string.Empty, parent, anchor, anchor, position, width, 36f);
        button.GetComponent<Button>().onClick.AddListener(onClick);
        return button.GetComponentInChildren<Text>();
    }
}

/// <summary>
/// Reports where on an image the pointer is pressed or dragged, from 0,0 at the bottom left to 1,1 at the top right.
/// </summary>
public class PaintPicker : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    /// <summary>Called with the point picked.</summary>
    public Action<Vector2> Picked;

    /// <inheritdoc/>
    public void OnPointerDown(PointerEventData eventData)
    {
        Report(eventData);
    }

    /// <inheritdoc/>
    public void OnDrag(PointerEventData eventData)
    {
        Report(eventData);
    }

    private void Report(PointerEventData eventData)
    {
        RectTransform rect = (RectTransform)transform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out Vector2 local))
        {
            Rect bounds = rect.rect;
            Picked?.Invoke(new Vector2(Mathf.Clamp01((local.x - bounds.xMin) / bounds.width), Mathf.Clamp01((local.y - bounds.yMin) / bounds.height)));
        }
    }
}
