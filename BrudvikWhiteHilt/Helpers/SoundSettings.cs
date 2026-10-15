using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Helpers;

/// <summary>
/// Config for the mod's own voices, ambience, music and effect sounds, section "Sound". Each player sets them for
/// themselves.
/// </summary>
public static class SoundSettings
{
    private const string Section = "Sound";

    /// <summary>Volume of the traders', the hunter's and the skipper's recorded voices; 0 for none.</summary>
    public static ConfigEntry<float> VoiceVolume { get; private set; }

    /// <summary>Volume of the trading posts' ambience of sea, gulls and jetty; 0 for none.</summary>
    public static ConfigEntry<float> AmbienceVolume { get; private set; }

    /// <summary>Volume of the trading posts' music, times the game's music volume; 0 for none.</summary>
    public static ConfigEntry<float> MusicVolume { get; private set; }

    /// <summary>Volume of the plagues' roars and the short sounds of bounties, tracks and charts; 0 for none.</summary>
    public static ConfigEntry<float> EffectsVolume { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        VoiceVolume = WhiteHiltConfig.BindLocal(Section, "VoiceVolume", 1f,
            "Volume of the recorded voices of the trading posts' traders, the hunter and the trading ship's skipper. 0: silent, only the speech bubbles.",
            new AcceptableValueRange<float>(0f, 1f));
        AmbienceVolume = WhiteHiltConfig.BindLocal(Section, "AmbienceVolume", 0.6f,
            "Volume of the trading posts' ambience: waves on the jetty, gulls, creaking boards. 0: none.", new AcceptableValueRange<float>(0f, 1f));
        MusicVolume = WhiteHiltConfig.BindLocal(Section, "MusicVolume", 0.7f,
            "Volume of the music under a trading post's dome, times the game's music volume. 0: none.", new AcceptableValueRange<float>(0f, 1f));
        EffectsVolume = WhiteHiltConfig.BindLocal(Section, "EffectsVolume", 1f,
            "Volume of the plagues' roars and the short sounds of a bounty paid, fresh tracks and a chart read. 0: none.", new AcceptableValueRange<float>(0f, 1f));
    }
}
