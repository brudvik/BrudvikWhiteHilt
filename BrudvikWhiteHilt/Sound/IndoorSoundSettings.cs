using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Sound;

/// <summary>
/// Config for how weather sounds indoors, section "Sound". All of it is the player's own.
/// </summary>
public static class IndoorSoundSettings
{
    private const string Section = "Sound";

    /// <summary>Whether wind, rain and sea are quieter and muffled under a roof.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>How loud the wind is inside a closed house, 0 to 1.</summary>
    public static ConfigEntry<float> WindVolume { get; private set; }

    /// <summary>How loud rain, sea, thunder and the weather's ambience are inside a closed house, 0 to 1.</summary>
    public static ConfigEntry<float> RainVolume { get; private set; }

    /// <summary>Whether the weather sounds muffled, as through walls, indoors.</summary>
    public static ConfigEntry<bool> Muffle { get; private set; }

    /// <summary>Highest frequency let through inside a closed house, in Hz.</summary>
    public static ConfigEntry<float> MuffleCutoffHz { get; private set; }

    /// <summary>Seconds the sound takes to change when going in or out.</summary>
    public static ConfigEntry<float> FadeSeconds { get; private set; }

    /// <summary>Whether the tent on the White Hilt Ship counts as partly indoors.</summary>
    public static ConfigEntry<bool> ShipTentCounts { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindLocal(Section, "IndoorSound", true,
            "Wind, rain, sea and thunder are quieter and muffled under a roof: a little under an open roof, most in a closed house.");
        WindVolume = WhiteHiltConfig.BindLocal(Section, "IndoorWindVolume", 0.25f, "How loud the wind is inside a closed house, 0 to 1.");
        RainVolume = WhiteHiltConfig.BindLocal(Section, "IndoorRainVolume", 0.3f,
            "How loud rain, sea, thunder and the weather's ambience are inside a closed house, 0 to 1.");
        Muffle = WhiteHiltConfig.BindLocal(Section, "IndoorMuffle", true, "Weather sounds muffled indoors, as heard through walls.");
        MuffleCutoffHz = WhiteHiltConfig.BindLocal(Section, "MuffleCutoffHz", 1200f,
            "Highest frequency let through inside a closed house, in Hz. Lower is more muffled.");
        FadeSeconds = WhiteHiltConfig.BindLocal(Section, "FadeSeconds", 1.5f, "Seconds the sound takes to change when going in or out.");
        ShipTentCounts = WhiteHiltConfig.BindLocal(Section, "ShipTentCounts", true,
            "Under the tent on the White Hilt Ship the weather is a little quieter, less than in a house.");
    }
}
