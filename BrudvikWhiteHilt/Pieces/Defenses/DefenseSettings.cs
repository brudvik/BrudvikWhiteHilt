using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Pieces.Defenses;

/// <summary>
/// Config for the palisade defences, section "Defenses". Server-synced.
/// </summary>
public static class DefenseSettings
{
    private const string Section = "Defenses";

    private static ConfigEntry<float> healthMultiplier;

    /// <summary>
    /// Multiplier on the built-in health of every defence piece.
    /// </summary>
    public static float HealthMultiplier => healthMultiplier != null ? healthMultiplier.Value : 1f;

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        healthMultiplier = WhiteHiltConfig.BindAdminOnly(Section, "HealthMultiplier", 1f,
            "Multiplier on the health of the palisade, rampart, gatehouse, watchtowers and cheval de frise. " +
            "Placed pieces keep the damage they have taken; a repair brings them to the new full health.",
            new AcceptableValueRange<float>(0.1f, 10f));
        healthMultiplier.SettingChanged += (_, _) => DefensePieceBase.ApplyHealth();
    }

    /// <summary>
    /// A defence piece's health with the multiplier applied.
    /// </summary>
    /// <param name="health">The built-in health.</param>
    /// <returns>The health to use.</returns>
    public static float Scale(float health)
    {
        return health * HealthMultiplier;
    }
}
