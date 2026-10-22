using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Household;

/// <summary>
/// A built High Seat: the master's place in the hall and the heart of a farm. Everything within
/// <see cref="HouseholdSettings.FarmRadius"/> of it is the farm, where its household lives, sleeps and (in later phases)
/// works; no other High Seat may stand within it. The use key sits the player down in it and opens the household panel;
/// with the alternative key the panel opens without sitting. Its reach shows as a ring while it is looked at or placed.
/// </summary>
public class HighSeat : MonoBehaviour, Hoverable, Interactable
{
    /// <summary>Name of the child that draws the farm's reach.</summary>
    public const string AreaMarkerName = "HighSeatArea";

    /// <summary>Name of the child the player sits at.</summary>
    public const string SitPointName = "SitPoint";

    // Where a member stands when sent home: before the steps, so they do not climb the seat.
    private static readonly Vector3 homeOffset = new(0f, 0f, 2f);

    // Where the player steps off to, from the place to sit: before the seat, on the steps.
    private static readonly Vector3 standUpOffset = new(0f, 0.3f, 0.9f);

    private const float HoverSeconds = 0.25f;
    private const float UseDistance = 3f;

    private static readonly List<HighSeat> loaded = new();

    private ZNetView view;
    private Piece piece;
    private Transform sitPoint;
    private GameObject areaMarker;
    private CircleProjector circle;
    private float hoveredAt = float.NegativeInfinity;

    /// <summary>The High Seats loaded on this machine, placement ghosts included (see <see cref="IsBuilt"/>).</summary>
    public static IReadOnlyList<HighSeat> Loaded => loaded;

    /// <summary>Whether this is a built High Seat, not the ghost of one being placed.</summary>
    public bool IsBuilt => view != null && view.IsValid();

    /// <summary>The High Seat's own number, which its members keep as their farm; 0 until its owner has given it one.</summary>
    public long Id => IsBuilt ? view.GetZDO().GetLong(HouseholdKeys.SeatId) : 0L;

    /// <summary>Where the household gathers when sent home: before the steps.</summary>
    public Vector3 HomePoint => transform.TransformPoint(homeOffset);

    /// <summary>The seat's name, as the player sees it.</summary>
    public string DisplayName => piece != null ? Localization.instance.Localize(piece.m_name) : string.Empty;

    /// <summary>
    /// The built High Seat whose farm a point lies on, or null.
    /// </summary>
    /// <param name="point">The point.</param>
    /// <returns>The nearest such High Seat.</returns>
    public static HighSeat FarmAt(Vector3 point)
    {
        float radius = HouseholdSettings.FarmRadius.Value;
        return loaded.Where(seat => seat != null && seat.IsBuilt && Vector3.Distance(seat.transform.position, point) <= radius)
            .OrderBy(seat => Vector3.Distance(seat.transform.position, point))
            .FirstOrDefault();
    }

    /// <summary>
    /// The loaded High Seat with this number, or null.
    /// </summary>
    /// <param name="id">The number.</param>
    /// <returns>The High Seat.</returns>
    public static HighSeat Find(long id)
    {
        return id == 0L ? null : loaded.FirstOrDefault(seat => seat != null && seat.IsBuilt && seat.Id == id);
    }

    /// <summary>
    /// Shows the reach on a High Seat being placed, and says why it cannot stand there. Call every frame for the
    /// placement ghost.
    /// </summary>
    /// <param name="ghost">The placement ghost.</param>
    /// <returns>Why the High Seat cannot be placed there (localized), or null.</returns>
    public static string CheckGhost(GameObject ghost)
    {
        HighSeat seat = ghost.GetComponent<HighSeat>();
        if (seat == null)
        {
            return null;
        }

        seat.ShowArea(true);
        return FarmAt(ghost.transform.position) != null ? Localization.instance.Localize("$whitehilt_hs_too_near") : null;
    }

    /// <summary>
    /// The household of this farm that is loaded on this machine.
    /// </summary>
    /// <returns>The members, by name.</returns>
    public List<HouseholdMember> Members()
    {
        long id = Id;
        return Householder.All.Select(human => human.Member)
            .Where(member => member != null && id != 0L && member.FarmId == id)
            .OrderBy(member => member.MemberName)
            .ToList();
    }

    /// <summary>
    /// The beds on the farm a member may have: no player sleeps in them, and no other member of this farm has them.
    /// </summary>
    /// <param name="member">The member who looks for a bed.</param>
    /// <returns>The beds, nearest the seat first.</returns>
    public List<Bed> FreeBeds(HouseholdMember member)
    {
        List<HouseholdMember> others = Members().Where(other => other != member && other.HasBed).ToList();
        float radius = HouseholdSettings.FarmRadius.Value;
        return FindObjectsByType<Bed>(FindObjectsSortMode.None)
            .Where(bed => bed.m_nview != null && bed.m_nview.IsValid() && bed.GetOwner() == 0L
                          && !others.Any(other => other.Sleeps(bed))
                          && Vector3.Distance(bed.transform.position, transform.position) <= radius)
            .OrderBy(bed => Vector3.Distance(bed.transform.position, transform.position))
            .ToList();
    }

    /// <inheritdoc/>
    public string GetHoverText()
    {
        hoveredAt = Time.time;
        if (!IsBuilt)
        {
            return string.Empty;
        }

        int count = Members().Count;
        return Localization.instance.Localize(
            $"{DisplayName}\n<color=#a0a0a0>$whitehilt_hs_count</color>"
            + "\n[<color=yellow><b>$KEY_Use</b></color>] $whitehilt_hs_sit"
            + "\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $whitehilt_hs_open",
            count.ToString(), HouseholdSettings.MaxPerFarm.Value.ToString());
    }

    /// <inheritdoc/>
    public string GetHoverName() => DisplayName;

    /// <inheritdoc/>
    public float GetHoverOffset() => 0f;

    /// <inheritdoc/>
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || !IsBuilt || user is not Player player || Vector3.Distance(player.transform.position, transform.position) > UseDistance + 2f)
        {
            return false;
        }

        if (!alt && sitPoint != null && !player.IsAttached())
        {
            if (Player.GetClosestPlayer(sitPoint.position, 0.1f) != null)
            {
                player.Message(MessageHud.MessageType.Center, "$msg_blocked");
                return false;
            }

            player.AttachStart(sitPoint, null, false, false, false, "attach_chair", standUpOffset);
        }

        HouseholdPanel.Open(this);
        return true;
    }

    /// <inheritdoc/>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

    private void Awake()
    {
        view = GetComponent<ZNetView>();
        piece = GetComponent<Piece>();
        sitPoint = transform.Find(SitPointName);
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

    // A built seat's owner gives it its number the first time it is loaded.
    private void Start()
    {
        if (IsBuilt && view.IsOwner() && Id == 0L)
        {
            long id = 0L;
            while (id == 0L)
            {
                id = ((long)Random.Range(int.MinValue, int.MaxValue) << 32) | (uint)Random.Range(int.MinValue, int.MaxValue);
            }

            view.GetZDO().Set(HouseholdKeys.SeatId, id);
        }
    }

    private void OnDisable()
    {
        loaded.Remove(this);
    }

    // A ghost's ring is shown by CheckGhost; a built High Seat shows it while looked at.
    private void Update()
    {
        if (IsBuilt)
        {
            ShowArea(Time.time - hoveredAt < HoverSeconds);
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
            circle.m_radius = HouseholdSettings.FarmRadius.Value;
        }

        if (areaMarker.activeSelf != show)
        {
            areaMarker.SetActive(show);
        }
    }
}
