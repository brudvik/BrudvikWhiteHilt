using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Ammunition;

/// <summary>
/// Tones down the game's burning effects (<c>vfx_Burning</c>, its blue and green kinds and <c>vfx_UndeadBurn</c>), the
/// flames a creature or player gets when set on fire. The flare, flames, sparks and light are strong enough to hide a
/// burning monster, which White Hilt arrows set on fire often. Each player sets it for themselves; it applies to all
/// burning, as the effect does not know what lit it.
/// </summary>
/// <remarks>
/// The big flames of <c>vfx_Burning</c> ("flames (1)", 100 a second) use the gradient-mapped particle shader, which
/// takes its colours from its own gradient, adds them to the screen and reads neither the particles' colour nor their
/// alpha. Fading them did nothing, and halving their number still left so many overlapping that the sum stayed white.
/// Such flames are toned down by number (the square of the strength) and size (the strength) instead.
/// </remarks>
public sealed class BurningFlames : MonoBehaviour
{
    private const string Section = "Gear.Ammunition";
    private const string GradientShader = "Gradient Mapped";

    // Particles that take their colour shrink by at most half, so faint flames still show where the creature burns.
    private const float MinimumSize = 0.5f;

    private static readonly string[] effectPrefabs = { "vfx_Burning", "vfx_Burning_blue", "vfx_Burning_green", "vfx_UndeadBurn" };

    private static ConfigEntry<float> strength;

    /// <summary>
    /// Binds the setting and adds the dimmer to the burning effect whenever the game's prefabs are registered. Call
    /// from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        strength = WhiteHiltConfig.BindLocal(Section, "BurningFlames", 0.5f,
            "How strong the flames, sparks and light of anything burning are: 1 as in the game, 0 none. Fire damage and " +
            "sound are unchanged. Applies to all burning, from White Hilt arrows and anything else.",
            new AcceptableValueRange<float>(0f, 1f));
        PrefabManager.OnPrefabsRegistered += AddToEffect;
    }

    // The effect is a network object every machine creates for itself, so the dimmer on the prefab reaches every copy.
    private static void AddToEffect()
    {
        foreach (string name in effectPrefabs)
        {
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(name) : null;
            if (prefab == null)
            {
                Jotunn.Logger.LogWarning($"{name} not found; its burning flames are not toned down.");
                continue;
            }

            if (prefab.GetComponent<BurningFlames>() == null)
            {
                prefab.AddComponent<BurningFlames>();
            }
        }
    }

    // Before the first particles are emitted: fewer, smaller and fainter flames, or none at strength 0.
    private void Awake()
    {
        float scale = Strength();
        if (scale >= 1f)
        {
            return;
        }

        foreach (ParticleSystem particles in GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.EmissionModule emission = particles.emission;
            if (scale <= 0f)
            {
                emission.enabled = false;
                continue;
            }

            // Flames that ignore their colour can only be made fewer and smaller.
            bool colourless = IgnoresColour(particles);
            float count = colourless ? scale * scale : scale;
            emission.rateOverTimeMultiplier *= count;
            emission.rateOverDistanceMultiplier *= count;
            ParticleSystem.MainModule main = particles.main;
            main.startSizeMultiplier *= colourless ? scale : Mathf.Lerp(MinimumSize, 1f, scale);
            main.startColor = Fade(main.startColor, scale);
        }
    }

    private static bool IgnoresColour(ParticleSystem particles)
    {
        Material material = particles.TryGetComponent(out ParticleSystemRenderer renderer) ? renderer.sharedMaterial : null;
        return material != null && material.shader != null
            && material.shader.name.IndexOf(GradientShader, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    // After every Awake, so a flickering light's own base brightness is scaled too.
    private void Start()
    {
        float scale = Strength();
        if (scale >= 1f)
        {
            return;
        }

        foreach (Light light in GetComponentsInChildren<Light>(true))
        {
            light.range *= scale;
            LightFlicker flicker = light.GetComponent<LightFlicker>();
            if (flicker != null)
            {
                flicker.m_baseIntensity *= scale;
            }
            else
            {
                light.intensity *= scale;
            }

            light.enabled = scale > 0f;
        }
    }

    private static float Strength()
    {
        return strength != null ? Mathf.Clamp01(strength.Value) : 1f;
    }

    private static ParticleSystem.MinMaxGradient Fade(ParticleSystem.MinMaxGradient colour, float scale)
    {
        switch (colour.mode)
        {
            case ParticleSystemGradientMode.Color:
                colour.color = Fade(colour.color, scale);
                break;
            case ParticleSystemGradientMode.TwoColors:
                colour.colorMin = Fade(colour.colorMin, scale);
                colour.colorMax = Fade(colour.colorMax, scale);
                break;
            case ParticleSystemGradientMode.Gradient:
            case ParticleSystemGradientMode.RandomColor:
                colour.gradient = Fade(colour.gradient, scale);
                break;
            case ParticleSystemGradientMode.TwoGradients:
                colour.gradientMin = Fade(colour.gradientMin, scale);
                colour.gradientMax = Fade(colour.gradientMax, scale);
                break;
        }

        return colour;
    }

    private static Color Fade(Color colour, float scale)
    {
        colour.a *= scale;
        return colour;
    }

    private static Gradient Fade(Gradient gradient, float scale)
    {
        if (gradient == null)
        {
            return null;
        }

        GradientAlphaKey[] alphas = gradient.alphaKeys;
        for (int i = 0; i < alphas.Length; i++)
        {
            alphas[i].alpha *= scale;
        }

        Gradient faded = new() { mode = gradient.mode };
        faded.SetKeys(gradient.colorKeys, alphas);
        return faded;
    }
}
