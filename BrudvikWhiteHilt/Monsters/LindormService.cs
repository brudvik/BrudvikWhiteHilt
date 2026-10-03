using System.Collections.Generic;
using BrudvikWhiteHilt.Difficulty;
using UnityEngine;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// Runs on the <see cref="Game"/> object. Once a minute, a player on foot in the forest or the swamp at night asks the
/// server; the server rolls the chance, keeps a cooldown per player and tells that client to let the Lindorm break out of
/// the ground nearby. Admins can call it with whitehilt_lindorm.
/// </summary>
public class LindormService : MonoBehaviour
{
    private const string RequestRpc = "WhiteHiltLindormRequest";
    private const string SpawnRpc = "WhiteHiltLindormSpawn";
    private const string AdminRpc = "WhiteHiltLindormAdmin";
    private const string ReplyRpc = "WhiteHiltLindormReply";
    private const float CheckInterval = 60f;
    private const float NearbyRange = 100f;
    private const float BaseMargin = 15f;
    private const float MinDistance = 9f;
    private const float MaxDistance = 15f;
    private const int Attempts = 30;

    private readonly Dictionary<long, float> lastLindorm = new();
    private float nextCheck;

    /// <summary>
    /// Asks the server, as an admin, to let the Lindorm break out near the local player now.
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
    /// Describes the conditions for the Lindorm where the local player is, for the console.
    /// </summary>
    /// <returns>One line per condition.</returns>
    public static string Describe()
    {
        Player player = Player.m_localPlayer;
        Vector3 position = player != null ? player.transform.position : Vector3.zero;
        string key = MonsterSettings.LindormRequiredKey.Value;
        return $"Lindorm: enabled {MonsterSettings.LindormEnabled.Value}\n" +
            $"  biome: {(player != null ? Heightmap.FindBiome(position) : Heightmap.Biome.None)} (allowed: {MonsterSettings.LindormBiomes.Value})\n" +
            $"  on foot: {player != null && OnFoot(player)}, outside a base: {player != null && EffectArea.IsPointInsideArea(position, EffectArea.Type.PlayerBase, BaseMargin) == null}\n" +
            $"  night: {EnvMan.IsNight()} (needed: {MonsterSettings.LindormNightOnly.Value})\n" +
            $"  key {(string.IsNullOrEmpty(key) ? "(none)" : key)}: {string.IsNullOrEmpty(key) || (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(key))}\n" +
            $"  Lindorms nearby: {CountNear(position)}, progression stars: {MonsterSettings.GetLindormLevel() - 1}";
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
        ZRoutedRpc.instance?.Register<int>(SpawnRpc, RPC_Spawn);
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
        if (!MonsterSettings.LindormEnabled.Value || player == null || player.IsDead() || !OnFoot(player))
        {
            return false;
        }

        Vector3 position = player.transform.position;
        string key = MonsterSettings.LindormRequiredKey.Value;
        return (string.IsNullOrEmpty(key) || ZoneSystem.instance.GetGlobalKey(key))
            && (!MonsterSettings.LindormNightOnly.Value || EnvMan.IsNight())
            && MonsterSettings.IsLindormBiome(Heightmap.FindBiome(position))
            && EffectArea.IsPointInsideArea(position, EffectArea.Type.PlayerBase, BaseMargin) == null
            && CountNear(position) == 0;
    }

    private static bool OnFoot(Player player)
    {
        return !player.InInterior() && !player.IsAttached() && !player.InWater() && player.GetStandingOnShip() == null;
    }

    private static int CountNear(Vector3 position)
    {
        int count = 0;
        foreach (LindormBurrow lindorm in LindormBurrow.Instances)
        {
            if (lindorm != null && Vector3.Distance(lindorm.transform.position, position) < NearbyRange)
            {
                count++;
            }
        }

        return count;
    }

    private void RPC_Request(long sender)
    {
        if (!ZNet.instance.IsServer() || !MonsterSettings.LindormEnabled.Value
            || (lastLindorm.TryGetValue(sender, out float last) && Time.time - last < MonsterSettings.LindormCooldownMinutes.Value * 60f)
            || Random.value * 100f >= MonsterSettings.LindormChancePerMinute.Value)
        {
            return;
        }

        lastLindorm[sender] = Time.time;
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, SpawnRpc, MonsterSettings.GetLindormLevel());
    }

    private void RPC_Spawn(long sender, int level)
    {
        if (sender == ZRoutedRpc.instance.GetServerPeerID() && Player.m_localPlayer != null)
        {
            Spawn(Player.m_localPlayer, level);
        }
    }

    private void RPC_Admin(long sender)
    {
        if (!ZNet.instance.IsServer())
        {
            return;
        }

        if (!IsAdmin(sender))
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(sender, ReplyRpc, "Only admins can do that.");
            return;
        }

        lastLindorm[sender] = Time.time;
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, SpawnRpc, MonsterSettings.GetLindormLevel());
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, ReplyRpc, "The ground heaves.");
    }

    private void RPC_Reply(long sender, string message)
    {
        if (sender == ZRoutedRpc.instance.GetServerPeerID())
        {
            Print(message);
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

    private static void Spawn(Player player, int level)
    {
        GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(MonsterRegistry.LindormName) : null;
        if (prefab == null || !TryFindPoint(player.transform.position, out Vector3 point))
        {
            Jotunn.Logger.LogInfo($"No place for the Lindorm near {player.GetPlayerName()}");
            return;
        }

        Vector3 toPlayer = player.transform.position - point;
        toPlayer.y = 0f;
        GameObject instance;
        CreatureStars.BeginSpawn(false);
        try
        {
            instance = Instantiate(prefab, point, toPlayer.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toPlayer) : Quaternion.identity);
        }
        finally
        {
            CreatureStars.EndSpawn();
        }
        instance.GetComponent<Character>()?.SetLevel(Mathf.Clamp(level, 1, CreatureStars.MaxLevel));
        instance.GetComponent<BaseAI>()?.SetHuntPlayer(true);
        Jotunn.Logger.LogInfo($"The Lindorm broke out of the ground near {player.GetPlayerName()} at {point}, level {level}");
    }

    private static bool TryFindPoint(Vector3 origin, out Vector3 point)
    {
        ZoneSystem zones = ZoneSystem.instance;
        for (int i = 0; i < Attempts; i++)
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            point = origin + new Vector3(direction.x, 0f, direction.y) * Random.Range(MinDistance, MaxDistance);
            // Ground, not the solid height: that would put it on top of a tree.
            float height = zones.GetGroundHeight(point);
            if (height < zones.m_waterLevel + 0.3f)
            {
                continue;
            }

            point.y = height;
            if (EffectArea.IsPointInsideArea(point, EffectArea.Type.PlayerBase, BaseMargin) == null)
            {
                return true;
            }
        }

        point = Vector3.zero;
        return false;
    }
}
