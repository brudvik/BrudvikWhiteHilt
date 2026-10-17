using System.Collections.Generic;
using BrudvikWhiteHilt.Difficulty;
using UnityEngine;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// Runs on the <see cref="Game"/> object. Once a minute, a player close to the lava in the Ashlands asks the server; the
/// server rolls the chance, keeps a cooldown per player and tells that client to let the Lava Worm rise out of the lava
/// nearby. Admins can call it with whitehilt_lavaworm.
/// </summary>
public class LavaWormService : MonoBehaviour
{
    private const string RequestRpc = "WhiteHiltLavaWormRequest";
    private const string SpawnRpc = "WhiteHiltLavaWormSpawn";
    private const string AdminRpc = "WhiteHiltLavaWormAdmin";
    private const string ReplyRpc = "WhiteHiltLavaWormReply";
    private const float CheckInterval = 60f;
    private const float NearbyRange = 100f;
    private const float BaseMargin = 15f;
    private const float MinDistance = 6f;
    private const int Attempts = 40;
    private const float LevelUpChance = 10f;
    private const int MaxRolledLevel = 3;

    private readonly Dictionary<long, float> lastWorm = new();
    private float nextCheck;

    /// <summary>
    /// Asks the server, as an admin, to let the Lava Worm rise near the local player now.
    /// </summary>
    public static void Summon()
    {
        if (ZRoutedRpc.instance == null || Player.m_localPlayer == null)
        {
            Print("Enter a world first.");
            return;
        }

        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), AdminRpc);
    }

    /// <summary>
    /// Describes the conditions for the Lava Worm where the local player is, for the console.
    /// </summary>
    /// <returns>One line per condition.</returns>
    public static string Describe()
    {
        Player player = Player.m_localPlayer;
        Vector3 position = player != null ? player.transform.position : Vector3.zero;
        string key = LavaWormSettings.RequiredKey.Value;
        return $"Lava Worm: enabled {LavaWormSettings.Enabled.Value}\n" +
            $"  biome: {(player != null ? Heightmap.FindBiome(position) : Heightmap.Biome.None)} (needed: AshLands)\n" +
            $"  outside: {player != null && Outside(player)}, outside a base: {player != null && EffectArea.IsPointInsideArea(position, EffectArea.Type.PlayerBase, BaseMargin) == null}\n" +
            $"  lava within {LavaWormSettings.LavaRange.Value:0} m: {player != null && TryFindLava(position, out _)}\n" +
            $"  key {(string.IsNullOrEmpty(key) ? "(none)" : key)}: {string.IsNullOrEmpty(key) || (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(key))}\n" +
            $"  Lava Worms nearby: {CountNear(position)}";
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
        ZRoutedRpc.instance?.Register(RequestRpc, RPC_Request);
        ZRoutedRpc.instance?.Register(SpawnRpc, RPC_Spawn);
        ZRoutedRpc.instance?.Register(AdminRpc, RPC_Admin);
        ZRoutedRpc.instance?.Register<string>(ReplyRpc, RPC_Reply);
        nextCheck = Time.time + CheckInterval;
    }

    private void Update()
    {
        if (Time.time < nextCheck || ZRoutedRpc.instance == null)
        {
            return;
        }

        nextCheck = Time.time + CheckInterval;
        if (Conditions(Player.m_localPlayer))
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), RequestRpc);
        }
    }

    private static bool Conditions(Player player)
    {
        if (!LavaWormSettings.Enabled.Value || player == null || player.IsDead() || !Outside(player))
        {
            return false;
        }

        Vector3 position = player.transform.position;
        string key = LavaWormSettings.RequiredKey.Value;
        return (string.IsNullOrEmpty(key) || ZoneSystem.instance.GetGlobalKey(key))
            && Heightmap.FindBiome(position) == Heightmap.Biome.AshLands
            && EffectArea.IsPointInsideArea(position, EffectArea.Type.PlayerBase, BaseMargin) == null
            && CountNear(position) == 0
            && TryFindLava(position, out _);
    }

    // On foot or riding an asksvin, but not in a dungeon or aboard a ship.
    private static bool Outside(Player player)
    {
        return !player.InInterior() && player.GetStandingOnShip() == null && (LindormService.OnFoot(player) || player.IsRiding());
    }

    private static int CountNear(Vector3 position)
    {
        int count = 0;
        foreach (LindormBurrow worm in LindormBurrow.Instances)
        {
            if (worm is LavaWormBurrow && Vector3.Distance(worm.transform.position, position) < NearbyRange)
            {
                count++;
            }
        }

        return count;
    }

    private void RPC_Request(long sender)
    {
        if (!ZNet.instance.IsServer() || !LavaWormSettings.Enabled.Value
            || (lastWorm.TryGetValue(sender, out float last) && Time.time - last < LavaWormSettings.CooldownMinutes.Value * 60f)
            || Random.value * 100f >= LavaWormSettings.ChancePerMinute.Value)
        {
            return;
        }

        lastWorm[sender] = Time.time;
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, SpawnRpc);
    }

    private void RPC_Spawn(long sender)
    {
        if (sender == ZRoutedRpc.instance.GetServerPeerID() && Player.m_localPlayer != null)
        {
            Spawn(Player.m_localPlayer);
        }
    }

    private void RPC_Admin(long sender)
    {
        if (!ZNet.instance.IsServer())
        {
            return;
        }

        if (!LindormService.IsAdmin(sender))
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(sender, ReplyRpc, "Only admins can do that.");
            return;
        }

        lastWorm[sender] = Time.time;
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, SpawnRpc);
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, ReplyRpc, "The lava stirs.");
    }

    private void RPC_Reply(long sender, string message)
    {
        if (sender == ZRoutedRpc.instance.GetServerPeerID())
        {
            Print(message);
        }
    }

    // Spawns the Lava Worm in the lava near the player, hunting them. Its stars are rolled as a spawn system rolls them,
    // so the difficulty's level-up chance and higher stars apply.
    private static void Spawn(Player player)
    {
        GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(LavaWormRegistry.PrefabName) : null;
        if (prefab == null || !TryFindLava(player.transform.position, out Vector3 point))
        {
            Jotunn.Logger.LogInfo($"No lava for the Lava Worm near {player.GetPlayerName()}");
            return;
        }

        Vector3 toPlayer = player.transform.position - point;
        toPlayer.y = 0f;
        int level = 1;
        CreatureStars.BeginSpawn(false);
        try
        {
            GameObject instance = Instantiate(prefab, point, toPlayer.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toPlayer) : Quaternion.identity);
            while (level < MaxRolledLevel && Random.value * 100f < LevelUpChance * CreatureStars.LevelUpChanceMultiplier())
            {
                level++;
            }

            instance.GetComponent<Character>()?.SetLevel(level);
            instance.GetComponent<BaseAI>()?.SetHuntPlayer(true);
        }
        finally
        {
            CreatureStars.EndSpawn();
        }

        Jotunn.Logger.LogInfo($"The Lava Worm rose out of the lava near {player.GetPlayerName()} at {point}, level {level}");
    }

    // The nearest lava within range of the player, outside any base, and not right under their feet.
    private static bool TryFindLava(Vector3 origin, out Vector3 point)
    {
        ZoneSystem zones = ZoneSystem.instance;
        float range = LavaWormSettings.LavaRange.Value;
        point = Vector3.zero;
        float best = float.MaxValue;
        for (int i = 0; i < Attempts; i++)
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            float distance = Mathf.Lerp(MinDistance, range, (i % 8 + Random.value) / 8f);
            Vector3 candidate = origin + new Vector3(direction.x, 0f, direction.y) * distance;
            if (distance >= best || !zones.IsLava(candidate))
            {
                continue;
            }

            candidate.y = zones.GetGroundHeight(candidate);
            if (EffectArea.IsPointInsideArea(candidate, EffectArea.Type.PlayerBase, BaseMargin) == null)
            {
                point = candidate;
                best = distance;
            }
        }

        return best < float.MaxValue;
    }
}
