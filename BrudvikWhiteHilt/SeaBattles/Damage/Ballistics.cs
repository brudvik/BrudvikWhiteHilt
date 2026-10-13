using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles.Damage;

/// <summary>
/// Aiming a lobbed projectile, from plain values so it can be tested without the game. Gravity is the downward
/// acceleration in metres per second squared, as a positive number.
/// </summary>
public static class Ballistics
{
    private const float MinReach = 0.01f;

    /// <summary>
    /// The launch velocity that makes a projectile of a given speed pass through a target point under gravity.
    /// </summary>
    /// <param name="from">Where it is launched.</param>
    /// <param name="to">The point to hit.</param>
    /// <param name="speed">Launch speed, in metres per second.</param>
    /// <param name="gravity">Downward acceleration; 0 or less gives a straight shot.</param>
    /// <param name="high">True for the steep, slow arc; false for the flat, quick one.</param>
    /// <param name="velocity">The launch velocity, or zero if there is none.</param>
    /// <returns>False if the target is out of reach, or straight above or below the launch point.</returns>
    public static bool LaunchVelocity(Vector3 from, Vector3 to, float speed, float gravity, bool high, out Vector3 velocity)
    {
        velocity = Vector3.zero;
        if (speed <= 0f)
        {
            return false;
        }

        Vector3 offset = to - from;
        if (gravity <= 0f)
        {
            if (offset.sqrMagnitude < MinReach * MinReach)
            {
                return false;
            }

            velocity = offset.normalized * speed;
            return true;
        }

        Vector3 flat = new(offset.x, 0f, offset.z);
        float reach = flat.magnitude;
        if (reach < MinReach)
        {
            return false;
        }

        float speedSquared = speed * speed;
        float discriminant = speedSquared * speedSquared
            - gravity * (gravity * reach * reach + 2f * offset.y * speedSquared);
        // A target exactly at the limit of reach must not be lost to rounding.
        if (discriminant < -1e-5f * speedSquared * speedSquared)
        {
            return false;
        }

        float root = Mathf.Sqrt(Mathf.Max(0f, discriminant));
        float tangent = (speedSquared + (high ? root : -root)) / (gravity * reach);
        float cosine = 1f / Mathf.Sqrt(1f + tangent * tangent);
        float sine = tangent * cosine;
        velocity = flat / reach * (speed * cosine) + Vector3.up * (speed * sine);
        return true;
    }

    /// <summary>
    /// Where a moving target will be after a time, for aiming ahead of it.
    /// </summary>
    /// <param name="target">The target's position now.</param>
    /// <param name="targetVelocity">The target's velocity.</param>
    /// <param name="flightSeconds">Seconds the projectile takes to arrive; negative counts as 0.</param>
    /// <returns>The point to aim at.</returns>
    public static Vector3 Lead(Vector3 target, Vector3 targetVelocity, float flightSeconds)
    {
        return target + targetVelocity * Mathf.Max(0f, flightSeconds);
    }

    /// <summary>
    /// Seconds a launched projectile takes to cover the horizontal distance to a point.
    /// </summary>
    /// <param name="from">Where it is launched.</param>
    /// <param name="to">The point it travels to.</param>
    /// <param name="velocity">The launch velocity.</param>
    /// <returns>The flight time, or 0 if the projectile has no horizontal speed.</returns>
    public static float FlightSeconds(Vector3 from, Vector3 to, Vector3 velocity)
    {
        float horizontalSpeed = new Vector2(velocity.x, velocity.z).magnitude;
        if (horizontalSpeed < MinReach)
        {
            return 0f;
        }

        return new Vector2(to.x - from.x, to.z - from.z).magnitude / horizontalSpeed;
    }

    /// <summary>
    /// The launch velocity that hits a moving target: aims at where the target will be when the projectile arrives, working
    /// the flight time out again for each new aim point.
    /// </summary>
    /// <param name="from">Where it is launched.</param>
    /// <param name="target">The target's position now.</param>
    /// <param name="targetVelocity">The target's velocity.</param>
    /// <param name="speed">Launch speed, in metres per second.</param>
    /// <param name="gravity">Downward acceleration; 0 or less gives a straight shot.</param>
    /// <param name="high">True for the steep, slow arc; false for the flat, quick one.</param>
    /// <param name="velocity">The launch velocity, or zero if there is none.</param>
    /// <param name="passes">How many times the aim point is moved to where the target will be; 0 aims at the target as it is.</param>
    /// <returns>False if the target, or the place it is led to, is out of reach.</returns>
    public static bool Intercept(Vector3 from, Vector3 target, Vector3 targetVelocity, float speed, float gravity, bool high,
        out Vector3 velocity, int passes = 3)
    {
        Vector3 aim = target;
        for (int i = 0; i < passes; i++)
        {
            if (!LaunchVelocity(from, aim, speed, gravity, high, out velocity))
            {
                return false;
            }

            aim = Lead(target, targetVelocity, FlightSeconds(from, aim, velocity));
        }

        return LaunchVelocity(from, aim, speed, gravity, high, out velocity);
    }
}
