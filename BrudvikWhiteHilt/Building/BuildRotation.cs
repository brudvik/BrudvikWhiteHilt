using UnityEngine;

namespace BrudvikWhiteHilt.Building;

/// <summary>
/// The extra placement controls on top of vanilla's yaw: rotation step, tilt, roll, flip, nudge, grid and a snap switch.
/// Vanilla builds the ghost's rotation from <c>m_placeRotation</c> steps of <c>m_placeRotationDegrees</c>; a transpiler
/// routes that call through <see cref="Compose"/>, so snap points and the placed piece follow the full rotation.
/// </summary>
public static class BuildRotation
{
    private static readonly float[] Steps = { 22.5f, 15f, 5f, 1f };

    private static int stepIndex;
    private static float pitch;
    private static float roll;
    private static float extraYaw;
    private static bool flipped;
    private static Vector3 nudge;

    /// <summary>Degrees per rotation step, used for yaw, tilt and roll alike.</summary>
    public static float Step => Steps[stepIndex];

    /// <summary>Forward tilt in degrees.</summary>
    public static float Pitch => pitch;

    /// <summary>Sideways roll in degrees.</summary>
    public static float Roll => roll;

    /// <summary>True if the piece is upside down.</summary>
    public static bool Flipped => flipped;

    /// <summary>True while snapping to other pieces is switched off.</summary>
    public static bool SnapOff { get; private set; }

    /// <summary>True while the grid is on.</summary>
    public static bool GridOn { get; private set; }

    /// <summary>Set when the ghost snapped to another piece this frame; the grid then leaves it alone.</summary>
    public static bool SnappedThisFrame { get; set; }

    /// <summary>
    /// Stands in for vanilla's <c>Quaternion.Euler(0, yaw, 0)</c> when the ghost is rotated.
    /// Unity's Euler applies roll, then pitch, then yaw, so the tilt follows the piece's own heading.
    /// </summary>
    /// <param name="x">Vanilla pitch, always 0.</param>
    /// <param name="y">Vanilla yaw.</param>
    /// <param name="z">Vanilla roll, always 0.</param>
    /// <returns>The full rotation.</returns>
    public static Quaternion Compose(float x, float y, float z)
    {
        return Quaternion.Euler(x + pitch, y + extraYaw, z + roll + (flipped ? 180f : 0f));
    }

    /// <summary>
    /// The heading of the ghost in degrees, 0-360.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <returns>The yaw.</returns>
    public static float Yaw(Player player)
    {
        return Mathf.Repeat(player.m_placeRotationDegrees * player.m_placeRotation + extraYaw, 360f);
    }

    /// <summary>
    /// Keeps the player's rotation step in line with ours; a new player object starts at 22.5 degrees.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Apply(Player player)
    {
        if (!Mathf.Approximately(player.m_placeRotationDegrees, Step))
        {
            player.m_placeRotationDegrees = Step;
        }
    }

    /// <summary>
    /// Moves to the next rotation step and keeps the current heading as close as the new step allows.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void CycleStep(Player player)
    {
        float yaw = player.m_placeRotationDegrees * player.m_placeRotation + extraYaw;
        stepIndex = (stepIndex + 1) % Steps.Length;
        player.m_placeRotationDegrees = Step;
        player.m_placeRotation = Mathf.RoundToInt(yaw / Step);
        extraYaw = 0f;
        pitch = Mathf.Round(pitch / Step) * Step;
        roll = Mathf.Round(roll / Step) * Step;
    }

    /// <summary>
    /// Tilts the piece forward or back by one step.
    /// </summary>
    /// <param name="direction">1 forward, -1 back.</param>
    public static void Tilt(int direction)
    {
        pitch = Normalize(pitch + direction * Step);
    }

    /// <summary>
    /// Rolls the piece left or right by one step.
    /// </summary>
    /// <param name="direction">1 right, -1 left.</param>
    public static void RollBy(int direction)
    {
        roll = Normalize(roll - direction * Step);
    }

    /// <summary>
    /// Turns the piece upside down, or back.
    /// </summary>
    public static void Flip()
    {
        flipped = !flipped;
    }

    /// <summary>
    /// Sets the tilt to the next of 45, 90 and 0 degrees.
    /// </summary>
    public static void QuickTilt()
    {
        pitch = NextQuickAngle(pitch);
    }

    /// <summary>
    /// Sets the roll to the next of 45, 90 and 0 degrees.
    /// </summary>
    public static void QuickRoll()
    {
        roll = NextQuickAngle(roll);
    }

    /// <summary>
    /// Clears rotation, tilt, roll, flip and nudge.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Reset(Player player)
    {
        pitch = 0f;
        roll = 0f;
        extraYaw = 0f;
        flipped = false;
        nudge = Vector3.zero;
        player.m_placeRotation = 0;
    }

    /// <summary>
    /// Switches snapping off or on.
    /// </summary>
    /// <returns>True if snapping is now on.</returns>
    public static bool ToggleSnap()
    {
        SnapOff = !SnapOff;
        return !SnapOff;
    }

    /// <summary>
    /// Switches the grid on or off.
    /// </summary>
    /// <returns>True if the grid is now on.</returns>
    public static bool ToggleGrid()
    {
        GridOn = !GridOn;
        return GridOn;
    }

    /// <summary>
    /// Takes over the full rotation of a placed piece, so a copy lines up with it.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="piece">The piece that was copied.</param>
    public static void CopyFrom(Player player, Piece piece)
    {
        Vector3 euler = piece.transform.rotation.eulerAngles;
        player.m_placeRotation = Mathf.RoundToInt(euler.y / player.m_placeRotationDegrees);
        extraYaw = euler.y - player.m_placeRotation * player.m_placeRotationDegrees;
        pitch = Normalize(euler.x);
        roll = Normalize(euler.z);
        flipped = false;
        nudge = Vector3.zero;
    }

    /// <summary>
    /// Nudges the piece one step. Sideways nudges follow whichever of the piece's own axes is closest to the direction
    /// seen from the camera, so the piece stays lined up with itself.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="forward">1 away from the camera, -1 towards it.</param>
    /// <param name="right">1 right, -1 left.</param>
    /// <param name="up">1 up, -1 down.</param>
    public static void Nudge(Player player, int forward, int right, int up)
    {
        float step = Mathf.Max(0.001f, BuildToolSettings.NudgeStep.Value);
        nudge.y += up * step;
        if (forward == 0 && right == 0)
        {
            return;
        }

        Transform view = GameCamera.instance.transform;
        Vector3 wanted = Vector3.ProjectOnPlane(view.forward * forward + view.right * right, Vector3.up);
        if (wanted.sqrMagnitude < 0.0001f)
        {
            wanted = Vector3.ProjectOnPlane(view.up * forward + view.right * right, Vector3.up);
        }

        Quaternion heading = Quaternion.Euler(0f, Yaw(player), 0f);
        Vector3 best = Vector3.forward;
        float bestDot = float.NegativeInfinity;
        foreach (Vector3 axis in new[] { Vector3.forward, Vector3.back, Vector3.right, Vector3.left })
        {
            float dot = Vector3.Dot(heading * axis, wanted);
            if (dot > bestDot)
            {
                bestDot = dot;
                best = axis;
            }
        }

        nudge += best * step;
    }

    /// <summary>
    /// Forgets the nudge, done after a piece is placed.
    /// </summary>
    public static void ClearNudge()
    {
        nudge = Vector3.zero;
    }

    /// <summary>
    /// Moves the ghost onto the grid and by the nudge, after vanilla has placed it.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void AdjustGhost(Player player)
    {
        GameObject ghost = player.m_placementGhost;
        if (ghost == null || !ghost.activeSelf || (!GridOn && nudge == Vector3.zero))
        {
            return;
        }

        Vector3 position = ghost.transform.position;
        if (GridOn && !SnappedThisFrame)
        {
            float size = Mathf.Max(0.05f, BuildToolSettings.GridSize.Value);
            position.x = Mathf.Round(position.x / size) * size;
            position.z = Mathf.Round(position.z / size) * size;
        }

        ghost.transform.position = position + Quaternion.Euler(0f, Yaw(player), 0f) * nudge;
    }

    private static float NextQuickAngle(float angle)
    {
        float size = Mathf.Abs(angle);
        return size < 44.9f ? 45f : size < 89.9f ? 90f : 0f;
    }

    private static float Normalize(float angle)
    {
        angle = Mathf.Repeat(angle + 180f, 360f) - 180f;
        return Mathf.Abs(angle) < 0.01f ? 0f : angle;
    }
}
