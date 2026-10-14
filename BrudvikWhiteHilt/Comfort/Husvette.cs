using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Comfort;

/// <summary>
/// A built Husvette: the house spirit that binds a room. Every machine knows the Husvettes it has loaded, so the comfort
/// count (<see cref="ComfortService"/>) can add the pieces within their reach; on the local player's machine it also
/// keeps the player warm enough to rest when a fire burns within its reach, and shows its reach as a ring on the ground
/// while it is looked at or placed.
/// </summary>
public class Husvette : MonoBehaviour, Hoverable
{
    /// <summary>Name of the child that draws the reach.</summary>
    public const string AreaMarkerName = "HusvetteArea";

    private const float WarmthInterval = 0.5f;
    private const float FireListSeconds = 5f;
    private const float HoverSeconds = 0.25f;

    private static readonly List<Husvette> loaded = new();

    private ZNetView view;
    private GameObject areaMarker;
    private CircleProjector circle;
    private Piece piece;
    private float nextWarmth;
    private float firesListedAt = float.NegativeInfinity;
    private List<Fireplace> fires = new();
    private float hoveredAt = float.NegativeInfinity;

    /// <summary>The Husvettes loaded on this machine, placement ghosts included (see <see cref="IsBuilt"/>).</summary>
    public static IReadOnlyList<Husvette> Loaded => loaded;

    /// <summary>Whether this is a built Husvette, not the ghost of one being placed.</summary>
    public bool IsBuilt => view != null && view.IsValid();

    /// <summary>
    /// Shows the reach on a Husvette being placed. Call every frame for the placement ghost.
    /// </summary>
    /// <param name="ghost">The placement ghost.</param>
    public static void ShowOnGhost(GameObject ghost)
    {
        Husvette husvette = ghost.GetComponent<Husvette>();
        if (husvette != null)
        {
            husvette.ShowArea(true);
        }
    }

    /// <inheritdoc/>
    public string GetHoverText()
    {
        hoveredAt = Time.time;
        string name = piece != null ? Localization.instance.Localize(piece.m_name) : string.Empty;
        return name + "\n" + Localization.instance.Localize("$whitehilt_husvette_hover", Mathf.RoundToInt(ComfortSettings.HusvetteRadius.Value).ToString());
    }

    /// <inheritdoc/>
    public float GetHoverOffset() => 0f;

    /// <inheritdoc/>
    public string GetHoverName()
    {
        return piece != null ? Localization.instance.Localize(piece.m_name) : string.Empty;
    }

    private void Awake()
    {
        piece = GetComponent<Piece>();
        view = GetComponent<ZNetView>();
        Transform marker = transform.Find(AreaMarkerName);
        if (marker != null)
        {
            areaMarker = marker.gameObject;
            circle = areaMarker.GetComponent<CircleProjector>();
            areaMarker.SetActive(false);
        }
    }

    private void OnEnable()
    {
        loaded.Add(this);
    }

    private void OnDisable()
    {
        loaded.Remove(this);
    }

    // A ghost's ring is shown by ShowOnGhost; a built Husvette shows it while looked at.
    private void Update()
    {
        if (!IsBuilt)
        {
            return;
        }

        ShowArea(Time.time - hoveredAt < HoverSeconds);
        if (Time.time >= nextWarmth)
        {
            nextWarmth = Time.time + WarmthInterval;
            KeepWarm();
        }
    }

    // On the local player's machine: when the player stands under a roof within reach and a fire burns within reach, the
    // player counts as near a fire, as the fire's own warmth would make them. The game then lets them rest.
    private void KeepWarm()
    {
        Player player = Player.m_localPlayer;
        float radius = ComfortSettings.HusvetteRadius.Value;
        if (!ComfortSettings.Enabled.Value || !ComfortSettings.HusvetteWarmth.Value || player == null
            || Vector3.Distance(player.transform.position, transform.position) > radius || !player.InShelter())
        {
            return;
        }

        if (Time.time - firesListedAt > FireListSeconds)
        {
            firesListedAt = Time.time;
            fires = FindObjectsByType<Fireplace>(FindObjectsSortMode.None)
                .Where(fire => Vector3.Distance(fire.transform.position, transform.position) <= radius)
                .ToList();
        }

        Fireplace burning = fires.FirstOrDefault(fire => fire != null && fire.IsBurning());
        if (burning != null)
        {
            player.OnNearFire(burning.transform.position);
        }
    }

    private void ShowArea(bool show)
    {
        if (areaMarker == null)
        {
            return;
        }

        if (show && circle != null)
        {
            circle.m_radius = ComfortSettings.HusvetteRadius.Value;
        }

        if (areaMarker.activeSelf != show)
        {
            areaMarker.SetActive(show);
        }
    }
}
