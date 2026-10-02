using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Navigation.Discoveries;

/// <summary>
/// The panel under the bottom-left corner of the large map where the player picks which discoveries to show. Its header
/// stays under the map and the list opens upwards over it. It lists only kinds that have been found, by group, as icons
/// that are grey while switched off. It shows while Munin's Perch shares discoveries; below the Exploration level it
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
    private const float FooterHeight = 42f;
    private const float Width = Padding * 2f + Columns * IconSize + (Columns - 1) * Gap;
    private const float CollapsedHeight = Padding * 2f + HeaderHeight;

    private static readonly Color offBack = new(0.2f, 0.2f, 0.2f, 0.85f);
    private static readonly Color offIcon = new(0.45f, 0.45f, 0.45f, 0.7f);
    private static readonly List<KindToggle> toggles = new();

    private static GameObject panel;
    private static RectTransform content;
    private static Text footer;
    private static string footerDefault = string.Empty;
    private static bool expanded;
    private static bool builtExpanded;
    private static bool builtLocked;
    private static int builtData = -1;
    private static int paintedFilter = -1;

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
        bool visible = map.m_mode == Minimap.MapMode.Large && Player.m_localPlayer != null && DiscoveryOverlay.Kinds.Count > 0
            && GUIManager.CustomGUIFront != null;
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

        GameObject headerButton = CreateButton(Padding, y, Width - Padding * 2f, HeaderHeight, () => expanded = !expanded);
        CreateLabel(headerButton.transform, header, 18, GUIManager.Instance.ValheimOrange, TextAnchor.MiddleCenter);
        headerButton.GetComponent<Button>().interactable = !locked;
        y += HeaderHeight;

        ((RectTransform)panel.transform).sizeDelta = new Vector2(Width, y + Padding);
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
            string hover = $"{DiscoveryCatalog.GetLabel(key)}\n{string.Format(Localization.instance.Localize("$whitehilt_disc_found"), kind.Total)}";
            AddHover(button, hover);
            toggles.Add(new KindToggle { Key = key, Back = back, Icon = icon, Colour = DiscoveryOverlay.GroupColour(group) });
        }

        int rows = (kinds.Count + Columns - 1) / Columns;
        return y + rows * IconSize + (rows - 1) * Gap;
    }

    private static void Paint()
    {
        paintedFilter = DiscoveryFilter.Version;
        foreach (KindToggle toggle in toggles)
        {
            bool on = DiscoveryFilter.IsShown(toggle.Key);
            toggle.Back.color = on ? toggle.Colour : offBack;
            toggle.Icon.color = on ? Color.white : offIcon;
        }
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

    private static void AddHover(GameObject target, string text)
    {
        EventTrigger trigger = target.AddComponent<EventTrigger>();
        EventTrigger.Entry enter = new() { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => SetFooter(text));
        EventTrigger.Entry exit = new() { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => SetFooter(footerDefault));
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
        public Image Back;
        public Image Icon;
        public Color Colour;
    }
}
