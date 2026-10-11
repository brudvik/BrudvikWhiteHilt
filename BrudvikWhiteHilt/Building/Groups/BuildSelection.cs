using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Building.Groups;

/// <summary>
/// The selected pieces, shown in a light blue glow. Pieces are picked one by one, with a box between two corners, or
/// as a whole building: a piece and everything joined to it.
/// </summary>
public static class BuildSelection
{
    private const float HighlightInterval = 0.25f;
    private const float BoxDepthBelow = 1f;

    private static readonly Color glow = new(0.35f, 0.75f, 1f);
    private static readonly HashSet<Piece> selected = new();
    private static readonly List<Piece> buffer = new();
    private static readonly Collider[] overlaps = new Collider[512];

    private static float nextHighlight;
    private static int joinMask;

    /// <summary>First corner of a box being drawn, or null.</summary>
    public static Vector3? BoxStart { get; private set; }

    /// <summary>Height of the box above its lowest corner, in metres.</summary>
    public static float BoxHeight { get; private set; } = 10f;

    /// <summary>How many selected pieces still stand.</summary>
    public static int Count
    {
        get
        {
            selected.RemoveWhere(piece => piece == null);
            return selected.Count;
        }
    }

    /// <summary>
    /// The selected pieces that still stand.
    /// </summary>
    /// <returns>The pieces.</returns>
    public static List<Piece> Pieces()
    {
        selected.RemoveWhere(piece => piece == null);
        return selected.ToList();
    }

    /// <summary>
    /// Whether a piece may be selected: one the group tools can build again, and not a wild plant.
    /// </summary>
    /// <param name="piece">The piece.</param>
    /// <returns>True if it can be selected.</returns>
    public static bool CanSelect(Piece piece)
    {
        return piece != null && GroupPlacer.Resolve(Utils.GetPrefabName(piece.gameObject)) != null && !Planting.Plantables.IsWild(piece);
    }

    /// <summary>
    /// Adds a piece, or removes it if it was selected.
    /// </summary>
    /// <param name="piece">The piece.</param>
    public static void Toggle(Piece piece)
    {
        if (selected.Remove(piece))
        {
            Unhighlight(piece);
        }
        else
        {
            selected.Add(piece);
            Highlight(piece);
        }
    }

    /// <summary>
    /// Sets the first corner of a box, or selects everything in the box when the first corner is already set.
    /// </summary>
    /// <param name="corner">The clicked point.</param>
    public static void BoxClick(Vector3 corner)
    {
        if (!BoxStart.HasValue)
        {
            BoxStart = corner;
            return;
        }

        Bounds box = Box(BoxStart.Value, corner);
        BoxStart = null;
        buffer.Clear();
        Piece.GetAllPiecesInRadius(box.center, box.extents.magnitude, buffer);
        foreach (Piece piece in buffer)
        {
            if (piece != null && box.Contains(piece.transform.position) && CanSelect(piece) && selected.Add(piece))
            {
                Highlight(piece);
            }
        }

        buffer.Clear();
    }

    /// <summary>
    /// Selects a piece and every piece joined to it, directly or through other pieces: pieces whose colliders touch,
    /// or come within <see cref="GroupSettings.JoinGap"/> of each other, as the game looks for support. The ground does
    /// not join anything, and ships and carts are left out. Stops at <see cref="GroupSettings.MaxJoined"/> pieces.
    /// </summary>
    /// <param name="start">The piece clicked.</param>
    /// <param name="capped">True if the search stopped at the limit.</param>
    /// <returns>How many pieces the building has.</returns>
    public static int SelectJoined(Piece start, out bool capped)
    {
        if (joinMask == 0)
        {
            joinMask = LayerMask.GetMask("piece", "piece_nonsolid", "Default", "static_solid", "Default_small");
        }

        float gap = Mathf.Max(0f, GroupSettings.JoinGap.Value);
        int limit = GroupSettings.MaxJoined.Value;
        HashSet<Piece> found = new() { start };
        Queue<Piece> queue = new();
        queue.Enqueue(start);
        capped = false;
        while (queue.Count > 0 && !capped)
        {
            Piece piece = queue.Dequeue();
            if (piece == null)
            {
                continue;
            }

            foreach (Collider collider in piece.GetComponentsInChildren<Collider>())
            {
                if (collider.isTrigger || collider.attachedRigidbody != null)
                {
                    continue;
                }

                GrownBox(collider, gap, out Vector3 centre, out Vector3 half, out Quaternion rotation);
                int count = Physics.OverlapBoxNonAlloc(centre, half, overlaps, rotation, joinMask, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count && !capped; i++)
                {
                    Piece other = overlaps[i].attachedRigidbody == null ? overlaps[i].GetComponentInParent<Piece>() : null;
                    if (other == null || found.Contains(other) || !CanSelect(other))
                    {
                        continue;
                    }

                    if (limit > 0 && found.Count >= limit)
                    {
                        capped = true;
                        break;
                    }

                    found.Add(other);
                    queue.Enqueue(other);
                }

                if (capped)
                {
                    break;
                }
            }
        }

        foreach (Piece piece in found)
        {
            if (selected.Add(piece))
            {
                Highlight(piece);
            }
        }

        return found.Count;
    }

    /// <summary>
    /// Changes the box height.
    /// </summary>
    /// <param name="metres">Metres to add.</param>
    public static void ChangeBoxHeight(float metres)
    {
        BoxHeight = Mathf.Clamp(BoxHeight + metres, 1f, 100f);
    }

    /// <summary>
    /// Forgets the first box corner.
    /// </summary>
    public static void CancelBox()
    {
        BoxStart = null;
    }

    /// <summary>
    /// The box between the first corner and a point, as it would be selected.
    /// </summary>
    /// <param name="a">First corner.</param>
    /// <param name="b">Second corner.</param>
    /// <returns>The box.</returns>
    public static Bounds Box(Vector3 a, Vector3 b)
    {
        float bottom = Mathf.Min(a.y, b.y) - BoxDepthBelow;
        float top = Mathf.Min(a.y, b.y) + BoxHeight;
        Vector3 min = new(Mathf.Min(a.x, b.x), bottom, Mathf.Min(a.z, b.z));
        Vector3 max = new(Mathf.Max(a.x, b.x), top, Mathf.Max(a.z, b.z));
        Bounds bounds = new();
        bounds.SetMinMax(min, max);
        return bounds;
    }

    /// <summary>
    /// Clears the selection.
    /// </summary>
    public static void Clear()
    {
        foreach (Piece piece in selected)
        {
            Unhighlight(piece);
        }

        selected.Clear();
        BoxStart = null;
    }

    /// <summary>
    /// Keeps the glow on; hovering with the hammer resets it. Called every frame.
    /// </summary>
    public static void Tick()
    {
        if (selected.Count == 0 || Time.unscaledTime < nextHighlight)
        {
            return;
        }

        nextHighlight = Time.unscaledTime + HighlightInterval;
        selected.RemoveWhere(piece => piece == null);
        foreach (Piece piece in selected)
        {
            Highlight(piece);
        }
    }

    // A collider's box in the world, grown by the gap on every side: a box collider's own turned box, else its
    // bounds, as the game measures what holds a piece up.
    private static void GrownBox(Collider collider, float gap, out Vector3 centre, out Vector3 half, out Quaternion rotation)
    {
        if (collider is BoxCollider box)
        {
            Transform t = box.transform;
            Vector3 scale = t.lossyScale;
            rotation = t.rotation;
            centre = t.TransformPoint(box.center);
            half = Vector3.Scale(box.size, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z))) / 2f;
        }
        else
        {
            Bounds bounds = collider.bounds;
            rotation = Quaternion.identity;
            centre = bounds.center;
            half = bounds.extents;
        }

        half += Vector3.one * gap;
    }

    private static void Highlight(Piece piece)
    {
        if (piece != null && MaterialMan.instance != null)
        {
            MaterialMan.instance.SetValue(piece.gameObject, ShaderProps._EmissionColor, glow * 0.5f);
            MaterialMan.instance.SetValue(piece.gameObject, ShaderProps._Color, glow);
        }
    }

    private static void Unhighlight(Piece piece)
    {
        if (piece != null && MaterialMan.instance != null)
        {
            MaterialMan.instance.ResetValue(piece.gameObject, ShaderProps._EmissionColor);
            MaterialMan.instance.ResetValue(piece.gameObject, ShaderProps._Color);
        }
    }
}
