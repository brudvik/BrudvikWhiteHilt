using BrudvikWhiteHilt.Building.Doors;
using UnityEngine;

namespace BrudvikWhiteHilt.Household;

/// <summary>
/// Lets a member of the household through doors, which the game's creatures cannot open: on the owner's machine, a
/// closed door just ahead of a member on the move is opened towards where they walk. The mod's auto-door closes it again
/// after them; where it does not look after the door (turned off, or not a door it closes), the member shuts it behind
/// them. Doors that need a key stay shut.
/// </summary>
public class HouseholdDoors : MonoBehaviour
{
    private const float TickSeconds = 0.25f;
    private const float Ahead = 0.9f;
    private const float Reach = 1.1f;
    private const float Wanting = 0.3f;
    private const float CloseBehind = 2.5f;

    private Householder human;
    private int mask;
    private float nextTick;
    private Door opened;

    private void Awake()
    {
        human = GetComponent<Householder>();
        mask = LayerMask.GetMask("piece", "piece_nonsolid", "Default_small");
    }

    private void Update()
    {
        if (Time.time < nextTick || human == null || human.View == null || !human.View.IsValid() || !human.View.IsOwner())
        {
            return;
        }

        nextTick = Time.time + TickSeconds;
        ShutBehind();
        // Where the AI wants to go, not where the body moves: a member pressed against a shut door does not move.
        Vector3 heading = human.m_moveDir;
        heading.y = 0f;
        if (heading.magnitude < Wanting || human.IsAttached())
        {
            return;
        }

        heading.Normalize();
        Vector3 probe = transform.position + Vector3.up + heading * Ahead;
        foreach (Collider collider in Physics.OverlapSphere(probe, Reach, mask))
        {
            Door door = collider.GetComponentInParent<Door>();
            if (door != null && Open(door))
            {
                return;
            }
        }
    }

    /// <summary>
    /// Whether a member may open this door: it is shut and needs no key.
    /// </summary>
    /// <param name="door">The door.</param>
    /// <returns>True when it can be opened.</returns>
    public static bool CanOpen(Door door)
    {
        ZNetView view = door != null ? door.m_nview : null;
        return view != null && view.IsValid() && door.m_keyItem == null && view.GetZDO().GetInt(ZDOVars.s_state) == 0;
    }

    /// <summary>
    /// Opens a closed door away from the member, as the game does for a player standing where the member stands, and
    /// shuts it behind them later where the auto-door does not.
    /// </summary>
    /// <param name="door">The door.</param>
    /// <returns>True when it was opened.</returns>
    public bool Open(Door door)
    {
        if (!CanOpen(door))
        {
            return false;
        }

        door.Open((transform.position - door.transform.position).normalized);
        AutoDoor auto = door.GetComponent<AutoDoor>();
        opened = auto != null && auto.IsManaged && AutoDoorSettings.Enabled.Value ? null : door;
        return true;
    }

    // A door the auto-door does not close is shut once the member is through and away from it.
    private void ShutBehind()
    {
        if (opened == null)
        {
            return;
        }

        ZNetView view = opened.m_nview;
        if (view == null || !view.IsValid())
        {
            opened = null;
            return;
        }

        if (Vector3.Distance(transform.position, opened.transform.position) > CloseBehind)
        {
            if (view.GetZDO().GetInt(ZDOVars.s_state) != 0)
            {
                opened.Open((transform.position - opened.transform.position).normalized);
            }

            opened = null;
        }
    }
}
