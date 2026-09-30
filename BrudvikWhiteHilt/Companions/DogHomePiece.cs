using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Companions;

/// <summary>
/// Marks a dog house, dog bed or water bowl, and tells a dog where to walk to and where to lie or stand at it.
/// </summary>
public sealed class DogHomePiece : MonoBehaviour
{
    private static readonly List<DogHomePiece> pieces = new();

    /// <summary>
    /// What the piece is.
    /// </summary>
    public DogHomeKind Kind;

    /// <summary>
    /// Where the dog lies, in the piece's local space.
    /// </summary>
    public Vector3 RestOffset;

    /// <summary>
    /// Which way the lying dog faces, in degrees around the piece's up axis.
    /// </summary>
    public float RestYaw;

    /// <summary>
    /// Where the dog walks to before it lies down, in the piece's local space.
    /// </summary>
    public Vector3 ApproachOffset;

    /// <summary>
    /// Where the dog lies, in world space.
    /// </summary>
    public Vector3 RestPosition => transform.TransformPoint(RestOffset);

    /// <summary>
    /// Which way the lying dog faces, in world space.
    /// </summary>
    public Quaternion RestRotation => transform.rotation * Quaternion.Euler(0f, RestYaw, 0f);

    /// <summary>
    /// Where the dog walks to before it lies down, in world space.
    /// </summary>
    public Vector3 ApproachPosition => transform.TransformPoint(ApproachOffset);

    /// <summary>
    /// Finds the closest placed piece of a kind within <paramref name="range"/> metres.
    /// </summary>
    /// <param name="kind">The kind of piece.</param>
    /// <param name="position">Where to search from.</param>
    /// <param name="range">Search radius in metres.</param>
    /// <param name="filter">Extra condition, or null.</param>
    /// <returns>The piece, or null.</returns>
    public static DogHomePiece FindNearest(DogHomeKind kind, Vector3 position, float range, Func<DogHomePiece, bool> filter = null)
    {
        pieces.RemoveAll(piece => piece == null);
        return pieces
            .Where(piece => piece.Kind == kind && Vector3.Distance(piece.transform.position, position) <= range && (filter == null || filter(piece)))
            .OrderBy(piece => Vector3.Distance(piece.transform.position, position))
            .FirstOrDefault();
    }

    /// <summary>
    /// True when the piece stands under a roof.
    /// </summary>
    /// <returns>Whether there is a roof above the piece.</returns>
    public bool IsUnderRoof()
    {
        Cover.GetCoverForPoint(transform.position + Vector3.up * 0.6f, out _, out bool underRoof);
        return underRoof;
    }

    private void Awake()
    {
        // The placement ghost has no ZDO and must not attract dogs.
        ZNetView nview = GetComponent<ZNetView>();
        if (nview != null && nview.GetZDO() != null)
        {
            pieces.Add(this);
        }
    }

    private void OnDestroy()
    {
        pieces.Remove(this);
    }
}

/// <summary>
/// The pieces that make a dog's home.
/// </summary>
public enum DogHomeKind
{
    /// <summary>The dog house, which also marks where home is.</summary>
    House,

    /// <summary>The dog bed, which must stand under a roof.</summary>
    Bed,

    /// <summary>The water bowl, which the dog drinks from.</summary>
    WaterBowl
}
