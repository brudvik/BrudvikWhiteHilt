using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Building.Moats;

/// <summary>
/// What a ditch does to the creatures down in it: they move slower and, for a while, slide down its steep sides as
/// players do, until they claw their way out. Players, tamed animals and bosses are left alone.
/// </summary>
public static class MoatEffects
{
    private const float CheckSeconds = 0.25f;
    private const int PruneAbove = 512;
    private const float ForgetSeconds = 10f;

    private static readonly Dictionary<int, State> states = new();
    private static readonly List<int> stale = new();

    /// <summary>
    /// Speed multiplier for a creature.
    /// </summary>
    /// <param name="character">The creature.</param>
    /// <returns>1, or the ditch speed while it is down in one.</returns>
    public static float SpeedFactor(Character character)
    {
        return InDitch(character, out _) ? MoatSettings.CreatureSpeed.Value : 1f;
    }

    /// <summary>
    /// True if a creature slides down steep ground in a ditch now.
    /// </summary>
    /// <param name="character">The creature.</param>
    /// <returns>True if so.</returns>
    public static bool Slides(Character character)
    {
        return InDitch(character, out float since) && since < MoatSettings.ClimbOutSeconds.Value;
    }

    // Whether a creature stands in a moat, and since when. The position is checked only now and then per creature and
    // remembered, as this runs for every creature every frame. Players, tamed creatures and bosses are never slowed.
    private static bool InDitch(Character character, out float since)
    {
        since = 0f;
        if (MoatSettings.Enabled == null || !MoatSettings.Enabled.Value || character == null || character.IsPlayer() || character.IsTamed()
            || character.IsBoss())
        {
            return false;
        }

        float now = Time.time;
        int id = character.GetInstanceID();
        if (!states.TryGetValue(id, out State state))
        {
            state = new State { CheckedAt = float.NegativeInfinity };
            states[id] = state;
            Prune(now);
        }

        if (now - state.CheckedAt >= CheckSeconds)
        {
            state.CheckedAt = now;
            bool inside = MoatSection.IsInDitch(character.transform.position);
            if (inside && !state.Inside)
            {
                state.EnteredAt = now;
            }

            state.Inside = inside;
        }

        since = now - state.EnteredAt;
        return state.Inside;
    }

    // Forgets creatures not checked for a while once the table grows large, so it does not keep every creature ever
    // seen.
    private static void Prune(float now)
    {
        if (states.Count <= PruneAbove)
        {
            return;
        }

        stale.Clear();
        foreach (KeyValuePair<int, State> entry in states)
        {
            if (now - entry.Value.CheckedAt > ForgetSeconds)
            {
                stale.Add(entry.Key);
            }
        }

        foreach (int id in stale)
        {
            states.Remove(id);
        }
    }

    private sealed class State
    {
        public float CheckedAt;
        public float EnteredAt;
        public bool Inside;
    }
}
