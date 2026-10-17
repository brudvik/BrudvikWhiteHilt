using System.Globalization;
using UnityEngine;

namespace BrudvikWhiteHilt.Building.Terrain;

/// <summary>
/// Picks points on the ground and rectangles between two clicked corners, for the hoe and cultivator tools.
/// After the first corner, the arrow keys switch to a fixed size set in whole metres. A size chosen on the panel holds
/// for every rectangle until it is set back to free, and the hoe's rectangles can be turned (<see cref="Yaw"/>).
/// </summary>
public sealed class AreaPicker
{
    /// <summary>Largest side of a rectangle, in metres.</summary>
    public const float MaxSide = 200f;

    private static int terrainMask;

    private bool fixedSize;
    private float width;
    private float length;

    /// <summary>The first corner, or null.</summary>
    public Vector3? First { get; private set; }

    /// <summary>Heading of the rectangles in degrees; 0 lies along the world's axes.</summary>
    public float Yaw { get; set; }

    /// <summary>A size chosen for every rectangle (width, length), or null to pick freely.</summary>
    public Vector2? Preset { get; set; }

    /// <summary>
    /// Reads a size as players type it: "40x40", "40 x 40", "40*10", "1,5x40" or one number for a square.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="size">Width and length, each 1 to <see cref="MaxSide"/> m.</param>
    /// <returns>True if it was a size.</returns>
    public static bool TryParseSize(string text, out Vector2 size)
    {
        size = default;
        string[] parts = (text ?? string.Empty).ToLowerInvariant().Replace(',', '.').Replace('*', 'x').Replace('×', 'x').Split('x');
        if (parts.Length > 2 || !float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float first))
        {
            return false;
        }

        float second = first;
        if (parts.Length == 2 && !float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out second))
        {
            return false;
        }

        if (float.IsNaN(first) || float.IsNaN(second))
        {
            return false;
        }

        size = new Vector2(Mathf.Clamp(Mathf.Round(first), 1f, MaxSide), Mathf.Clamp(Mathf.Round(second), 1f, MaxSide));
        return true;
    }

    /// <summary>
    /// Finds the ground point the camera aims at.
    /// </summary>
    /// <param name="point">The point.</param>
    /// <returns>True if the ground is hit.</returns>
    public static bool Aim(out Vector3 point)
    {
        point = default;
        if (GameCamera.instance == null)
        {
            return false;
        }

        if (terrainMask == 0)
        {
            terrainMask = LayerMask.GetMask("terrain");
        }

        Transform view = GameCamera.instance.transform;
        float reach = BuildCamera.Active ? BuildToolSettings.MaxPlaceDistance.Value : TerrainSettings.AreaReach.Value;
        if (!Physics.Raycast(view.position, view.forward, out RaycastHit hit, reach, terrainMask))
        {
            return false;
        }

        point = hit.point;
        return true;
    }

    /// <summary>
    /// Sets the first corner.
    /// </summary>
    /// <param name="corner">The corner.</param>
    public void SetFirst(Vector3 corner)
    {
        SetFirstExact(new Vector3(Mathf.Round(corner.x), corner.y, Mathf.Round(corner.z)));
    }

    /// <summary>
    /// Sets the first corner where it is, without rounding, e.g. the corner of the next tile of a turned rectangle.
    /// </summary>
    /// <param name="corner">The corner.</param>
    public void SetFirstExact(Vector3 corner)
    {
        First = corner;
        fixedSize = false;
    }

    /// <summary>
    /// The rectangle from the first corner (or the aimed point before it is set) to the aimed point, or of the fixed or
    /// chosen size towards it.
    /// </summary>
    /// <param name="aim">The aimed ground point.</param>
    /// <returns>The rectangle.</returns>
    public GroundRect Rect(Vector3 aim)
    {
        Vector3 first = First ?? new Vector3(Mathf.Round(aim.x), aim.y, Mathf.Round(aim.z));
        if (fixedSize)
        {
            return GroundRect.Fixed(first, Yaw, aim, width, length);
        }

        if (Preset.HasValue)
        {
            // Before the first corner the rectangle hangs from the aimed point, forward and to the right.
            return First.HasValue
                ? GroundRect.Fixed(first, Yaw, aim, Preset.Value.x, Preset.Value.y)
                : new GroundRect(first, Yaw, Preset.Value);
        }

        return GroundRect.Towards(first, Yaw, aim);
    }

    /// <summary>
    /// Forgets the corners.
    /// </summary>
    public void Clear()
    {
        First = null;
        fixedSize = false;
    }

    /// <summary>
    /// The second corner for where the camera aims, or the fixed size in the aimed direction.
    /// </summary>
    /// <param name="aim">The aimed ground point.</param>
    /// <returns>The second corner.</returns>
    public Vector3 Second(Vector3 aim)
    {
        Vector3 first = First ?? aim;
        if (!fixedSize)
        {
            return new Vector3(Mathf.Round(aim.x), aim.y, Mathf.Round(aim.z));
        }

        float signX = aim.x >= first.x ? 1f : -1f;
        float signZ = aim.z >= first.z ? 1f : -1f;
        return new Vector3(first.x + signX * width, aim.y, first.z + signZ * length);
    }

    /// <summary>
    /// The arrow keys set a fixed size: left/right the width, up/down the length.
    /// </summary>
    /// <param name="aim">The aimed ground point, to start from the current size.</param>
    public void HandleArrows(Vector3 aim)
    {
        int dx = (Pressed(BuildToolSettings.KeyNudgeRight) ? 1 : 0) - (Pressed(BuildToolSettings.KeyNudgeLeft) ? 1 : 0);
        int dz = (Pressed(BuildToolSettings.KeyNudgeForward) ? 1 : 0) - (Pressed(BuildToolSettings.KeyNudgeBack) ? 1 : 0);
        if (dx == 0 && dz == 0 || !First.HasValue)
        {
            return;
        }

        if (!fixedSize)
        {
            Vector2 extent = Rect(aim).Extent;
            width = Mathf.Max(1f, extent.x);
            length = Mathf.Max(1f, extent.y);
            fixedSize = true;
        }

        width = Mathf.Clamp(width + dx, 1f, MaxSide);
        length = Mathf.Clamp(length + dz, 1f, MaxSide);
    }

    /// <summary>
    /// Size of the rectangle to a second corner, in metres.
    /// </summary>
    /// <param name="second">The second corner.</param>
    /// <returns>Width and length.</returns>
    public Vector2 Size(Vector3 second)
    {
        Vector3 first = First ?? second;
        return new Vector2(Mathf.Abs(second.x - first.x), Mathf.Abs(second.z - first.z));
    }

    /// <summary>
    /// Draws the rectangle as a flat outline at a height.
    /// </summary>
    /// <param name="second">The second corner.</param>
    /// <param name="height">The height to draw it at.</param>
    public void Show(Vector3 second, float height)
    {
        Vector3 first = First ?? second;
        Bounds box = new();
        box.SetMinMax(new Vector3(Mathf.Min(first.x, second.x), height, Mathf.Min(first.z, second.z)),
            new Vector3(Mathf.Max(first.x, second.x), height + 0.15f, Mathf.Max(first.z, second.z)));
        BuildGizmos.ShowBox(box);
    }

    /// <summary>
    /// Draws a rectangle, turned any way, as a flat outline at a height.
    /// </summary>
    /// <param name="rect">The rectangle.</param>
    /// <param name="height">The height to draw it at.</param>
    public static void ShowRect(GroundRect rect, float height)
    {
        Vector3[] low = rect.Corners(height);
        Vector3[] high = rect.Corners(height + 0.15f);
        BuildGizmos.ShowPath(new[]
        {
            low[0], low[1], low[2], low[3], low[0], high[0], high[1], low[1], high[1], high[2], low[2], high[2], high[3], low[3], high[3], high[0]
        });
    }

    private static bool Pressed(BepInEx.Configuration.ConfigEntry<BepInEx.Configuration.KeyboardShortcut> key)
    {
        return key.Value.MainKey != KeyCode.None && key.Value.IsDown();
    }
}
