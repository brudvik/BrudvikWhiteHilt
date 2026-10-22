using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Shelter;

/// <summary>
/// Config for how the weather looks indoors, section "IndoorWeather". All of it is the player's own.
/// </summary>
public static class IndoorWeatherSettings
{
    private const string Section = "IndoorWeather";

    /// <summary>Whether fog, snow and rain stay outside a house.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>Fog density at most inside a closed house.</summary>
    public static ConfigEntry<float> FogDensity { get; private set; }

    /// <summary>How much snow and rain is made round the player inside a closed house, 0 to 1.</summary>
    public static ConfigEntry<float> ParticleAmount { get; private set; }

    /// <summary>How far round the player, in metres, snow and rain under a roof are taken away.</summary>
    public static ConfigEntry<float> ClearReach { get; private set; }

    /// <summary>Whether rain and snow the game lets fall through roofs land on them instead.</summary>
    public static ConfigEntry<bool> RoofCollision { get; private set; }

    /// <summary>Seconds the fog and the snow take to change when going in or out.</summary>
    public static ConfigEntry<float> FadeSeconds { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindLocal(Section, "IndoorWeather", true,
            "Fog, snow and rain stay outside a house: the fog thins under a roof, and no snow or rain falls inside.");
        FogDensity = WhiteHiltConfig.BindLocal(Section, "IndoorFogDensity", 0.004f,
            "Fog density at most inside a closed house. Clear weather is about 0.003, a snowstorm 0.05.",
            new AcceptableValueRange<float>(0f, 0.05f));
        ParticleAmount = WhiteHiltConfig.BindLocal(Section, "IndoorParticleAmount", 0.3f,
            "How much snow and rain is made round you inside a closed house, 0 to 1. What comes in under the roof is taken away anyway; less is made so less flickers.",
            new AcceptableValueRange<float>(0f, 1f));
        ClearReach = WhiteHiltConfig.BindLocal(Section, "ClearReach", 12f,
            "How far round you, in metres, snow and rain under a roof are taken away.", new AcceptableValueRange<float>(4f, 30f));
        RoofCollision = WhiteHiltConfig.BindLocal(Section, "RoofCollision", true,
            "Rain and snow the game lets fall through roofs land on them instead, as its other snow does.");
        FadeSeconds = WhiteHiltConfig.BindLocal(Section, "FadeSeconds", 1.5f, "Seconds the fog and the snow take to change when going in or out.");
    }
}
