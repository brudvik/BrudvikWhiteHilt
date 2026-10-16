using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// The storms' weathers: each is a copy of the game's own nearest weather (the thunderstorm, the snowstorm, the
/// Ashlands' storm) with thicker fog, harder wind and colours of its own, and a darker "gathering" copy for the warning.
/// The game is told to use them where the local player is, after its own forced weathers (dungeons, raids, bosses), so
/// it blends into and out of them as into any other weather.
/// </summary>
public static class StormWeather
{
    private const string Prefix = "WhiteHilt_Storm_";
    private const string GatheringSuffix = "_Gathering";
    private const string Fallback = "ThunderStorm";

    // How each storm looks, and the game's weathers to copy, best first.
    private static readonly Dictionary<StormKind, Look> looks = new()
    {
        [StormKind.Tempest] = new Look(new[] { "ThunderStorm" }, 0.05f, null, 0.75f, 1f, true, false, 0.6f),
        [StormKind.Sea] = new Look(new[] { "ThunderStorm" }, 0.07f, null, 0.85f, 1f, true, true, 0.55f),
        [StormKind.Sand] = new Look(new[] { "Heath clear", "Clear" }, 0.09f, new Color(0.62f, 0.48f, 0.3f), 0.8f, 1f, false, false, 0.7f),
        [StormKind.Blizzard] = new Look(new[] { "SnowStorm" }, 0.09f, new Color(0.85f, 0.88f, 0.92f), 0.9f, 1f, false, true, 0.7f),
        [StormKind.Mist] = new Look(new[] { "Mistlands_thunder", "Mistlands_rain", "ThunderStorm" }, 0.11f, new Color(0.5f, 0.53f, 0.58f), 0.5f, 0.8f, true, true, 0.5f),
        [StormKind.Fire] = new Look(new[] { "Ashlands_storm", "Ashlands_ashrain", "Ashrain", "ThunderStorm" }, 0.06f, new Color(0.55f, 0.18f, 0.06f), 0.7f, 1f, false, false, 0.8f),
        [StormKind.Arctic] = new Look(new[] { "Twilight_SnowStorm", "SnowStorm" }, 0.11f, new Color(0.88f, 0.9f, 0.95f), 1f, 1f, false, true, 0.65f)
    };

    private static EnvMan registeredWith;
    private static float extraFog;

    /// <summary>
    /// The weather a storm forces where the local player is, if any. Called after the game's own forced weathers.
    /// </summary>
    /// <param name="env">The environment manager.</param>
    /// <returns>The weather's name, or null to leave the weather alone.</returns>
    public static string Override(EnvMan env)
    {
        Player player = Player.m_localPlayer;
        if (player == null || !StormState.Visible || !StormSettings.Enabled.Value)
        {
            return null;
        }

        StormProfile profile = StormProfile.Here(player.GetCurrentBiome());
        if (!StormSettings.HasStorms(player.GetCurrentBiome()))
        {
            return null;
        }

        // The eye has the warning's sky: dark and windy, without the rain or snow.
        Register(env);
        string name = Name(profile.Kind, StormState.Coming || StormState.InEye);
        return env.GetEnv(name) != null ? name : null;
    }

    /// <summary>
    /// The name of a storm's weather.
    /// </summary>
    /// <param name="kind">The storm.</param>
    /// <param name="gathering">The warning's darker sky instead of the storm itself.</param>
    /// <returns>The name.</returns>
    public static string Name(StormKind kind, bool gathering) => $"{Prefix}{kind}{(gathering ? GatheringSuffix : string.Empty)}";

    /// <summary>
    /// Sets how much thicker the fog is for the local player, as their view closes in.
    /// </summary>
    /// <param name="vision">How far the view has closed in, 0 to 1.</param>
    public static void SetVision(float vision)
    {
        extraFog = Mathf.Clamp01(vision);
    }

    /// <summary>
    /// Thickens the fog the game has just set. Called after <see cref="EnvMan.SetEnv"/>.
    /// </summary>
    public static void ApplyFog()
    {
        if (extraFog > 0.001f)
        {
            RenderSettings.fogDensity *= 1f + extraFog * 2.5f;
        }
    }

    // Adds the storms' weathers to the game's list, once per environment manager.
    private static void Register(EnvMan env)
    {
        if (registeredWith == env)
        {
            return;
        }

        registeredWith = env;
        foreach (KeyValuePair<StormKind, Look> pair in looks)
        {
            EnvSetup source = pair.Value.Sources.Select(env.GetEnv).FirstOrDefault(found => found != null) ?? env.GetEnv(Fallback);
            if (source == null)
            {
                Jotunn.Logger.LogWarning($"Storms: no weather to copy for the {pair.Key} storm");
                continue;
            }

            env.AppendEnvironment(Storm(source, pair.Key, pair.Value));
            env.AppendEnvironment(Gathering(source, pair.Key, pair.Value));
            Jotunn.Logger.LogInfo($"Storms: the {pair.Key} storm is a copy of {source.m_name}");
        }
    }

    private static EnvSetup Storm(EnvSetup source, StormKind kind, Look look)
    {
        EnvSetup storm = source.Clone();
        storm.m_name = Name(kind, false);
        storm.m_default = false;
        storm.m_isWet = look.Wet;
        storm.m_isCold = look.Cold;
        storm.m_isColdAtNight = true;
        storm.m_isFreezing = false;
        storm.m_isFreezingAtNight = false;
        storm.m_windMin = look.WindMin;
        storm.m_windMax = look.WindMax;
        storm.m_fogDensityDay = Mathf.Max(source.m_fogDensityDay, look.Fog);
        storm.m_fogDensityMorning = Mathf.Max(source.m_fogDensityMorning, look.Fog);
        storm.m_fogDensityEvening = Mathf.Max(source.m_fogDensityEvening, look.Fog);
        storm.m_fogDensityNight = Mathf.Max(source.m_fogDensityNight, look.Fog);
        storm.m_lightIntensityDay = source.m_lightIntensityDay * look.Light;
        if (look.Fog > 0f && look.FogColor.HasValue)
        {
            Color fog = look.FogColor.Value;
            storm.m_fogColorDay = storm.m_fogColorMorning = storm.m_fogColorEvening = fog;
            storm.m_fogColorSunDay = storm.m_fogColorSunMorning = storm.m_fogColorSunEvening = Color.Lerp(fog, Color.white, 0.15f);
            storm.m_fogColorNight = storm.m_fogColorSunNight = fog * 0.25f;
            storm.m_ambColorDay = Color.Lerp(source.m_ambColorDay, fog, 0.5f);
        }

        return storm;
    }

    // The warning's sky: darker, windier and foggier than usual, without the storm's rain or snow.
    private static EnvSetup Gathering(EnvSetup source, StormKind kind, Look look)
    {
        EnvSetup gathering = source.Clone();
        gathering.m_name = Name(kind, true);
        gathering.m_default = false;
        gathering.m_psystems = new GameObject[0];
        gathering.m_envObject = null;
        gathering.m_isWet = false;
        gathering.m_isCold = false;
        gathering.m_isFreezing = false;
        gathering.m_isFreezingAtNight = false;
        gathering.m_windMin = look.WindMin * 0.6f;
        gathering.m_windMax = look.WindMax * 0.8f;
        gathering.m_fogDensityDay = Mathf.Lerp(source.m_fogDensityDay, look.Fog, 0.35f);
        gathering.m_fogDensityMorning = Mathf.Lerp(source.m_fogDensityMorning, look.Fog, 0.35f);
        gathering.m_fogDensityEvening = Mathf.Lerp(source.m_fogDensityEvening, look.Fog, 0.35f);
        gathering.m_fogDensityNight = Mathf.Lerp(source.m_fogDensityNight, look.Fog, 0.35f);
        gathering.m_lightIntensityDay = source.m_lightIntensityDay * Mathf.Lerp(1f, look.Light, 0.6f);
        return gathering;
    }

    private sealed class Look
    {
        public Look(string[] sources, float fog, Color? fogColor, float windMin, float windMax, bool wet, bool cold, float light)
        {
            Sources = sources;
            Fog = fog;
            FogColor = fogColor;
            WindMin = windMin;
            WindMax = windMax;
            Wet = wet;
            Cold = cold;
            Light = light;
        }

        public string[] Sources { get; }

        public float Fog { get; }

        public Color? FogColor { get; }

        public float WindMin { get; }

        public float WindMax { get; }

        public bool Wet { get; }

        public bool Cold { get; }

        public float Light { get; }
    }
}
