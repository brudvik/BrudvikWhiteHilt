using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Household;

/// <summary>
/// Phase 0 of the household (docs/plans/household.md): a goal given from the console (walk or run to a point, sit on a
/// chair, lie in a bed, follow), carried out by the owner from the <c>MonsterAI.UpdateAI</c> postfix. It proves that
/// the walking body works before the household's own work loop is written, and is replaced by it.
/// </summary>
public class HouseholdProbe : MonoBehaviour
{
    // How near the point counts as there, and how near a chair or bed they must come before sitting or lying down.
    private const float Arrived = 0.6f;
    private const float AttachReach = 1.2f;

    // How near a chair or bed counts as there when the way is blocked: furniture, the bed's own frame and the walls of a
    // small room keep a body from the exact spot, so close enough is enough.
    private const float StuckReach = 4f;

    // How often progress is checked, and how far they must have come since to count as getting on.
    private const float ProgressSeconds = 2.5f;
    private const float ProgressMetres = 0.4f;

    // A goal not reached in this many seconds is given up, so a body stuck behind a wall does not walk for ever.
    private const float GiveUpSeconds = 60f;

    // A stuck body looks for a shut door this near; walks to a spot this far before it, on its own side; opens it from
    // this near; and gives up on the door after this many seconds.
    private const float DoorSearch = 16f;
    private const float DoorStand = 1f;
    private const float DoorReach = 1.5f;
    private const float DoorSeconds = 15f;

    private readonly HashSet<Door> triedDoors = new();

    private Householder human;
    private MonsterAI ai;
    private HouseholdDoors doors;
    private Door detour;
    private Vector3 detourPoint;
    private float detourStarted;
    private Goal goal;
    private Vector3 target;
    private bool run;
    private Component seat;
    private float started;
    private float checkedAt;
    private Vector3 checkedFrom;
    private bool stuck;

    private enum Goal
    {
        None,
        Walk,
        Sit,
        Sleep,
    }

    /// <summary>What the body is doing, for the console.</summary>
    public string Status => goal == Goal.None ? "idle" : $"{goal.ToString().ToLowerInvariant()} to {target}";

    /// <summary>
    /// Walks or runs to a point.
    /// </summary>
    /// <param name="point">Where to go.</param>
    /// <param name="running">Whether to run.</param>
    public void WalkTo(Vector3 point, bool running)
    {
        Begin(Goal.Walk, point, running, null);
    }

    /// <summary>
    /// Walks to a chair and sits on it.
    /// </summary>
    /// <param name="chair">The chair.</param>
    public void SitOn(Chair chair)
    {
        Begin(Goal.Sit, chair.m_attachPoint.position, false, chair);
    }

    /// <summary>
    /// Walks to a bed and lies down in it.
    /// </summary>
    /// <param name="bed">The bed.</param>
    public void SleepIn(Bed bed)
    {
        Begin(Goal.Sleep, bed.m_spawnPoint.position, false, bed);
    }

    /// <summary>
    /// Follows a player, as a tamed creature does, or stops following with null.
    /// </summary>
    /// <param name="player">The player, or null.</param>
    public void Follow(Player player)
    {
        Stop();
        ai.SetFollowTarget(player != null ? player.gameObject : null);
    }

    /// <summary>
    /// Gives up the goal and stands up.
    /// </summary>
    public void Stop()
    {
        goal = Goal.None;
        seat = null;
        human.AttachStop();
        ai.StopMoving();
    }

    /// <summary>
    /// Moves on towards the goal. Called by the owner after the creature's AI has had its turn, so the goal wins over
    /// the AI's own wandering.
    /// </summary>
    /// <param name="dt">Seconds since the last update.</param>
    public void UpdateGoal(float dt)
    {
        if (goal == Goal.None || human.IsAttached())
        {
            return;
        }

        if (Time.time - started > GiveUpSeconds)
        {
            Jotunn.Logger.LogWarning($"Household probe: gave up {Status}.");
            Stop();
            return;
        }

        float reach = goal == Goal.Walk ? Arrived : AttachReach;
        float away = Utils.DistanceXZ(target, transform.position);
        CheckProgress();
        if (detour != null)
        {
            GoThroughDoor(dt);
            return;
        }

        bool there = away <= reach || (goal != Goal.Walk && stuck && Vector3.Distance(target, transform.position) <= StuckReach);
        if (!there)
        {
            if (stuck && TakeDoor())
            {
                return;
            }

            // The game's path finding says it is there when it finds no way at all, as for a bed on a floor its map
            // does not reach; then they steer straight at it and round what is in the way.
            if (ai.MoveTo(dt, target, reach, run))
            {
                ai.MoveAndAvoid(dt, target, reach, run);
            }

            return;
        }

        ai.StopMoving();
        if (goal == Goal.Sit && seat is Chair chair && chair != null)
        {
            human.AttachStart(chair.m_attachPoint, null, false, false, false, chair.m_attachAnimation, chair.m_detachOffset);
        }
        else if (goal == Goal.Sleep && seat is Bed bed && bed != null)
        {
            human.AttachStart(bed.m_spawnPoint, bed.gameObject, true, true, false, "attach_bed", new Vector3(0f, 0.5f, 0f));
        }

        goal = Goal.None;
    }

    private void Awake()
    {
        human = GetComponent<Householder>();
        ai = GetComponent<MonsterAI>();
        doors = GetComponent<HouseholdDoors>();
    }

    // Every few seconds: whether they came on since, or stand stuck against something.
    private void CheckProgress()
    {
        if (Time.time - checkedAt < ProgressSeconds)
        {
            return;
        }

        stuck = Vector3.Distance(transform.position, checkedFrom) < ProgressMetres;
        checkedAt = Time.time;
        checkedFrom = transform.position;
    }

    // Starts the progress check afresh, so being stuck before does not count against what comes next.
    private void ResetProgress()
    {
        checkedAt = Time.time;
        checkedFrom = transform.position;
        stuck = false;
    }

    // A stuck body may stand before a wall the game's path finding takes a shut door for. It picks the nearby shut door
    // that lies best on the way (the shortest way there and on to the goal) and not yet tried for this goal, and goes to
    // open it; the path is found afresh after.
    private bool TakeDoor()
    {
        if (doors == null)
        {
            return false;
        }

        Door best = null;
        float bestScore = float.MaxValue;
        foreach (Door door in FindObjectsByType<Door>(FindObjectsSortMode.None))
        {
            if (triedDoors.Contains(door) || !HouseholdDoors.CanOpen(door))
            {
                continue;
            }

            float near = Vector3.Distance(door.transform.position, transform.position);
            float score = near + Vector3.Distance(door.transform.position, target);
            if (near <= DoorSearch && score < bestScore)
            {
                best = door;
                bestScore = score;
            }
        }

        if (best == null)
        {
            return false;
        }

        // A door is passed along its forward axis (Door.Open swings it by that); stand before it on the body's side.
        Vector3 across = best.transform.forward;
        across.y = 0f;
        across.Normalize();
        float side = Vector3.Dot(transform.position - best.transform.position, across) >= 0f ? 1f : -1f;
        detour = best;
        detourPoint = best.transform.position + across * (side * DoorStand);
        detourStarted = Time.time;
        triedDoors.Add(best);
        ResetProgress();
        return true;
    }

    // Walks to the chosen door and opens it; gives up on it when it is opened by someone else, cannot be reached, or
    // takes too long.
    private void GoThroughDoor(float dt)
    {
        if (detour == null || !HouseholdDoors.CanOpen(detour) || Time.time - detourStarted > DoorSeconds)
        {
            EndDetour();
            return;
        }

        float near = Utils.DistanceXZ(detour.transform.position, transform.position);
        if (near <= DoorReach || (stuck && near <= DoorReach * 2f))
        {
            ai.StopMoving();
            doors.Open(detour);
            EndDetour();
            return;
        }

        if (ai.MoveTo(dt, detourPoint, Arrived, run))
        {
            ai.MoveAndAvoid(dt, detourPoint, Arrived, run);
        }
    }

    private void EndDetour()
    {
        detour = null;
        ResetProgress();
    }

    private void Begin(Goal next, Vector3 point, bool running, Component piece)
    {
        human.AttachStop();
        ai.SetFollowTarget(null);
        goal = next;
        target = point;
        run = running;
        seat = piece;
        started = Time.time;
        detour = null;
        triedDoors.Clear();
        ResetProgress();
    }
}
