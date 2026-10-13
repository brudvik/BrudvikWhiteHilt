using System;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles.Weapons;

/// <summary>
/// When a Nail Ship uses its weapons, from plain values so it can be tested without the game.
/// </summary>
public static class WeaponRules
{
    /// <summary>
    /// Whether a Nail Ship in a state throws pitch pots: while it lies alongside and while it closes in.
    /// </summary>
    /// <param name="state">What the ship is doing.</param>
    /// <returns>True for Broadside and Board.</returns>
    public static bool CanThrow(RaiderState state)
    {
        return state == RaiderState.Broadside || state == RaiderState.Board;
    }

    /// <summary>
    /// Whether a Nail Ship in a state shoots its ballista: on the way in, alongside and closing in, but not when it runs, is
    /// a prize or lies at a quay.
    /// </summary>
    /// <param name="state">What the ship is doing.</param>
    /// <returns>True for Approach, Broadside and Board.</returns>
    public static bool CanShoot(RaiderState state)
    {
        return state == RaiderState.Approach || state == RaiderState.Broadside || state == RaiderState.Board;
    }

    /// <summary>
    /// Whether the world has reached the key from which Nail Ships carry a ballista.
    /// </summary>
    /// <param name="key">The global key; empty or blank means always.</param>
    /// <param name="has">Whether the world has a global key; may be null before the world exists.</param>
    /// <returns>True if the ballista is carried.</returns>
    public static bool KeyReached(string key, Func<string, bool> has)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return true;
        }

        return has != null && has(key.Trim());
    }

    /// <summary>
    /// Whether a Nail Ship carries its ballista: the setting is on, and either the ship is a flagship, which always does,
    /// or the world has reached the key.
    /// </summary>
    /// <param name="enabled">The setting <c>NailBallista</c>.</param>
    /// <param name="flagship">Whether the ship is a flagship.</param>
    /// <param name="keyReached">Whether the world has reached the ballista's key.</param>
    /// <returns>True if the ballista is carried.</returns>
    public static bool BallistaOn(bool enabled, bool flagship, bool keyReached)
    {
        return enabled && (flagship || keyReached);
    }

    /// <summary>
    /// Whether a target is within range of a weapon.
    /// </summary>
    /// <param name="distance">Metres to the target.</param>
    /// <param name="range">Metres the weapon reaches.</param>
    /// <returns>True if the target can be shot at.</returns>
    public static bool InRange(float distance, float range)
    {
        return distance <= range;
    }

    /// <summary>
    /// Whether a sail is still torn.
    /// </summary>
    /// <param name="tornUntil">World ticks at which the tear mends; 0 for a sail never torn.</param>
    /// <param name="now">World ticks now.</param>
    /// <returns>True while the tear lasts.</returns>
    public static bool IsTorn(long tornUntil, long now)
    {
        return tornUntil > now;
    }

    /// <summary>
    /// The share of its sail force a ship keeps.
    /// </summary>
    /// <param name="torn">Whether the sail is torn.</param>
    /// <param name="share">The share of the force a torn sail loses, 0 to 1.</param>
    /// <returns>1 for a whole sail, less for a torn one.</returns>
    public static float TornFactor(bool torn, float share)
    {
        return torn ? 1f - Mathf.Clamp01(share) : 1f;
    }
}
