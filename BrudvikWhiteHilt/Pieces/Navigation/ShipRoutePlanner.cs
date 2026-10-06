using BrudvikWhiteHilt.Navigation;
using BrudvikWhiteHilt.Pieces.Ships;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Pieces.Navigation;

/// <summary>
/// The Navigator's Table's route on the map, for the local player: setting markers on the large map from the table
/// (Exploration <see cref="ShipSettings.RouteMarkersLevel"/>), "Take me there" (<see cref="ShipSettings.RouteSailLevel"/>),
/// the markers as pins while aboard, and the arrow on the minimap toward the next one.
/// </summary>
public static class ShipRoutePlanner
{
    private const float PanelWidth = 800f;
    private const float PanelHeight = 110f;

    // A right-click removes a marker within this share of the map's visible width.
    private const float RemoveReach = 0.03f;

    private static readonly List<Minimap.PinData> pins = new();

    private static ShipRoute editing;
    private static ShipRoute pinned;
    private static string pinnedSignature;
    private static bool plotting;
    private static bool plottingExplore;
    private static ShipRoute plottingRoute;
    private static GameObject panel;
    private static Text hint;
    private static Button sailButton;
    private static Text sailLabel;
    private static Button exploreButton;
    private static Text exploreLabel;

    /// <summary>True while markers are being set on the map.</summary>
    public static bool Planning => editing != null;

    /// <summary>
    /// True for a pin that shows a route marker.
    /// </summary>
    /// <param name="pin">The pin, or null.</param>
    /// <returns>True for a route pin.</returns>
    public static bool IsRoutePin(Minimap.PinData pin)
    {
        return pin != null && pins.Contains(pin);
    }

    /// <summary>
    /// Opens the map to set markers, if the player knows enough of exploring.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="route">The ship's route.</param>
    /// <returns>True if the map opened.</returns>
    public static bool Open(Player player, ShipRoute route)
    {
        if (route == null || Minimap.instance == null || Game.m_noMap || !ShipSettings.ShipRoutes.Value)
        {
            return false;
        }

        int needed = ShipSettings.RouteMarkersLevel.Value;
        if (ExplorationSkill.GetLevel(player) < needed)
        {
            player.Message(MessageHud.MessageType.Center,
                string.Format(Localization.instance.Localize("$whitehilt_route_need_markers"), needed));
            return true;
        }

        editing = route;
        Minimap.instance.SetMapMode(Minimap.MapMode.Large);
        EnsurePanel();
        panel.SetActive(true);
        return true;
    }

    /// <summary>
    /// Adds a marker where the map was clicked.
    /// </summary>
    /// <param name="world">The clicked world point.</param>
    public static void OnMapClick(Vector3 world)
    {
        List<Vector3> markers = editing.Markers.ToList();
        if (markers.Count >= ShipRoute.MaxMarkers)
        {
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center, "$whitehilt_route_full");
            return;
        }

        markers.Add(new Vector3(world.x, 0f, world.z));
        editing.SetMarkers(markers);
    }

    /// <summary>
    /// Removes the marker nearest to where the map was right-clicked.
    /// </summary>
    /// <param name="world">The clicked world point.</param>
    public static void OnMapRightClick(Vector3 world)
    {
        Minimap map = Minimap.instance;
        float reach = map.m_largeZoom * map.m_textureSize * map.m_pixelSize * RemoveReach;
        List<Vector3> markers = editing.Markers.ToList();
        int nearest = -1;
        float best = reach;
        for (int i = 0; i < markers.Count; i++)
        {
            float distance = Utils.DistanceXZ(markers[i], world);
            if (distance < best)
            {
                best = distance;
                nearest = i;
            }
        }

        if (nearest >= 0)
        {
            markers.RemoveAt(nearest);
            editing.SetMarkers(markers);
        }
    }

    /// <summary>
    /// Keeps the pins, the arrow and the panel up to date. Called every frame for the local player.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Tick(Player player)
    {
        ShipRoute aboard = ShipRoute.Aboard(player);

        // The coroutine dies with its ship.
        if (plotting && plottingRoute == null)
        {
            plotting = false;
        }
        if (editing != null && (editing != aboard || Minimap.instance == null || Minimap.instance.m_mode != Minimap.MapMode.Large))
        {
            Close();
        }

        UpdatePins(aboard);
        RouteArrow.Update(player, aboard);
        if (editing != null)
        {
            UpdatePanel(player);
        }
    }

    private static void Close()
    {
        editing = null;
        if (panel != null)
        {
            panel.SetActive(false);
        }

        if (Minimap.instance != null && Minimap.instance.m_mode == Minimap.MapMode.Large)
        {
            Minimap.instance.SetMapMode(Minimap.MapMode.Small);
        }
    }

    // Shows the route's markers as numbered map pins, and only rebuilds them when the markers changed.
    private static void UpdatePins(ShipRoute route)
    {
        IReadOnlyList<Vector3> markers = route != null ? route.Markers : null;
        string signature = markers == null ? string.Empty : string.Join(";", markers.Select(marker => $"{marker.x:F0},{marker.z:F0}"));
        if (route == pinned && signature == pinnedSignature)
        {
            return;
        }

        pinned = route;
        pinnedSignature = signature;
        foreach (Minimap.PinData pin in pins)
        {
            Minimap.instance?.RemovePin(pin);
        }

        pins.Clear();
        if (markers == null || Minimap.instance == null)
        {
            return;
        }

        for (int i = 0; i < markers.Count; i++)
        {
            pins.Add(Minimap.instance.AddPin(markers[i], Minimap.PinType.Icon3, (i + 1).ToString(), false, false));
        }
    }

    // Updates the panel's hint and buttons: sailing and exploring need a level of the exploration skill, and are greyed
    // out while a route is being plotted.
    private static void UpdatePanel(Player player)
    {
        int count = editing.Markers.Count;
        hint.text = string.Format(Localization.instance.Localize("$whitehilt_route_hint"), count, ShipRoute.MaxMarkers);
        int needed = ShipSettings.RouteSailLevel.Value;
        int level = Mathf.FloorToInt(ExplorationSkill.GetLevel(player));
        bool skilled = level >= needed;
        string label = plotting && !plottingExplore ? "$whitehilt_route_plotting"
            : editing.Sailing ? "$whitehilt_route_stop"
            : skilled ? "$whitehilt_route_sail"
            : string.Format(Localization.instance.Localize("$whitehilt_route_need_sail"), needed);
        sailLabel.text = Localization.instance.Localize(label);
        sailButton.interactable = !plotting && (editing.Sailing || (skilled && count > 0));

        int exploreNeeded = ShipSettings.RouteExploreLevel.Value;
        bool explorer = level >= exploreNeeded;
        string exploreText = plotting && plottingExplore ? "$whitehilt_route_plotting"
            : explorer ? "$whitehilt_route_explore"
            : string.Format(Localization.instance.Localize("$whitehilt_route_need_sail"), exploreNeeded);
        exploreLabel.text = Localization.instance.Localize(exploreText);
        exploreButton.interactable = !plotting && !editing.Sailing && explorer && count > 0;
        bool autopilot = ShipSettings.RouteAutopilot.Value;
        if (sailButton.gameObject.activeSelf != autopilot)
        {
            sailButton.gameObject.SetActive(autopilot);
            exploreButton.gameObject.SetActive(autopilot);
        }
    }

    private static void OnSail()
    {
        Plot(false);
    }

    private static void OnExplore()
    {
        Plot(true);
    }

    // Plots a sea route through the markers, in a coroutine spread over frames as it can take a while, then sets sail.
    // Exploring follows the coast instead of taking the shortest way.
    private static void Plot(bool explore)
    {
        ShipRoute route = editing;
        if (route == null || plotting || !ShipSettings.RouteAutopilot.Value)
        {
            return;
        }

        if (route.Sailing)
        {
            route.Stop();
            return;
        }

        int needed = explore ? ShipSettings.RouteExploreLevel.Value : ShipSettings.RouteSailLevel.Value;
        List<Vector3> markers = route.Markers.ToList();
        if (markers.Count == 0 || ExplorationSkill.GetLevel(Player.m_localPlayer) < needed)
        {
            return;
        }

        SeaRouteFinder.CoastOptions coast = explore
            ? new SeaRouteFinder.CoastOptions
            {
                Cells = ShipSettings.RouteExploreCoastCells.Value,
                OpenWaterCost = ShipSettings.RouteExploreOpenWaterCost.Value
            }
            : null;
        plotting = true;
        plottingExplore = explore;
        plottingRoute = route;
        route.StartCoroutine(SeaRouteFinder.Find(route.transform.position, markers, coast, (path, snapped) =>
        {
            plotting = false;
            if (route == null)
            {
                return;
            }

            if (path == null)
            {
                Player.m_localPlayer?.Message(MessageHud.MessageType.Center, "$whitehilt_route_none");
                return;
            }

            route.Sail(path, snapped, explore);
            Close();
        }));
    }

    private static void OnClear()
    {
        editing?.SetMarkers(new List<Vector3>());
    }

    // Builds the route panel once, at the bottom of the screen: the hint and the buttons to sail, explore, clear and
    // close.
    private static void EnsurePanel()
    {
        if (panel != null)
        {
            return;
        }

        Vector2 bottom = new(0.5f, 0f);
        panel = GUIManager.Instance.CreateWoodpanel(GUIManager.CustomGUIFront.transform, bottom, bottom, new Vector2(0f, 40f + PanelHeight / 2f),
            PanelWidth, PanelHeight, false);
        panel.name = "WhiteHiltShipRoute";
        RectTransform rect = (RectTransform)panel.transform;

        Vector2 top = new(0.5f, 1f);
        hint = GUIManager.Instance.CreateText(string.Empty, rect, top, top, new Vector2(0f, -28f), GUIManager.Instance.AveriaSerifBold, 18,
            GUIManager.Instance.ValheimOrange, true, Color.black, PanelWidth - 40f, 30f, false).GetComponent<Text>();
        hint.alignment = TextAnchor.MiddleCenter;

        sailButton = CreateButton(rect, new Vector2(-265f, 32f), 230f, OnSail, out sailLabel);
        exploreButton = CreateButton(rect, new Vector2(-25f, 32f), 230f, OnExplore, out exploreLabel);
        CreateButton(rect, new Vector2(165f, 32f), 130f, OnClear, out Text clearLabel);
        clearLabel.text = Localization.instance.Localize("$whitehilt_route_clear");
        CreateButton(rect, new Vector2(305f, 32f), 130f, Close, out Text closeLabel);
        closeLabel.text = Localization.instance.Localize("$whitehilt_route_close");
        panel.SetActive(false);
    }

    private static Button CreateButton(Transform parent, Vector2 position, float width, UnityEngine.Events.UnityAction onClick, out Text label)
    {
        Vector2 bottom = new(0.5f, 0f);
        GameObject button = GUIManager.Instance.CreateButton(string.Empty, parent, bottom, bottom, position, width, 36f);
        button.GetComponent<Button>().onClick.AddListener(onClick);
        label = button.GetComponentInChildren<Text>();
        return button.GetComponent<Button>();
    }
}
