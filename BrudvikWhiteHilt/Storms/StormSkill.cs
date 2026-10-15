using BrudvikWhiteHilt.Clock;
using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// The Weathering skill (Herdet): it rises with every second out in a raging storm and with every storm lived through
/// outside, and slows the exposure meter, by 30% at level 100.
/// </summary>
public static class StormSkill
{
    private const string Identifier = "com.jotunn.BrudvikWhiteHilt.weathering";
    private const string NameKey = "whitehilt_skill_weathering";

    // Skill points per second out in the storm, and for a storm lived through with the meter over Weary.
    private const float PerSecond = 0.2f;
    private const float PerStorm = 10f;

    private static SkillConfig config;

    /// <summary>The skill type, valid after <see cref="Register"/>.</summary>
    public static Skills.SkillType Type { get; private set; }

    /// <summary>
    /// Registers the skill and its English text. Call from the plugin's Awake.
    /// </summary>
    public static void Register()
    {
        Translations.AddEnglishNameAndDescription(NameKey, "Weathering",
            "Living through the great storms out in the open. The storm wears you down more slowly: 30% at level 100.");
        config = new SkillConfig
        {
            Identifier = Identifier,
            Name = Translations.Token(NameKey),
            Description = Translations.Token($"{NameKey}_description"),
            IncreaseStep = 1f
        };
        Type = SkillManager.Instance.AddSkill(config);
    }

    /// <summary>
    /// Gives the skill the clock's storm icon. Must happen before the first player spawns.
    /// </summary>
    public static void SetIcon()
    {
        if (config != null && config.Icon == null)
        {
            config.Icon = GameClock.LoadIcon("Storm");
        }
    }

    /// <summary>
    /// How much more slowly the meter climbs at a skill level.
    /// </summary>
    /// <param name="skillFactor">The skill, 0 to 1 (level 100).</param>
    /// <param name="reductionAt100">The share taken off at level 100.</param>
    /// <returns>The share of the pace left, 1 at level 0.</returns>
    public static float Pace(float skillFactor, float reductionAt100)
    {
        return 1f - Mathf.Clamp01(reductionAt100) * Mathf.Clamp01(skillFactor);
    }

    /// <summary>
    /// The skill out in the storm for some seconds.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="seconds">Seconds outside.</param>
    public static void Weather(Player player, float seconds)
    {
        player.RaiseSkill(Type, PerSecond * seconds);
    }

    /// <summary>
    /// The skill for a storm lived through out in it.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Survived(Player player)
    {
        player.RaiseSkill(Type, PerStorm);
    }
}
