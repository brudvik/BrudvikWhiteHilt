using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles;

/// <summary>
/// Finds places to stand on a ship's deck. The raycasts only run when a crew is spawned or a warrior leaps, never every
/// frame. What is done with the hits (picking posts that spread over the deck, the nearest point, the ring around the
/// mast) takes plain vectors, so it is tested without the game.
/// </summary>
public static class RaiderDeck
{
    // A grid over the float collider, this far apart, probed from this high above the ship's own up axis.
    private const float GridStep = 1.2f;
    private const float RayHeight = 5f;
    private const float RayLength = 12f;

    // Nobody stands this close to the mast, the helm, the holdfasts or the hold.
    private const float KeepClear = 1f;

    // A deck is level: the hit's normal within about 45 degrees of the ship's up, and not up on the mast or a roof.
    private const float MinUp = 0.7f;
    private const float MaxHeight = 3.5f;

    private static readonly RaycastHit[] hits = new RaycastHit[16];
    private static int deckMask;

    /// <summary>
    /// Standing places on the deck, spread over it.
    /// </summary>
    /// <param name="ship">The ship.</param>
    /// <param name="count">How many places are wanted.</param>
    /// <returns>Ship-local points on the deck, fewer when the deck has no room, in a fixed order.</returns>
    public static List<Vector3> Posts(Ship ship, int count)
    {
        List<Vector3> deck = DeckPoints(ship);
        return Spread(RemoveNear(deck, KeepClearPoints(ship), KeepClear), count);
    }

    /// <summary>
    /// The point on a ship's deck closest to a world point, for a leap.
    /// </summary>
    /// <param name="ship">The ship to land on.</param>
    /// <param name="near">Where the leap comes from.</param>
    /// <param name="world">The deck point in world space.</param>
    /// <returns>False when no deck was found.</returns>
    public static bool TryDeckPoint(Ship ship, Vector3 near, out Vector3 world)
    {
        world = near;
        List<Vector3> deck = DeckPoints(ship);
        if (deck.Count == 0)
        {
            return false;
        }

        world = ship.transform.TransformPoint(deck[Nearest(deck, ship.transform.InverseTransformPoint(near))]);
        return true;
    }

    /// <summary>
    /// Takes the points that are at least a clearance away (across the deck, not in height) from every obstacle.
    /// </summary>
    /// <param name="points">Candidate points.</param>
    /// <param name="obstacles">Points to keep clear of.</param>
    /// <param name="clearance">Metres.</param>
    /// <returns>The points that remain, in their order.</returns>
    public static List<Vector3> RemoveNear(IReadOnlyList<Vector3> points, IReadOnlyList<Vector3> obstacles, float clearance)
    {
        List<Vector3> kept = new();
        float limit = clearance * clearance;
        foreach (Vector3 point in points)
        {
            bool clear = true;
            foreach (Vector3 obstacle in obstacles)
            {
                if (FlatDistanceSquared(point, obstacle) < limit)
                {
                    clear = false;
                    break;
                }
            }

            if (clear)
            {
                kept.Add(point);
            }
        }

        return kept;
    }

    /// <summary>
    /// Picks points that lie far from each other: the one farthest forward first, then each time the point farthest
    /// from all picked so far. Deterministic, so a ship is always manned the same way.
    /// </summary>
    /// <param name="points">Candidate points, ship-local.</param>
    /// <param name="count">How many to pick.</param>
    /// <returns>The picks in the order they were made; all the points when there are not more than wanted.</returns>
    public static List<Vector3> Spread(IReadOnlyList<Vector3> points, int count)
    {
        if (points == null || count <= 0)
        {
            return new List<Vector3>();
        }

        if (count >= points.Count)
        {
            return new List<Vector3>(points);
        }

        int first = 0;
        for (int i = 1; i < points.Count; i++)
        {
            if (points[i].z > points[first].z || (points[i].z == points[first].z && points[i].x < points[first].x))
            {
                first = i;
            }
        }

        List<Vector3> picked = new(count) { points[first] };
        float[] nearest = new float[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            nearest[i] = FlatDistanceSquared(points[i], points[first]);
        }

        while (picked.Count < count)
        {
            int best = 0;
            for (int i = 1; i < points.Count; i++)
            {
                if (nearest[i] > nearest[best])
                {
                    best = i;
                }
            }

            picked.Add(points[best]);
            for (int i = 0; i < points.Count; i++)
            {
                nearest[i] = Mathf.Min(nearest[i], FlatDistanceSquared(points[i], points[best]));
            }
        }

        return picked;
    }

    /// <summary>
    /// Index of the point closest to a target in three dimensions.
    /// </summary>
    /// <param name="points">The points; must not be empty.</param>
    /// <param name="target">The point to be close to.</param>
    /// <returns>The index; the first of equally close ones.</returns>
    public static int Nearest(IReadOnlyList<Vector3> points, Vector3 target)
    {
        int best = 0;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < points.Count; i++)
        {
            float distance = (points[i] - target).sqrMagnitude;
            if (distance < bestDistance)
            {
                best = i;
                bestDistance = distance;
            }
        }

        return best;
    }

    /// <summary>
    /// The posts farthest from the middle of the deck (greatest sideways distance), where archers stand along the rail.
    /// </summary>
    /// <param name="posts">Ship-local posts.</param>
    /// <param name="count">How many.</param>
    /// <returns>Their indexes; the lower index wins a tie.</returns>
    public static List<int> OuterIndexes(IReadOnlyList<Vector3> posts, int count)
    {
        List<int> order = new();
        for (int i = 0; i < posts.Count; i++)
        {
            order.Add(i);
        }

        order.Sort((a, b) =>
        {
            int byWidth = Mathf.Abs(posts[b].x).CompareTo(Mathf.Abs(posts[a].x));
            return byWidth != 0 ? byWidth : a.CompareTo(b);
        });
        return order.GetRange(0, Mathf.Clamp(count, 0, order.Count));
    }

    /// <summary>
    /// Whether a point lies over a box on the deck, with some margin round it.
    /// </summary>
    /// <param name="local">The point in the box's own space.</param>
    /// <param name="centre">The box centre in that space.</param>
    /// <param name="size">The box size.</param>
    /// <param name="margin">Metres of room round the box.</param>
    /// <returns>True if the point is within the margin of the box, across and along it.</returns>
    public static bool InsideDeck(Vector3 local, Vector3 centre, Vector3 size, float margin)
    {
        return Mathf.Abs(local.x - centre.x) <= size.x / 2f + margin && Mathf.Abs(local.z - centre.z) <= size.z / 2f + margin;
    }

    // Ship-local points on the ship's own colliders with an upward-facing surface: probes a grid over the float
    // collider from above. Runs on the machine that spawns the crew or leaps, once per spawn or leap.
    private static List<Vector3> DeckPoints(Ship ship)
    {
        List<Vector3> deck = new();
        BoxCollider box = ship != null ? ship.m_floatCollider : null;
        Rigidbody body = ship != null ? ship.GetComponent<Rigidbody>() : null;
        if (box == null || body == null)
        {
            return deck;
        }

        if (deckMask == 0)
        {
            deckMask = LayerMask.GetMask("vehicle", "piece", "static_solid", "Default", "Default_small");
        }

        Transform root = ship.transform;
        Vector3 size = box.size;
        for (float x = -size.x / 2f + GridStep / 2f; x < size.x / 2f; x += GridStep)
        {
            for (float z = -size.z / 2f + GridStep / 2f; z < size.z / 2f; z += GridStep)
            {
                Vector3 local = root.InverseTransformPoint(box.transform.TransformPoint(box.center + new Vector3(x, 0f, z)));
                Vector3 origin = root.TransformPoint(new Vector3(local.x, RayHeight, local.z));
                if (TryHitDeck(root, body, origin, out Vector3 point))
                {
                    deck.Add(point);
                }
            }
        }

        return deck;
    }

    // The nearest hit on the ship's own body that faces up and is not high above the deck, in ship-local space.
    private static bool TryHitDeck(Transform root, Rigidbody body, Vector3 origin, out Vector3 local)
    {
        local = Vector3.zero;
        int count = Physics.RaycastNonAlloc(origin, -root.up, hits, RayLength, deckMask, QueryTriggerInteraction.Ignore);
        float nearest = float.MaxValue;
        bool found = false;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider.attachedRigidbody != body || hit.distance >= nearest || Vector3.Dot(hit.normal, root.up) < MinUp)
            {
                continue;
            }

            Vector3 point = root.InverseTransformPoint(hit.point);
            if (point.y <= MaxHeight)
            {
                local = point;
                nearest = hit.distance;
                found = true;
            }
        }

        return found;
    }

    // The mast, the helm, the holdfasts and the hold, in ship-local space.
    private static List<Vector3> KeepClearPoints(Ship ship)
    {
        List<Vector3> points = new();
        Transform root = ship.transform;
        if (ship.m_mastObject != null)
        {
            points.Add(root.InverseTransformPoint(ship.m_mastObject.transform.position));
        }

        foreach (Component part in ship.GetComponentsInChildren<ShipControlls>(true))
        {
            points.Add(root.InverseTransformPoint(part.transform.position));
        }

        foreach (Component part in ship.GetComponentsInChildren<Chair>(true))
        {
            points.Add(root.InverseTransformPoint(part.transform.position));
        }

        foreach (Component part in ship.GetComponentsInChildren<Container>(true))
        {
            points.Add(root.InverseTransformPoint(part.transform.position));
        }

        return points;
    }

    private static float FlatDistanceSquared(Vector3 a, Vector3 b)
    {
        float x = a.x - b.x;
        float z = a.z - b.z;
        return x * x + z * z;
    }
}
