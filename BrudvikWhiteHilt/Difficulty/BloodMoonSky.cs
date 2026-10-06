using UnityEngine;

namespace BrudvikWhiteHilt.Difficulty;

/// <summary>
/// Turns the night red while a blood moon is up: moonlight, fog, ambient light and the aurora.
/// Runs after the game has set the environment each frame, so it only tints what the game computed.
/// </summary>
public static class BloodMoonSky
{
    private const float FadeSeconds = 20f;

    private static readonly int sunColor = Shader.PropertyToID("_SunColor");
    private static readonly int sunFogColor = Shader.PropertyToID("_SunFogColor");
    private static readonly int ambientColor = Shader.PropertyToID("_AmbientColor");
    private static readonly int auroraStrength = Shader.PropertyToID("_AuroraStrength");
    private static readonly int auroraCurrent = Shader.PropertyToID("_AuroraGradientCurrent");
    private static readonly int auroraBlend = Shader.PropertyToID("_AuroraGradientBlend");

    private static readonly Color moonLight = new(1f, 0.16f, 0.08f);
    private static readonly Color fog = new(0.24f, 0.02f, 0.02f);
    private static readonly Color ambient = new(0.32f, 0.06f, 0.05f);

    private static float blend;
    private static bool auroraTinted;
    private static Texture2D auroraGradient;

    /// <summary>
    /// Tints the environment the game has just set.
    /// </summary>
    /// <param name="env">The environment manager.</param>
    /// <param name="nightInt">How much of night it is, from 0 to 1.</param>
    /// <param name="dt">Seconds since the last frame.</param>
    public static void Apply(EnvMan env, float nightInt, float dt)
    {
        bool on = DifficultyState.BloodMoon && DifficultySettings.RedSky.Value && DifficultySettings.Enabled.Value;
        blend = Mathf.MoveTowards(blend, on ? 1f : 0f, dt / FadeSeconds);
        float amount = blend * nightInt;
        if (amount <= 0.001f || env.m_dirLight == null)
        {
            RestoreAurora(env);
            return;
        }

        Light light = env.m_dirLight;
        Color red = moonLight * Mathf.Max(light.color.maxColorComponent, 0.35f);
        light.color = Color.Lerp(light.color, red, amount * 0.85f);
        RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, fog, amount * 0.75f);
        RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, ambient, amount * 0.6f);
        Shader.SetGlobalColor(sunColor, light.color * light.intensity);
        Shader.SetGlobalColor(sunFogColor, Color.Lerp(Shader.GetGlobalColor(sunFogColor), fog, amount * 0.75f));
        Shader.SetGlobalColor(ambientColor, RenderSettings.ambientLight);

        Shader.SetGlobalTexture(auroraCurrent, AuroraGradient());
        Shader.SetGlobalFloat(auroraBlend, 0f);
        Shader.SetGlobalFloat(auroraStrength, Mathf.Max(Shader.GetGlobalFloat(auroraStrength), amount * 0.8f));
        auroraTinted = true;
    }

    // The game sets the aurora gradient only when the environment changes, so it is put back by hand.
    private static void RestoreAurora(EnvMan env)
    {
        if (!auroraTinted)
        {
            return;
        }

        auroraTinted = false;
        Texture2D original = env.GetCurrentEnvironment()?.m_auroraGradientTexture;
        if (original != null)
        {
            Shader.SetGlobalTexture(auroraCurrent, original);
        }
    }

    // The red gradient the aurora takes during a blood moon, made once.
    private static Texture2D AuroraGradient()
    {
        if (auroraGradient != null)
        {
            return auroraGradient;
        }

        const int width = 64;
        auroraGradient = new Texture2D(width, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "whitehilt_bloodmoon_aurora" };
        for (int x = 0; x < width; x++)
        {
            float t = x / (width - 1f);
            float glow = Mathf.Sin(t * Mathf.PI);
            auroraGradient.SetPixel(x, 0, new Color(0.9f * glow + 0.1f, 0.05f * glow, 0.03f * glow, glow));
        }

        auroraGradient.Apply();
        return auroraGradient;
    }
}
