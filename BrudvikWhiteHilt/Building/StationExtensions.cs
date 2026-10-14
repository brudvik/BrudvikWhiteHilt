using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Managers;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Building;

/// <summary>
/// How a workbench extension may be placed. The game refuses one when any other extension, of any station, stands
/// within its space requirement, measured from centre to centre; the mod by default only refuses one that really
/// overlaps another (<see cref="ExtensionSpacing.Overlap"/>). How far an extension may stand from its station is a
/// setting too, the same for placing it and for counting it towards the station's level.
/// </summary>
public enum ExtensionSpacing
{
    /// <summary>Refused only where it overlaps another extension.</summary>
    Overlap,

    /// <summary>Never refused for other extensions.</summary>
    Off,

    /// <summary>As in the game: refused when another extension is within the piece's space requirement.</summary>
    Vanilla
}

/// <summary>
/// The rules for placing workbench extensions, and the ring that shows a station's reach while one is placed.
/// </summary>
public static class StationExtensions
{
    private const string Section = "StationExtensions";

    // Two extensions may touch: their bounds are shrunk by this before they are compared.
    private const float Tolerance = 0.05f;

    // Stations up to this many times the reach away get the ring, so it shows before the extension is in reach.
    private const float RingSearch = 3f;

    private static GameObject ring;
    private static CircleProjector ringCircle;

    /// <summary>How other extensions limit where one may be placed.</summary>
    public static ConfigEntry<ExtensionSpacing> Spacing { get; private set; }

    /// <summary>How far an extension may stand from its station, in metres; 0 for each extension's own distance.</summary>
    public static ConfigEntry<float> Reach { get; private set; }

    /// <summary>Whether the station's reach shows as a ring while an extension is placed.</summary>
    public static ConfigEntry<bool> ShowReachRing { get; private set; }

    /// <summary>
    /// Binds the settings. Call once from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Spacing = WhiteHiltConfig.BindAdminOnly(Section, "Spacing", ExtensionSpacing.Overlap,
            "Overlap (default): a workbench extension is refused only where it overlaps another extension. Off: never refused " +
            "for other extensions. Vanilla: as in the game, refused when any other extension stands within its space requirement.");
        Reach = WhiteHiltConfig.BindAdminOnly(Section, "Reach", 10f,
            "How far an extension may stand from its station, in metres, for placing it and for the station's level " +
            "(5 m in the game). 0: each extension's own distance from the game.", new AcceptableValueRange<float>(0f, 50f));
        ShowReachRing = WhiteHiltConfig.BindLocal(Section, "ShowReachRing", true,
            "Show the station's reach as a ring on the ground while you place one of its extensions.");
        Reach.SettingChanged += (_, _) => ApplyToAll();
    }

    /// <summary>
    /// Gives an extension the configured reach. Called when it comes to life; the prefab keeps the game's value.
    /// </summary>
    /// <param name="extension">The extension.</param>
    public static void Apply(StationExtension extension)
    {
        if (extension != null)
        {
            extension.m_maxStationDistance = ReachOf(extension);
        }
    }

    /// <summary>
    /// Whether another extension is in the way of one being placed, by the configured rule.
    /// </summary>
    /// <param name="extension">The extension being placed.</param>
    /// <param name="radius">The piece's space requirement, which the game measures from centre to centre.</param>
    /// <returns>True when it must be refused.</returns>
    public static bool InTheWay(StationExtension extension, float radius)
    {
        switch (Spacing.Value)
        {
            case ExtensionSpacing.Off:
                return false;
            case ExtensionSpacing.Vanilla:
                return StationExtension.m_allExtensions.Any(other => other != null && other != extension
                    && Vector3.Distance(other.transform.position, extension.transform.position) < radius);
        }

        Bounds? own = BoundsOf(extension.gameObject);
        if (own == null)
        {
            return false;
        }

        Bounds shrunk = Shrink(own.Value);
        foreach (StationExtension other in StationExtension.m_allExtensions)
        {
            if (other == null || other == extension || Vector3.Distance(other.transform.position, extension.transform.position) > radius + 20f)
            {
                continue;
            }

            Bounds? theirs = BoundsOf(other.gameObject);
            if (theirs != null && shrunk.Intersects(Shrink(theirs.Value)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Shows the reach of the station nearest an extension being placed, or hides the ring. Call every frame.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void UpdateRing(Player player)
    {
        StationExtension extension = player != null && player.m_placementGhost != null ? player.m_placementGhost.GetComponent<StationExtension>() : null;
        CraftingStation station = null;
        float reach = 0f;
        if (extension != null && extension.m_craftingStation != null && ShowReachRing.Value && !VisualHelper.IsHeadless)
        {
            reach = ReachOf(extension);
            station = CraftingStation.FindClosestStationInRange(extension.m_craftingStation.m_name, extension.transform.position, reach * RingSearch);
        }

        if (station == null)
        {
            if (ring != null && ring.activeSelf)
            {
                ring.SetActive(false);
            }

            return;
        }

        if (ring == null && !CreateRing())
        {
            return;
        }

        ring.transform.position = station.transform.position;
        ringCircle.m_radius = reach;
        if (!ring.activeSelf)
        {
            ring.SetActive(true);
        }
    }

    private static float ReachOf(StationExtension extension)
    {
        if (Reach.Value > 0f)
        {
            return Reach.Value;
        }

        GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(Utils.GetPrefabName(extension.gameObject)) : null;
        StationExtension original = prefab != null ? prefab.GetComponent<StationExtension>() : null;
        return original != null ? original.m_maxStationDistance : extension.m_maxStationDistance;
    }

    private static void ApplyToAll()
    {
        foreach (StationExtension extension in StationExtension.m_allExtensions)
        {
            Apply(extension);
        }
    }

    // The space a piece takes: its solid colliders, or its renderers when it has none switched on (a placement ghost
    // may have its colliders off).
    private static Bounds? BoundsOf(GameObject piece)
    {
        Bounds? bounds = null;
        foreach (Collider collider in piece.GetComponentsInChildren<Collider>().Where(collider => collider.enabled && !collider.isTrigger))
        {
            bounds = Grow(bounds, collider.bounds);
        }

        if (bounds != null)
        {
            return bounds;
        }

        foreach (Renderer renderer in piece.GetComponentsInChildren<Renderer>().Where(renderer => renderer.enabled && renderer is MeshRenderer or SkinnedMeshRenderer))
        {
            bounds = Grow(bounds, renderer.bounds);
        }

        return bounds;
    }

    private static Bounds Grow(Bounds? bounds, Bounds add)
    {
        if (bounds == null)
        {
            return add;
        }

        Bounds grown = bounds.Value;
        grown.Encapsulate(add);
        return grown;
    }

    private static Bounds Shrink(Bounds bounds)
    {
        bounds.Expand(-2f * Tolerance);
        return bounds;
    }

    // The ring is a copy of the workbench's own area marker, kept for the session.
    private static bool CreateRing()
    {
        CraftingStation workbench = PrefabManager.Instance.GetPrefab("piece_workbench")?.GetComponent<CraftingStation>();
        if (workbench == null || workbench.m_areaMarker == null)
        {
            return false;
        }

        ring = Object.Instantiate(workbench.m_areaMarker);
        ring.name = "whitehilt_extension_reach";
        Object.DontDestroyOnLoad(ring);
        ringCircle = ring.GetComponent<CircleProjector>();
        if (ringCircle == null)
        {
            Object.Destroy(ring);
            ring = null;
            return false;
        }

        return true;
    }
}
