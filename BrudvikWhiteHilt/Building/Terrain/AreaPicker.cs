using UnityEngine;

namespace BrudvikWhiteHilt.Building.Terrain;

/// <summary>
/// Picks points on the ground and rectangles between two clicked corners, for the hoe and cultivator tools.
/// After the first corner, the arrow keys switch to a fixed size set in whole metres.
/// </summary>
public sealed class AreaPicker
{
    private static int terrainMask;

    private bool fixedSize;
    private float width;
    private float length;

    /// <summary>The first corner, or null.</summary>
    public Vector3? First { get; private set; }

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
        First = new Vector3(Mathf.Round(corner.x), corner.y, Mathf.Round(corner.z));
        fixedSize = false;
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
            Vector3 second = Second(aim);
            width = Mathf.Max(1f, Mathf.Abs(second.x - First.Value.x));
            length = Mathf.Max(1f, Mathf.Abs(second.z - First.Value.z));
            fixedSize = true;
        }

        width = Mathf.Clamp(width + dx, 1f, 200f);
        length = Mathf.Clamp(length + dz, 1f, 200f);
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

    private static bool Pressed(BepInEx.Configuration.ConfigEntry<BepInEx.Configuration.KeyboardShortcut> key)
    {
        return key.Value.MainKey != KeyCode.None && key.Value.IsDown();
    }
}
