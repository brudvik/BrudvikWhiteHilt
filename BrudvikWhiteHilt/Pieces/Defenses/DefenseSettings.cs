using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Pieces.Defenses;

/// <summary>
/// Config for the palisade defences, section "Defences". Server-synced.
/// </summary>
public static class DefenseSettings
{
    private const string Section = "Defences";

    private static ConfigEntry<float> healthMultiplier;
    private static ConfigEntry<float> drawbridgeLinkRange;

    /// <summary>
    /// Multiplier on the built-in health of every defence piece.
    /// </summary>
    public static float HealthMultiplier => healthMultiplier != null ? healthMultiplier.Value : 1f;

    /// <summary>
    /// How far a drawbridge looks for the gate it follows, in metres; 0 turns following off.
    /// </summary>
    public static float DrawbridgeLinkRange => drawbridgeLinkRange != null ? drawbridgeLinkRange.Value : 0f;

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
        drawbridgeLinkRange = WhiteHiltConfig.BindAdminOnly(Section, "DrawbridgeLinkRange", 12f,
            "A drawbridge follows the nearest gate within this many metres: it is lowered when the gate opens and raised when it closes. 0 turns it off.",
            new AcceptableValueRange<float>(0f, 30f));
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
