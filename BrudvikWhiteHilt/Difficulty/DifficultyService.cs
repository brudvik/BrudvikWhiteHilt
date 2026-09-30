using BrudvikWhiteHilt.Difficulty.Beasts;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Difficulty;

/// <summary>
/// Runs on the <see cref="Game"/> object. On the server it computes the pressure, answers beast
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

    private float nextClient;
    private long clientNight = long.MinValue;
    private int clientRequests;
    private int publishedBiomes = -1;

    /// <summary>
    /// Sends an admin command to the server.
    /// </summary>
    /// <param name="command">beast.</param>
    /// <param name="argument">The beast.</param>
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

    private void UpdateServer()
    {
        long night = DifficultyState.NightId();
        if (night != serverNight)
        {
            serverNight = night;
            beastsTonight.Clear();
            requestsTonight.Clear();
        }

        if (Time.time < nextCompute)
        {
            return;
        }

        nextCompute = Time.time + ComputeInterval;
        Compute();
        if (Time.time >= nextSend)
        {
            Broadcast();
        }
    }

    private void Compute()
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

        DifficultyState.Set(pressure, raw, biomes, count, day, gear);
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

    private void Broadcast()
    {
        nextSend = Time.time + SendInterval;
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

    private void WatchForBeast(Player player)
    {
        if (!DifficultySettings.Enabled.Value || !DifficultySettings.BeastsEnabled.Value || player.IsDead() || player.InInterior() || player.IsTeleporting())
        {
            return;
        }

        if (!InWindow(DifficultyState.Hours()))
        {
            return;
        }

        long night = DifficultyState.NightId();
        if (night != clientNight)
        {
            clientNight = night;
            clientRequests = 0;
        }

        if (clientRequests >= 1)
        {
            return;
        }

        Heightmap.Biome biome = player.GetCurrentBiome();
        bool atSea = Ship.GetLocalShip() != null;
        if (BeastDefinition.Resolve(biome, atSea) == null
            || !DifficultySettings.IsBadWeather(EnvMan.instance.GetCurrentEnvironment())
            || EffectArea.IsPointInsideArea(player.transform.position, EffectArea.Type.PlayerBase) != null)
        {
            return;
        }

        clientRequests++;
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

    private void RPC_BeastRequest(long sender, Vector3 position, int biome, bool atSea)
    {
        if (!ZNet.instance.IsServer() || !DifficultySettings.Enabled.Value || !DifficultySettings.BeastsEnabled.Value)
        {
            return;
        }

        requestsTonight.TryGetValue(sender, out int made);
        if (made >= 1)
        {
            return;
        }

        requestsTonight[sender] = made + 1;
        BeastDefinition beast = BeastDefinition.Resolve((Heightmap.Biome)biome, atSea);
        if (!CanCome(beast) || beastsTonight.Any(other => Vector3.Distance(other, position) < DifficultySettings.BeastSpacing.Value))
        {
            return;
        }

        float chance = DifficultySettings.BeastChance.Value / 100f * (0.5f + DifficultyState.Pressure);
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
