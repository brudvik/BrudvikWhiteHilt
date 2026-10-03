using UnityEngine;

namespace BrudvikWhiteHilt.Navigation.Compass;

/// <summary>Shared camera-relative positioning for compass ticks and world markers.</summary>
public static class CompassGeometry
{
    /// <summary>Normalizes an angle to the range zero (inclusive) to 360 (exclusive).</summary>
    /// <param name="angle">The angle in degrees.</param>
    /// <returns>The normalized heading.</returns>
    public static float Normalize(float angle) => Mathf.Repeat(angle, 360f);

    /// <summary>Finds the horizontal bearing from the player to a world position.</summary>
    /// <param name="origin">The player's position.</param>
    /// <param name="target">The world marker's position.</param>
    /// <returns>The bearing, with north at zero and east at 90 degrees.</returns>
    public static float Bearing(Vector3 origin, Vector3 target)
    {
        Vector3 direction = target - origin;
        return Normalize(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg);
    }

    /// <summary>Projects a heading onto the tape without rounding the camera heading.</summary>
    /// <param name="heading">The current camera heading.</param>
    /// <param name="target">The tick or marker heading.</param>
    /// <param name="width">The tape width in UI units.</param>
    /// <param name="visibleDegrees">The total visible angle.</param>
    /// <param name="position">The horizontal offset from the fixed centre.</param>
    /// <returns>Whether the heading is inside the visible interval.</returns>
    public static bool Project(float heading, float target, float width, float visibleDegrees, out float position)
    {
        float delta = Mathf.DeltaAngle(heading, target);
        position = delta * width / visibleDegrees;
        return Mathf.Abs(delta) <= visibleDegrees / 2f;
    }
}