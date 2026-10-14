using System;

namespace BrudvikWhiteHilt.SeaBattles;

/// <summary>The stage of a chase, as seen from the hunted ship.</summary>
public enum PursuitPhase
{
    /// <summary>No cap: the ship may run as fast as it can.</summary>
    Free = 0,

    /// <summary>The shock: the Nail Ships have just come, and the hunted ship is held below their speed.</summary>
    Shock = 1,

    /// <summary>The shock is over, but live Nail crew still stand on the ship, and they hold it back.</summary>
    Held = 2,
}

/// <summary>
/// The rules of the pursuit speed cap, from plain values so they can be tested without the game. A ship that can outrun a
/// Nail Ship is never caught; so while the raiders attack, the hunted ship is held a little below their speed, and the
/// cap stays for as long as their crew fights aboard.
/// </summary>
public static class PursuitRules
{
    /// <summary>How fast, in seconds, the reference speed follows a faster raider.</summary>
    public const float RiseSeconds = 1.5f;

    /// <summary>How slowly, in seconds, the reference speed follows a slower raider, so a turn does not lift the cap.</summary>
    public const float FallSeconds = 10f;

    /// <summary>
    /// The stage of the chase.
    /// </summary>
    /// <param name="secondsSinceStart">Seconds since the first raider began to hunt the ship.</param>
    /// <param name="shockSeconds">How long the shock lasts.</param>
    /// <param name="enemiesAboard">Whether live Nail crew stand on the ship.</param>
    /// <returns>The phase.</returns>
    public static PursuitPhase Phase(float secondsSinceStart, float shockSeconds, bool enemiesAboard)
    {
        if (secondsSinceStart < shockSeconds)
        {
            return PursuitPhase.Shock;
        }

        return enemiesAboard ? PursuitPhase.Held : PursuitPhase.Free;
    }

    /// <summary>
    /// The top speed of the hunted ship in a phase.
    /// </summary>
    /// <param name="phase">The phase.</param>
    /// <param name="referenceSpeed">The raiders' reference speed, in metres per second.</param>
    /// <param name="factor">Share of the reference speed the hunted ship may keep, below 1 to be caught.</param>
    /// <param name="floorSpeed">Least cap, so the ship is never stopped dead while the raiders turn.</param>
    /// <returns>The cap in metres per second, or null for no cap.</returns>
    public static float? Cap(PursuitPhase phase, float referenceSpeed, float factor, float floorSpeed)
    {
        if (phase == PursuitPhase.Free)
        {
            return null;
        }

        return Math.Max(floorSpeed, Math.Max(0f, referenceSpeed) * factor);
    }

    /// <summary>
    /// Follows the fastest raider's speed: quickly up, slowly down.
    /// </summary>
    /// <param name="reference">The reference speed so far.</param>
    /// <param name="sample">The fastest hunter's speed now.</param>
    /// <param name="deltaTime">Seconds since the last sample.</param>
    /// <returns>The new reference speed.</returns>
    public static float Reference(float reference, float sample, float deltaTime)
    {
        float seconds = sample > reference ? RiseSeconds : FallSeconds;
        float step = Math.Min(1f, Math.Max(0f, deltaTime) / seconds);
        return reference + (sample - reference) * step;
    }

    /// <summary>
    /// The ship's horizontal speed after the cap is applied. A ship above the cap is braked by at most a set amount each
    /// second, so the cap is felt as a drag and not as a wall.
    /// </summary>
    /// <param name="speed">The ship's horizontal speed now.</param>
    /// <param name="cap">The cap, or null for none.</param>
    /// <param name="deltaTime">Seconds of the physics step.</param>
    /// <param name="brake">Most speed taken off per second, in metres per second squared.</param>
    /// <returns>The speed the ship is to have.</returns>
    public static float Limit(float speed, float? cap, float deltaTime, float brake)
    {
        if (cap == null || speed <= cap.Value)
        {
            return speed;
        }

        return Math.Max(cap.Value, speed - brake * deltaTime);
    }
}
