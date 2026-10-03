using System.Collections.Generic;
using System.Linq;
using BrudvikWhiteHilt.Helpers;
using UnityEngine;

namespace BrudvikWhiteHilt.Kraken;

/// <summary>
/// Sits on the Kraken. It rises out of the deep beside a ship, raises its tentacles around it, holds the ship fast while it
/// lives and sinks back into the deep when it gives up, at dawn or when no one is near.
/// </summary>
public class KrakenBody : MonoBehaviour
{
    /// <summary>ZDO key of the ship the Kraken came for.</summary>
    public const string ShipKey = "whitehilt_kraken_ship";

    /// <summary>ZDO key of the crew size when the Kraken rose, shared with its tentacles.</summary>
    public const string CrewKey = "whitehilt_kraken_crew";

    /// <summary>ZDO key of the time (ticks) the Kraken started to sink back; 0 while it stays.</summary>
    public const string RetreatKey = "whitehilt_kraken_retreat";
    /// <summary>ZDO key of the synchronized enraged phase.</summary>
    public const string EnragedKey = "whitehilt_kraken_enraged";

    private const string StartKey = "whitehilt_kraken_start";
    private const string SpawnedKey = "whitehilt_kraken_spawned";
    private const string LiftKey = "whitehilt_kraken_lift";
    private const string LiftDurationKey = "whitehilt_kraken_lift_duration";
    private const float RiseSeconds = 7f;
    private const float SinkSeconds = 6f;
    private const float Depth = 16f;
    private const float TentacleDelay = 2.5f;
    private const float QuietRange = 80f;
    private const float HoldRange = 60f;
    private const float AnnounceRange = 120f;

    private static readonly List<KrakenBody> instances = new();

    private ZNetView nview;
    private Character character;
    private Transform visual;
    private float baseY;
    private float nextTick;
    private float lastPlayerNear;
    private Animator animator;
    private MonsterAI ai;
    private float nextAmbient;
    private long warnedCycle = -1;
    private bool observedEnrage;
    private EffectList ambientSound;

    /// <summary>Every Kraken in the loaded world.</summary>
    public static IReadOnlyList<KrakenBody> Instances => instances;

    /// <summary>Whether the Kraken is sinking back into the deep.</summary>
    public bool IsRetreating => nview != null && nview.IsValid() && nview.GetZDO().GetLong(RetreatKey) != 0L;

    /// <summary>
    /// Whether a living Kraken holds this ship.
    /// </summary>
    /// <param name="ship">The ship.</param>
    /// <returns>True if it is held.</returns>
    public static bool Holds(Ship ship)
    {
        if (!KrakenSettings.HoldShip.Value || instances.Count == 0 || ship.m_nview == null || !ship.m_nview.IsValid())
        {
            return false;
        }

        ZDOID id = ship.m_nview.GetZDO().m_uid;
        return instances.Any(kraken => kraken.nview.IsValid() && !kraken.character.IsDead() && !kraken.IsRetreating
            && kraken.nview.GetZDO().GetZDOID(ShipKey) == id
            && Vector3.Distance(kraken.transform.position, ship.transform.position) < HoldRange);
    }

    /// <summary>Applies a bounded lift to the ship on its physics owner only.</summary>
    /// <param name="ship">The held ship.</param>
    /// <param name="deltaTime">Physics step duration.</param>
    public static void Lift(Ship ship, float deltaTime)
    {
        if (!KrakenSettings.LiftShip.Value || !ship.m_nview.IsOwner() || !Holds(ship) || deltaTime <= 0f)
            return;
        KrakenBody kraken = instances.FirstOrDefault(candidate => candidate.nview.IsValid()
            && !candidate.character.IsDead() && !candidate.IsRetreating
            && candidate.nview.GetZDO().GetZDOID(ShipKey) == ship.m_nview.GetZDO().m_uid);
        if (kraken == null)
            return;
        ZDO zdo = kraken.nview.GetZDO();
        if (zdo.GetLong(LiftKey) == 0L)
            return;
        float warning = KrakenSettings.LiftWarningSeconds.Value;
        float duration = zdo.GetFloat(LiftDurationKey, KrakenSettings.LiftSeconds.Value);
        float phase = kraken.Seconds(LiftKey) - warning;
        if (phase < 0f || phase >= duration || duration <= 0f)
            return;
        Rigidbody body = ship.m_body;
        float water = Floating.GetWaterLevel(body.worldCenterOfMass, ref ship.m_previousCenter);
        float height = LiftOffset(phase, duration, KrakenSettings.LiftHeight.Value);
        float rate = Mathf.PI / duration;
        float target = water + ship.m_waterLevelOffset + height;
        float desired = KrakenSettings.LiftHeight.Value * rate * Mathf.Sin(2f * phase * rate)
            + (target - body.worldCenterOfMass.y) * rate;
        float velocity = Mathf.MoveTowards(body.linearVelocity.y, desired, KrakenSettings.LiftAcceleration.Value * deltaTime);
        float support = body.useGravity ? -Physics.gravity.y * deltaTime : 0f;
        body.AddForce(Vector3.up * (velocity - body.linearVelocity.y + support), ForceMode.VelocityChange);
    }

    /// <summary>Returns the smooth lift height within one raising-and-lowering cycle.</summary>
    /// <param name="phase">Seconds since the lift began.</param>
    /// <param name="duration">Cycle duration in seconds.</param>
    /// <param name="height">Maximum lift in metres.</param>
    /// <returns>Zero outside the cycle; a bounded lift height inside it.</returns>
    public static float LiftOffset(float phase, float duration, float height)
    {
        if (duration <= 0f || phase <= 0f || phase >= duration)
            return 0f;
        float wave = Mathf.Sin(Mathf.PI * phase / duration);
        return Mathf.Max(0f, height) * wave * wave;
    }

    /// <summary>Returns the retained potion share near a living Kraken, or full strength outside the fight.</summary>
    /// <param name="target">The character benefiting from the potion.</param>
    /// <param name="share">The configured retained share.</param>
    /// <returns>The local multiplier, without removing or shortening the potion.</returns>
    public static float PotionFactor(Character target, float share)
    {
        return target != null && KrakenSettings.PotionRange.Value > 0f
            && instances.Any(kraken => kraken.nview != null && kraken.nview.IsValid()
                && !kraken.character.IsDead() && !kraken.IsRetreating
                && Vector3.Distance(target.transform.position, kraken.transform.position) < KrakenSettings.PotionRange.Value)
            ? Mathf.Clamp01(share) : 1f;
    }

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        character = GetComponent<Character>();
        ai = GetComponent<MonsterAI>();
        ambientSound = ai != null ? ai.m_idleSound : new EffectList();
        if (ai != null && KrakenSettings.Sounds.Value)
            ai.m_idleSound = new EffectList();
        animator = GetComponentInChildren<Animator>();
        visual = transform.Find("Visual/kraken_visual");
        baseY = visual != null ? visual.localPosition.y : 0f;
        instances.Add(this);
        lastPlayerNear = Time.time;
        if (nview.IsValid() && nview.IsOwner() && nview.GetZDO().GetLong(StartKey) == 0L)
        {
            nview.GetZDO().Set(StartKey, ZNet.instance.GetTime().Ticks);
        }
    }

    private void Start()
    {
        Player player = Player.m_localPlayer;
        if (player == null || Seconds(StartKey) > RiseSeconds || Vector3.Distance(player.transform.position, transform.position) > AnnounceRange)
        {
            return;
        }

        player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_kraken_rises");
        if (GameCamera.instance != null)
        {
            GameCamera.instance.AddShake(transform.position, 120f, 2.5f, false);
        }
    }

    private void OnDestroy()
    {
        instances.Remove(this);
    }

    private void Update()
    {
        if (!nview.IsValid())
        {
            return;
        }

        UpdateVisual();
        UpdateThreat();
        if (!nview.IsOwner() || Time.time < nextTick)
        {
            return;
        }

        nextTick = Time.time + 1f;
        UpdateOwner();
    }

    // Rising and sinking only move the model, on every client from the same ZDO times, so the Kraken never leaves its place.
    private void UpdateVisual()
    {
        if (visual == null)
        {
            return;
        }

        float rise = Mathf.SmoothStep(0f, 1f, Seconds(StartKey) / RiseSeconds);
        float sink = IsRetreating ? Mathf.SmoothStep(0f, 1f, Seconds(RetreatKey) / SinkSeconds) : 0f;
        Vector3 position = visual.localPosition;
        position.y = baseY - Depth * (1f - rise) - Depth * sink;
        visual.localPosition = position;
    }

    private void UpdateOwner()
    {
        if (character.IsDead())
        {
            return;
        }

        if (IsRetreating)
        {
            if (Seconds(RetreatKey) > SinkSeconds)
            {
                nview.Destroy();
            }

            return;
        }

        ZDO zdo = nview.GetZDO();
        zdo.Set(EnragedKey, KrakenSettings.EnrageHealthShare.Value > 0f
            && character.GetHealth() / Mathf.Max(1f, character.GetMaxHealth()) <= KrakenSettings.EnrageHealthShare.Value);
        float speed = zdo.GetBool(EnragedKey) ? KrakenSettings.EnrageSpeed.Value : 1f;
        float previousDuration = zdo.GetFloat(LiftDurationKey, KrakenSettings.LiftSeconds.Value);
        float interval = Mathf.Max(KrakenSettings.LiftInterval.Value / speed, KrakenSettings.LiftWarningSeconds.Value + previousDuration);
        if (KrakenSettings.HoldShip.Value && KrakenSettings.LiftShip.Value && Seconds(StartKey) >= RiseSeconds
            && (zdo.GetLong(LiftKey) == 0L || Seconds(LiftKey) >= interval))
        {
            zdo.Set(LiftDurationKey, KrakenSettings.LiftSeconds.Value / speed);
            zdo.Set(LiftKey, ZNet.instance.GetTime().Ticks);
        }
        if (!zdo.GetBool(SpawnedKey) && Seconds(StartKey) > TentacleDelay)
        {
            zdo.Set(SpawnedKey, true);
            KrakenSpawner.SpawnTentacles(this, FindShip());
        }

        if (Player.IsPlayerInRange(transform.position, QuietRange))
        {
            lastPlayerNear = Time.time;
        }

        bool tooLong = Seconds(StartKey) > KrakenSettings.RetreatMinutes.Value * 60f;
        bool dawn = KrakenSettings.NightOnly.Value && !EnvMan.IsNight()
            && !zdo.GetBool(Items.Summoning.SummoningHornService.SummonedKey);
        if (tooLong || dawn || Time.time - lastPlayerNear > 20f)
        {
            Retreat();
        }
    }

    /// <summary>
    /// Starts sinking back into the deep; the tentacles go with it.
    /// </summary>
    public void Retreat()
    {
        if (!nview.IsValid() || !nview.IsOwner() || IsRetreating)
        {
            return;
        }

        nview.GetZDO().Set(RetreatKey, ZNet.instance.GetTime().Ticks);
        Jotunn.Logger.LogInfo($"The Kraken at {transform.position} sinks back into the deep");
    }

    /// <summary>
    /// The ship the Kraken came for, if it is loaded.
    /// </summary>
    /// <returns>The ship, or null.</returns>
    public Ship FindShip()
    {
        ZDOID id = nview.GetZDO().GetZDOID(ShipKey);
        GameObject found = id.IsNone() ? null : ZNetScene.instance.FindInstance(id);
        return found != null ? found.GetComponent<Ship>() : null;
    }

    private void UpdateThreat()
    {
        if (VisualHelper.IsHeadless || character.IsDead() || IsRetreating)
            return;
        bool enraged = nview.GetZDO().GetBool(EnragedKey);
        if (animator != null)
            animator.speed = enraged ? KrakenSettings.EnrageSpeed.Value : 1f;
        Player player = Player.m_localPlayer;
        if (player == null || Vector3.Distance(player.transform.position, transform.position) > KrakenSettings.SoundRange.Value)
            return;
        long cycle = nview.GetZDO().GetLong(LiftKey);
        bool warn = KrakenSettings.HoldShip.Value && KrakenSettings.LiftShip.Value
            && cycle != 0 && cycle != warnedCycle && Seconds(LiftKey) < KrakenSettings.LiftWarningSeconds.Value;
        if (warn || (enraged && !observedEnrage))
        {
            warnedCycle = cycle;
            if (KrakenSettings.Sounds.Value && ai != null)
                ai.m_alertedEffects.Create(transform.position, transform.rotation);
            GameCamera.instance?.AddShake(transform.position, KrakenSettings.SoundRange.Value, 2.5f, false);
            nextAmbient = Time.time + KrakenSettings.AmbientSeconds.Value;
        }
        else if (Time.time >= nextAmbient && KrakenSettings.Sounds.Value && ai != null)
        {
            ambientSound.Create(transform.position, transform.rotation);
            nextAmbient = Time.time + KrakenSettings.AmbientSeconds.Value;
        }
        observedEnrage = enraged;
    }

    private float Seconds(string key)
    {
        long ticks = nview.IsValid() ? nview.GetZDO().GetLong(key) : 0L;
        return ticks == 0L ? 0f : (float)System.TimeSpan.FromTicks(ZNet.instance.GetTime().Ticks - ticks).TotalSeconds;
    }
}
