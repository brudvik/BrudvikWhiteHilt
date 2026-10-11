using BrudvikWhiteHilt.Items.Summoning;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// A White Dragon called by the Dragon Horn. It appears out of sight behind its caller, flies in and arrives with a
/// roar and a dive at the nearest foe. While it helps it goes after whatever hits its caller, dives and slams into the
/// ground by a foe now and then, roars to stagger and slow the foes around it, and blesses its caller while near.
/// When its time is up or its caller is gone it flies off and vanishes once no player is near enough to see it go.
/// Its phase, times and power live in its ZDO and are read on the world clock, so whoever owns it carries on.
/// </summary>
public class WhiteDragonAlly : MonoBehaviour
{
    private const float ArrivedDistance = 20f;
    private const float LostSeconds = 10f;
    private const float BlessInterval = 1f;

    // A dive that has not reached its foe by then is given up, so the dragon never hangs low for long.
    private const float MaxDiveSeconds = 5f;
    private const float DiveReach = 4f;
    private const float DiveAltitude = 0.5f;

    // Yagluth's nova: a burst on the ground under the roaring dragon, with its own camera shake.
    private const string RoarEffect = "fx_goblinking_nova";
    private const string GuardRpc = "WhiteHilt_DragonGuard";
    private const string RoarRpc = "WhiteHilt_DragonRoar";

    private static readonly int CallerKey = "whitehilt_whitedragon_caller".GetStableHashCode();
    private static readonly int UntilKey = "whitehilt_whitedragon_until".GetStableHashCode();
    private static readonly int PhaseKey = "whitehilt_whitedragon_phase".GetStableHashCode();
    private static readonly int PhaseAtKey = "whitehilt_whitedragon_phase_at".GetStableHashCode();
    private static readonly int AwayKey = "whitehilt_whitedragon_away".GetStableHashCode();
    private static readonly int PowerKey = "whitehilt_whitedragon_power".GetStableHashCode();

    private static readonly Dictionary<BaseAI, WhiteDragonAlly> byAI = new();
    private static readonly HashSet<Character> characters = new();

    private ZNetView view;
    private Character character;
    private MonsterAI ai;
    private float lostFor;
    private Character guard;
    private float guardUntil;
    private Character diveTarget;
    private float diveStart;
    private float nextDive;
    private float nextRoar;
    private float nextBless;
    private float normalAltitude;
    private float normalFlySpeed;

    private enum Phase
    {
        Arriving,
        Helping,
        Leaving
    }

    /// <summary>Whether any White Dragon is loaded here.</summary>
    public static bool Any => characters.Count > 0;

    /// <summary>Hash of the blessing its caller gets while near; set when the effect is made.</summary>
    public static int BlessingHash { get; set; }

    /// <summary>Hash of the fear its roar puts on foes; set when the effect is made.</summary>
    public static int TerrorHash { get; set; }

    private Phase CurrentPhase => (Phase)view.GetZDO().GetInt(PhaseKey);

    private double PhaseSeconds => (ZNet.instance.GetTime() - new DateTime(view.GetZDO().GetLong(PhaseAtKey))).TotalSeconds;

    /// <summary>
    /// Calls a White Dragon for the local player: it appears out of sight behind them and flies in. It is called with
    /// the power the world has earned by then.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <returns>True if the dragon was created.</returns>
    public static bool Summon(Player player)
    {
        GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(WhiteDragonRegistry.DragonName) : null;
        if (prefab == null)
        {
            Jotunn.Logger.LogWarning($"White Dragon prefab {WhiteDragonRegistry.DragonName} not found");
            return false;
        }

        Vector3 origin = player.transform.position;
        Vector3 behind = -(GameCamera.instance != null ? GameCamera.instance.transform.forward : player.transform.forward);
        behind.y = 0f;
        behind = behind.sqrMagnitude > 0.01f ? behind.normalized : Vector3.back;
        Vector3 direction = Quaternion.Euler(0f, UnityEngine.Random.Range(-50f, 50f), 0f) * behind;
        Vector3 point = origin + direction * DragonHorn.ArriveDistance.Value;
        // WorldGenerator knows the ground even where the zone is not loaded.
        float ground = Mathf.Max(WorldGenerator.instance.GetHeight(point.x, point.z), ZoneSystem.instance.m_waterLevel, origin.y);
        point.y = ground + MonsterSettings.DragonFlyHeightMax.Value + 10f;

        GameObject instance = Instantiate(prefab, point, Quaternion.LookRotation(-direction));
        Character dragon = instance.GetComponent<Character>();
        ZDO zdo = dragon.m_nview.GetZDO();
        DateTime now = ZNet.instance.GetTime();
        float power = WorldPower();
        zdo.Set(CallerKey, player.GetPlayerID());
        zdo.Set(UntilKey, now.AddSeconds(DragonHorn.StayHours.Value * DragonHorn.GameHourSeconds).Ticks);
        zdo.Set(PhaseKey, (int)Phase.Arriving);
        zdo.Set(PhaseAtKey, now.Ticks);
        // Before the level, which sets the health the power raises.
        zdo.Set(PowerKey, power);
        dragon.SetLevel(DragonHorn.Stars.Value + 1);
        instance.GetComponent<Tameable>()?.Command(player, message: false);
        Jotunn.Logger.LogInfo($"White Dragon called by {player.GetPlayerName()}, coming from {point}, power {power:0.##}");
        return true;
    }

    /// <summary>
    /// Whether a player already has a White Dragon at their side, loaded here.
    /// </summary>
    /// <param name="playerId">The player's ID.</param>
    /// <returns>True if one is arriving or helping.</returns>
    public static bool HasDragon(long playerId)
    {
        return byAI.Values.Any(ally => ally.view.IsValid() && ally.view.GetZDO().GetLong(CallerKey) == playerId && ally.CurrentPhase != Phase.Leaving);
    }

    /// <summary>
    /// Whether a creature is a White Dragon.
    /// </summary>
    /// <param name="character">The creature.</param>
    /// <returns>True for a White Dragon.</returns>
    public static bool Is(Character character)
    {
        return character != null && characters.Contains(character);
    }

    /// <summary>
    /// The multiplier of health and damage a creature was called with: above 1 for a White Dragon called after the
    /// bosses in <see cref="DragonHorn.PowerKeys"/> were slain, 1 for anything else. Works before the dragon's own
    /// Awake, as the game sets its health in its Awake.
    /// </summary>
    /// <param name="character">The creature.</param>
    /// <returns>The multiplier.</returns>
    public static float Power(Character character)
    {
        return character != null && character.m_nview != null && character.m_nview.IsValid() && character.GetComponent<WhiteDragonAlly>() != null
            ? character.m_nview.GetZDO().GetFloat(PowerKey, 1f)
            : 1f;
    }

    /// <summary>
    /// Tells the White Dragons of the local player that something hit them, so they go after it. Runs where the
    /// player is simulated; the dragon's owner gets it by RPC.
    /// </summary>
    /// <param name="player">The local player, who was hit.</param>
    /// <param name="attacker">Who hit them.</param>
    public static void CallerHit(Player player, ZDOID attacker)
    {
        if (attacker.IsNone() || DragonHorn.GuardSeconds.Value <= 0f)
        {
            return;
        }

        long id = player.GetPlayerID();
        foreach (WhiteDragonAlly ally in byAI.Values)
        {
            if (ally.view.IsValid() && ally.view.GetZDO().GetLong(CallerKey) == id && ally.CurrentPhase == Phase.Helping)
            {
                ally.view.InvokeRPC(GuardRpc, attacker);
            }
        }
    }

    /// <summary>
    /// Flies a White Dragon in, off or down in a dive instead of letting its AI decide, and holds it to the foe it
    /// guards against. Runs on the owner from the AI's update.
    /// </summary>
    /// <param name="ai">The creature's AI.</param>
    /// <param name="dt">Seconds since the last update.</param>
    /// <returns>True if the dragon was steered here and its AI must not run.</returns>
    public static bool Steer(BaseAI ai, float dt)
    {
        return byAI.TryGetValue(ai, out WhiteDragonAlly ally) && ally.Fly(dt);
    }

    // How much stronger a dragon called now is: a share more for each boss key the world has.
    private static float WorldPower()
    {
        int count = (DragonHorn.PowerKeys.Value ?? string.Empty).Split(',').Select(key => key.Trim())
            .Count(key => key.Length > 0 && ZoneSystem.instance.GetGlobalKey(key));
        return 1f + count * DragonHorn.PowerPerKey.Value;
    }

    private void Awake()
    {
        view = GetComponent<ZNetView>();
        character = GetComponent<Character>();
        ai = GetComponent<MonsterAI>();
        normalAltitude = ai.m_flyAltitudeMin;
        normalFlySpeed = character.m_flyFastSpeed;
        if (view.IsValid())
        {
            byAI[ai] = this;
            characters.Add(character);
            view.Register<ZDOID>(GuardRpc, RPC_Guard);
            view.Register(RoarRpc, RPC_Roar);
        }
    }

    private void OnDestroy()
    {
        byAI.Remove(ai);
        characters.Remove(character);
    }

    // On every machine its caller is blessed while near it; on the owner it moves on from arriving to helping, and
    // from helping to leaving when its time is up or its caller is lost, fights while helping, and once it has left
    // everyone's sight, it is gone.
    private void Update()
    {
        if (!view.IsValid() || character.IsDead() || ZNet.instance == null)
        {
            return;
        }

        Bless();
        if (!view.IsOwner())
        {
            if (diveTarget != null)
            {
                EndDive();
            }

            return;
        }

        switch (CurrentPhase)
        {
            case Phase.Arriving:
                Player caller = Caller();
                if (caller == null || PhaseSeconds > DragonHorn.FlightSeconds.Value
                    || Utils.DistanceXZ(caller.transform.position, transform.position) < ArrivedDistance)
                {
                    SetPhase(Phase.Helping);
                    Arrive(caller);
                }

                break;
            case Phase.Helping:
                GameObject follow = ai.GetFollowTarget();
                lostFor = follow == null ? lostFor + Time.deltaTime : 0f;
                if (ZNet.instance.GetTime().Ticks > view.GetZDO().GetLong(UntilKey) || lostFor > LostSeconds
                    || (follow != null && Vector3.Distance(follow.transform.position, transform.position) > DragonHorn.LeaveDistance.Value))
                {
                    Leave();
                }
                else
                {
                    Fight();
                }

                break;
            case Phase.Leaving:
                if (!Player.IsPlayerInRange(transform.position, DragonHorn.VanishDistance.Value) || PhaseSeconds > DragonHorn.FlightSeconds.Value)
                {
                    ZNetScene.instance.Destroy(gameObject);
                }

                break;
        }
    }

    // Its entrance: a roar over its caller, and at once a dive at the foe nearest to them.
    private void Arrive(Player caller)
    {
        Roar();
        Character foe = caller != null ? NearestFoe(caller.transform.position, DragonHorn.DiveRange.Value) : null;
        if (foe != null)
        {
            StartDive(foe);
        }
        else
        {
            nextDive = Time.time + DragonHorn.DiveSeconds.Value / 2f;
        }
    }

    // Roars again when foes are near and its roar has rested, and dives at its foe when its dive has.
    private void Fight()
    {
        if (DragonHorn.RoarSeconds.Value > 0f && Time.time >= nextRoar && NearestFoe(transform.position, DragonHorn.RoarRadius.Value) != null)
        {
            Roar();
        }

        if (diveTarget == null && DragonHorn.DiveDamage.Value > 0f && Time.time >= nextDive)
        {
            Character foe = Guarded() ?? ai.GetTargetCreature();
            if (foe != null && !foe.IsDead() && BaseAI.IsEnemy(character, foe)
                && Vector3.Distance(foe.transform.position, transform.position) <= DragonHorn.DiveRange.Value)
            {
                StartDive(foe);
            }
        }
    }

    // On the owner: every player hears the roar, the ground bursts under the dragon, and the foes around it stagger
    // and are slowed by fear.
    private void Roar()
    {
        nextRoar = Time.time + Mathf.Max(1f, DragonHorn.RoarSeconds.Value);
        view.InvokeRPC(ZNetView.Everybody, RoarRpc);
        Vector3 below = transform.position;
        below.y = ZoneSystem.instance.GetGroundHeight(below);
        GameObject effect = ZNetScene.instance.GetPrefab(RoarEffect);
        if (effect != null)
        {
            Instantiate(effect, below, Quaternion.identity);
        }

        float radius = DragonHorn.RoarRadius.Value;
        foreach (Character foe in Character.GetAllCharacters())
        {
            if (foe == character || foe.IsDead() || !BaseAI.IsEnemy(character, foe) || Utils.DistanceXZ(foe.transform.position, transform.position) > radius)
            {
                continue;
            }

            Vector3 away = foe.transform.position - transform.position;
            away.y = 0f;
            foe.Stagger(away.sqrMagnitude > 0.01f ? away.normalized : transform.forward);
            if (TerrorHash != 0 && DragonHorn.RoarSlowSeconds.Value > 0f && DragonHorn.RoarSlow.Value > 0f)
            {
                foe.GetSEMan().AddStatusEffect(TerrorHash, resetTime: true);
            }
        }
    }

    // On every machine: the roar itself. Only the sounds are played here; anything with its own network object would
    // be made once per player.
    private void RPC_Roar(long sender)
    {
        foreach (EffectList.EffectData effect in ai.m_alertedEffects.m_effectPrefabs ?? Array.Empty<EffectList.EffectData>())
        {
            if (effect.m_enabled && effect.m_prefab != null && effect.m_prefab.GetComponent<ZNetView>() == null
                && effect.m_prefab.GetComponentInChildren<ZSFX>(true) != null)
            {
                Instantiate(effect.m_prefab, transform.position, transform.rotation, transform);
            }
        }
    }

    // On the owner: remembers what hit the caller, so the dragon goes after it for a while.
    private void RPC_Guard(long sender, ZDOID attacker)
    {
        if (!view.IsOwner() || DragonHorn.GuardSeconds.Value <= 0f || ZNetScene.instance == null)
        {
            return;
        }

        Character foe = ZNetScene.instance.FindInstance(attacker)?.GetComponent<Character>();
        if (foe != null && !foe.IsDead() && BaseAI.IsEnemy(character, foe))
        {
            guard = foe;
            guardUntil = Time.time + DragonHorn.GuardSeconds.Value;
        }
    }

    private Character Guarded()
    {
        if (guard != null && (Time.time > guardUntil || guard.IsDead() || !BaseAI.IsEnemy(character, guard)))
        {
            guard = null;
        }

        return guard;
    }

    private Character NearestFoe(Vector3 around, float range)
    {
        Character nearest = null;
        float best = range;
        foreach (Character foe in Character.GetAllCharacters())
        {
            if (foe == character || foe.IsDead() || !BaseAI.IsEnemy(character, foe))
            {
                continue;
            }

            float distance = Vector3.Distance(around, foe.transform.position);
            if (distance <= best)
            {
                best = distance;
                nearest = foe;
            }
        }

        return nearest;
    }

    // Low and fast at its foe; the slam comes when it gets there.
    private void StartDive(Character foe)
    {
        diveTarget = foe;
        diveStart = Time.time;
        ai.m_flyAltitudeMin = DiveAltitude;
        character.m_flyFastSpeed = normalFlySpeed * DragonHorn.DiveSpeed.Value;
    }

    private void EndDive()
    {
        diveTarget = null;
        ai.m_flyAltitudeMin = normalAltitude;
        character.m_flyFastSpeed = normalFlySpeed;
        nextDive = Time.time + DragonHorn.DiveSeconds.Value;
    }

    // Flies towards the caller while arriving, and away and up while leaving. While helping it dives when it has begun
    // one, and otherwise lets the AI fight, held to the foe that hit its caller.
    private bool Fly(float dt)
    {
        if (!view.IsValid() || !view.IsOwner() || character.IsDead())
        {
            return false;
        }

        Vector3 point;
        switch (CurrentPhase)
        {
            case Phase.Arriving:
                Player caller = Caller();
                if (caller == null)
                {
                    return false;
                }

                point = caller.transform.position + Vector3.up * MonsterSettings.DragonFlyHeightMax.Value;
                break;
            case Phase.Leaving:
                point = transform.position + view.GetZDO().GetVec3(AwayKey, transform.forward) * 40f + Vector3.up * 12f;
                break;
            default:
                return Dive(dt) || HoldGuard();
        }

        ai.MoveTo(dt, point, 0f, run: true);
        return true;
    }

    private bool Dive(float dt)
    {
        if (diveTarget == null)
        {
            return false;
        }

        if (diveTarget.IsDead() || Time.time - diveStart > MaxDiveSeconds)
        {
            EndDive();
            return false;
        }

        Vector3 point = diveTarget.transform.position;
        if (Vector3.Distance(transform.position, point) <= DiveReach + diveTarget.GetRadius() + character.GetRadius())
        {
            DragonBreath breath = DragonHorn.Breath.Value;
            bool frost = breath == DragonBreath.Frost || (breath == DragonBreath.Both && UnityEngine.Random.value < 0.5f);
            DragonBlast.Slam(character, point, DragonHorn.DiveDamage.Value * DragonBlast.LevelFactor(character), DragonHorn.DiveRadius.Value,
                DragonHorn.DivePush.Value, frost);
            EndDive();
            return false;
        }

        ai.MoveTo(dt, point, 0f, run: true);
        return true;
    }

    // The game's AI picks the nearest foe every few seconds; the one that hit the caller is kept as its target.
    private bool HoldGuard()
    {
        Character foe = Guarded();
        if (foe != null)
        {
            ai.m_targetCreature = foe;
            ai.m_targetStatic = null;
            ai.m_updateTargetTimer = Mathf.Max(ai.m_updateTargetTimer, 1f);
        }

        return false;
    }

    // On every machine: the local player is blessed while their own dragon helps them nearby; the effect lasts a few
    // seconds, so it fades soon after they part.
    private void Bless()
    {
        Player local = Player.m_localPlayer;
        if (local == null || BlessingHash == 0 || DragonHorn.BlessingRange.Value <= 0f || Time.time < nextBless)
        {
            return;
        }

        nextBless = Time.time + BlessInterval;
        if (CurrentPhase == Phase.Helping && view.GetZDO().GetLong(CallerKey) == local.GetPlayerID()
            && Vector3.Distance(local.transform.position, transform.position) <= DragonHorn.BlessingRange.Value)
        {
            local.GetSEMan().AddStatusEffect(BlessingHash, resetTime: true);
        }
    }

    // Turns away from the caller, or the nearest player, and remembers the way so a new owner keeps it.
    private void Leave()
    {
        if (diveTarget != null)
        {
            EndDive();
        }

        Player from = Caller() ?? Player.GetClosestPlayer(transform.position, DragonHorn.VanishDistance.Value);
        Vector3 away = from != null ? transform.position - from.transform.position : transform.forward;
        away.y = 0f;
        view.GetZDO().Set(AwayKey, away.sqrMagnitude > 0.01f ? away.normalized : Vector3.forward);
        SetPhase(Phase.Leaving);
    }

    private void SetPhase(Phase phase)
    {
        view.GetZDO().Set(PhaseKey, (int)phase);
        view.GetZDO().Set(PhaseAtKey, ZNet.instance.GetTime().Ticks);
        lostFor = 0f;
    }

    private Player Caller()
    {
        return Player.GetPlayer(view.GetZDO().GetLong(CallerKey));
    }
}
