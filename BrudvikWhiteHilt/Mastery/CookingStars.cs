using UnityEngine;

namespace BrudvikWhiteHilt.Mastery;

/// <summary>
/// The Cooking milestones: stars on dishes, faster cooking stations and food that does not burn near a watchful cook.
/// </summary>
public static class CookingStars
{
    /// <summary>How near, in metres, a cook must be to speed up a station or keep its food from burning.</summary>
    public static float CookRange => MasterySettings.CookRange.Value;

    /// <summary>
    /// Rolls the stars a cook adds to a dish.
    /// </summary>
    /// <param name="level">Cooking level.</param>
    /// <returns>Stars.</returns>
    public static int Roll(float level)
    {
        int max = Stars.MaxAt(level, Perks.FineCooking, Perks.WatchfulCook, Perks.MasterChef);
        if (max == 0)
        {
            return 0;
        }

        int stars = Stars.Roll(level, max);
        return Perks.ChefsTouch.ReachedAt(level) ? Mathf.Max(stars, Stars.Roll(level, max)) : stars;
    }

    /// <summary>
    /// True if a cook who keeps food from burning is near.
    /// </summary>
    /// <param name="position">The cooking station.</param>
    /// <returns>True if food should not burn.</returns>
    public static bool WatchfulCookNear(Vector3 position)
    {
        return Perks.WatchfulCook.ReachedAt(SkillLevels.BestNear(position, CookRange, Skills.SkillType.Cooking));
    }
}
