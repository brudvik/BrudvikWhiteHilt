using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships.Ram;

/// <summary>
/// The maths of the Ram Prow, from plain values so it can be tested without the game.
/// </summary>
public static class RamRules
{
    /// <summary>
    /// How fast the ramming ship closes on its target along its bow.
    /// </summary>
    /// <param name="relativeVelocity">Velocity of the ramming ship relative to the target (its own velocity minus the
    /// target's), in world space. The caller must check which way round Unity's <c>Collision.relativeVelocity</c>
    /// comes and pass it this way.</param>
    /// <param name="forward">The direction the bow points; it does not have to be a unit vector.</param>
    /// <returns>Metres per second towards the bow's direction, never negative.</returns>
    public static float ClosingSpeed(Vector3 relativeVelocity, Vector3 forward)
    {
        if (forward.sqrMagnitude < 1e-6f)
        {
            return 0f;
        }

        return Mathf.Max(0f, Vector3.Dot(relativeVelocity, forward.normalized));
    }

    /// <summary>
    /// How fast two bodies come together along the ramming ship's bow, whichever way round the engine reports their
    /// relative velocity. Unity's documentation does not say which body the vector of <c>Collision.relativeVelocity</c> is
    /// measured from, and the two sides of one collision get opposite vectors, so only its size along the bow is used. That
    /// is right because a collision begins with the bodies coming together: for a contact at the bow the closing speed is
    /// along the bow.
    /// </summary>
    /// <param name="relativeVelocity">The collision's relative velocity, in world space, in either direction.</param>
    /// <param name="forward">The direction the bow points.</param>
    /// <returns>Metres per second along the bow, never negative.</returns>
    public static float ApproachSpeed(Vector3 relativeVelocity, Vector3 forward)
    {
        return Mathf.Max(ClosingSpeed(relativeVelocity, forward), ClosingSpeed(-relativeVelocity, forward));
    }

    /// <summary>
    /// Whether a contact is on the ship's bow, in the foremost quarter of the hull's length.
    /// </summary>
    /// <param name="localContact">The contact in the ship's local space, measured from the middle of the hull (z along it).</param>
    /// <param name="hullLength">Length of the hull, in metres.</param>
    /// <returns>True if the contact is in the front quarter.</returns>
    public static bool IsBowContact(Vector3 localContact, float hullLength)
    {
        return hullLength > 0f && localContact.z >= hullLength * 0.25f;
    }

    /// <summary>
    /// Push of a ram on a creature.
    /// </summary>
    /// <param name="closingSpeed">Closing speed, in metres per second.</param>
    /// <param name="perSpeed">Push per metre per second of closing speed.</param>
    /// <returns>The push force of the hit; 0 for no speed.</returns>
    public static float Push(float closingSpeed, float perSpeed)
    {
        return closingSpeed <= 0f ? 0f : closingSpeed * Mathf.Max(0f, perSpeed);
    }

    /// <summary>
    /// Whether a target may be rammed again: it has not been hit within the cooldown.
    /// </summary>
    /// <param name="lastHit">Time of the last ram of this target, or a negative number if it has never been rammed.</param>
    /// <param name="now">The time now, on the same clock.</param>
    /// <param name="cooldown">Seconds between two rams of one target.</param>
    /// <returns>True if the target can be rammed.</returns>
    public static bool CooledDown(float lastHit, float now, float cooldown)
    {
        return lastHit < 0f || now - lastHit >= cooldown;
    }

    /// <summary>
    /// Damage of a ram.
    /// </summary>
    /// <param name="closingSpeed">Closing speed, in metres per second.</param>
    /// <param name="minSpeed">Least closing speed that counts as a ram.</param>
    /// <param name="perSpeed">Damage per metre per second of closing speed.</param>
    /// <returns>The damage; 0 below the least speed.</returns>
    public static float Damage(float closingSpeed, float minSpeed, float perSpeed)
    {
        if (closingSpeed < minSpeed || closingSpeed <= 0f)
        {
            return 0f;
        }

        return closingSpeed * Mathf.Max(0f, perSpeed);
    }
}
