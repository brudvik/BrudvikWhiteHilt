using BepInEx.Configuration;
using BrudvikWhiteHilt.Difficulty;
using BrudvikWhiteHilt.Difficulty.Beasts;
using BrudvikWhiteHilt.Kraken;
using BrudvikWhiteHilt.Progression;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Summoning;

/// <summary>Validates horn calls on the server and authorizes spawning on the caller's client.</summary>
public class SummoningHornService : MonoBehaviour
{
    /// <summary>The horn's prefab name.</summary>
    public const string HornName = "WhiteHiltSummoningHorn";
    /// <summary>The synchronized start time of a horn call.</summary>
    public const string CallKey = "whitehilt_horn_call";
    /// <summary>Marks an encounter deliberately called by a horn.</summary>
    public const string SummonedKey = "whitehilt_horn_summoned";
    private const string RequestRpc = "WhiteHiltHornCall";
    private const string ReplyRpc = "WhiteHiltHornReply";
    private const string SpawnRpc = "WhiteHiltHornSpawn";
    private const string ResultRpc = "WhiteHiltHornResult";
    private float nextCall;
    private readonly Dictionary<long, long> handledCalls = new();
    private readonly HashSet<long> spawnedCalls = new();
    private long pendingSender;
    private long pendingCall;
    private float pendingUntil;

    /// <summary>Whether the horn can summon encounters.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }
    /// <summary>World-wide delay between successful calls, in seconds.</summary>
    public static ConfigEntry<float> CooldownSeconds { get; private set; }
    /// <summary>Length of a horn call, in seconds.</summary>
    public static ConfigEntry<float> BlowSeconds { get; private set; }
    /// <summary>Distance within which an existing encounter blocks a call.</summary>
    public static ConfigEntry<float> EncounterRange { get; private set; }
    /// <summary>Distance over which the horn is audible.</summary>
    public static ConfigEntry<float> SoundRange { get; private set; }
    /// <summary>Allowed network delay after a completed horn call.</summary>
    public static ConfigEntry<float> NetworkGraceSeconds { get; private set; }

    /// <summary>Binds the server-synchronized settings.</summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindAdminOnly("SummoningHorn", "Enabled", true, "Allow the horn to summon Kraken and biome beasts.");
        CooldownSeconds = WhiteHiltConfig.BindAdminOnly("SummoningHorn", "CooldownSeconds", 300f, "World-wide delay between successful horn calls.", new AcceptableValueRange<float>(1f, 86400f));
        BlowSeconds = WhiteHiltConfig.BindAdminOnly("SummoningHorn", "BlowSeconds", 5f, "Seconds of uninterrupted blowing before a summon.", new AcceptableValueRange<float>(2f, 10f));
        EncounterRange = WhiteHiltConfig.BindAdminOnly("SummoningHorn", "EncounterRange", 150f, "A nearby black beast or Kraken prevents another call.", new AcceptableValueRange<float>(25f, 1000f));
        SoundRange = WhiteHiltConfig.BindAdminOnly("SummoningHorn", "SoundRange", 150f, "Maximum distance at which the horn can be heard.", new AcceptableValueRange<float>(10f, 500f));
        NetworkGraceSeconds = WhiteHiltConfig.BindAdminOnly("SummoningHorn", "NetworkGraceSeconds", 5f, "Allowed network delay after finishing a horn call, and timeout for client spawn confirmation.", new AcceptableValueRange<float>(1f, 30f));
    }

    /// <summary>Requests an encounter after the local player finishes blowing.</summary>
    public static void Request()
    {
        Player player = Player.m_localPlayer;
        Ship ship = Ship.Instances.OfType<Ship>().FirstOrDefault(candidate => candidate.m_players.Contains(player));
        ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), RequestRpc,
            ship != null ? ship.m_nview.GetZDO().m_uid : ZDOID.None);
    }

    private void Start()
    {
        ZRoutedRpc.instance.Register<ZDOID>(RequestRpc, RPC_Call);
        ZRoutedRpc.instance.Register<long, ZDOID, string>(SpawnRpc, RPC_Spawn);
        ZRoutedRpc.instance.Register<long, bool>(ResultRpc, RPC_Result);
        ZRoutedRpc.instance.Register<string>(ReplyRpc, RPC_Reply);
    }

    private void RPC_Call(long sender, ZDOID shipId)
    {
        if (ZNet.instance == null || !ZNet.instance.IsServer())
            return;
        if (!Enabled.Value)
        {
            Reject(sender, "horn disabled");
            return;
        }
        ZNetPeer peer = ZNet.instance.GetPeer(sender);
        ZDOID playerId = peer != null ? peer.m_characterID
            : sender == ZDOMan.GetSessionID() && Player.m_localPlayer != null ? Player.m_localPlayer.GetZDOID() : ZDOID.None;
        ZDO zdo = playerId.IsNone() ? null : ZDOMan.instance.GetZDO(playerId);
        if (zdo == null || zdo.GetOwner() != sender || zdo.GetFloat(ZDOVars.s_health, 1f) <= 0f)
        {
            Reject(sender, "caller network data missing, not owned by sender, or dead");
            return;
        }
        if (zdo.GetInt(ZDOVars.s_rightItem) != HornName.GetStableHashCode())
        {
            Reject(sender, "horn not equipped");
            return;
        }
        long started = zdo.GetLong(CallKey);
        double elapsed = (ZNet.instance.GetTime().Ticks - started) / (double)System.TimeSpan.TicksPerSecond;
        if (started == 0 || elapsed < BlowSeconds.Value || elapsed > BlowSeconds.Value + NetworkGraceSeconds.Value
            || (handledCalls.TryGetValue(sender, out long previous) && started <= previous))
        {
            Reject(sender, $"invalid or repeated call timestamp (elapsed {elapsed:F2}s)");
            return;
        }
        handledCalls[sender] = started;
        if (pendingCall != 0 && Time.time >= pendingUntil)
        {
            Reject(pendingSender, "spawn confirmation timed out", "$whitehilt_horn_space");
            pendingCall = 0;
        }
        if (Time.time < nextCall || pendingCall != 0)
        {
            Reply(sender, "$whitehilt_horn_wait");
            return;
        }
        Vector3 position = zdo.GetPosition();
        if (HasNearbyEncounter(position))
        {
            Reply(sender, "$whitehilt_horn_nearby");
            return;
        }
        Heightmap.Biome biome = WorldGenerator.instance.GetBiome(position);
        string beastKey = "";
        if (biome == Heightmap.Biome.Ocean)
        {
            ZDO ship = shipId.IsNone() ? null : ZDOMan.instance.GetZDO(shipId);
            Ship prefab = ship != null ? ZNetScene.instance.GetPrefab(ship.GetPrefab())?.GetComponent<Ship>() : null;
            if (prefab == null || !KrakenSettings.Enabled.Value
                || Vector3.Distance(position, ship.GetPosition()) > prefab.m_floatCollider.size.magnitude * prefab.transform.lossyScale.magnitude
                || WorldGenerator.instance.GetHeight(ship.GetPosition().x, ship.GetPosition().z)
                    >= ZoneSystem.instance.m_waterLevel - KrakenSettings.MinDepth.Value)
            {
                Reply(sender, "$whitehilt_horn_ship");
                return;
            }
            if (!HasKey(KrakenSettings.RequiredKey.Value))
            {
                Reply(sender, "$whitehilt_horn_locked");
                return;
            }
        }
        else
        {
            var candidates = BeastDefinition.Candidates(biome, false, false)
                .Where(beast => DifficultySettings.BeastsEnabled.Value && DifficultySettings.IsBeastEnabled(beast.Key)).ToList();
            if (candidates.Count == 0)
            {
                Reply(sender, "$whitehilt_horn_empty");
                return;
            }
            var unlocked = candidates.Where(beast => HasKey(beast.BossKey)).ToList();
            if (unlocked.Count == 0)
            {
                Reply(sender, "$whitehilt_horn_locked");
                return;
            }
            beastKey = unlocked[UnityEngine.Random.Range(0, unlocked.Count)].Key;
        }
        pendingSender = sender;
        pendingCall = started;
        pendingUntil = Time.time + NetworkGraceSeconds.Value;
        Jotunn.Logger.LogInfo($"Horn: authorizing {sender}, biome {biome}, encounter {(beastKey.Length == 0 ? "Kraken" : beastKey)}");
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, SpawnRpc, started, shipId, beastKey);
    }

    private void RPC_Spawn(long sender, long started, ZDOID shipId, string beastKey)
    {
        if (sender != ZRoutedRpc.instance.GetServerPeerID() || !spawnedCalls.Add(started))
            return;
        Jotunn.Logger.LogInfo($"Horn: received authorization for {(string.IsNullOrEmpty(beastKey) ? "Kraken" : beastKey)}");
        bool spawned = false;
        try
        {
            Player player = Player.m_localPlayer;
            if (player != null && !player.IsDead() && !player.InInterior() && Enabled.Value
                && player.m_nview.GetZDO().GetLong(CallKey) == started
                && player.m_nview.GetZDO().GetInt(ZDOVars.s_rightItem) == HornName.GetStableHashCode())
            {
                if (string.IsNullOrEmpty(beastKey))
                {
                    Ship ship = ZNetScene.instance.FindInstance(shipId)?.GetComponent<Ship>();
                    if (ship != null && ship.m_players.Contains(player) && KrakenSettings.Enabled.Value
                        && Heightmap.FindBiome(player.transform.position) == Heightmap.Biome.Ocean
                        && KrakenSpawner.DeepEnough(ship.transform.position, KrakenSettings.MinDepth.Value))
                        spawned = KrakenSpawner.Spawn(ship, summoned: true);
                }
                else
                {
                    BeastDefinition beast = BeastDefinition.Candidates(Heightmap.FindBiome(player.transform.position), false, false)
                        .FirstOrDefault(candidate => candidate.Key == beastKey);
                    if (beast != null && DifficultySettings.BeastsEnabled.Value && DifficultySettings.IsBeastEnabled(beast.Key))
                        spawned = BeastSpawner.Spawn(player, beast, summoned: true);
                }
            }
        }
        catch (Exception exception)
        {
            Jotunn.Logger.LogError($"Horn: client spawn failed: {exception}");
        }
        Jotunn.Logger.LogInfo($"Horn: client spawn result={spawned}");
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), ResultRpc, started, spawned);
    }

    private void RPC_Result(long sender, long started, bool spawned)
    {
        if (!ZNet.instance.IsServer() || pendingCall == 0 || sender != pendingSender || started != pendingCall)
            return;
        pendingCall = 0;
        if (spawned)
            nextCall = Time.time + CooldownSeconds.Value;
        Jotunn.Logger.LogInfo($"Horn: caller {sender} confirmed spawn={spawned}");
        Reply(sender, spawned ? "$whitehilt_horn_answer" : "$whitehilt_horn_space");
    }

    private static bool HasNearbyEncounter(Vector3 position)
    {
        var centre = ZoneSystem.GetZone(position);
        int radius = Mathf.CeilToInt(EncounterRange.Value / ZoneSystem.instance.m_zoneSize) + 1;
        var encounters = new List<ZDO>();
        ZDOMan.instance.FindSectorObjects(centre, new SimulationDistance(radius, 0, classic: true), encounters);
        foreach (ZDO encounter in encounters)
        {
            if (encounter.GetFloat(ZDOVars.s_health, 1f) <= 0f
                || Vector3.Distance(encounter.GetPosition(), position) >= EncounterRange.Value)
                continue;
            GameObject prefab = ZNetScene.instance.GetPrefab(encounter.GetPrefab());
            if (prefab != null && (prefab.GetComponent<KrakenBody>() != null || prefab.GetComponent<BeastBehaviour>() != null))
                return true;
        }
        return false;
    }

    private static bool HasKey(string key) => string.IsNullOrEmpty(key) || ZoneSystem.instance.GetGlobalKey(key);

    private static void Reject(long sender, string reason, string text = null)
    {
        Jotunn.Logger.LogWarning($"Horn: rejected caller {sender}: {reason}");
        if (text != null)
            Reply(sender, text);
    }

    private static void Reply(long sender, string text)
    {
        Jotunn.Logger.LogInfo($"Horn: reply to {sender}: {text}");
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, ReplyRpc, text);
    }

    private static void RPC_Reply(long sender, string text)
    {
        if (sender == ZRoutedRpc.instance.GetServerPeerID())
        {
            Jotunn.Logger.LogInfo($"Horn: server reply: {text}");
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center, text);
        }
    }
}