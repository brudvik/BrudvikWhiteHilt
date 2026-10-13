using System;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles.Weapons;

/// <summary>
/// The grapnel rope between two ships, from plain values so it can be tested without the game.
/// </summary>
public static class GrappleRules
{
    /// <summary>
    /// How hard the rope pulls a ship towards the other. It pulls only when it is taut.
    /// </summary>
    /// <param name="distance">Metres between the two ends of the rope.</param>
    /// <param name="length">Metres the rope pulls the ships to.</param>
    /// <param name="strength">Acceleration, in metres per second squared, for each metre of slack.</param>
    /// <returns>The acceleration, or 0 when the rope is slack.</returns>
    public static float Pull(float distance, float length, float strength)
    {
        return Mathf.Max(0f, distance - length) * Mathf.Max(0f, strength);
    }

    /// <summary>
    /// Whether the rope gives way: it is stretched too far, or it has held as long as it can.
    /// </summary>
    /// <param name="distance">Metres between the two ends of the rope.</param>
    /// <param name="breakDistance">Metres at which it snaps.</param>
    /// <param name="ageSeconds">Seconds since the rope was made fast.</param>
    /// <param name="maxSeconds">Seconds it holds before it frays.</param>
    /// <returns>True if the rope is gone.</returns>
    public static bool Snaps(float distance, float breakDistance, float ageSeconds, float maxSeconds)
    {
        return distance > breakDistance || ageSeconds >= maxSeconds;
    }

    /// <summary>
    /// The acceleration the rope gives one of the two ships. It points along the rope, level with the water, and only while
    /// the rope is taut; the faster the ship closes in on the other one, the less it pulls, so the two ships do not swing
    /// past each other. A rope never pushes.
    /// </summary>
    /// <param name="toOther">From this ship's end of the rope to the other end.</param>
    /// <param name="relativeVelocity">This ship's velocity minus the other ship's.</param>
    /// <param name="length">Metres the rope pulls the ships to.</param>
    /// <param name="strength">Acceleration, in metres per second squared, for each metre of slack.</param>
    /// <param name="damping">Pull taken off per metre per second of closing speed, per second.</param>
    /// <param name="share">This ship's share of the pull; each of two ships takes half.</param>
    /// <returns>The acceleration, or zero when the rope is slack.</returns>
    public static Vector3 Acceleration(Vector3 toOther, Vector3 relativeVelocity, float length, float strength, float damping, float share)
    {
        Vector3 flat = new(toOther.x, 0f, toOther.z);
        float distance = flat.magnitude;
        if (distance < 1e-3f)
        {
            return Vector3.zero;
        }

        float pull = Pull(distance, length, strength);
        if (pull <= 0f)
        {
            return Vector3.zero;
        }

        Vector3 axis = flat / distance;
        float closing = Vector3.Dot(relativeVelocity, axis);
        float acceleration = (pull - closing * Mathf.Max(0f, damping)) * Mathf.Max(0f, share);
        return acceleration > 0f ? axis * acceleration : Vector3.zero;
    }

    /// <summary>
    /// Seconds a rope has held, from world ticks.
    /// </summary>
    /// <param name="startTicks">World ticks when it was made fast.</param>
    /// <param name="nowTicks">World ticks now.</param>
    /// <returns>Seconds; never negative.</returns>
    public static float AgeSeconds(long startTicks, long nowTicks)
    {
        return nowTicks <= startTicks ? 0f : (float)((nowTicks - startTicks) / (double)TimeSpan.TicksPerSecond);
    }
}
