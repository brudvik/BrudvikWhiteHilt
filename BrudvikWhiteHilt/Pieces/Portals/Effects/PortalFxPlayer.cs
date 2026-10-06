using BrudvikWhiteHilt.Helpers;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Portals.Effects;

/// <summary>
/// Plays the portal travel effects on a player, on every client. The travelling player's own client writes the
/// stage of the trip into the player's ZDO; each client reads it and poses the body, throws sparks and leaves the
/// rune ring in the world. Nothing is sent but those three values.
/// </summary>
public class PortalFxPlayer : MonoBehaviour
{
    /// <summary>No trip.</summary>
    public const int None = 0;

    /// <summary>Standing in the portal, about to vanish.</summary>
    public const int Departing = 1;

    /// <summary>Moved to the far side, hidden while it loads.</summary>
    public const int Incoming = 2;

    /// <summary>Appearing at the far side.</summary>
    public const int Arriving = 3;

    /// <summary>
    /// Seconds the body stays hidden after the trip ends, while the loading screen fades, so the traveller sees the arrival.
    /// </summary>
    public const float ArriveDelay = 0.4f;

    private const float Hidden = 0.001f;
    private const float StretchSeconds = 0.45f;
    private const float CollapseSeconds = 0.12f;
    private const float ThinWidth = 0.35f;
    private const float TallHeight = 1.35f;

    private static readonly int PhaseKey = "whitehilt_portalfx".GetStableHashCode();
    private static readonly int TimeKey = "whitehilt_portalfx_t".GetStableHashCode();
    private static readonly int ColourKey = "whitehilt_portalfx_c".GetStableHashCode();

    private Player player;
    private ZNetView nview;
    private Transform visual;
    private Vector3 basePosition;
    private Quaternion baseRotation;
    private Vector3 baseScale;
    private bool posed;
    private int seenPhase = -1;
    private long seenTime;
    private PortalFxWorld incoming;
    private bool arrivalPending;
    private Color arrivalColour;
    private ParticleSystem sparks;
    private Material sparkMaterial;

    /// <summary>
    /// Reads where the local player is in a trip.
    /// </summary>
    /// <param name="phase">The stage, <see cref="None"/> without a trip.</param>
    /// <param name="elapsed">Seconds since the stage began; negative before it begins.</param>
    /// <param name="colour">Colour of the trip's effects.</param>
    /// <returns>True during a trip of the local player.</returns>
    public static bool TryGetLocal(out int phase, out float elapsed, out Color colour)
    {
        Player local = Player.m_localPlayer;
        PortalFxPlayer effects = local != null ? local.GetComponent<PortalFxPlayer>() : null;
        if (effects == null || !effects.Read(out phase, out elapsed, out colour))
        {
            phase = None;
            elapsed = 0f;
            colour = Color.white;
            return false;
        }

        return phase != None;
    }

    /// <summary>
    /// Starts the departure of the local player, who has just stepped into a portal. Call on the owner.
    /// </summary>
    /// <param name="colour">Colour of the trip's effects.</param>
    public void BeginDeparture(Color colour)
    {
        SetPhase(Departing, 0f, PortalFx.Pack(colour));
        string emote = PortalFxSettings.Emote.Value?.Trim();
        if (PortalFxSettings.Enabled.Value && !string.IsNullOrEmpty(emote) && !player.IsAttached())
        {
            // Player.StartEmote refuses while teleporting, so the emote is set the way it does.
            ZDO zdo = nview.GetZDO();
            zdo.Set(ZDOVars.s_emoteID, zdo.GetInt(ZDOVars.s_emoteID) + 1);
            zdo.Set(ZDOVars.s_emote, emote.ToLowerInvariant());
            zdo.Set(ZDOVars.s_emoteOneshot, true);
        }
    }

    private void Awake()
    {
        player = GetComponent<Player>();
        nview = GetComponent<ZNetView>();
        visual = player != null && player.m_visual != null ? player.m_visual.transform : null;
    }

    // Moves the trip on to its next stage; only the traveller's own client does this.
    private void Update()
    {
        if (player == null || nview == null || !nview.IsValid() || !nview.IsOwner())
        {
            return;
        }

        int phase = nview.GetZDO().GetInt(PhaseKey);
        if (phase == Departing)
        {
            if (!player.IsTeleporting())
            {
                SetPhase(Arriving, 0f, nview.GetZDO().GetInt(ColourKey));
            }
            else if ((player.transform.position - player.m_teleportTargetPos).sqrMagnitude < 0.25f)
            {
                SetPhase(Incoming, 0f, nview.GetZDO().GetInt(ColourKey));
            }
        }
        else if (phase == Incoming && !player.IsTeleporting())
        {
            SetPhase(Arriving, ArriveDelay, nview.GetZDO().GetInt(ColourKey));
        }
    }

    // Plays the travel effect on a player as their network data says: departing, incoming or arriving, and since when.
    // Every machine reads the same data, so everyone sees the same effect without an RPC per frame.
    private void LateUpdate()
    {
        if (visual == null || VisualHelper.IsHeadless)
        {
            return;
        }

        if (!PortalFxSettings.Enabled.Value || !Read(out int phase, out float elapsed, out Color colour) || player.IsDead())
        {
            EndIncoming();
            Restore();
            SetSparkRate(0f);
            return;
        }

        long time = nview.GetZDO().GetLong(TimeKey);
        if (phase != seenPhase || time != seenTime)
        {
            bool first = seenPhase < 0;
            seenPhase = phase;
            seenTime = time;
            StartPhase(phase, elapsed, colour, first);
        }

        Pose(phase, elapsed);
    }

    private void OnDestroy()
    {
        EndIncoming();
        Destroy(sparkMaterial);
    }

    private bool Read(out int phase, out float elapsed, out Color colour)
    {
        phase = None;
        elapsed = 0f;
        colour = Color.white;
        if (nview == null || !nview.IsValid() || ZNet.instance == null)
        {
            return false;
        }

        ZDO zdo = nview.GetZDO();
        phase = zdo.GetInt(PhaseKey);
        elapsed = (float)((ZNet.instance.GetTime().Ticks - zdo.GetLong(TimeKey)) / (double)System.TimeSpan.TicksPerSecond);
        colour = PortalFx.Unpack(zdo.GetInt(ColourKey));
        return true;
    }

    private void SetPhase(int phase, float delay, int colour)
    {
        ZDO zdo = nview.GetZDO();
        zdo.Set(PhaseKey, phase);
        zdo.Set(TimeKey, ZNet.instance.GetTime().Ticks + (long)(delay * System.TimeSpan.TicksPerSecond));
        zdo.Set(ColourKey, colour);
    }

    // Leaves the ring in the world and plays the sound as a stage begins, or catches up with one already under way.
    private void StartPhase(int phase, float elapsed, Color colour, bool first)
    {
        if (phase == Departing || phase == None)
        {
            EndIncoming();
        }

        // The ZDO's position comes with the stage; the body may still be gliding there on other clients.
        Vector3 position = nview.GetZDO().GetPosition();
        Vector3 ground = PortalFx.Ground(position);
        switch (phase)
        {
            case Departing:
                if (elapsed < PortalFxSettings.FlashAt.Value + 1f)
                {
                    PortalFxWorld.Create(PortalFxKind.Depart, ground, colour, 1f, Mathf.Max(0f, elapsed));
                    if (elapsed < 0.3f)
                    {
                        PortalFx.PlaySound(false, position);
                    }
                }

                break;

            case Incoming:
                if (incoming == null)
                {
                    incoming = PortalFxWorld.Create(PortalFxKind.Incoming, ground, colour, 1f, Mathf.Max(0f, elapsed));
                }

                break;

            case Arriving:
                if (!first || elapsed < PortalFxSettings.ArriveSeconds.Value)
                {
                    arrivalPending = true;
                    arrivalColour = colour;
                }

                break;
        }
    }

    // Poses the player for a phase of the effect: lifting, spinning and stretching thin as they depart, hidden while in
    // transit, and growing back as they arrive, with sparks that grow with it.
    private void Pose(int phase, float elapsed)
    {
        float flashAt = PortalFxSettings.FlashAt.Value;
        float arrive = PortalFxSettings.ArriveSeconds.Value;
        float lift = PortalFxSettings.LiftHeight.Value;
        float turns = PortalFxSettings.Spins.Value;
        float rise;
        float angle;
        float width;
        float height;
        float sparkRate;

        if (phase == Departing && elapsed < flashAt)
        {
            float share = Mathf.Clamp01(elapsed / flashAt);
            float stretch = Mathf.InverseLerp(flashAt - StretchSeconds, flashAt, elapsed);
            float collapse = 1f - Mathf.InverseLerp(flashAt - CollapseSeconds, flashAt, elapsed);
            rise = lift * Mathf.SmoothStep(0f, 1f, share);
            angle = turns * 360f * share * share;
            width = Mathf.Lerp(1f, ThinWidth, stretch) * collapse;
            height = Mathf.Lerp(1f, TallHeight, stretch) * collapse;
            sparkRate = Mathf.Lerp(15f, 140f, share);
        }
        else if (phase == Departing || phase == Incoming || (phase == Arriving && elapsed < 0f))
        {
            rise = 0f;
            angle = 0f;
            width = 0f;
            height = 0f;
            sparkRate = 0f;
        }
        else if (phase == Arriving && elapsed < arrive)
        {
            if (arrivalPending)
            {
                arrivalPending = false;
                EndIncoming();
                Vector3 position = nview.GetZDO().GetPosition();
                PortalFxWorld.Create(PortalFxKind.Arrive, PortalFx.Ground(position), arrivalColour, 1f, elapsed);
                if (elapsed < 0.3f)
                {
                    PortalFx.PlaySound(true, position);
                }
            }

            float share = elapsed / arrive;
            float appear = Mathf.Clamp01(elapsed / CollapseSeconds);
            float stretch = 1f - Mathf.SmoothStep(0f, 1f, elapsed / StretchSeconds);
            rise = lift * 0.6f * (1f - Mathf.SmoothStep(0f, 1f, elapsed / (arrive * 0.7f)));
            angle = -turns * 180f * (1f - share) * (1f - share);
            width = Mathf.Lerp(1f, ThinWidth, stretch) * appear;
            height = Mathf.Lerp(1f, TallHeight, stretch) * appear;
            sparkRate = elapsed < 0.6f ? 60f : 0f;
        }
        else
        {
            arrivalPending = false;
            EndIncoming();
            Restore();
            SetSparkRate(0f);
            return;
        }

        if (!posed)
        {
            posed = true;
            basePosition = visual.localPosition;
            baseRotation = visual.localRotation;
            baseScale = visual.localScale;
        }

        width = Mathf.Max(width, Hidden);
        height = Mathf.Max(height, Hidden);
        visual.localPosition = basePosition + Vector3.up * rise;
        visual.localRotation = baseRotation * Quaternion.Euler(0f, angle, 0f);
        visual.localScale = Vector3.Scale(baseScale, new Vector3(width, height, width));
        SetSparkRate(sparkRate);
    }

    private void Restore()
    {
        if (!posed)
        {
            return;
        }

        posed = false;
        visual.localPosition = basePosition;
        visual.localRotation = baseRotation;
        visual.localScale = baseScale;
        player.ResetCloth();
    }

    private void EndIncoming()
    {
        if (incoming != null)
        {
            incoming.Release();
        }

        incoming = null;
    }

    // Sets how many sparks fly, in the portal's colour; the spark system is only made the first time any are needed.
    private void SetSparkRate(float rate)
    {
        if (rate <= 0f && sparks == null)
        {
            return;
        }

        ParticleSystem system = GetSparks();
        if (system == null)
        {
            return;
        }

        Color colour = PortalFx.Unpack(nview.GetZDO().GetInt(ColourKey));
        float glow = PortalFxSettings.Glow.Value;
        sparkMaterial.color = new Color(colour.r * glow, colour.g * glow, colour.b * glow, 1f);
        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = rate;
    }

    // Sparks streaming off the body, from the skin itself if the game lets the particles read the body mesh.
    private ParticleSystem GetSparks()
    {
        if (sparks != null)
        {
            return sparks;
        }

        sparkMaterial = PortalFx.CreateMaterial(null);
        if (sparkMaterial == null)
        {
            return null;
        }

        SkinnedMeshRenderer body = player.GetComponent<VisEquipment>()?.m_bodyModel;
        GameObject holder = new("whitehilt_portalfx_sparks");
        holder.transform.SetParent(visual, false);
        sparks = holder.AddComponent<ParticleSystem>();
        sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = sparks.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.09f);
        main.startColor = Color.white;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.maxParticles = 500;
        ParticleSystem.EmissionModule emission = sparks.emission;
        emission.rateOverTime = 0f;
        ParticleSystem.ShapeModule shape = sparks.shape;
        shape.enabled = true;
        if (body != null && body.sharedMesh != null && body.sharedMesh.isReadable)
        {
            shape.shapeType = ParticleSystemShapeType.SkinnedMeshRenderer;
            shape.skinnedMeshRenderer = body;
        }
        else
        {
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.position = Vector3.up * 0.9f;
            shape.scale = new Vector3(0.45f, 1.7f, 0.3f);
        }

        ParticleSystem.VelocityOverLifetimeModule velocity = sparks.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.6f, 1.8f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
        ParticleSystem.ColorOverLifetimeModule colourOverLifetime = sparks.colorOverLifetime;
        colourOverLifetime.enabled = true;
        Gradient gradient = new();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colourOverLifetime.color = gradient;
        ParticleSystemRenderer renderer = holder.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = sparkMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        sparks.Play();
        return sparks;
    }
}
