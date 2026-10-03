using BepInEx.Configuration;
using BrudvikWhiteHilt.Backpack;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Portals.WhiteHiltPortal;
using BrudvikWhiteHilt.Progression;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Settings;

/// <summary>
/// The White Hilt settings window. Changes are collected and applied on Save; server settings changed by an admin are
/// then sent to the server, which passes them on to every player.
/// </summary>
public class ConfigWindow : MonoBehaviour
{
    private const float Width = 1100f;
    private const float Height = 760f;
    private const float GroupWidth = 250f;
    private const float RowHeight = 38f;
    private const float LabelWidth = 330f;
    private const float FieldWidth = 300f;
    private const float HeaderHeight = 34f;

    private static readonly Color labelColor = new(0.9f, 0.88f, 0.8f);
    private static readonly Color changedColor = new(1f, 0.75f, 0.3f);
    private static readonly Color dimColor = new(0.6f, 0.6f, 0.6f);
    private static readonly Color errorColor = new(1f, 0.45f, 0.4f);

    private static readonly KeyCode[] keyCodes = Enum.GetValues(typeof(KeyCode)).Cast<KeyCode>().Distinct()
        .Where(code => code != KeyCode.None && (code < KeyCode.Mouse0 || code > KeyCode.Mouse6) && code < KeyCode.JoystickButton0)
        .ToArray();

    private static readonly KeyCode[] modifierCodes =
    {
        KeyCode.LeftShift, KeyCode.RightShift, KeyCode.LeftControl, KeyCode.RightControl, KeyCode.LeftAlt, KeyCode.RightAlt
    };

    private static ConfigWindow instance;
    private static ConfigEntry<KeyboardShortcut> keyOpen;

    private readonly Dictionary<ConfigEntryBase, object> pending = new();
    private readonly Dictionary<ConfigEntryBase, Text> labels = new();
    private readonly List<GameObject> groupRows = new();
    private readonly List<GameObject> rows = new();

    private bool serverTab;
    private string family;
    private bool inputBlocked;
    private ConfigEntryBase capturing;
    private int captureEndFrame = -1;
    private Text captureLabel;

    private Text tabLocalLabel;
    private Text tabServerLabel;
    private InputField search;
    private RectTransform groupContent;
    private RectTransform rowContent;
    private Text description;
    private Text status;
    private Text saveLabel;
    private Button saveButton;

    /// <summary>
    /// True while the window is open.
    /// </summary>
    public static bool IsOpen => instance != null && instance.gameObject.activeSelf;

    /// <summary>
    /// The key that opens the window, as text, or empty when none is bound.
    /// </summary>
    public static string OpenKeyText => keyOpen == null || keyOpen.Value.MainKey == KeyCode.None ? string.Empty : keyOpen.Value.ToString();

    private static bool CanEditServer => SynchronizationManager.Instance.PlayerIsAdmin;

    /// <summary>
    /// Registers the English text. Call from the plugin's Awake.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("whitehilt_settings_title", "White Hilt settings");
        Translations.AddEnglish("whitehilt_settings_tab_local", "My settings");
        Translations.AddEnglish("whitehilt_settings_tab_server", "Server and world");
        Translations.AddEnglish("whitehilt_settings_search", "Search...");
        Translations.AddEnglish("whitehilt_settings_save", "Save");
        Translations.AddEnglish("whitehilt_settings_save_count", "Save ({0})");
        Translations.AddEnglish("whitehilt_settings_close", "Close");
        Translations.AddEnglish("whitehilt_settings_restart", "needs restart");
        Translations.AddEnglish("whitehilt_settings_adminonly", "Only admins can change these. You see the values the server uses.");
        Translations.AddEnglish("whitehilt_settings_saved", "Saved");
        Translations.AddEnglish("whitehilt_settings_sent", "Saved and sent to the server, which passes it on to everyone online ({0} players)");
        Translations.AddEnglish("whitehilt_settings_pressakey", "Press a key... (Esc cancels, Backspace clears)");
        Translations.AddEnglish("whitehilt_settings_none", "None");
        Translations.AddEnglish("whitehilt_settings_nothing", "No settings match");
        Translations.AddEnglish("whitehilt_settings_invalid", "Not a valid value: {0}");
        Translations.AddEnglish("whitehilt_settings_range", "{0} to {1}");
        Translations.AddEnglish("whitehilt_settings_default", "Default: {0}");
        Translations.AddEnglish("whitehilt_settings_reset", "Back to the default value");
        Translations.AddEnglish("whitehilt_settings_open", "White Hilt settings");
        Translations.AddEnglish("whitehilt_settings_panel_title", "White Hilt");
        Translations.AddEnglish("whitehilt_settings_panel_text", "All White Hilt settings: your own and the server's.");
        Translations.AddEnglish("whitehilt_settings_panel_text_key", "All White Hilt settings: your own and the server's. Shortcut: {0}");
        Translations.AddEnglish("whitehilt_settings_button", "Settings");
    }

    /// <summary>
    /// Binds the key that opens the window. Call from the plugin's Awake, after the config is initialized.
    /// </summary>
    public static void BindKey()
    {
        keyOpen = WhiteHiltConfig.BindLocal("Settings.Keys", "OpenSettings", new KeyboardShortcut(KeyCode.F7), "Opens or closes the White Hilt settings window.");
    }

    /// <summary>
    /// Opens or closes the window when its key is pressed. Call every frame.
    /// </summary>
    public static void CheckKey()
    {
        if (keyOpen == null || !BackpackInput.Pressed(keyOpen))
        {
            return;
        }

        if (IsOpen)
        {
            // The same press may just have been captured as a new key binding in the window.
            if (instance.capturing == null && instance.captureEndFrame != Time.frameCount)
            {
                instance.Close();
            }

            return;
        }

        if (!BackpackInput.Typing())
        {
            Toggle();
        }
    }

    /// <summary>
    /// Opens the window, or closes it when it is open.
    /// </summary>
    public static void Toggle()
    {
        if (IsOpen)
        {
            instance.Close();
            return;
        }

        if (GUIManager.CustomGUIFront == null || WhiteHiltConfig.File == null)
        {
            return;
        }

        if (instance == null)
        {
            instance = Build();
        }

        instance.Open();
    }

    private static ConfigWindow Build()
    {
        Vector2 middle = new(0.5f, 0.5f);
        GameObject panel = GUIManager.Instance.CreateWoodpanel(GUIManager.CustomGUIFront.transform, middle, middle, Vector2.zero, Width, Height, false);
        panel.name = "WhiteHiltSettings";
        ConfigWindow window = panel.AddComponent<ConfigWindow>();
        window.BuildContent((RectTransform)panel.transform);
        panel.SetActive(false);
        return window;
    }

    private void BuildContent(RectTransform panel)
    {
        Vector2 top = new(0.5f, 1f);
        Vector2 topLeft = new(0f, 1f);
        Vector2 topRight = new(1f, 1f);
        Vector2 bottomLeft = new(0f, 0f);
        Vector2 bottomRight = new(1f, 0f);

        Text title = GUIManager.Instance.CreateText(Localization.instance.Localize("$whitehilt_settings_title"), panel, top, top, new Vector2(0f, -38f),
            GUIManager.Instance.AveriaSerifBold, 28, GUIManager.Instance.ValheimOrange, true, Color.black, Width - 40f, 40f, false).GetComponent<Text>();
        title.alignment = TextAnchor.MiddleCenter;

        CreateButton(panel, topLeft, new Vector2(20f + 115f, -85f), 230f, () => SetTab(false), out tabLocalLabel);
        CreateButton(panel, topLeft, new Vector2(260f + 115f, -85f), 230f, () => SetTab(true), out tabServerLabel);
        search = GUIManager.Instance.CreateInputField(panel, topRight, topRight, new Vector2(-180f, -85f), InputField.ContentType.Standard,
            Localization.instance.Localize("$whitehilt_settings_search"), 18, 320f, 36f).GetComponent<InputField>();
        search.onValueChanged.AddListener(_ => RebuildRows());

        groupContent = CreateScroll(panel, new Vector2(20f, 160f), new Vector2(20f + GroupWidth, -115f), anchorRight: 0f);
        rowContent = CreateScroll(panel, new Vector2(40f + GroupWidth, 160f), new Vector2(-20f, -115f), anchorRight: 1f);

        // Same dark backing as the lists, filling the space down to the buttons.
        RectTransform descriptionFrame = new GameObject("DescriptionFrame", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        descriptionFrame.SetParent(panel, false);
        descriptionFrame.anchorMin = new Vector2(0f, 0f);
        descriptionFrame.anchorMax = new Vector2(1f, 0f);
        descriptionFrame.offsetMin = new Vector2(20f, 68f);
        descriptionFrame.offsetMax = new Vector2(-20f, 152f);
        Image frameImage = descriptionFrame.GetComponent<Image>();
        frameImage.color = new Color(0f, 0f, 0f, 0.35f);
        frameImage.raycastTarget = false;

        description = GUIManager.Instance.CreateText(string.Empty, panel, bottomLeft, bottomLeft, Vector2.zero, GUIManager.Instance.AveriaSerif, 16,
            labelColor, true, Color.black, Width - 40f, 80f, false).GetComponent<Text>();
        description.alignment = TextAnchor.UpperLeft;
        RectTransform descriptionRect = description.rectTransform;
        descriptionRect.anchorMin = new Vector2(0f, 0f);
        descriptionRect.anchorMax = new Vector2(1f, 0f);
        descriptionRect.offsetMin = new Vector2(32f, 74f);
        descriptionRect.offsetMax = new Vector2(-32f, 146f);

        status = GUIManager.Instance.CreateText(string.Empty, panel, bottomLeft, bottomLeft, new Vector2(24f + 330f, 40f), GUIManager.Instance.AveriaSerif, 16,
            changedColor, true, Color.black, 660f, 36f, false).GetComponent<Text>();
        status.alignment = TextAnchor.MiddleLeft;

        saveButton = CreateButton(panel, bottomRight, new Vector2(-330f, 40f), 180f, Save, out saveLabel);
        CreateButton(panel, bottomRight, new Vector2(-120f, 40f), 180f, Close, out Text closeLabel);
        closeLabel.text = Localization.instance.Localize("$whitehilt_settings_close");
    }

    private static RectTransform CreateScroll(RectTransform panel, Vector2 offsetMin, Vector2 offsetMax, float anchorRight)
    {
        GameObject scroll = GUIManager.Instance.CreateScrollView(panel, false, true, 8f, 4f, GUIManager.Instance.ValheimScrollbarHandleColorBlock,
            new Color(0f, 0f, 0f, 0.35f), 300f, 300f);
        RectTransform rect = (RectTransform)scroll.transform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(anchorRight, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        PortalTravelPanel.StretchScrollView(rect);
        ScrollRect settingsScroll = scroll.GetComponentInChildren<ScrollRect>();
        settingsScroll.scrollSensitivity = 132f;
        RectTransform content = settingsScroll.content;
        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 2f;
        layout.padding = new RectOffset(6, 14, 6, 6);
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        return content;
    }

    private static Button CreateButton(Transform parent, Vector2 anchor, Vector2 position, float width, UnityEngine.Events.UnityAction onClick, out Text label)
    {
        GameObject button = GUIManager.Instance.CreateButton(string.Empty, parent, anchor, anchor, position, width, 36f);
        button.GetComponent<Button>().onClick.AddListener(onClick);
        label = button.GetComponentInChildren<Text>();
        return button.GetComponent<Button>();
    }

    private void Open()
    {
        pending.Clear();
        capturing = null;
        status.text = string.Empty;
        search.text = string.Empty;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        if (!inputBlocked)
        {
            GUIManager.BlockInput(true);
            inputBlocked = true;
        }

        SetTab(serverTab);
    }

    private void Close()
    {
        pending.Clear();
        capturing = null;
        gameObject.SetActive(false);
        if (inputBlocked)
        {
            GUIManager.BlockInput(false);
            inputBlocked = false;
        }
    }

    private void OnDestroy()
    {
        if (inputBlocked)
        {
            GUIManager.BlockInput(false);
        }
    }

    private void Update()
    {
        if (capturing != null)
        {
            UpdateCapture();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    private void SetTab(bool server)
    {
        serverTab = server;
        tabLocalLabel.text = Localization.instance.Localize("$whitehilt_settings_tab_local");
        tabServerLabel.text = Localization.instance.Localize("$whitehilt_settings_tab_server");
        tabLocalLabel.color = server ? labelColor : changedColor;
        tabServerLabel.color = server ? changedColor : labelColor;

        List<string> families = Entries().Select(entry => ConfigText.Family(entry.Definition.Section)).Distinct()
            .OrderBy(ConfigText.FamilyName, StringComparer.CurrentCultureIgnoreCase).ToList();
        if (family == null || !families.Contains(family))
        {
            family = families.FirstOrDefault();
        }

        groupRows.ForEach(Destroy);
        groupRows.Clear();
        foreach (string name in families)
        {
            GameObject row = GUIManager.Instance.CreateButton(ConfigText.FamilyName(name), groupContent, Vector2.zero, Vector2.zero, Vector2.zero, GroupWidth - 30f, 34f);
            row.AddComponent<LayoutElement>().preferredHeight = 34f;
            Text label = row.GetComponentInChildren<Text>();
            label.alignment = TextAnchor.MiddleLeft;
            label.fontSize = 17;
            label.color = name == family ? changedColor : labelColor;
            label.rectTransform.offsetMin = new Vector2(10f, 0f);
            row.GetComponent<Button>().onClick.AddListener(() => SelectFamily(name));
            groupRows.Add(row);
        }

        RebuildRows();
    }

    private void SelectFamily(string name)
    {
        family = name;
        search.text = string.Empty;
        SetTab(serverTab);
    }

    private IEnumerable<ConfigEntryBase> Entries()
    {
        ConfigFile file = WhiteHiltConfig.File;
        return file.Keys.Select(definition => file[definition])
            .Where(entry => IsAdminOnly(entry) == serverTab && IsBrowsable(entry));
    }

    private static bool IsAdminOnly(ConfigEntryBase entry)
    {
        return entry.Description?.Tags?.OfType<ConfigurationManagerAttributes>().Any(attributes => attributes.IsAdminOnly == true) == true;
    }

    private static bool IsBrowsable(ConfigEntryBase entry)
    {
        return entry.Description?.Tags?.OfType<ConfigurationManagerAttributes>().All(attributes => attributes.Browsable != false) != false;
    }

    private void RebuildRows()
    {
        rows.ForEach(Destroy);
        rows.Clear();
        labels.Clear();
        description.text = string.Empty;
        UpdateSaveButton();

        bool readOnly = serverTab && !CanEditServer;
        if (readOnly)
        {
            AddNote(Localization.instance.Localize("$whitehilt_settings_adminonly"), dimColor);
        }

        string filter = search.text.Trim();
        List<ConfigEntryBase> visible = filter.Length > 0
            ? Entries().Where(entry => Matches(entry, filter)).ToList()
            : Entries().Where(entry => ConfigText.Family(entry.Definition.Section) == family).ToList();

        string lastHeading = null;
        foreach (ConfigEntryBase entry in visible)
        {
            string section = entry.Definition.Section;
            string heading = filter.Length > 0
                ? JoinHeading(ConfigText.FamilyName(ConfigText.Family(section)), ConfigText.SectionName(section))
                : ConfigText.SectionName(section);
            if (heading != null && heading != lastHeading)
            {
                AddHeading(heading);
            }

            lastHeading = heading;
            rows.Add(CreateRow(entry, readOnly));
        }

        if (visible.Count == 0)
        {
            AddNote(Localization.instance.Localize("$whitehilt_settings_nothing"), dimColor);
        }
    }

    private static string JoinHeading(string familyName, string sectionName)
    {
        return sectionName == null ? familyName : $"{familyName} - {sectionName}";
    }

    private static bool Matches(ConfigEntryBase entry, string filter)
    {
        ConfigDefinition definition = entry.Definition;
        return Contains(ConfigText.Name(definition), filter)
            || Contains(ConfigText.Description(definition, entry.Description?.Description), filter)
            || Contains(definition.Key, filter)
            || Contains(ConfigText.FamilyName(ConfigText.Family(definition.Section)), filter)
            || Contains(ConfigText.SectionName(definition.Section), filter);
    }

    private static bool Contains(string text, string filter)
    {
        return text != null && text.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0;
    }

    private void AddHeading(string text)
    {
        GameObject heading = GUIManager.Instance.CreateText(text, rowContent, Vector2.zero, Vector2.zero, Vector2.zero, GUIManager.Instance.AveriaSerifBold, 19,
            GUIManager.Instance.ValheimOrange, true, Color.black, 600f, HeaderHeight, false);
        heading.GetComponent<Text>().alignment = TextAnchor.LowerLeft;
        heading.AddComponent<LayoutElement>().preferredHeight = HeaderHeight;
        rows.Add(heading);
    }

    private void AddNote(string text, Color color)
    {
        GameObject note = GUIManager.Instance.CreateText(text, rowContent, Vector2.zero, Vector2.zero, Vector2.zero, GUIManager.Instance.AveriaSerif, 17,
            color, true, Color.black, 600f, RowHeight, false);
        note.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        note.AddComponent<LayoutElement>().preferredHeight = RowHeight;
        rows.Add(note);
    }

    private GameObject CreateRow(ConfigEntryBase entry, bool readOnly)
    {
        GameObject row = new("setting", typeof(RectTransform));
        row.transform.SetParent(rowContent, false);
        row.AddComponent<LayoutElement>().preferredHeight = RowHeight;
        // Transparent, but catches the pointer so hovering anywhere on the row shows its description.
        row.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        AddHover(row, entry);

        Vector2 left = new(0f, 0.5f);
        Text label = GUIManager.Instance.CreateText(ConfigText.Name(entry.Definition), row.transform, left, left, new Vector2(10f + LabelWidth / 2f, 0f),
            GUIManager.Instance.AveriaSerifBold, 16, labelColor, true, Color.black, LabelWidth, RowHeight, false).GetComponent<Text>();
        label.alignment = TextAnchor.MiddleLeft;
        label.raycastTarget = false;
        labels[entry] = label;
        UpdateLabel(entry);

        float fieldX = 20f + LabelWidth;
        CreateField(entry, row.transform, fieldX, readOnly);

        float extraX = fieldX + FieldWidth + 10f;
        if (!readOnly)
        {
            Button reset = CreateButton(row.transform, left, new Vector2(extraX + 20f, 0f), 40f, () => ResetEntry(entry), out Text resetLabel);
            resetLabel.text = "↺";
            ((RectTransform)reset.transform).sizeDelta = new Vector2(40f, 30f);
            AddHover(reset.gameObject, entry, "$whitehilt_settings_reset");
            extraX += 50f;
        }

        string extra = RangeText(entry);
        if (ConfigText.NeedsRestart(entry))
        {
            extra = (extra.Length > 0 ? extra + "   " : string.Empty) + "↻ " + Localization.instance.Localize("$whitehilt_settings_restart");
        }

        if (extra.Length > 0)
        {
            Text hint = GUIManager.Instance.CreateText(extra, row.transform, left, left, new Vector2(extraX + 110f, 0f), GUIManager.Instance.AveriaSerif, 14,
                dimColor, true, Color.black, 220f, RowHeight, false).GetComponent<Text>();
            hint.alignment = TextAnchor.MiddleLeft;
            hint.raycastTarget = false;
        }

        return row;
    }

    private void CreateField(ConfigEntryBase entry, Transform row, float x, bool readOnly)
    {
        Vector2 left = new(0f, 0.5f);
        Type type = entry.SettingType;
        object value = CurrentValue(entry);

        if (type == typeof(bool))
        {
            GameObject toggleObject = GUIManager.Instance.CreateToggle(row, 28f, 28f);
            RectTransform rect = (RectTransform)toggleObject.transform;
            rect.anchorMin = left;
            rect.anchorMax = left;
            rect.anchoredPosition = new Vector2(x + 14f, 0f);
            Toggle toggle = toggleObject.GetComponent<Toggle>();
            toggle.isOn = (bool)value;
            toggle.interactable = !readOnly;
            toggle.onValueChanged.AddListener(isOn => SetPending(entry, isOn));
            return;
        }

        if (type == typeof(KeyboardShortcut) || type == typeof(KeyCode))
        {
            Button button = CreateButton(row, left, new Vector2(x + FieldWidth / 2f, 0f), FieldWidth, () => { }, out Text keyLabel);
            ((RectTransform)button.transform).sizeDelta = new Vector2(FieldWidth, 32f);
            keyLabel.text = KeyText(value);
            button.interactable = !readOnly;
            button.onClick.AddListener(() => StartCapture(entry, keyLabel));
            return;
        }

        if (type.IsEnum || entry.Description?.AcceptableValues is AcceptableValueList<string>)
        {
            List<object> options = type.IsEnum
                ? Enum.GetValues(type).Cast<object>().ToList()
                : ((AcceptableValueList<string>)entry.Description.AcceptableValues).AcceptableValues.Cast<object>().ToList();
            Dropdown dropdown = GUIManager.Instance.CreateDropDown(row, left, left, new Vector2(x + FieldWidth / 2f, 0f), 16, FieldWidth, 32f).GetComponent<Dropdown>();
            dropdown.options = options.Select(option => new Dropdown.OptionData(type.IsEnum ? ConfigText.EnumName(type, option.ToString()) : option.ToString())).ToList();
            dropdown.value = Math.Max(0, options.FindIndex(option => Equals(option, value)));
            dropdown.interactable = !readOnly;
            dropdown.onValueChanged.AddListener(index => SetPending(entry, options[index]));
            return;
        }

        InputField field = GUIManager.Instance.CreateInputField(row, left, left, new Vector2(x + FieldWidth / 2f, 0f), InputField.ContentType.Standard,
            null, 16, FieldWidth, 32f).GetComponent<InputField>();
        field.text = FormatValue(value, type);
        field.interactable = !readOnly;
        field.onEndEdit.AddListener(text => OnFieldEdited(entry, field, text));
    }

    private void OnFieldEdited(ConfigEntryBase entry, InputField field, string text)
    {
        Type type = entry.SettingType;
        if (!TryParse(text, type, out object value))
        {
            status.color = errorColor;
            status.text = string.Format(Localization.instance.Localize("$whitehilt_settings_invalid"), text);
            field.text = FormatValue(CurrentValue(entry), type);
            return;
        }

        if (entry.Description?.AcceptableValues != null)
        {
            value = entry.Description.AcceptableValues.Clamp(value);
        }

        field.text = FormatValue(value, type);
        SetPending(entry, value);
    }

    private static bool TryParse(string text, Type type, out object value)
    {
        value = null;
        string trimmed = (text ?? string.Empty).Trim();
        try
        {
            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            {
                trimmed = trimmed.Replace(',', '.');
            }

            value = type == typeof(string) ? text ?? string.Empty : TomlTypeConverter.ConvertToValue(trimmed, type);
            return value != null;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string FormatValue(object value, Type type)
    {
        return value switch
        {
            null => string.Empty,
            float number => number.ToString("0.#####", CultureInfo.InvariantCulture),
            double number => number.ToString("0.#####", CultureInfo.InvariantCulture),
            string text => text,
            _ => TomlTypeConverter.ConvertToString(value, type)
        };
    }

    private static string RangeText(ConfigEntryBase entry)
    {
        if (entry.Description?.AcceptableValues == null || entry.Description.AcceptableValues is AcceptableValueList<string>)
        {
            return string.Empty;
        }

        Type rangeType = entry.Description.AcceptableValues.GetType();
        if (!rangeType.IsGenericType || rangeType.GetGenericTypeDefinition() != typeof(AcceptableValueRange<>))
        {
            return string.Empty;
        }

        object min = rangeType.GetProperty("MinValue")?.GetValue(entry.Description.AcceptableValues);
        object max = rangeType.GetProperty("MaxValue")?.GetValue(entry.Description.AcceptableValues);
        return string.Format(Localization.instance.Localize("$whitehilt_settings_range"),
            FormatValue(min, entry.SettingType), FormatValue(max, entry.SettingType));
    }

    private static string ShortcutText(KeyboardShortcut shortcut)
    {
        return shortcut.MainKey == KeyCode.None ? Localization.instance.Localize("$whitehilt_settings_none") : shortcut.ToString();
    }

    private static string KeyText(object value)
    {
        return value switch
        {
            KeyboardShortcut shortcut => ShortcutText(shortcut),
            KeyCode.None => Localization.instance.Localize("$whitehilt_settings_none"),
            _ => value?.ToString() ?? string.Empty
        };
    }

    private void AddHover(GameObject target, ConfigEntryBase entry, string extraToken = null)
    {
        EventTrigger trigger = target.GetComponent<EventTrigger>() ?? target.AddComponent<EventTrigger>();
        EventTrigger.Entry enter = new() { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => ShowDescription(entry, extraToken));
        trigger.triggers.Add(enter);
    }

    private void ShowDescription(ConfigEntryBase entry, string extraToken)
    {
        string text = ConfigText.Description(entry.Definition, entry.Description?.Description);
        string defaultText = entry.SettingType == typeof(KeyboardShortcut) || entry.SettingType == typeof(KeyCode)
            ? KeyText(entry.DefaultValue)
            : entry.SettingType == typeof(bool) || entry.SettingType.IsEnum ? DisplayValue(entry, entry.DefaultValue) : FormatValue(entry.DefaultValue, entry.SettingType);
        text += "\n" + string.Format(Localization.instance.Localize("$whitehilt_settings_default"), defaultText.Length > 0 ? defaultText : "-");
        if (extraToken != null)
        {
            text = Localization.instance.Localize(extraToken) + "\n" + text;
        }

        description.text = text;
    }

    private static string DisplayValue(ConfigEntryBase entry, object value)
    {
        if (entry.SettingType == typeof(bool))
        {
            return Localization.instance.Localize((bool)value ? "$menu_on" : "$menu_off");
        }

        return ConfigText.EnumName(entry.SettingType, value.ToString());
    }

    private object CurrentValue(ConfigEntryBase entry)
    {
        return pending.TryGetValue(entry, out object value) ? value : entry.BoxedValue;
    }

    private void SetPending(ConfigEntryBase entry, object value)
    {
        string current = TomlTypeConverter.ConvertToString(entry.BoxedValue, entry.SettingType);
        string changed = TomlTypeConverter.ConvertToString(value, entry.SettingType);
        if (current == changed)
        {
            pending.Remove(entry);
        }
        else
        {
            pending[entry] = value;
        }

        status.text = string.Empty;
        UpdateLabel(entry);
        UpdateSaveButton();
    }

    private void ResetEntry(ConfigEntryBase entry)
    {
        SetPending(entry, entry.DefaultValue);
        RebuildRows();
    }

    private void UpdateLabel(ConfigEntryBase entry)
    {
        if (labels.TryGetValue(entry, out Text label) && label != null)
        {
            label.color = pending.ContainsKey(entry) ? changedColor : labelColor;
        }
    }

    private void UpdateSaveButton()
    {
        saveButton.interactable = pending.Count > 0;
        saveLabel.text = pending.Count > 0
            ? string.Format(Localization.instance.Localize("$whitehilt_settings_save_count"), pending.Count)
            : Localization.instance.Localize("$whitehilt_settings_save");
    }

    private void StartCapture(ConfigEntryBase entry, Text label)
    {
        capturing = entry;
        captureLabel = label;
        captureLabel.text = Localization.instance.Localize("$whitehilt_settings_pressakey");
    }

    private void UpdateCapture()
    {
        bool plainKey = capturing.SettingType == typeof(KeyCode);
        foreach (KeyCode code in keyCodes)
        {
            // A shortcut waits for its main key while modifiers are held; a plain key setting may be a modifier itself.
            if (!Input.GetKeyDown(code) || (!plainKey && modifierCodes.Contains(code)))
            {
                continue;
            }

            ConfigEntryBase entry = capturing;
            capturing = null;
            captureEndFrame = Time.frameCount;
            if (code == KeyCode.Escape)
            {
                captureLabel.text = KeyText(CurrentValue(entry));
                return;
            }

            object value = plainKey
                ? code == KeyCode.Backspace ? KeyCode.None : code
                : code == KeyCode.Backspace
                    ? KeyboardShortcut.Empty
                    : new KeyboardShortcut(code, modifierCodes.Where(Input.GetKey).ToArray());
            captureLabel.text = KeyText(value);
            SetPending(entry, value);
            return;
        }
    }

    private void Save()
    {
        if (pending.Count == 0)
        {
            return;
        }

        bool serverChanged = false;
        foreach (KeyValuePair<ConfigEntryBase, object> change in pending)
        {
            bool adminOnly = IsAdminOnly(change.Key);
            if (adminOnly && !CanEditServer)
            {
                continue;
            }

            change.Key.BoxedValue = change.Value;
            serverChanged |= adminOnly;
        }

        pending.Clear();
        ConfigFile file = WhiteHiltConfig.File;
        file.Save();
        // Jotunn sends changed server settings when the file is reloaded; the server passes them on to every player.
        if (serverChanged)
        {
            file.Reload();
        }

        bool online = ZNet.instance != null && !ZNet.instance.IsServer();
        status.color = changedColor;
        status.text = serverChanged && online
            ? string.Format(Localization.instance.Localize("$whitehilt_settings_sent"), ZNet.instance.GetNrOfPlayers())
            : Localization.instance.Localize("$whitehilt_settings_saved");
        RebuildRows();
    }
}
