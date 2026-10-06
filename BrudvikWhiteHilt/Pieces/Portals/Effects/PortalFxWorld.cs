using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Portals.Effects;

/// <summary>
/// Which part of a trip a <see cref="PortalFxWorld"/> shows.
/// </summary>
public enum PortalFxKind
{
    /// <summary>Someone leaves: runes gather and spin faster until a flash.</summary>
    Depart,

    /// <summary>Someone is on their way here: a slow pulsing ring until they arrive.</summary>
    Incoming,

    /// <summary>Someone arrives: a flash, a burst of sparks and dust, and the ring fades.</summary>
    Arrive,
}

/// <summary>
/// The effect left in the world at either end of a trip: the rune ring on the ground, a light and sparks. It lives on
/// its own, so it plays out even when the traveller is already gone, and removes itself when done.
/// </summary>
public class PortalFxWorld : MonoBehaviour
{
    private const float RingLift = 0.04f;
    private const float FadeAfterFlash = 0.8f;
    private const float IncomingFade = 0.3f;

    private PortalFxKind kind;
    private Color colour;
    private float scale;
    private float elapsed;
    private float flashAt;
    private float arriveSeconds;
    private float releasedAt = -1f;
    private bool flashed;
    private float ringAngle;
    private Transform ring;
    private Material ringMaterial;
    private Material sparkMaterial;
    private Light glow;
    private ParticleSystem spiral;
    private ParticleSystem burst;
    private ParticleSystem dust;

    /// <summary>
    /// Starts an effect on the ground.
    /// </summary>
    /// <param name="kind">Which part of the trip.</param>
    /// <param name="ground">Point on the ground at the traveller's feet.</param>
    /// <param name="colour">Colour of the runes and sparks.</param>
    /// <param name="scale">Size, 1 for a player.</param>
    /// <param name="startElapsed">Seconds already past, for an effect that started before it was seen.</param>
    /// <returns>The effect, or null if it cannot be shown.</returns>
    public static PortalFxWorld Create(PortalFxKind kind, Vector3 ground, Color colour, float scale, float startElapsed)
    {
        Material ringMaterial = PortalFx.CreateMaterial(PortalFx.RingTexture);
        Material sparkMaterial = PortalFx.CreateMaterial(null);
        if (ringMaterial == null || sparkMaterial == null)
        {
            Destroy(ringMaterial);
            Destroy(sparkMaterial);
            return null;
        }

        GameObject root = new($"whitehilt_portalfx_{kind.ToString().ToLowerInvariant()}");
        root.transform.position = ground;
        PortalFxWorld effect = root.AddComponent<PortalFxWorld>();
        effect.Setup(kind, colour, scale, startElapsed, ringMaterial, sparkMaterial);
        return effect;
    }

    /// <summary>
    /// Ends an <see cref="PortalFxKind.Incoming"/> effect: it fades out and removes itself.
    /// </summary>
    public void Release()
    {
        if (releasedAt < 0f)
        {
            releasedAt = elapsed;
        }
    }

    // The effect at a portal: a glowing rune ring on the ground, a light and sparks in the portal's colour, and a flash
    // on arrival.
    private void Setup(PortalFxKind effectKind, Color tint, float size, float startElapsed, Material ringMat, Material sparkMat)
    {
        kind = effectKind;
        colour = tint;
        scale = size;
        elapsed = startElapsed;
        flashAt = PortalFxSettings.FlashAt.Value;
        arriveSeconds = PortalFxSettings.ArriveSeconds.Value;
        ringMaterial = ringMat;
        sparkMaterial = sparkMat;
        float glowStrength = PortalFxSettings.Glow.Value;
        sparkMaterial.color = new Color(colour.r * glowStrength, colour.g * glowStrength, colour.b * glowStrength, 1f);

        GameObject ringObject = new("ring");
        ring = ringObject.transform;
        ring.SetParent(transform, false);
        ring.localPosition = Vector3.up * RingLift;
        ringObject.AddComponent<MeshFilter>().sharedMesh = PortalFx.Quad;
        MeshRenderer ringRenderer = ringObject.AddComponent<MeshRenderer>();
        ringRenderer.sharedMaterial = ringMaterial;
        ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ringRenderer.receiveShadows = false;

        GameObject light = new("light");
        light.transform.SetParent(transform, false);
        light.transform.localPosition = Vector3.up * 1.2f * scale;
        glow = light.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = colour;
        glow.range = 6f * scale;
        glow.intensity = 0f;
        glow.shadows = LightShadows.None;

        float radius = PortalFxSettings.RingSize.Value * 0.4f * scale;
        spiral = CreateSpiral(radius);
        burst = CreateBurst();
        dust = CreateDust(radius);

        if (kind == PortalFxKind.Arrive && elapsed < 0.2f)
        {
            Flash();
        }

        Apply();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (!Apply())
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        Destroy(ringMaterial);
        Destroy(sparkMaterial);
    }

    // Sets ring, light and emission for the current moment; false once the effect is over.
    private bool Apply()
    {
        float ringAlpha;
        float ringSize = 1f;
        float spin;
        float light;
        float rate;
        switch (kind)
        {
            case PortalFxKind.Depart:
                if (elapsed < flashAt)
                {
                    float share = elapsed / flashAt;
                    ringAlpha = Mathf.Clamp01(elapsed / 0.4f);
                    spin = 30f + 330f * share * share;
                    light = 2.5f * share;
                    rate = Mathf.Lerp(20f, 90f, share);
                }
                else
                {
                    if (!flashed)
                    {
                        Flash();
                    }

                    float after = (elapsed - flashAt) / FadeAfterFlash;
                    if (after >= 1.5f)
                    {
                        return false;
                    }

                    ringAlpha = Mathf.Clamp01(1f - after);
                    ringSize = 1f + 0.6f * Mathf.Clamp01(after);
                    spin = 360f;
                    light = 8f * Mathf.Exp(-after * 5f);
                    rate = 0f;
                }

                break;

            case PortalFxKind.Incoming:
                float pulse = 0.75f + 0.25f * Mathf.Sin(elapsed * 3f);
                float fadeOut = releasedAt < 0f ? 1f : 1f - (elapsed - releasedAt) / IncomingFade;
                if (fadeOut <= 0f)
                {
                    return false;
                }

                ringAlpha = Mathf.Clamp01(elapsed / 0.6f) * pulse * fadeOut;
                spin = 40f;
                light = 1.5f * pulse * fadeOut;
                rate = releasedAt < 0f ? 25f : 0f;
                break;

            default:
                float share2 = elapsed / arriveSeconds;
                if (share2 >= 1.3f)
                {
                    return false;
                }

                ringAlpha = Mathf.Clamp01(1f - share2);
                ringSize = Mathf.Lerp(1.3f, 1f, Mathf.Clamp01(share2 * 2f));
                spin = Mathf.Lerp(360f, 20f, Mathf.Clamp01(share2));
                light = 8f * Mathf.Exp(-share2 * 4f);
                rate = 0f;
                break;
        }

        ringAngle += spin * Time.deltaTime;
        ring.localRotation = Quaternion.Euler(0f, ringAngle, 0f);
        float width = PortalFxSettings.RingSize.Value * scale * ringSize;
        ring.localScale = new Vector3(width, 1f, width);
        float strength = PortalFxSettings.Glow.Value;
        ringMaterial.color = new Color(colour.r * strength, colour.g * strength, colour.b * strength, ringAlpha);
        glow.intensity = light * scale;
        ParticleSystem.EmissionModule emission = spiral.emission;
        emission.rateOverTime = rate * scale;
        return true;
    }

    private void Flash()
    {
        flashed = true;
        burst.Emit(Mathf.RoundToInt(80 * scale));
        if (kind == PortalFxKind.Arrive)
        {
            dust.Emit(Mathf.RoundToInt(50 * scale));
        }
    }

    // Sparks rising from the ring in a slow spiral.
    private ParticleSystem CreateSpiral(float radius)
    {
        ParticleSystem system = CreateSystem("spiral", Vector3.zero, sparkMaterial, ParticleSystemSimulationSpace.Local);
        ParticleSystem.MainModule main = system.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 1.8f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f * scale, 0.14f * scale);
        main.startColor = Color.white;
        main.maxParticles = 400;
        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1f;
        shape.radiusThickness = 0f;
        shape.scale = new Vector3(radius, 0.02f, radius);
        ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = 0f;
        velocity.y = new ParticleSystem.MinMaxCurve(1.2f * scale, 2.2f * scale);
        velocity.z = 0f;
        velocity.orbitalY = 1.5f;
        velocity.radial = -0.15f * scale;
        FadeOverLifetime(system);
        system.Play();
        return system;
    }

    // The flash: sparks thrown out from the body in every direction.
    private ParticleSystem CreateBurst()
    {
        ParticleSystem system = CreateSystem("burst", Vector3.up * 1.1f * scale, sparkMaterial, ParticleSystemSimulationSpace.World);
        ParticleSystem.MainModule main = system.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f * scale, 6f * scale);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f * scale, 0.18f * scale);
        main.startColor = Color.white;
        main.gravityModifier = -0.1f;
        main.maxParticles = 200;
        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.35f * scale;
        ParticleSystem.LimitVelocityOverLifetimeModule limit = system.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = 0.5f;
        limit.dampen = 0.08f;
        FadeOverLifetime(system);
        return system;
    }

    // Dust kicked up along the ground on arrival.
    private ParticleSystem CreateDust(float radius)
    {
        ParticleSystem system = CreateSystem("dust", Vector3.up * 0.1f, sparkMaterial, ParticleSystemSimulationSpace.Local);
        ParticleSystem.MainModule main = system.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.3f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f * scale, 0.6f * scale);
        main.startColor = new Color(0.55f, 0.48f, 0.4f, 0.45f);
        main.maxParticles = 120;
        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1f;
        shape.radiusThickness = 0f;
        shape.scale = new Vector3(radius * 0.4f, 0.02f, radius * 0.4f);
        ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = 0f;
        velocity.y = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        velocity.z = 0f;
        velocity.radial = new ParticleSystem.MinMaxCurve(1.5f * scale, 3f * scale);
        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.6f));
        FadeOverLifetime(system);

        // Brown dust, not the shining rune colour of the spark material.
        ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
        Material plain = new(sparkMaterial) { color = Color.white };
        renderer.sharedMaterial = plain;
        system.gameObject.AddComponent<MaterialOwner>().Material = plain;
        return system;
    }

    // An empty particle system that is driven by code: no looping, nothing on its own, scaled with the effect.
    private ParticleSystem CreateSystem(string name, Vector3 localPosition, Material material, ParticleSystemSimulationSpace space)
    {
        GameObject holder = new(name);
        holder.transform.SetParent(transform, false);
        holder.transform.localPosition = localPosition;
        ParticleSystem system = holder.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = system.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = space;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 0f;
        ParticleSystemRenderer renderer = holder.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return system;
    }

    private static void FadeOverLifetime(ParticleSystem system)
    {
        ParticleSystem.ColorOverLifetimeModule colourOverLifetime = system.colorOverLifetime;
        colourOverLifetime.enabled = true;
        Gradient gradient = new();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
        colourOverLifetime.color = gradient;
    }

    /// <summary>
    /// Destroys a material made for one object together with it.
    /// </summary>
    private class MaterialOwner : MonoBehaviour
    {
        /// <summary>The material to destroy.</summary>
        public Material Material;

        private void OnDestroy()
        {
            Destroy(Material);
        }
    }
}
