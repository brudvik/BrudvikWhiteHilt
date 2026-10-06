using BrudvikWhiteHilt.Difficulty.Beasts;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Difficulty;

/// <summary>
/// Runs on the <see cref="Game"/> object. On the server it computes the pressure, schedules blood moons, answers beast
/// requests and sends the state to every client. On a client it publishes the biomes the player has visited, watches for
/// the dark hour and spawns the beasts the server allows.
/// </summary>
public class DifficultyService : MonoBehaviour
{
    private const string StateRpc = "WhiteHiltDifficulty";
    private const string BeastRequestRpc = "WhiteHiltBeastRequest";
    private const string BeastSpawnRpc = "WhiteHiltBeastSpawn";
    private const string AdminRpc = "WhiteHiltDifficultyAdmin";
    private const string AdminReplyRpc = "WhiteHiltDifficultyReply";
    private const string BiomesKey = "whitehilt_biomes";
    private const float ComputeInterval = 10f;
    private const float SendInterval = 30f;
    private const float ClientInterval = 2f;

    // During a blood moon a player's second beast roll waits this long after the first.
    private const float BloodMoonRollGap = 120f;

    private static readonly Heightmap.Biome[] landBiomes =
    {
        Heightmap.Biome.Meadows, Heightmap.Biome.BlackForest, Heightmap.Biome.Swamp, Heightmap.Biome.Mountain,
        Heightmap.Biome.Plains, Heightmap.Biome.Mistlands, Heightmap.Biome.AshLands
    };

    private readonly List<Vector3> beastsTonight = new();
    private readonly Dictionary<long, int> requestsTonight = new();
    private float smoothedPlayers = -1f;
    private float smoothedGear = -1f;
    private float nextCompute;
    private float lastCompute;
    private float nextSend;
    private long serverNight = long.MinValue;
    private long rolledNight = long.MinValue;
    private long bloodMoonNight = long.MinValue;
    private long lastBloodMoonNight = long.MinValue;
    private bool sentBloodMoon;

    private float nextClient;
    private long clientNight = long.MinValue;
    private int clientRequests;
    private float lastRequest;
    private bool shownBloodMoon;
    private int publishedBiomes = -1;

    /// <summary>
    /// Sends an admin command to the server.
    /// </summary>
    /// <param name="command">bloodmoon or beast.</param>
    /// <param name="argument">start/stop, or the beast.</param>
    public static void SendAdmin(string command, string argument)
    {
        if (ZRoutedRpc.instance == null || ZNet.instance == null)
        {
            Print("Join a world first.");
            return;
        }

        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), AdminRpc, command, argument ?? string.Empty);
    }

    private static void Print(string message)
    {
        if (global::Console.instance != null)
        {
            global::Console.instance.Print(message);
        }
    }

    private void Start()
    {
        DifficultyState.Reset();
        ZRoutedRpc.instance?.Register<ZPackage>(StateRpc, RPC_State);
        ZRoutedRpc.instance?.Register<Vector3, int, bool>(BeastRequestRpc, RPC_BeastRequest);
        ZRoutedRpc.instance?.Register<string>(BeastSpawnRpc, RPC_BeastSpawn);
        ZRoutedRpc.instance?.Register<string, string>(AdminRpc, RPC_Admin);
        ZRoutedRpc.instance?.Register<string>(AdminReplyRpc, RPC_AdminReply);
    }

    // Runs the server's and the client's parts; a failure is logged and retried later instead of stopping the feature.
    private void Update()
    {
        if (ZNet.instance == null || ZRoutedRpc.instance == null)
        {
            return;
        }

        try
        {
            if (ZNet.instance.IsServer())
            {
                UpdateServer();
            }

            UpdateClient();
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Difficulty: update failed: {ex.Message}");
            nextCompute = Time.time + ComputeInterval;
            nextClient = Time.time + ComputeInterval;
        }
    }

    // On the server: rolls for a blood moon at nightfall, works out the pressure now and then, and sends it to the
    // players.
    private void UpdateServer()
    {
        long night = DifficultyState.NightId();
        bool isNight = DifficultyState.IsNightHour(DifficultyState.Hours());
        if (night != serverNight)
        {
            serverNight = night;
            beastsTonight.Clear();
            requestsTonight.Clear();
        }

        if (isNight && rolledNight != night)
        {
            rolledNight = night;
            RollBloodMoon(night);
        }

        bool bloodMoon = DifficultySettings.Enabled.Value && DifficultySettings.BloodMoonEnabled.Value && bloodMoonNight == night && isNight;
        if (Time.time < nextCompute && bloodMoon == sentBloodMoon)
        {
            return;
        }

        nextCompute = Time.time + ComputeInterval;
        Compute(bloodMoon);
        if (Time.time >= nextSend || bloodMoon != sentBloodMoon)
        {
            Broadcast();
        }
    }

    // Works out the pressure from the players online, the days, the biomes reached and White Hilt gear worn. Players
    // and gear are smoothed, as they change when people come and go.
    private void Compute(bool bloodMoon)
    {
        List<ZDO> players = PlayerZdos();
        int count = players.Count;
        int day = EnvMan.instance != null ? EnvMan.instance.GetDay() : 0;
        int biomes = count > 0 ? players.Max(zdo => zdo.GetInt(BiomesKey, 1)) : DifficultyState.Biomes;
        int gear = players.Count(WhiteHiltGear.IsWearing);

        // Players and gear change as people come and go, so only they are smoothed; days and biomes only grow.
        float elapsed = Mathf.Max(0f, Time.time - lastCompute);
        lastCompute = Time.time;
        float playerFactor = Mathf.Clamp01((count - 1f) / Mathf.Max(1, DifficultySettings.PlayersForMax.Value - 1));
        float gearFactor = count > 0 ? gear / (float)count : 0f;
        if (count > 0)
        {
            smoothedPlayers = Smooth(smoothedPlayers, playerFactor, elapsed);
            smoothedGear = Smooth(smoothedGear, gearFactor, elapsed);
        }

        float dayFactor = Mathf.Clamp01(day / (float)Mathf.Max(1, DifficultySettings.DaysForMax.Value));
        float biomeFactor = Mathf.Clamp01((biomes - 1f) / Mathf.Max(1, DifficultySettings.BiomesForMax.Value - 1));
        float raw = Weigh(playerFactor, dayFactor, biomeFactor, gearFactor);
        float pressure = Weigh(Mathf.Max(0f, smoothedPlayers), dayFactor, biomeFactor, Mathf.Max(0f, smoothedGear));
        float forced = DifficultySettings.OverridePressure.Value;
        if (forced >= 0f)
        {
            raw = pressure = forced;
        }

        DifficultyState.Set(pressure, raw, bloodMoon, biomes, count, day, gear);
    }

    private static float Smooth(float current, float target, float elapsed)
    {
        float seconds = DifficultySettings.SmoothingMinutes.Value * 60f;
        if (current < 0f || seconds <= 0f)
        {
            return target;
        }

        return current + (target - current) * (1f - Mathf.Exp(-elapsed / seconds));
    }

    private static float Weigh(float players, float days, float biomes, float gear)
    {
        float wPlayers = DifficultySettings.WeightPlayers.Value;
        float wDays = DifficultySettings.WeightDays.Value;
        float wBiomes = DifficultySettings.WeightBiomes.Value;
        float wGear = DifficultySettings.WeightGear.Value;
        float total = wPlayers + wDays + wBiomes + wGear;
        return total <= 0f ? 0f : Mathf.Clamp01((players * wPlayers + days * wDays + biomes * wBiomes + gear * wGear) / total);
    }

    // The network data of every player online, the host's own included.
    private static List<ZDO> PlayerZdos()
    {
        List<ZDO> players = new();
        foreach (ZNetPeer peer in ZNet.instance.GetPeers())
        {
            ZDO zdo = peer.m_characterID.IsNone() ? null : ZDOMan.instance.GetZDO(peer.m_characterID);
            if (zdo != null)
            {
                players.Add(zdo);
            }
        }

        if (!ZNet.instance.IsDedicated() && Player.m_localPlayer != null)
        {
            ZDO zdo = Player.m_localPlayer.m_nview.GetZDO();
            if (zdo != null)
            {
                players.Add(zdo);
            }
        }

        return players;
    }

    // Rolls for a blood moon tonight, once some boss is defeated and not too soon after the last one; more likely under
    // more pressure.
    private void RollBloodMoon(long night)
    {
        if (!DifficultySettings.Enabled.Value || !DifficultySettings.BloodMoonEnabled.Value || !AnyBossDefeated())
        {
            return;
        }

        if (lastBloodMoonNight != long.MinValue && night - lastBloodMoonNight <= DifficultySettings.BloodMoonMinNights.Value)
        {
            return;
        }

        float chance = DifficultySettings.BloodMoonChance.Value / 100f * (0.5f + DifficultyState.Pressure);
        if (UnityEngine.Random.value < chance)
        {
            bloodMoonNight = night;
            lastBloodMoonNight = night;
            Jotunn.Logger.LogInfo($"Difficulty: a blood moon rises (night {night})");
        }
    }

    private static bool AnyBossDefeated()
    {
        return ZoneSystem.instance != null && BeastDefinition.BossKeys.Any(ZoneSystem.instance.GetGlobalKey);
    }

    private void Broadcast()
    {
        nextSend = Time.time + SendInterval;
        sentBloodMoon = DifficultyState.BloodMoon;
        ZPackage package = DifficultyState.Write();
        foreach (ZNetPeer peer in ZNet.instance.GetPeers())
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, StateRpc, package);
        }
    }

    private void UpdateClient()
    {
        Player player = Player.m_localPlayer;
        if (player == null || Time.time < nextClient)
        {
            return;
        }

        nextClient = Time.time + ClientInterval;
        PublishBiomes(player);
        AnnounceBloodMoon(player);
        WatchForBeast(player);
    }

    private void PublishBiomes(Player player)
    {
        int count = landBiomes.Count(biome => player.m_knownBiome.Contains(BiomeSector.GetBiomeName(biome)));
        if (count == publishedBiomes || !player.m_nview.IsValid() || !player.m_nview.IsOwner())
        {
            return;
        }

        publishedBiomes = count;
        player.m_nview.GetZDO().Set(BiomesKey, Math.Max(1, count));
    }

    private void AnnounceBloodMoon(Player player)
    {
        bool up = DifficultyState.BloodMoon;
        if (up == shownBloodMoon)
        {
            return;
        }

        shownBloodMoon = up;
        player.Message(MessageHud.MessageType.Center, up ? "$msg_whitehilt_bloodmoon_start" : "$msg_whitehilt_bloodmoon_end");
    }

    // On a client: in bad weather at night (or any night with a blood moon) away from bases, asks the server whether a
    // beast comes, a limited number of times per night.
    private void WatchForBeast(Player player)
    {
        if (!DifficultySettings.Enabled.Value || !DifficultySettings.BeastsEnabled.Value || player.IsDead() || player.InInterior() || player.IsTeleporting())
        {
            return;
        }

        bool bloodMoon = DifficultyState.BloodMoon;
        float hours = DifficultyState.Hours();
        if (bloodMoon ? !DifficultyState.IsNightHour(hours) : !InWindow(hours))
        {
            return;
        }

        long night = DifficultyState.NightId();
        if (night != clientNight)
        {
            clientNight = night;
            clientRequests = 0;
        }

        int allowed = bloodMoon ? DifficultySettings.BloodMoonBeastRolls.Value : 1;
        if (clientRequests >= allowed || (clientRequests > 0 && Time.time - lastRequest < BloodMoonRollGap))
        {
            return;
        }

        Heightmap.Biome biome = player.GetCurrentBiome();
        bool atSea = Ship.GetLocalShip() != null;
        if (BeastDefinition.Candidates(biome, atSea, bloodMoon).Count == 0
            || (!bloodMoon && !DifficultySettings.IsBadWeather(EnvMan.instance.GetCurrentEnvironment()))
            || EffectArea.IsPointInsideArea(player.transform.position, EffectArea.Type.PlayerBase) != null)
        {
            return;
        }

        clientRequests++;
        lastRequest = Time.time;
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), BeastRequestRpc, player.transform.position, (int)biome, atSea);
    }

    private static bool InWindow(float hours)
    {
        float start = DifficultySettings.WindowStart.Value;
        float end = DifficultySettings.WindowEnd.Value;
        return start <= end ? hours >= start && hours < end : hours >= start || hours < end;
    }

    private void RPC_State(long sender, ZPackage package)
    {
        if (sender == ZRoutedRpc.instance.GetServerPeerID() && !ZNet.instance.IsServer())
        {
            DifficultyState.Read(package);
        }
    }

    // On the server: decides whether a beast comes for a player and which, keeping count per player and night and
    // keeping beasts apart.
    private void RPC_BeastRequest(long sender, Vector3 position, int biome, bool atSea)
    {
        if (!ZNet.instance.IsServer() || !DifficultySettings.Enabled.Value || !DifficultySettings.BeastsEnabled.Value)
        {
            return;
        }

        bool bloodMoon = DifficultyState.BloodMoon;
        requestsTonight.TryGetValue(sender, out int made);
        if (made >= (bloodMoon ? DifficultySettings.BloodMoonBeastRolls.Value : 1))
        {
            return;
        }

        requestsTonight[sender] = made + 1;
        List<BeastDefinition> candidates = BeastDefinition.Candidates((Heightmap.Biome)biome, atSea, bloodMoon).Where(CanCome).ToList();
        if (candidates.Count == 0 || beastsTonight.Any(other => Vector3.Distance(other, position) < DifficultySettings.BeastSpacing.Value))
        {
            return;
        }

        BeastDefinition beast = candidates[UnityEngine.Random.Range(0, candidates.Count)];

        float chance = DifficultySettings.BeastChance.Value / 100f * (0.5f + DifficultyState.Pressure)
            * (bloodMoon ? DifficultySettings.BloodMoonBeastChance.Value : 1f);
        if (UnityEngine.Random.value >= Mathf.Min(chance, 0.95f))
        {
            return;
        }

        beastsTonight.Add(position);
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, BeastSpawnRpc, beast.Key);
    }

    private static bool CanCome(BeastDefinition beast)
    {
        return beast != null && DifficultySettings.IsBeastEnabled(beast.Key)
            && (!DifficultySettings.BeastRequiresBoss.Value || (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(beast.BossKey)));
    }

    private void RPC_BeastSpawn(long sender, string key)
    {
        Player player = Player.m_localPlayer;
        BeastDefinition beast = BeastDefinition.Find(key);
        if (sender == ZRoutedRpc.instance.GetServerPeerID() && player != null && beast != null)
        {
            BeastSpawner.Spawn(player, beast);
        }
    }

    // On the server: an admin's command to start or stop a blood moon or summon a beast.
    private void RPC_Admin(long sender, string command, string argument)
    {
        if (!ZNet.instance.IsServer())
        {
            return;
        }

        if (!IsAdmin(sender))
        {
            Reply(sender, "Only admins can do that.");
            return;
        }

        switch (command)
        {
            case "bloodmoon":
                SetBloodMoon(sender, !string.Equals(argument, "stop", StringComparison.OrdinalIgnoreCase));
                break;
            case "beast":
                BeastDefinition beast = BeastDefinition.Find(argument);
                if (beast == null)
                {
                    Reply(sender, $"Unknown beast '{argument}'. Use one of: {string.Join(", ", BeastDefinition.All.Select(each => each.Key))}");
                    return;
                }

                ZRoutedRpc.instance.InvokeRoutedRPC(sender, BeastSpawnRpc, beast.Key);
                break;
        }
    }

    // Starts a blood moon tonight (or at nightfall) or stops it.
    private void SetBloodMoon(long sender, bool start)
    {
        long night = DifficultyState.NightId();
        bool isNight = DifficultyState.IsNightHour(DifficultyState.Hours());
        if (start)
        {
            bloodMoonNight = isNight ? night : night + 1;
            lastBloodMoonNight = bloodMoonNight;
            Reply(sender, isNight ? "The blood moon rises." : "The blood moon rises at nightfall.");
        }
        else
        {
            bloodMoonNight = long.MinValue;
            Reply(sender, "The blood moon is stopped.");
        }

        nextCompute = 0f;
    }

    private static bool IsAdmin(long sender)
    {
        if (sender == ZRoutedRpc.instance.m_id)
        {
            return true;
        }

        ZNetPeer peer = ZNet.instance.GetPeer(sender);
        return peer?.m_socket != null && ZNet.instance.IsAdmin(peer.m_socket.GetHostName());
    }

    private static void Reply(long target, string message)
    {
        ZRoutedRpc.instance.InvokeRoutedRPC(target, AdminReplyRpc, message);
    }

    private void RPC_AdminReply(long sender, string message)
    {
        if (sender == ZRoutedRpc.instance.GetServerPeerID())
        {
            Print(message);
        }
    }
}
