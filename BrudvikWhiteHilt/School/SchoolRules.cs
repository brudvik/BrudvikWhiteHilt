using System.Collections.Generic;

namespace BrudvikWhiteHilt.School;

/// <summary>
/// The arithmetic of studying at the School Bench, on plain values so it can be tested without the game.
/// </summary>
public static class SchoolRules
{
    /// <summary>
    /// The level a reader can reach in a skill by studying it.
    /// </summary>
    /// <param name="bestLevel">The best level written in the bench.</param>
    /// <param name="share">Share of that level a reader can reach.</param>
    /// <returns>The level, rounded down to a whole level.</returns>
    public static int StudyTarget(float bestLevel, float share)
    {
        return (int)(bestLevel * share + 0.0001f);
    }

    /// <summary>
    /// Whether a reader can still study a skill: the next whole level must not pass the target.
    /// </summary>
    /// <param name="level">The reader's level.</param>
    /// <param name="target">The target from <see cref="StudyTarget"/>.</param>
    /// <returns>True if studying may raise it.</returns>
    public static bool CanStudy(float level, int target)
    {
        return (int)level + 1 <= target;
    }

    /// <summary>
    /// The skill to study next: the one furthest below its target.
    /// </summary>
    /// <param name="skills">Each skill's id, the reader's level and the target.</param>
    /// <returns>The id, or null if none can be studied.</returns>
    public static int? PickSkill(IEnumerable<(int Id, float Level, int Target)> skills)
    {
        int? best = null;
        float gap = 0f;
        foreach ((int id, float level, int target) in skills)
        {
            if (!CanStudy(level, target))
            {
                continue;
            }

            float behind = target - level;
            if (best == null || behind > gap)
            {
                best = id;
                gap = behind;
            }
        }

        return best;
    }
}
