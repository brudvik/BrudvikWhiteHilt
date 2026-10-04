using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Navigation.Discoveries;

/// <summary>
/// The panel under the bottom-left corner of the large map where the player picks which discoveries to show. Its header
/// stays under the map and the list opens upwards over it. It lists only kinds that have been found, by group, as icons
/// on light discs, with coloured rims and checkmarks while switched on, and quick buttons to show or hide everything and
/// to keep what is unlimited in chests off the map. It shows while Munin's Perch shares discoveries; below the Exploration level it
/// only says what level is needed.
/// </summary>
public static class DiscoveryPanel
{
    private const float Padding = 16f;
    private const int Columns = 6;
    private const float IconSize = 34f;
    private const float Gap = 5f;
    private const float HeaderHeight = 30f;
    private const float GroupHeight = 24f;
    private const float FooterHeight = 72f;
    private const float QuickHeight = 28f;
    private const float Width = Padding * 2f + Columns * IconSize + (Columns - 1) * Gap;
    private const float CollapsedHeight = Padding * 2f + HeaderHeight;

    private static readonly Color offRim = new(0.48f, 0.48f, 0.48f, 1f);
    private static readonly Color iconBack = new(0.82f, 0.82f, 0.82f, 1f);
    private static readonly Color biomeBack = new(1f, 0.9f, 0.62f, 1f);
    private static readonly Color unlimitedGold = new(1f, 210f / 255f, 77f / 255f, 1f);
    private static readonly List<KindToggle> toggles = new();

    internal static global::BrudvikWhiteHilt.Chests.ChestModule Chests { get; set; }

    private static GameObject panel;
    private static RectTransform content;
    private static Text footer;
    private static Text hideUnlimitedLabel;
    private static string footerDefault = string.Empty;
    private static bool expanded;
    private static bool builtExpanded;
    private static bool builtLocked;
    private static int builtData = -1;
    private static int paintedFilter = -1;
    private static ZoneSystem builtLocationsFor;
    private static Dictionary<string, Heightmap.Biome> locationBiomes = new();

    /// <summary>
    /// True if the kind is an item the local player has unlimited in chests.
    /// </summary>
    /// <param name="key">Kind key.</param>
    /// <returns>True if unlimited.</returns>
    internal static bool IsUnlimited(string key)
    {
        if (Chests == null || ObjectDB.instance == null || !key.StartsWith(DiscoveryCatalog.ItemPrefix, System.StringComparison.Ordinal))
        {
            return false;
        }

        string name = key.Substring(DiscoveryCatalog.ItemPrefix.Length);
        ItemDrop item = ObjectDB.instance.GetItemPrefab(name)?.GetComponent<ItemDrop>();
        return item != null && Chests.GetUnlimitedChest(name, item.m_itemData.m_shared) != null;
    }

    /// <summary>
    /// The panel while it shows on the large map, else null; the overview lines up to its right.
    /// </summary>
    public static RectTransform Shown => panel != null && panel.activeSelf ? (RectTransform)panel.transform : null;

    /// <summary>
    /// Shows or hides the panel with the large map and keeps it up to date. Called after the map's own update.
    /// </summary>
    /// <param name="map">The map.</param>
    public static void Update(Minimap map)
    {
        // The chart table's route panel takes the space under the map while a route is planned.
        bool visible = map.m_mode == Minimap.MapMode.Large && Player.m_localPlayer != null && DiscoveryOverlay.Kinds.Count > 0
            && GUIManager.CustomGUIFront != null && !Pieces.Navigation.ShipRoutePlanner.Planning;
        if (!visible)
        {
            if (panel != null && panel.activeSelf)
            {
                panel.SetActive(false);
            }

            return;
        }

        if (panel == null)
        {
            Create();
        }

        bool locked = !DiscoveryOverlay.LargeMapUnlocked();
        if (builtData != DiscoveryOverlay.DataVersion || builtExpanded != expanded || builtLocked != locked)
        {
            Rebuild(locked);
        }

        if (!panel.activeSelf)
        {
            panel.SetActive(true);
        }

        LargeMapFrame.PlaceBelow(map, (RectTransform)panel.transform, 0f, CollapsedHeight);
        if (paintedFilter != DiscoveryFilter.Version)
        {
            Paint();
        }

        PaintStatus();
    }

    private static void Create()
    {
        Vector2 bottomLeft = Vector2.zero;
        panel = GUIManager.Instance.CreateWoodpanel(GUIManager.CustomGUIFront.transform, bottomLeft, bottomLeft, Vector2.zero, Width, 100f, false);
        panel.name = "WhiteHiltDiscoveries";
        content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(panel.transform, false);
        content.anchorMin = Vector2.zero;
        content.anchorMax = Vector2.one;
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;
        builtData = -1;
    }

    private static void Rebuild(bool locked)
    {
        builtData = DiscoveryOverlay.DataVersion;
        builtExpanded = expanded;
        builtLocked = locked;
        paintedFilter = -1;
        toggles.Clear();
        hideUnlimitedLabel = null;
        foreach (Transform child in content)
        {
            Object.Destroy(child.gameObject);
        }

        // Laid out top-down with the header last, so it stays at the bottom while the list opens upwards.
        float y = Padding;
        string title = Localization.instance.Localize("$whitehilt_disc_title");
        string header = locked ? title : $"{title}  {(expanded ? "-" : "+")}";

        footerDefault = locked
            ? string.Format(Localization.instance.Localize("$whitehilt_disc_locked"), DiscoverySettings.LargeMapLevel.Value)
            : expanded ? Localization.instance.Localize("$whitehilt_disc_hint") : string.Empty;
        if (!locked && expanded)
        {
            foreach (IGrouping<DiscoveryGroup, DiscoveryKind> group in DiscoveryOverlay.Kinds.GroupBy(kind => kind.Group).OrderBy(group => group.Key))
            {
                y = AddGroup(group.Key, group.OrderBy(kind => DiscoveryCatalog.GetLabel(kind.Key)).ToList(), y) + Gap;
            }
        }

        if (!string.IsNullOrEmpty(footerDefault))
        {
            GameObject footerObject = new("Footer", typeof(RectTransform));
            Place((RectTransform)footerObject.transform, Padding, y, Width - Padding * 2f, FooterHeight);
            footer = CreateLabel(footerObject.transform, footerDefault, 14, Color.white, TextAnchor.MiddleCenter);
            y += FooterHeight + Gap;
        }
        else
        {
            footer = null;
        }

        if (!locked && expanded)
        {
            y = AddQuickButtons(y);
        }

        GameObject headerButton = CreateButton(Padding, y, Width - Padding * 2f, HeaderHeight, () => expanded = !expanded);
        CreateLabel(headerButton.transform, header, 18, GUIManager.Instance.ValheimOrange, TextAnchor.MiddleCenter);
        headerButton.GetComponent<Button>().interactable = !locked;
        y += HeaderHeight;

        ((RectTransform)panel.transform).sizeDelta = new Vector2(Width, y + Padding);
    }

    // Show all and Hide all side by side, and the switch for unlimited items below them.
    private static float AddQuickButtons(float y)
    {
        float inner = Width - Padding * 2f;
        float half = (inner - Gap) / 2f;
        AddQuickButton(Padding, y, half, "$whitehilt_disc_show_all", "$whitehilt_disc_show_all_hint",
            () => DiscoveryFilter.Set(DiscoveryOverlay.Kinds.Select(kind => kind.Key), true));
        AddQuickButton(Padding + half + Gap, y, half, "$whitehilt_disc_hide_all", "$whitehilt_disc_hide_all_hint",
            () => DiscoveryFilter.Set(DiscoveryOverlay.Kinds.Select(kind => kind.Key), false));
        y += QuickHeight + Gap;
        hideUnlimitedLabel = AddQuickButton(Padding, y, inner, string.Empty, "$whitehilt_disc_hide_unlimited_hint", DiscoveryFilter.ToggleHideUnlimited);
        return y + QuickHeight + Gap;
    }

    private static Text AddQuickButton(float x, float y, float width, string label, string hint, UnityEngine.Events.UnityAction onClick)
    {
        GameObject button = GUIManager.Instance.CreateButton(Localization.instance.Localize(label), content, Vector2.zero, Vector2.zero, Vector2.zero, width, QuickHeight);
        Place((RectTransform)button.transform, x, y, width, QuickHeight);
        button.GetComponent<Button>().onClick.AddListener(onClick);
        Text text = button.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.fontSize = 14;
            text.resizeTextForBestFit = false;
        }

        string hintText = Localization.instance.Localize(hint);
        EventTrigger trigger = button.AddComponent<EventTrigger>();
        EventTrigger.Entry enter = new() { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => SetFooter(hintText));
        EventTrigger.Entry exit = new() { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => SetFooter(footerDefault));
        trigger.triggers.Add(enter);
        trigger.triggers.Add(exit);
        return text;
    }

    private static float AddGroup(DiscoveryGroup group, List<DiscoveryKind> kinds, float y)
    {
        List<string> keys = kinds.Select(kind => kind.Key).ToList();
        GameObject groupButton = CreateButton(Padding, y, Width - Padding * 2f, GroupHeight, () =>
        {
            DiscoveryFilter.Set(keys, !keys.Any(DiscoveryFilter.IsShown));
        });
        CreateLabel(groupButton.transform, DiscoveryCatalog.GetGroupLabel(group), 15, DiscoveryOverlay.GroupColour(group), TextAnchor.MiddleLeft);
        y += GroupHeight;

        for (int i = 0; i < kinds.Count; i++)
        {
            DiscoveryKind kind = kinds[i];
            float x = Padding + i % Columns * (IconSize + Gap);
            float top = y + i / Columns * (IconSize + Gap);
            string key = kind.Key;
            GameObject button = CreateButton(x, top, IconSize, IconSize, () => DiscoveryFilter.Toggle(key));
            Image back = button.GetComponent<Image>();
            back.sprite = DiscoveryOverlay.Disc();
            Image inner = CreateDisc(button.transform, "Background", iconBack);
            inner.rectTransform.offsetMin = new Vector2(2f, 2f);
            inner.rectTransform.offsetMax = new Vector2(-2f, -2f);
            GameObject iconObject = new("Icon", typeof(RectTransform), typeof(Image));
            RectTransform iconRect = (RectTransform)iconObject.transform;
            iconRect.SetParent(button.transform, false);
            iconRect.anchorMin = new Vector2(0.14f, 0.14f);
            iconRect.anchorMax = new Vector2(0.86f, 0.86f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = DiscoveryCatalog.GetIcon(key);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.color = Color.white;
            Image check = CreateDisc(button.transform, "Check", new Color(0.16f, 0.16f, 0.16f, 1f));
            RectTransform checkRect = check.rectTransform;
            checkRect.anchorMin = checkRect.anchorMax = new Vector2(1f, 0f);
            checkRect.sizeDelta = new Vector2(12f, 12f);
            checkRect.anchoredPosition = new Vector2(-4f, 4f);
            AddCheckStroke(check.transform, new Vector2(-2f, -1f), 4f, -45f);
            AddCheckStroke(check.transform, new Vector2(1f, 0f), 7f, 45f);
            Image badgeBack = CreateDisc(button.transform, "UnlimitedBackground", new Color(0.16f, 0.16f, 0.16f, 1f));
            RectTransform badgeBackRect = badgeBack.rectTransform;
            badgeBackRect.anchorMin = badgeBackRect.anchorMax = new Vector2(0f, 1f);
            badgeBackRect.pivot = new Vector2(0f, 1f);
            badgeBackRect.anchoredPosition = new Vector2(-2f, 3f);
            badgeBackRect.sizeDelta = new Vector2(23f, 18f);
            GameObject badgeObject = new("Unlimited", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform badgeRect = (RectTransform)badgeObject.transform;
            badgeRect.SetParent(badgeBack.transform, false);
            badgeRect.anchorMin = Vector2.zero;
            badgeRect.anchorMax = Vector2.one;
            badgeRect.offsetMin = Vector2.zero;
            badgeRect.offsetMax = Vector2.zero;
            TextMeshProUGUI badge = badgeObject.GetComponent<TextMeshProUGUI>();
            badge.font = InventoryGui.instance != null ? InventoryGui.instance.m_containerName.font : TMP_Settings.defaultFontAsset;
            badge.text = badge.font != null && badge.font.HasCharacter('\u221E', true, true) ? "\u221E" : "MAX";
            badge.fontSize = badge.text == "MAX" ? 10f : 20f;
            badge.fontStyle = FontStyles.Bold;
            badge.alignment = TextAlignmentOptions.Center;
            badge.color = unlimitedGold;
            badge.raycastTarget = false;
            badgeBack.gameObject.SetActive(false);
            KindToggle toggle = new() { Key = key, Rim = back, Background = inner, Icon = icon, UnlimitedBadge = badgeBack.gameObject,
                Check = check.gameObject, Colour = DiscoveryOverlay.GroupColour(group) };
            string hover = $"{DiscoveryCatalog.GetLabel(key)}\n{string.Format(Localization.instance.Localize("$whitehilt_disc_found"), kind.Total)}";
            AddHover(button, hover, toggle);
            toggles.Add(toggle);
        }

        int rows = (kinds.Count + Columns - 1) / Columns;
        return y + rows * IconSize + (rows - 1) * Gap;
    }

    private static void PaintStatus()
    {
        if (builtLocationsFor != ZoneSystem.instance)
        {
            locationBiomes = DiscoveryCatalog.GetLocationBiomes();
            builtLocationsFor = ZoneSystem.instance;
        }

        Heightmap.Biome biome = Player.m_localPlayer.GetCurrentBiome();
        IReadOnlyList<string> items = Chests?.GetGatherableItems(biome);
        foreach (KindToggle toggle in toggles)
        {
            bool inBiome;
            bool unlimited = false;
            if (toggle.Key.StartsWith(DiscoveryCatalog.ItemPrefix, System.StringComparison.Ordinal))
            {
                string name = toggle.Key.Substring(DiscoveryCatalog.ItemPrefix.Length);
                inBiome = items != null && items.Contains(name);
                unlimited = IsUnlimited(toggle.Key);
            }
            else
            {
                inBiome = locationBiomes.TryGetValue(toggle.Key, out Heightmap.Biome biomes) && (biomes & biome) != 0;
            }

            toggle.Background.color = inBiome ? biomeBack : iconBack;
            toggle.UnlimitedBadge.SetActive(unlimited);
            toggle.InBiome = inBiome;
            toggle.Unlimited = unlimited;
            if (toggle.Hovered)
            {
                SetFooter(GetHoverText(toggle));
            }
        }
    }

    private static string GetHoverText(KindToggle toggle)
    {
        string text = toggle.HoverText;
        if (toggle.Unlimited)
        {
            text += "\n" + Localization.instance.Localize("$whitehilt_disc_unlimited");
        }

        if (toggle.InBiome)
        {
            text += "\n" + Localization.instance.Localize("$whitehilt_disc_current_biome");
        }

        if (DiscoveryFilter.IsHiddenAsUnlimited(toggle.Key))
        {
            text += "\n" + Localization.instance.Localize("$whitehilt_disc_hidden_unlimited");
        }

        return text;
    }

    private static void Paint()
    {
        paintedFilter = DiscoveryFilter.Version;
        foreach (KindToggle toggle in toggles)
        {
            PaintToggle(toggle);
        }

        if (hideUnlimitedLabel != null)
        {
            bool hide = DiscoveryFilter.HideUnlimited;
            hideUnlimitedLabel.text = string.Format(Localization.instance.Localize("$whitehilt_disc_hide_unlimited"),
                Localization.instance.Localize(hide ? "$whitehilt_disc_on" : "$whitehilt_disc_off"));
            hideUnlimitedLabel.color = hide ? GUIManager.Instance.ValheimOrange : Color.white;
        }
    }

    private static void PaintToggle(KindToggle toggle)
    {
        bool on = DiscoveryFilter.IsShown(toggle.Key);
        Color rim = on ? toggle.Colour : offRim;
        toggle.Rim.color = toggle.Hovered ? Color.Lerp(rim, Color.white, 0.65f) : rim;
        toggle.Check.SetActive(on);

        // Switched on but hidden as unlimited: faded, so it is clear why it is not on the map.
        toggle.Icon.color = DiscoveryFilter.IsHiddenAsUnlimited(toggle.Key) ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
    }

    private static Image CreateDisc(Transform parent, string name, Color colour)
    {
        GameObject disc = new(name, typeof(RectTransform), typeof(Image));
        Image image = disc.GetComponent<Image>();
        image.rectTransform.SetParent(parent, false);
        image.rectTransform.anchorMin = Vector2.zero;
        image.rectTransform.anchorMax = Vector2.one;
        image.rectTransform.offsetMin = Vector2.zero;
        image.rectTransform.offsetMax = Vector2.zero;
        image.sprite = DiscoveryOverlay.Disc();
        image.color = colour;
        image.raycastTarget = false;
        return image;
    }

    private static void AddCheckStroke(Transform parent, Vector2 position, float length, float angle)
    {
        GameObject stroke = new("Stroke", typeof(RectTransform), typeof(Image));
        RectTransform rect = (RectTransform)stroke.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(length, 2f);
        rect.localRotation = Quaternion.Euler(0f, 0f, angle);
        Image image = stroke.GetComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = false;
    }

    private static GameObject CreateButton(float x, float y, float width, float height, UnityEngine.Events.UnityAction onClick)
    {
        GameObject button = new("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        Place((RectTransform)button.transform, x, y, width, height);
        Image image = button.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f);
        Button component = button.GetComponent<Button>();
        component.transition = Selectable.Transition.None;
        component.onClick.AddListener(onClick);
        return button;
    }

    private static Text CreateLabel(Transform parent, string text, int size, Color colour, TextAnchor alignment)
    {
        GameObject label = GUIManager.Instance.CreateText(text, parent, Vector2.zero, Vector2.one, Vector2.zero, GUIManager.Instance.AveriaSerifBold, size,
            colour, true, Color.black, 0f, 0f, false);
        RectTransform rect = (RectTransform)label.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Text component = label.GetComponent<Text>();
        component.alignment = alignment;
        component.raycastTarget = false;
        component.horizontalOverflow = HorizontalWrapMode.Wrap;
        component.verticalOverflow = VerticalWrapMode.Overflow;
        return component;
    }

    private static void AddHover(GameObject target, string text, KindToggle toggle)
    {
        toggle.HoverText = text;
        EventTrigger trigger = target.AddComponent<EventTrigger>();
        EventTrigger.Entry enter = new() { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ =>
        {
            toggle.Hovered = true;
            PaintToggle(toggle);
            SetFooter(GetHoverText(toggle));
        });
        EventTrigger.Entry exit = new() { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ =>
        {
            toggle.Hovered = false;
            PaintToggle(toggle);
            SetFooter(footerDefault);
        });
        trigger.triggers.Add(enter);
        trigger.triggers.Add(exit);
    }

    private static void SetFooter(string text)
    {
        if (footer != null)
        {
            footer.text = text;
        }
    }

    // Children are laid out from the top-left corner, y growing downwards.
    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.SetParent(content, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private sealed class KindToggle
    {
        public string Key;
        public Image Rim;
        public Image Background;
        public Image Icon;
        public GameObject UnlimitedBadge;
        public GameObject Check;
        public Color Colour;
        public bool Hovered;
        public string HoverText;
        public bool InBiome;
        public bool Unlimited;
    }
}
