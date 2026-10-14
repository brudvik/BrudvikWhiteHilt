using UnityEngine;

namespace BrudvikWhiteHilt.Comfort;

/// <summary>
/// How far comfort reaches. The game counts the comfort pieces within 10 m of where a player stands; a player's own
/// reach grows with the book of the hall and with Homecraft, up to a cap, and a Husvette makes the pieces within its
/// own reach count for everyone under a roof in it. Pure numbers, so the rules can be tested without the game.
/// </summary>
public static class ComfortReach
{
    /// <summary>The game's own reach of comfort, in metres (<c>SE_Rested.c_ComfortRadius</c>).</summary>
    public const float GameReach = 10f;

    /// <summary>
    /// A player's own comfort reach: the game's 10 m, plus the book's bonus once read, plus the skill's share of its
    /// bonus, but never more than the cap (and never less than the game's).
    /// </summary>
    /// <param name="knowsBook">Whether the player has read the book of the hall.</param>
    /// <param name="skillLevel">Homecraft, 0 to 100.</param>
    /// <param name="bookBonus">Metres the book adds.</param>
    /// <param name="skillBonus">Metres the skill adds at level 100.</param>
    /// <param name="max">Most the reach can be.</param>
    /// <returns>The reach, in metres.</returns>
    public static float Personal(bool knowsBook, float skillLevel, float bookBonus, float skillBonus, float max)
    {
        float reach = GameReach + (knowsBook ? Mathf.Max(0f, bookBonus) : 0f) + Mathf.Max(0f, skillBonus) * Mathf.Clamp01(skillLevel / 100f);
        return Mathf.Max(GameReach, Mathf.Min(reach, max));
    }
}
