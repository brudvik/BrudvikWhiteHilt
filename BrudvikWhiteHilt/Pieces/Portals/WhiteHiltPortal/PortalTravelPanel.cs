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
    private const float RowSpacing = 4f;
    private const float HandleSize = 8f;
    private const float HandleBorder = 4f;
    private const float ScrollbarWidth = 24f;
    private const float RowsPerNotch = 3f;

    // What the game's wheel input (ZInput) gives for one notch: the mouse's scroll scaled by 0.15.
    private const float WheelNotch = 0.15f;

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
    private ScrollRect listScroll;
    private RectTransform listContent;
    private Button renameButton;
    private Button privateButton;
    private Button homeButton;

    /// <summary>
    /// True while the panel is open.
    /// </summary>
    public static bool IsOpen => instance != null && instance.HasOrigin && instance.gameObject.activeSelf;

    /// <summary>
    /// True while the player types in the search field, so the map keys do not close the map.
    /// </summary>
    public static bool SearchFocused => IsOpen && instance.search != null && instance.search.isFocused;

    /// <summary>
    /// True while the pointer is over the open portal panel rather than the map behind it.
    /// </summary>
    public static bool PointerOverPanel
    {
        get
        {
            if (!IsOpen)
            {
                return false;
            }

            Canvas canvas = instance.GetComponentInParent<Canvas>().rootCanvas;
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            return RectTransformUtility.RectangleContainsScreenPoint((RectTransform)instance.transform, ZInput.pointerPosition, camera);
        }
    }

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
        rect.offsetMin = new Vector2(rect.offsetMin.x, 112f);

        PortalTravelPanel travelPanel = panel.AddComponent<PortalTravelPanel>();
        travelPanel.BuildContent(rect);
        panel.SetActive(false);
        return travelPanel;
    }

    // Builds the travel panel once: title, buttons to rename the portal, make it private and set it as home, a search
    // with sorting, the list of destinations and the travel buttons.
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
        search.onValueChanged.AddListener(_ => RefreshFromTop());
        CreateButton(panel, top, new Vector2(140f, -138f), 125f, OnToggleSort, out sortLabel);
        BuildList(panel);

        CreateButton(panel, bottom, new Vector2(-135f, 40f), 125f, OnTravel, out Text travelLabel);
        travelLabel.text = Localization.instance.Localize("$whitehilt_portal_travel");
        homeButton = CreateButton(panel, bottom, new Vector2(0f, 40f), 125f, OnTravelHome, out Text homeTravelLabel);
        homeTravelLabel.text = Localization.instance.Localize("$whitehilt_portal_take_me_home");
        CreateButton(panel, bottom, new Vector2(135f, 40f), 125f, Close, out Text closeLabel);
        closeLabel.text = Localization.instance.Localize("$whitehilt_portal_close");
    }

    // The list of destinations: a scroll view built here rather than with Jotunn's, whose nested canvases and sizes
    // fixed at creation left the scrollbar out of sight. The scrollbar is always shown, and the wheel is read in Update
    // (ScrollList), as the game's input gives the scroll view only a fraction of a row per notch.
    private void BuildList(RectTransform panel)
    {
        GameObject view = new("PortalList", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        view.transform.SetParent(panel, false);
        RectTransform viewRect = (RectTransform)view.transform;
        Stretch(viewRect, new Vector2(20f, 70f), new Vector2(-20f, -165f));
        view.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);

        GameObject viewport = new("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(viewRect, false);
        RectTransform viewportRect = (RectTransform)viewport.transform;
        Stretch(viewportRect, new Vector2(HandleBorder, HandleBorder), new Vector2(-(ScrollbarWidth + 2f * HandleBorder), -HandleBorder));

        GameObject content = new("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewportRect, false);
        listContent = (RectTransform)content.transform;
        listContent.anchorMin = new Vector2(0f, 1f);
        listContent.anchorMax = Vector2.one;
        listContent.pivot = new Vector2(0.5f, 1f);
        listContent.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = RowSpacing;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject bar = new("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        bar.transform.SetParent(viewRect, false);
        RectTransform barRect = (RectTransform)bar.transform;
        barRect.anchorMin = new Vector2(1f, 0f);
        barRect.anchorMax = Vector2.one;
        barRect.pivot = new Vector2(1f, 0.5f);
        barRect.sizeDelta = new Vector2(ScrollbarWidth, -2f * HandleBorder);
        barRect.anchoredPosition = new Vector2(-HandleBorder, 0f);
        bar.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

        GameObject area = new("Sliding Area", typeof(RectTransform));
        area.transform.SetParent(barRect, false);
        Stretch((RectTransform)area.transform, new Vector2(3f, 3f), new Vector2(-3f, -3f));
        GameObject handle = new("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(area.transform, false);
        RectTransform handleRect = (RectTransform)handle.transform;
        Stretch(handleRect, Vector2.zero, Vector2.zero);
        Image handleImage = handle.GetComponent<Image>();
        handleImage.sprite = GUIManager.Instance.GetSprite("UISprite");
        handleImage.type = Image.Type.Sliced;
        handleImage.color = Color.white;

        Scrollbar scrollbar = bar.GetComponent<Scrollbar>();
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleImage;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        ColorBlock colors = GUIManager.Instance.ValheimScrollbarHandleColorBlock;
        colors.normalColor = selectedColor;
        colors.highlightedColor = new Color(1f, 0.9f, 0.6f);
        colors.pressedColor = new Color(1f, 0.95f, 0.8f);
        colors.selectedColor = colors.highlightedColor;
        scrollbar.colors = colors;

        listScroll = view.GetComponent<ScrollRect>();
        listScroll.horizontal = false;
        listScroll.vertical = true;
        listScroll.viewport = viewportRect;
        listScroll.content = listContent;
        listScroll.verticalScrollbar = scrollbar;
        listScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        listScroll.movementType = ScrollRect.MovementType.Clamped;
        listScroll.scrollSensitivity = 0f;
    }

    // Moves the list three rows for each notch of the wheel while the pointer is over it. The game's wheel input gives
    // WheelNotch for a notch.
    private void ScrollList()
    {
        float wheel = ZInput.GetMouseScrollWheel();
        if (wheel == 0f || !PointerOver(listScroll.viewport))
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        float scrollable = listContent.rect.height - listScroll.viewport.rect.height;
        if (scrollable <= 0f)
        {
            return;
        }

        float y = listContent.anchoredPosition.y - wheel / WheelNotch * RowsPerNotch * (RowHeight + RowSpacing);
        listContent.anchoredPosition = new Vector2(listContent.anchoredPosition.x, Mathf.Clamp(y, 0f, scrollable));
    }

    private static bool PointerOver(RectTransform rect)
    {
        Canvas canvas = rect.GetComponentInParent<Canvas>().rootCanvas;
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        return RectTransformUtility.RectangleContainsScreenPoint(rect, ZInput.pointerPosition, camera);
    }

    // Jotunn sizes the parts of a scroll view once, at the size it is made with; stretch them with the view instead.
    internal static void StretchScrollView(RectTransform root)
    {
        foreach (RectTransform part in root.GetComponentsInChildren<RectTransform>(true))
        {
            switch (part.name)
            {
                case "Scroll View":
                case "Viewport":
                    Stretch(part, Vector2.zero, Vector2.zero);
                    break;
                case "Scrollbar Vertical":
                    float x = part.anchoredPosition.x;
                    part.anchorMin = new Vector2(1f, 0f);
                    part.anchorMax = new Vector2(1f, 1f);
                    part.sizeDelta = new Vector2(HandleSize, -(2f * HandleBorder + HandleSize));
                    part.anchoredPosition = new Vector2(x, 0f);
                    break;
                case "Sliding Area" when part.parent != null && part.parent.name == "Scrollbar Vertical":
                    Stretch(part, Vector2.zero, Vector2.zero);
                    break;
            }
        }
    }

    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
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

    // Opens the panel for a portal with the large map, where the destinations are pinned. Without a map in the world
    // the panel stops the player's input itself.
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

    // Closes the panel, removes its pins, gives input back and returns the map to its small size.
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
        ScrollList();
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
        homeButton.interactable = !isHome && PortalTravel.Find(PortalTravel.GetHome(player)) != null;
        homeLabel.text = Localization.instance.Localize(isHome ? $"★ $whitehilt_portal_is_home" : "$whitehilt_portal_set_home");
        sortLabel.text = Localization.instance.Localize(PortalSettings.SortByDistance ? "$whitehilt_portal_sort_distance" : "$whitehilt_portal_sort_name");
    }

    // A new search or sort shows the list from its top, where a scroll position kept from the longer list would leave
    // the few rows found out of sight.
    private void RefreshFromTop()
    {
        RefreshList();
        listContent.anchoredPosition = new Vector2(listContent.anchoredPosition.x, 0f);
    }

    // Lists the destinations that match the search, by name or by distance.
    private void RefreshList()
    {
        if (!HasOrigin || Player.m_localPlayer == null)
        {
            return;
        }

        Player player = Player.m_localPlayer;
        Vector3 here = player.transform.position;
        string home = PortalTravel.GetHome(player);
        string[] words = Simplify(search.text).Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries);
        IEnumerable<PortalDestination> visible = PortalTravel.Destinations
            .Where(destination => Matches(DisplayName(destination), words));
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

    // One destination as a row: a star for home, its name, private or not, and its distance; the portal the player
    // stands at is shown but cannot be chosen.
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

    private void OnTravelHome()
    {
        Travel(PortalTravel.Find(PortalTravel.GetHome(Player.m_localPlayer)));
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
        RefreshFromTop();
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

    // Pins every destination on the map, the chosen one larger.
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

    /// <summary>
    /// Whether a portal's name holds every word of a search, in any order.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="words">The search's words, each made with <see cref="Simplify"/>.</param>
    /// <returns>True when every word is found; always with no words.</returns>
    internal static bool Matches(string name, string[] words)
    {
        string simple = Simplify(name);
        return words.All(word => simple.IndexOf(word, System.StringComparison.Ordinal) >= 0);
    }

    /// <summary>
    /// Text as it is compared in a search: without rich-text tags, in lower case, its letters without their accents
    /// (å as a, é as e), and every run of white space, the non-breaking space too, as one space.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>The simplified text.</returns>
    internal static string Simplify(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        string plain = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]*>", string.Empty).Normalize(System.Text.NormalizationForm.FormD);
        System.Text.StringBuilder simple = new(plain.Length);
        bool space = false;
        foreach (char c in plain)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                space = true;
                continue;
            }

            if (space && simple.Length > 0)
            {
                simple.Append(' ');
            }

            space = false;
            simple.Append(char.ToLowerInvariant(c));
        }

        return simple.ToString();
    }

    private static string FormatDistance(float metres)
    {
        return metres >= 1000f ? $"{metres / 1000f:0.0} km" : $"{Mathf.RoundToInt(metres)} m";
    }
}
