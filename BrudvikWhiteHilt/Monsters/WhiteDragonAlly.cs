using BrudvikWhiteHilt.Items.Summoning;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// A White Dragon called by the Dragon Horn. It appears out of sight behind its caller and flies in, fights at their
/// side, and when its time is up or its caller is gone it flies off and vanishes once no player is near enough to see
/// it go. Its phase and times live in its ZDO and are read on the world clock, so whoever owns it carries on.
/// </summary>
public class WhiteDragonAlly : MonoBehaviour
{
    private const float ArrivedDistance = 20f;
    private const float LostSeconds = 10f;

    private static readonly int CallerKey = "whitehilt_whitedragon_caller".GetStableHashCode();
    private static readonly int UntilKey = "whitehilt_whitedragon_until".GetStableHashCode();
    private static readonly int PhaseKey = "whitehilt_whitedragon_phase".GetStableHashCode();
    private static readonly int PhaseAtKey = "whitehilt_whitedragon_phase_at".GetStableHashCode();
    private static readonly int AwayKey = "whitehilt_whitedragon_away".GetStableHashCode();

    private static readonly Dictionary<BaseAI, WhiteDragonAlly> byAI = new();
    private static readonly HashSet<Character> characters = new();

    private ZNetView view;
    private Character character;
    private MonsterAI ai;
    private float lostFor;

    private enum Phase
    {
        Arriving,
        Helping,
        Leaving
    }

    /// <summary>Whether any White Dragon is loaded here.</summary>
    public static bool Any => characters.Count > 0;

    /// <summary>
    /// Calls a White Dragon for the local player: it appears out of sight behind them and flies in.
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
        zdo.Set(CallerKey, player.GetPlayerID());
        zdo.Set(UntilKey, now.AddSeconds(DragonHorn.StayHours.Value * DragonHorn.GameHourSeconds).Ticks);
        zdo.Set(PhaseKey, (int)Phase.Arriving);
        zdo.Set(PhaseAtKey, now.Ticks);
        dragon.SetLevel(DragonHorn.Stars.Value + 1);
        instance.GetComponent<Tameable>()?.Command(player, message: false);
        Jotunn.Logger.LogInfo($"White Dragon called by {player.GetPlayerName()}, coming from {point}");
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
    /// Flies a White Dragon in or off instead of letting its AI fight. Runs on the owner from the AI's update.
    /// </summary>
    /// <param name="ai">The creature's AI.</param>
    /// <param name="dt">Seconds since the last update.</param>
    /// <returns>True if the dragon was steered here and its AI must not run.</returns>
    public static bool Steer(BaseAI ai, float dt)
    {
        return byAI.TryGetValue(ai, out WhiteDragonAlly ally) && ally.Fly(dt);
    }

    private Phase CurrentPhase => (Phase)view.GetZDO().GetInt(PhaseKey);

    private double PhaseSeconds => (ZNet.instance.GetTime() - new DateTime(view.GetZDO().GetLong(PhaseAtKey))).TotalSeconds;

    private void Awake()
    {
        view = GetComponent<ZNetView>();
        character = GetComponent<Character>();
        ai = GetComponent<MonsterAI>();
        if (view.IsValid())
        {
            byAI[ai] = this;
            characters.Add(character);
        }
    }

    private void OnDestroy()
    {
        byAI.Remove(ai);
        characters.Remove(character);
    }

    // On the owner: moves the dragon on from arriving to helping, and from helping to leaving when its time is up or
    // its caller is lost; once it has left everyone's sight, it is gone.
    private void Update()
    {
        if (!view.IsValid() || !view.IsOwner() || character.IsDead() || ZNet.instance == null)
        {
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

                break;
            case Phase.Leaving:
                if (!Player.IsPlayerInRange(transform.position, DragonHorn.VanishDistance.Value) || PhaseSeconds > DragonHorn.FlightSeconds.Value)
                {
                    ZNetScene.instance.Destroy(gameObject);
                }

                break;
        }
    }

    // Flies towards the caller while arriving, and away and up while leaving; lets the AI fight while helping.
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
                return false;
        }

        ai.MoveTo(dt, point, 0f, run: true);
        return true;
    }

    // Turns away from the caller, or the nearest player, and remembers the way so a new owner keeps it.
    private void Leave()
    {
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
