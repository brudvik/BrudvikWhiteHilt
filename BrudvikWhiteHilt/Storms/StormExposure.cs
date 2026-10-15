using UnityEngine;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// Where someone is when a storm rages, from worst to best.
/// </summary>
public enum StormCover
{
    /// <summary>Out in the open.</summary>
    Open,

    /// <summary>In the lee of a rock, a cliff or a wall: exposure climbs at half the pace.</summary>
    Lee,

    /// <summary>Among close trees, where the forest storm shelters a little.</summary>
    Forest,

    /// <summary>Under a roof without a fire: exposure holds.</summary>
    Roof,

    /// <summary>Under a roof by a burning fire: exposure falls.</summary>
    RoofAndFire,

    /// <summary>In a dungeon or cave: exposure falls.</summary>
    Interior
}

/// <summary>
/// The exposure meter, from 0 to 100: it climbs out in a storm and falls in shelter, and every 20 points makes it worse.
/// No clothes, food or potion changes it.
/// </summary>
public static class StormExposure
{
    /// <summary>The top of the meter.</summary>
    public const float Max = 100f;

    /// <summary>From here stamina comes back at half the pace.</summary>
    public const float Tired = 20f;

    /// <summary>From here stamina comes back slowly.</summary>
    public const float Weary = 40f;

    /// <summary>From here you freeze (or burn) and the view closes in.</summary>
    public const float Freezing = 60f;

    /// <summary>From here health is lost, unless the storm's own threshold is lower.</summary>
    public const float Dying = 80f;

    private const float LeeShare = 0.5f;
    private const float ForestShare = 0.75f;
    private const float LeastDamageShare = 0.005f;
    private const float MostDamageShare = 0.04f;

    /// <summary>
    /// Works out where someone is from what the game knows of their surroundings.
    /// </summary>
    /// <param name="profile">The storm where they are.</param>
    /// <param name="interior">In a dungeon or cave.</param>
    /// <param name="roof">Under a roof that shelters, as the game counts it.</param>
    /// <param name="fire">Warmed by a fire.</param>
    /// <param name="stoneRoof">The roof is stone, not wood.</param>
    /// <param name="lee">In the lee of something solid.</param>
    /// <param name="forest">Among close trees.</param>
    /// <returns>The cover.</returns>
    public static StormCover Resolve(StormProfile profile, bool interior, bool roof, bool fire, bool stoneRoof, bool lee, bool forest)
    {
        if (interior)
        {
            return StormCover.Interior;
        }

        if (roof && (!profile.NeedsStoneRoof || stoneRoof))
        {
            return fire ? StormCover.RoofAndFire : StormCover.Roof;
        }

        if (lee)
        {
            return StormCover.Lee;
        }

        return forest && profile.ForestShelters ? StormCover.Forest : StormCover.Open;
    }

    /// <summary>
    /// The meter after some time.
    /// </summary>
    /// <param name="exposure">The meter now.</param>
    /// <param name="seconds">Seconds that passed.</param>
    /// <param name="raging">Whether a storm rages where they are.</param>
    /// <param name="profile">The storm where they are.</param>
    /// <param name="cover">Where they are.</param>
    /// <param name="minutesToFull">Minutes out in the open, at the profile's pace 1, from 0 to full.</param>
    /// <param name="minutesToRecover">Minutes in shelter from full to 0.</param>
    /// <returns>The new meter, 0 to <see cref="Max"/>.</returns>
    public static float Step(float exposure, float seconds, bool raging, StormProfile profile, StormCover cover, float minutesToFull, float minutesToRecover)
    {
        float rise = Max / Mathf.Max(0.1f, minutesToFull * 60f) * profile.Rate;
        float recover = Max / Mathf.Max(0.1f, minutesToRecover * 60f);
        float change;
        if (!raging || profile.Kind == StormKind.None)
        {
            change = -recover;
        }
        else
        {
            change = cover switch
            {
                StormCover.Open => rise,
                StormCover.Lee => rise * LeeShare,
                StormCover.Forest => rise * ForestShare,
                StormCover.Roof => 0f,
                _ => -recover
            };
        }

        return Mathf.Clamp(exposure + change * seconds, 0f, Max);
    }

    /// <summary>
    /// Which step of the meter it is at: 0 below <see cref="Tired"/>, up to 4 from <see cref="Dying"/>.
    /// </summary>
    /// <param name="exposure">The meter.</param>
    /// <returns>The stage.</returns>
    public static int Stage(float exposure)
    {
        return exposure >= Dying ? 4 : exposure >= Freezing ? 3 : exposure >= Weary ? 2 : exposure >= Tired ? 1 : 0;
    }

    /// <summary>
    /// The share of the normal stamina regeneration that is left.
    /// </summary>
    /// <param name="exposure">The meter.</param>
    /// <returns>1 when unexposed, down to 0.1.</returns>
    public static float StaminaRegen(float exposure)
    {
        return Stage(exposure) switch
        {
            0 => 1f,
            1 => 0.5f,
            2 => 0.25f,
            _ => 0.1f
        };
    }

    /// <summary>
    /// How far the view has closed in, 0 to 1.
    /// </summary>
    /// <param name="exposure">The meter.</param>
    /// <param name="profile">The storm where they are.</param>
    /// <param name="outside">Whether they are out in a storm that rages.</param>
    /// <returns>The share.</returns>
    public static float Vision(float exposure, StormProfile profile, bool outside)
    {
        float fromExposure = Mathf.Clamp01((exposure - Freezing) / (Max - Freezing));
        return Mathf.Max(outside ? profile.BaseVision : 0f, fromExposure);
    }

    /// <summary>
    /// The share of maximum health lost per second.
    /// </summary>
    /// <param name="exposure">The meter.</param>
    /// <param name="profile">The storm where they are.</param>
    /// <returns>0 until the storm's threshold, then from half a percent to 4 percent at the top.</returns>
    public static float DamageShare(float exposure, StormProfile profile)
    {
        if (!profile.Hurts || exposure < profile.HurtFrom)
        {
            return 0f;
        }

        float t = Mathf.Clamp01((exposure - profile.HurtFrom) / Mathf.Max(1f, Max - profile.HurtFrom));
        return Mathf.Lerp(LeastDamageShare, MostDamageShare, t);
    }

    /// <summary>
    /// Whether the meter is high enough to freeze (or burn, in the fire storm).
    /// </summary>
    /// <param name="exposure">The meter.</param>
    /// <param name="profile">The storm where they are.</param>
    /// <returns>True when freezing or burning.</returns>
    public static bool Suffering(float exposure, StormProfile profile)
    {
        return profile.Kind != StormKind.Sand && exposure >= Mathf.Min(Freezing, profile.HurtFrom);
    }
}
