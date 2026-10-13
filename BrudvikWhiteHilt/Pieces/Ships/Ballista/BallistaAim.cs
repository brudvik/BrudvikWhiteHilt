using System;

namespace BrudvikWhiteHilt.Pieces.Ships.Ballista;

/// <summary>
/// The aim of the Ship Ballista in plain degrees, so the arc it can turn through is tested without the game. Yaw is
/// measured from straight ahead along the ship, positive to starboard; pitch is positive upwards. Both are in the
/// ship's own frame, so the aim does not change when the ship turns.
/// </summary>
public static class BallistaAim
{
    /// <summary>
    /// Wraps an angle into (-180, 180].
    /// </summary>
    /// <param name="degrees">The angle in degrees.</param>
    /// <returns>The same direction as an angle from -180 (not included) to 180.</returns>
    public static float Wrap(float degrees)
    {
        float wrapped = degrees % 360f;
        if (wrapped > 180f)
        {
            wrapped -= 360f;
        }
        else if (wrapped <= -180f)
        {
            wrapped += 360f;
        }

        return wrapped;
    }

    /// <summary>
    /// Keeps an aim inside the firing arc. A yaw behind the ship is moved to the nearest side limit.
    /// </summary>
    /// <param name="yaw">Wanted yaw in degrees, any angle.</param>
    /// <param name="pitch">Wanted pitch in degrees.</param>
    /// <param name="yawArc">Degrees it can turn to either side of straight ahead.</param>
    /// <param name="pitchMin">Lowest pitch in degrees.</param>
    /// <param name="pitchMax">Highest pitch in degrees. If it is below <paramref name="pitchMin"/> they swap places.</param>
    /// <returns>The yaw, in (-arc, arc], and the pitch that can be reached.</returns>
    public static (float Yaw, float Pitch) Clamp(float yaw, float pitch, float yawArc, float pitchMin, float pitchMax)
    {
        float arc = Math.Max(0f, yawArc);
        float low = Math.Min(pitchMin, pitchMax);
        float high = Math.Max(pitchMin, pitchMax);
        return (Math.Min(arc, Math.Max(-arc, Wrap(yaw))), Math.Min(high, Math.Max(low, pitch)));
    }

    /// <summary>
    /// Turns a direction in the ship's frame (x to starboard, y up, z towards the bow) into yaw and pitch.
    /// </summary>
    /// <param name="x">Starboard part of the direction.</param>
    /// <param name="y">Upward part of the direction.</param>
    /// <param name="z">Forward part of the direction.</param>
    /// <returns>Yaw and pitch in degrees; straight ahead if the direction has no length.</returns>
    public static (float Yaw, float Pitch) FromDirection(float x, float y, float z)
    {
        double horizontal = Math.Sqrt((double)x * x + (double)z * z);
        if (horizontal < 1e-6 && Math.Abs(y) < 1e-6)
        {
            return (0f, 0f);
        }

        float yaw = horizontal < 1e-6 ? 0f : (float)(Math.Atan2(x, z) * 180.0 / Math.PI);
        float pitch = (float)(Math.Atan2(y, horizontal) * 180.0 / Math.PI);
        return (yaw, pitch);
    }
}
