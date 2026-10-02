using BrudvikWhiteHilt.Clock;
using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Navigation.Weather;

/// <summary>
/// Foretells the weather where the player is. The game draws each weather period from the biome's weathers with the
/// period's number as the random seed, and the wind from the time, so the coming periods are worked out the same way.
/// Weather forced by events, bosses or special places cannot be foretold.
/// </summary>
public static class WeatherForecast
{
    // Wind intensity (0 to 1) up to which each wind word is used.
    private static readonly float[] windSteps = { 0.15f, 0.35f, 0.6f, 0.85f };
    private static readonly string[] windWords = { "calm", "light", "breeze", "strong", "gale" };

    /// <summary>
    /// One weather period.
    /// </summary>
    public struct Period
    {
        /// <summary>The weather.</summary>
        public EnvSetup Env;

        /// <summary>Game seconds until it begins; 0 for the current period.</summary>
        public double StartsIn;

        /// <summary>Degrees from north the wind blows from.</summary>
        public float WindFrom;

        /// <summary>Wind intensity, 0 to 1.</summary>
        public float Wind;

        /// <summary>Whether it begins at night.</summary>
        public bool Night;
    }

    /// <summary>
    /// The English texts. Call from the plugin's Awake.
    /// </summary>
    public static void RegisterEnglish()
    {
        Translations.AddEnglish("whitehilt_weather_sun", "Clear");
        Translations.AddEnglish("whitehilt_weather_moon", "Clear");
        Translations.AddEnglish("whitehilt_weather_cloud", "Cloudy");
        Translations.AddEnglish("whitehilt_weather_rain", "Rain");
        Translations.AddEnglish("whitehilt_weather_fog", "Fog");
        Translations.AddEnglish("whitehilt_weather_storm", "Storm");
        Translations.AddEnglish("whitehilt_weather_snow", "Snow");
        Translations.AddEnglish("whitehilt_weather_ash", "Ash rain");
        Translations.AddEnglish("whitehilt_wind_calm", "calm");
        Translations.AddEnglish("whitehilt_wind_light", "light wind from {0}");
        Translations.AddEnglish("whitehilt_wind_breeze", "fresh breeze from {0}");
        Translations.AddEnglish("whitehilt_wind_strong", "strong wind from {0}");
        Translations.AddEnglish("whitehilt_wind_gale", "gale from {0}");
    }

    /// <summary>
    /// Whether the weather where the player is now is forced, by an event, a boss or a special place.
    /// </summary>
    /// <returns>True when the forecast does not hold here now.</returns>
    public static bool Overridden()
    {
        return EnvMan.instance != null && !string.IsNullOrEmpty(EnvMan.instance.GetEnvironmentOverride());
    }

    /// <summary>
    /// The current period and the ones after it, for the biome where the camera is.
    /// </summary>
    /// <param name="ahead">How many periods after the current one.</param>
    /// <returns>The periods, or an empty list when the game has no weather yet.</returns>
    public static List<Period> Foretell(int ahead)
    {
        List<Period> periods = new();
        EnvMan env = EnvMan.instance;
        Camera camera = Utils.GetMainCamera();
        if (env == null || ZNet.instance == null || WorldGenerator.instance == null || camera == null)
        {
            return periods;
        }

        double now = ZNet.instance.GetTimeSeconds();
        long current = (long)now / env.m_environmentDuration;
        BiomeSector sector = env.GetBiome();
        Vector3 position = camera.transform.position;
        for (int i = 0; i <= ahead; i++)
        {
            long period = current + i;
            double start = period * (double)env.m_environmentDuration;
            EnvSetup setup = Select(env, period, sector, position);
            if (setup == null)
            {
                continue;
            }

            // The wind of a coming period is taken at its middle; the current one is the wind that blows now.
            Vector3 wind;
            float intensity;
            if (i == 0)
            {
                wind = env.GetWindDir();
                intensity = env.GetWindIntensity();
            }
            else
            {
                Wind(env, setup, (long)(start + env.m_environmentDuration / 2.0), out wind, out intensity);
            }

            double dayFraction = (start % env.m_dayLengthSec) / env.m_dayLengthSec;
            periods.Add(new Period
            {
                Env = setup,
                StartsIn = i == 0 ? 0.0 : start - now,
                WindFrom = Mathf.Repeat(Mathf.Atan2(-wind.x, -wind.z) * Mathf.Rad2Deg, 360f),
                Wind = intensity,
                Night = i == 0 ? EnvMan.IsNight() : dayFraction < 0.15 || dayFraction > 0.85
            });
        }

        return periods;
    }

    /// <summary>
    /// Whether a weather is a storm: thunder or a snowstorm.
    /// </summary>
    /// <param name="setup">The weather.</param>
    /// <returns>True for a storm.</returns>
    public static bool IsStorm(EnvSetup setup)
    {
        string name = setup?.m_name?.ToLowerInvariant() ?? string.Empty;
        return name.Contains("thunder") || name.Contains("storm");
    }

    /// <summary>
    /// The weather's name in the player's language.
    /// </summary>
    /// <param name="period">The period.</param>
    /// <returns>E.g. "Fog".</returns>
    public static string Describe(Period period)
    {
        return Translations.Word($"whitehilt_weather_{GameClock.WeatherKey(period.Env, period.Night).ToLowerInvariant()}");
    }

    /// <summary>
    /// The wind in words, e.g. "light wind from SW".
    /// </summary>
    /// <param name="period">The period.</param>
    /// <returns>The text.</returns>
    public static string DescribeWind(Period period)
    {
        int step = 0;
        while (step < windSteps.Length && period.Wind > windSteps[step])
        {
            step++;
        }

        return string.Format(Translations.Word($"whitehilt_wind_{windWords[step]}"), Pieces.Ships.ShipHud.Compass(period.WindFrom));
    }

    // The same draw as EnvMan.UpdateEnvironment, with its random state put back afterwards.
    private static EnvSetup Select(EnvMan env, long period, BiomeSector sector, Vector3 position)
    {
        Random.State state = Random.state;
        try
        {
            Random.InitState((int)period);
            List<EnvEntry> available = env.GetAvailableEnvironments(sector);
            if (available == null || available.Count == 0)
            {
                return null;
            }

            EnvSetup setup = env.SelectWeightedEnvironment(available);
            // The game checks the deep north with the height as second coordinate; done the same to foretell the same.
            bool ashlands = WorldGenerator.IsAshlands(position.x, position.z);
            bool deepNorth = WorldGenerator.IsDeepnorth(position.x, position.y);
            foreach (EnvEntry entry in available)
            {
                if ((entry.m_ashlandsOverride && ashlands) || (entry.m_deepnorthOverride && deepNorth))
                {
                    setup = entry.m_env;
                }
            }

            return setup;
        }
        finally
        {
            Random.state = state;
        }
    }

    // The same octaves as EnvMan.UpdateWind.
    private static void Wind(EnvMan env, EnvSetup setup, long time, out Vector3 direction, out float intensity)
    {
        Random.State state = Random.state;
        float angle = 0f;
        float strength = 0.5f;
        env.AddWindOctave(time, 1, ref angle, ref strength);
        env.AddWindOctave(time, 2, ref angle, ref strength);
        env.AddWindOctave(time, 4, ref angle, ref strength);
        env.AddWindOctave(time, 8, ref angle, ref strength);
        Random.state = state;
        direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
        intensity = Mathf.Clamp(Mathf.Lerp(setup.m_windMin, setup.m_windMax, strength), 0.05f, 1f);
    }
}
