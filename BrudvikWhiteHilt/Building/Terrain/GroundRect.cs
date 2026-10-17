using UnityEngine;

namespace BrudvikWhiteHilt.Building.Terrain;

/// <summary>
/// A rectangle on the ground for the hoe's area tools: a corner, a heading and a size along its own axes. A heading of
/// 0 lies along the world's axes, as the tools always did; any other turns the rectangle round its corner, so an area
/// can follow a wall or the way the camera looks.
/// </summary>
public readonly struct GroundRect
{
    /// <summary>
    /// Creates a rectangle.
    /// </summary>
    /// <param name="corner">The corner it starts from; its height is where the first corner was clicked.</param>
    /// <param name="yaw">Heading in degrees, clockwise seen from above, as Unity's.</param>
    /// <param name="size">Size along the rectangle's right and forward axes; negative goes the other way.</param>
    public GroundRect(Vector3 corner, float yaw, Vector2 size)
    {
        Corner = corner;
        Yaw = yaw;
        Size = size;
    }

    /// <summary>The corner it starts from.</summary>
    public Vector3 Corner { get; }

    /// <summary>Heading in degrees.</summary>
    public float Yaw { get; }

    /// <summary>Size along its own right and forward axes, with sign.</summary>
    public Vector2 Size { get; }

    /// <summary>Width and length in metres.</summary>
    public Vector2 Extent => new(Mathf.Abs(Size.x), Mathf.Abs(Size.y));

    /// <summary>Square metres.</summary>
    public float Area => Extent.x * Extent.y;

    /// <summary>The rectangle's right axis on the ground.</summary>
    public Vector3 Right => new(Mathf.Cos(Yaw * Mathf.Deg2Rad), 0f, -Mathf.Sin(Yaw * Mathf.Deg2Rad));

    /// <summary>The rectangle's forward axis on the ground.</summary>
    public Vector3 Forward => new(Mathf.Sin(Yaw * Mathf.Deg2Rad), 0f, Mathf.Cos(Yaw * Mathf.Deg2Rad));

    /// <summary>
    /// The rectangle from a corner to an aimed point, its size rounded to whole metres along its own axes.
    /// </summary>
    /// <param name="corner">The first corner.</param>
    /// <param name="yaw">Heading in degrees.</param>
    /// <param name="aim">The aimed point.</param>
    /// <returns>The rectangle.</returns>
    public static GroundRect Towards(Vector3 corner, float yaw, Vector3 aim)
    {
        GroundRect turned = new(corner, yaw, Vector2.zero);
        Vector2 local = turned.ToLocal(aim.x, aim.z);
        return new GroundRect(corner, yaw, new Vector2(Mathf.Round(local.x), Mathf.Round(local.y)));
    }

    /// <summary>
    /// A rectangle of a fixed size from a corner, on the side of the aimed point.
    /// </summary>
    /// <param name="corner">The first corner.</param>
    /// <param name="yaw">Heading in degrees.</param>
    /// <param name="aim">The aimed point.</param>
    /// <param name="width">Width in metres.</param>
    /// <param name="length">Length in metres.</param>
    /// <returns>The rectangle.</returns>
    public static GroundRect Fixed(Vector3 corner, float yaw, Vector3 aim, float width, float length)
    {
        Vector2 local = new GroundRect(corner, yaw, Vector2.zero).ToLocal(aim.x, aim.z);
        return new GroundRect(corner, yaw, new Vector2(local.x >= 0f ? width : -width, local.y >= 0f ? length : -length));
    }

    /// <summary>
    /// A point in the rectangle's own axes, measured from its corner.
    /// </summary>
    /// <param name="x">World x.</param>
    /// <param name="z">World z.</param>
    /// <returns>Along its right axis and along its forward axis.</returns>
    public Vector2 ToLocal(float x, float z)
    {
        Vector3 right = Right;
        Vector3 forward = Forward;
        float dx = x - Corner.x;
        float dz = z - Corner.z;
        return new Vector2(dx * right.x + dz * right.z, dx * forward.x + dz * forward.z);
    }

    /// <summary>
    /// How far a point lies outside the rectangle, 0 inside or on its edge.
    /// </summary>
    /// <param name="x">World x.</param>
    /// <param name="z">World z.</param>
    /// <returns>The distance in metres.</returns>
    public float OutsideDistance(float x, float z)
    {
        Vector2 local = ToLocal(x, z);
        float dx = Mathf.Max(Mathf.Min(0f, Size.x) - local.x, 0f, local.x - Mathf.Max(0f, Size.x));
        float dz = Mathf.Max(Mathf.Min(0f, Size.y) - local.y, 0f, local.y - Mathf.Max(0f, Size.y));
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    /// <summary>
    /// The four corners on the ground, in order round the edge.
    /// </summary>
    /// <param name="height">Height to put them at.</param>
    /// <returns>The corners.</returns>
    public Vector3[] Corners(float height)
    {
        Vector3 start = new(Corner.x, height, Corner.z);
        Vector3 along = Right * Size.x;
        Vector3 across = Forward * Size.y;
        return new[] { start, start + along, start + along + across, start + across };
    }

    /// <summary>
    /// The smallest world-aligned box round the rectangle and a margin, on the ground.
    /// </summary>
    /// <param name="margin">Metres added on every side.</param>
    /// <param name="min">Lowest x and z.</param>
    /// <param name="max">Highest x and z.</param>
    public void Bounds(float margin, out Vector2 min, out Vector2 max)
    {
        min = new Vector2(float.MaxValue, float.MaxValue);
        max = new Vector2(float.MinValue, float.MinValue);
        foreach (Vector3 corner in Corners(0f))
        {
            min = Vector2.Min(min, new Vector2(corner.x, corner.z));
            max = Vector2.Max(max, new Vector2(corner.x, corner.z));
        }

        min -= Vector2.one * margin;
        max += Vector2.one * margin;
    }

    /// <summary>
    /// The rectangle of the same size and heading right next to this one, on the side the direction points to most.
    /// </summary>
    /// <param name="direction">Which way to go, e.g. where the camera looks.</param>
    /// <returns>The next rectangle.</returns>
    public GroundRect Next(Vector3 direction)
    {
        float alongRight = direction.x * Right.x + direction.z * Right.z;
        float alongForward = direction.x * Forward.x + direction.z * Forward.z;
        Vector3 step = Mathf.Abs(alongRight) >= Mathf.Abs(alongForward)
            ? Right * (Extent.x * Mathf.Sign(alongRight))
            : Forward * (Extent.y * Mathf.Sign(alongForward));
        return new GroundRect(Corner + step, Yaw, Size);
    }
}
