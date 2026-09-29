using BrudvikWhiteHilt.Pieces.Portals.PortalMap;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Pieces.Portals.WhiteHiltPortal;

/// <summary>
/// The travel map of the White Hilt portals: the large map with a list on the left. The list can be searched and sorted
/// by name or distance; picking a portal in the list shows it on the map, clicking one on the map picks it in the list.
/// Double-click or Travel goes there. The portal used can be renamed, made private and set as home from here.
/// </summary>
public class PortalTravelPanel : MonoBehaviour
{
    private const float PanelWidth = 440f;
    private const float RowHeight = 40f;
    private const float DoubleClickTime = 0.4f;

    private static readonly Color selectedColor = new(1f, 0.75f, 0.3f);
    private static readonly Color otherColor = new(0.9f, 0.88f, 0.8f);
    private static readonly Color hereColor = new(0.6f, 0.6f, 0.6f);

    private static PortalTravelPanel instance;

    private readonly List<(Minimap.PinData Pin, PortalDestination Destination)> pins = new();
    private readonly List<GameObject> rows = new();

    private IWhiteHiltPortal origin;
    private string selectedId;
    private float lastClickTime;
    private string lastClickId;
    private bool inputBlocked;

    private Text title;
    private Text privateLabel;
    private Text homeLabel;
    private Text sortLabel;
    private Text renameLabel;
    private InputField search;
    private RectTransform listContent;
    private Button renameButton;
    private Button privateButton;

    /// <summary>
    /// True while the panel is open.
    /// </summary>
    public static bool IsOpen => instance != null && instance.HasOrigin && instance.gameObject.activeSelf;

    /// <summary>
    /// True while the player types in the search field, so the map keys do not close the map.
    /// </summary>
    public static bool SearchFocused => IsOpen && instance.search != null && instance.search.isFocused;

    /// <summary>
    /// Opens the travel map at a portal.
    /// </summary>
    /// <param name="portal">The portal used.</param>
    public static void Open(IWhiteHiltPortal portal)
    {
        if (GUIManager.CustomGUIFront == null || Minimap.instance == null || Player.m_localPlayer == null)
        {
            return;
        }

        if (instance == null)
        {
            instance = Build();
        }

        instance.Show(portal);
    }

    /// <summary>
    /// Handles a click on the large map: picks the portal nearest the click.
    /// </summary>
    /// <param name="worldPosition">World position clicked.</param>
    /// <param name="radius">How close the click must be, in metres.</param>
    public static void OnMapClick(Vector3 worldPosition, float radius)
    {
        PortalDestination destination = FindNear(worldPosition, radius);
        if (destination != null)
        {
            instance.Select(destination.Id, center: false);
        }
    }

    /// <summary>
    /// Handles a double-click on the large map: travels to the portal nearest the click.
    /// </summary>
    /// <param name="worldPosition">World position clicked.</param>
    /// <param name="radius">How close the click must be, in metres.</param>
    public static void OnMapDoubleClick(Vector3 worldPosition, float radius)
    {
        PortalDestination destination = FindNear(worldPosition, radius);
        if (destination != null)
        {
            instance.Travel(destination);
        }
    }

    private static PortalDestination FindNear(Vector3 worldPosition, float radius)
    {
        if (!IsOpen)
        {
            return null;
        }

        return instance.pins
            .Select(pin => (pin.Destination, Distance: Utils.DistanceXZ(worldPosition, pin.Destination.Position)))
            .Where(candidate => candidate.Distance < radius)
            .OrderBy(candidate => candidate.Distance)
            .Select(candidate => candidate.Destination)
            .FirstOrDefault();
    }

    private static PortalTravelPanel Build()
    {
        GameObject panel = GUIManager.Instance.CreateWoodpanel(GUIManager.CustomGUIFront.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, PanelWidth, 600f, false);
        panel.name = "WhiteHiltPortalTravel";
        RectTransform rect = (RectTransform)panel.transform;
        rect.anchorMin = new Vector2(0f, 0.06f);
        rect.anchorMax = new Vector2(0f, 0.94f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, 0f);
        rect.anchoredPosition = new Vector2(20f, 0f);

        PortalTravelPanel travelPanel = panel.AddComponent<PortalTravelPanel>();
        travelPanel.BuildContent(rect);
        panel.SetActive(false);
        return travelPanel;
    }

    private void BuildContent(RectTransform panel)
    {
        Vector2 top = new(0.5f, 1f);
        Vector2 bottom = new(0.5f, 0f);

        title = GUIManager.Instance.CreateText(string.Empty, panel, top, top, new Vector2(0f, -38f), GUIManager.Instance.AveriaSerifBold, 26,
            GUIManager.Instance.ValheimOrange, true, Color.black, PanelWidth - 40f, 40f, false).GetComponent<Text>();
        title.alignment = TextAnchor.MiddleCenter;

        renameButton = CreateButton(panel, top, new Vector2(-135f, -88f), 125f, OnRename, out renameLabel);
        privateButton = CreateButton(panel, top, new Vector2(0f, -88f), 125f, OnTogglePrivate, out privateLabel);
        CreateButton(panel, top, new Vector2(135f, -88f), 125f, OnSetHome, out homeLabel);

        search = GUIManager.Instance.CreateInputField(panel, top, top, new Vector2(-65f, -138f), InputField.ContentType.Standard,
            Localization.instance.Localize("$whitehilt_portal_search"), 18, 265f, 36f).GetComponent<InputField>();
        search.onValueChanged.AddListener(_ => RefreshList());
        CreateButton(panel, top, new Vector2(140f, -138f), 125f, OnToggleSort, out sortLabel);

        GameObject scroll = GUIManager.Instance.CreateScrollView(panel, false, true, 8f, 4f, GUIManager.Instance.ValheimScrollbarHandleColorBlock,
            new Color(0f, 0f, 0f, 0.35f), PanelWidth - 40f, 400f);
        RectTransform scrollRect = (RectTransform)scroll.transform;
        scrollRect.anchorMin = new Vector2(0f, 0f);
        scrollRect.anchorMax = new Vector2(1f, 1f);
        scrollRect.pivot = new Vector2(0.5f, 0.5f);
        scrollRect.offsetMin = new Vector2(20f, 80f);
        scrollRect.offsetMax = new Vector2(-20f, -165f);
        listContent = scroll.GetComponentInChildren<ScrollRect>().content;
        VerticalLayoutGroup layout = listContent.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;

        CreateButton(panel, bottom, new Vector2(-95f, 40f), 180f, OnTravel, out Text travelLabel);
        travelLabel.text = Localization.instance.Localize("$whitehilt_portal_travel");
        CreateButton(panel, bottom, new Vector2(95f, 40f), 180f, Close, out Text closeLabel);
        closeLabel.text = Localization.instance.Localize("$whitehilt_portal_close");
    }

    private static Button CreateButton(Transform parent, Vector2 anchor, Vector2 position, float width, UnityEngine.Events.UnityAction onClick, out Text label)
    {
        GameObject button = GUIManager.Instance.CreateButton(string.Empty, parent, anchor, anchor, position, width, 36f);
        button.GetComponent<Button>().onClick.AddListener(onClick);
        label = button.GetComponentInChildren<Text>();
        return button.GetComponent<Button>();
    }

    // The portal is a Unity object, which compares equal to null once destroyed.
    private bool HasOrigin => origin as Object != null;

    private void Show(IWhiteHiltPortal portal)
    {
        origin = portal;
        selectedId = null;
        search.text = string.Empty;
        gameObject.SetActive(true);

        // The open map already stops the player; without a map the panel has to.
        if (Game.m_noMap && !inputBlocked)
        {
            GUIManager.BlockInput(true);
            inputBlocked = true;
        }

        if (!Game.m_noMap)
        {
            Minimap.instance.SetMapMode(Minimap.MapMode.Large);
        }

        PortalTravel.Changed -= RefreshList;
        PortalTravel.Changed += RefreshList;
        RefreshList();
        CenterOn(portal.Position);
    }

    private void Close()
    {
        if (!HasOrigin && !gameObject.activeSelf)
        {
            return;
        }

        origin = null;
        PortalTravel.Changed -= RefreshList;
        RemovePins();
        gameObject.SetActive(false);
        if (inputBlocked)
        {
            GUIManager.BlockInput(false);
            inputBlocked = false;
        }

        if (Minimap.instance != null && Minimap.instance.m_mode == Minimap.MapMode.Large)
        {
            Minimap.instance.SetMapMode(Minimap.MapMode.Small);
        }
    }

    private void Update()
    {
        Player player = Player.m_localPlayer;
        bool mapClosed = !Game.m_noMap && Minimap.instance != null && Minimap.instance.m_mode != Minimap.MapMode.Large;
        bool naming = TextInput.instance != null && TextInput.instance.m_panel != null && TextInput.instance.m_panel.activeSelf;
        if (!HasOrigin || player == null || player.IsDead() || mapClosed || (Input.GetKeyDown(KeyCode.Escape) && !naming))
        {
            Close();
            return;
        }

        UpdateHeader(player);
    }

    private void OnDestroy()
    {
        PortalTravel.Changed -= RefreshList;
        if (inputBlocked)
        {
            GUIManager.BlockInput(false);
        }
    }

    private void UpdateHeader(Player player)
    {
        string name = origin.PortalName;
        title.text = string.IsNullOrEmpty(name) ? Localization.instance.Localize(origin.GetHoverName()) : name;
        bool canEdit = origin.CanEdit(player);
        renameButton.interactable = canEdit;
        privateButton.interactable = canEdit;
        renameLabel.text = Localization.instance.Localize("$whitehilt_portal_rename");
        privateLabel.text = Localization.instance.Localize(origin.IsPrivate ? "$whitehilt_portal_make_public" : "$whitehilt_portal_make_private");
        bool isHome = !string.IsNullOrEmpty(origin.Id) && PortalTravel.GetHome(player) == origin.Id;
        homeLabel.text = Localization.instance.Localize(isHome ? $"★ $whitehilt_portal_is_home" : "$whitehilt_portal_set_home");
        sortLabel.text = Localization.instance.Localize(PortalSettings.SortByDistance ? "$whitehilt_portal_sort_distance" : "$whitehilt_portal_sort_name");
    }

    private void RefreshList()
    {
        if (!HasOrigin || Player.m_localPlayer == null)
        {
            return;
        }

        Player player = Player.m_localPlayer;
        Vector3 here = player.transform.position;
        string home = PortalTravel.GetHome(player);
        string filter = search.text.Trim();
        IEnumerable<PortalDestination> visible = PortalTravel.Destinations
            .Where(destination => filter.Length == 0 || DisplayName(destination).IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0);
        visible = PortalSettings.SortByDistance
            ? visible.OrderBy(destination => Utils.DistanceXZ(here, destination.Position))
            : visible.OrderBy(DisplayName, System.StringComparer.CurrentCultureIgnoreCase);

        rows.ForEach(Destroy);
        rows.Clear();
        foreach (PortalDestination destination in visible)
        {
            rows.Add(CreateRow(destination, here, home));
        }

        if (rows.Count == 0)
        {
            GameObject empty = GUIManager.Instance.CreateText(Localization.instance.Localize("$whitehilt_portal_none"), listContent, Vector2.zero, Vector2.zero,
                Vector2.zero, GUIManager.Instance.AveriaSerif, 18, hereColor, false, Color.black, PanelWidth - 60f, RowHeight, false);
            empty.AddComponent<LayoutElement>().preferredHeight = RowHeight;
            rows.Add(empty);
        }

        RefreshPins();
    }

    private GameObject CreateRow(PortalDestination destination, Vector3 here, string home)
    {
        bool isHere = IsOrigin(destination);
        float distance = Utils.DistanceXZ(here, destination.Position);
        string text = $"{(destination.Id == home ? "★ " : string.Empty)}{DisplayName(destination)}";
        if (destination.Private)
        {
            text += Localization.instance.Localize(" ($whitehilt_portalmap_private)");
        }

        text += isHere ? Localization.instance.Localize("  ($whitehilt_portal_here)") : $"  -  {FormatDistance(distance)}";

        GameObject row = GUIManager.Instance.CreateButton(text, listContent, Vector2.zero, Vector2.zero, Vector2.zero, PanelWidth - 70f, RowHeight);
        row.AddComponent<LayoutElement>().preferredHeight = RowHeight;
        Text label = row.GetComponentInChildren<Text>();
        label.alignment = TextAnchor.MiddleLeft;
        label.fontSize = 18;
        label.color = isHere ? hereColor : destination.Id == selectedId ? selectedColor : otherColor;
        label.rectTransform.offsetMin = new Vector2(12f, 0f);
        Button button = row.GetComponent<Button>();
        button.interactable = !isHere;
        button.onClick.AddListener(() => OnRowClicked(destination));
        return row;
    }

    private void OnRowClicked(PortalDestination destination)
    {
        if (lastClickId == destination.Id && Time.unscaledTime - lastClickTime < DoubleClickTime)
        {
            Travel(destination);
            return;
        }

        lastClickId = destination.Id;
        lastClickTime = Time.unscaledTime;
        Select(destination.Id, center: true);
    }

    private void Select(string id, bool center)
    {
        PortalDestination destination = PortalTravel.Find(id);
        if (destination == null || IsOrigin(destination))
        {
            return;
        }

        selectedId = id;
        RefreshList();
        if (center)
        {
            CenterOn(destination.Position);
        }
    }

    private void Travel(PortalDestination destination)
    {
        if (!HasOrigin || destination == null || IsOrigin(destination))
        {
            return;
        }

        if (PortalTravel.TryTravel(Player.m_localPlayer, destination, origin.Position))
        {
            Close();
        }
    }

    private void OnTravel()
    {
        Travel(PortalTravel.Find(selectedId));
    }

    private void OnRename()
    {
        if (HasOrigin && origin.CanEdit(Player.m_localPlayer))
        {
            TextInput.instance.RequestText(origin, "$whitehilt_portal_name", 30);
        }
    }

    private void OnTogglePrivate()
    {
        if (HasOrigin && origin.CanEdit(Player.m_localPlayer))
        {
            origin.TogglePrivate();
        }
    }

    private void OnSetHome()
    {
        if (HasOrigin && !string.IsNullOrEmpty(origin.Id))
        {
            PortalTravel.SetHome(Player.m_localPlayer, origin.Id);
            RefreshList();
        }
    }

    private void OnToggleSort()
    {
        PortalSettings.SortByDistance = !PortalSettings.SortByDistance;
        RefreshList();
    }

    private bool IsOrigin(PortalDestination destination)
    {
        return HasOrigin && (destination.Id == origin.Id || Utils.DistanceXZ(destination.Position, origin.Position) < 0.5f);
    }

    // Moves the map so the point shows in the middle of the part the panel does not cover.
    private void CenterOn(Vector3 point)
    {
        Minimap map = Minimap.instance;
        if (map == null || Game.m_noMap || Player.m_localPlayer == null)
        {
            return;
        }

        Vector3[] corners = new Vector3[4];
        ((RectTransform)transform).GetWorldCorners(corners);
        float covered = Mathf.Clamp01(corners[2].x / Screen.width);
        float visibleWidth = map.m_mapImageLarge.uvRect.width * map.m_textureSize * map.m_pixelSize;
        Vector3 offset = point - Player.m_localPlayer.transform.position;
        offset.x -= covered / 2f * visibleWidth;
        map.m_mapOffset = offset;
    }

    private void RefreshPins()
    {
        RemovePins();
        Minimap map = Minimap.instance;
        if (map == null || !HasOrigin)
        {
            return;
        }

        Sprite icon = PortalMapPins.GetPortalBadge();
        foreach (PortalDestination destination in PortalTravel.Destinations)
        {
            Minimap.PinData pin = map.AddPin(destination.Position, Minimap.PinType.None, DisplayName(destination), false, false);
            pin.m_icon = icon;
            pin.m_doubleSize = destination.Id == selectedId;
            pins.Add((pin, destination));
        }
    }

    private void RemovePins()
    {
        Minimap map = Minimap.instance;
        if (map != null)
        {
            pins.ForEach(pin => map.RemovePin(pin.Pin));
        }

        pins.Clear();
    }

    private static string DisplayName(PortalDestination destination)
    {
        if (string.IsNullOrEmpty(destination.Name))
        {
            return Localization.instance.Localize(destination.ShipId.IsNone() ? "$whitehilt_portalmap_unnamed" : "$whitehilt_shipportal_unnamed");
        }

        return destination.Name;
    }

    private static string FormatDistance(float metres)
    {
        return metres >= 1000f ? $"{metres / 1000f:0.0} km" : $"{Mathf.RoundToInt(metres)} m";
    }
}
