using UnityEngine;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// The storm remembers who braves it. Each storm a player lives through out in it (the meter past
/// <see cref="StormExposure.Weary"/>) makes the meter climb faster for them the next time and leaves them more finds;
/// each storm they sit out in shelter (the meter never past <see cref="StormExposure.Tired"/>) it forgets them a little.
/// Kept with the character.
/// </summary>
public static class StormGrudge
{
    /// <summary>The most storms it remembers.</summary>
    public const int Max = 10;

    private const string Key = "WhiteHilt_StormGrudge";

    /// <summary>
    /// How many storms the storm remembers of a player.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>0 to <see cref="Max"/>.</returns>
    public static int Of(Player player)
    {
        return player != null && player.m_customData.TryGetValue(Key, out string text) && int.TryParse(text, out int grudge)
            ? Mathf.Clamp(grudge, 0, Max)
            : 0;
    }

    /// <summary>
    /// Remembers a storm that has passed.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="peakExposure">The highest the meter climbed in it.</param>
    /// <returns>How the grudge changed: 1, 0 or -1.</returns>
    public static int Remember(Player player, float peakExposure)
    {
        int was = Of(player);
        int now = After(was, peakExposure);
        if (now == was)
        {
            return 0;
        }

        if (now == 0)
        {
            player.m_customData.Remove(Key);
        }
        else
        {
            player.m_customData[Key] = now.ToString();
        }

        return now > was ? 1 : -1;
    }

    /// <summary>
    /// The grudge after a storm.
    /// </summary>
    /// <param name="grudge">The grudge before.</param>
    /// <param name="peakExposure">The highest the meter climbed in the storm.</param>
    /// <returns>One more after braving it, one less after sitting it out, else the same.</returns>
    public static int After(int grudge, float peakExposure)
    {
        if (peakExposure >= StormExposure.Weary)
        {
            return Mathf.Min(Max, grudge + 1);
        }

        return peakExposure < StormExposure.Tired ? Mathf.Max(0, grudge - 1) : grudge;
    }

    /// <summary>
    /// How much faster the meter climbs for a grudge.
    /// </summary>
    /// <param name="grudge">The grudge.</param>
    /// <param name="step">The share added per storm remembered.</param>
    /// <returns>1 with none.</returns>
    public static float Pace(int grudge, float step)
    {
        return 1f + Mathf.Clamp(grudge, 0, Max) * Mathf.Max(0f, step);
    }

    /// <summary>
    /// Finds the storm leaves on top for a grudge: one for every two storms it remembers.
    /// </summary>
    /// <param name="grudge">The grudge.</param>
    /// <returns>The extra finds.</returns>
    public static int ExtraFinds(int grudge)
    {
        return Mathf.Clamp(grudge, 0, Max) / 2;
    }
}
