using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Difficulty;

/// <summary>
/// Runs on the <see cref="Game"/> object. On the server it computes the pressure and sends it to every client.
/// On a client it publishes the biomes the player has visited.
/// </summary>
public class DifficultyService : MonoBehaviour
{
    private const string StateRpc = "WhiteHiltDifficulty";
    private const string BiomesKey = "whitehilt_biomes";
    private const float ComputeInterval = 10f;
    private const float SendInterval = 30f;
    private const float ClientInterval = 2f;

    private static readonly Heightmap.Biome[] landBiomes =
    {
        Heightmap.Biome.Meadows, Heightmap.Biome.BlackForest, Heightmap.Biome.Swamp, Heightmap.Biome.Mountain,
        Heightmap.Biome.Plains, Heightmap.Biome.Mistlands, Heightmap.Biome.AshLands
    };

    private float smoothedPlayers = -1f;
    private float smoothedGear = -1f;
    private float nextCompute;
    private float lastCompute;
    private float nextSend;

    private float nextClient;
    private int publishedBiomes = -1;

    private void Start()
    {
        DifficultyState.Reset();
        ZRoutedRpc.instance?.Register<ZPackage>(StateRpc, RPC_State);
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

    private void RPC_State(long sender, ZPackage package)
    {
        if (sender == ZRoutedRpc.instance.GetServerPeerID() && !ZNet.instance.IsServer())
        {
            DifficultyState.Read(package);
        }
    }
}
