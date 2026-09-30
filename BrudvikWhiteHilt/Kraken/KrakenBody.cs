using System.Collections.Generic;
using System.Linq;
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

    /// <summary>ZDO key of the time (ticks) the Kraken started to sink back; 0 while it stays.</summary>
    public const string RetreatKey = "whitehilt_kraken_retreat";

    private const string StartKey = "whitehilt_kraken_start";
    private const string SpawnedKey = "whitehilt_kraken_spawned";
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

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        character = GetComponent<Character>();
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
        bool dawn = KrakenSettings.NightOnly.Value && !EnvMan.IsNight();
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

    private float Seconds(string key)
    {
        long ticks = nview.IsValid() ? nview.GetZDO().GetLong(key) : 0L;
        return ticks == 0L ? 0f : (float)System.TimeSpan.FromTicks(ZNet.instance.GetTime().Ticks - ticks).TotalSeconds;
    }
}
