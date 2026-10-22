using System.Collections.Generic;
using BrudvikWhiteHilt.Difficulty;
using UnityEngine;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// Runs on the <see cref="Game"/> object. Once a minute, a player out in the Ashlands asks the server; the server rolls
/// the chance, keeps a cooldown per player and tells that client to let a Muspell Giant walk out of the fire some way
/// off. The game's spawn system rolled once per loaded zone, so several zones round a player each had their chance and
/// the giant came far more often than its numbers said. Admins can call it with whitehilt_muspell.
/// </summary>
public class MuspellService : MonoBehaviour
{
    private const string RequestRpc = "WhiteHiltMuspellRequest";
    private const string SpawnRpc = "WhiteHiltMuspellSpawn";
    private const string AdminRpc = "WhiteHiltMuspellAdmin";
    private const string ReplyRpc = "WhiteHiltMuspellReply";
    private const float CheckInterval = 60f;
    private const float NearbyRange = 200f;
    private const float BaseMargin = 30f;
    private const float MinDistance = 45f;
    private const float MaxDistance = 70f;
    private const int Attempts = 40;

    private readonly Dictionary<long, float> lastGiant = new();
    private float nextCheck;

    /// <summary>
    /// Asks the server, as an admin, to let a Muspell Giant come near the local player now.
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
    /// Describes the conditions for the Muspell Giant where the local player is, for the console.
    /// </summary>
    /// <returns>One line per condition.</returns>
    public static string Describe()
    {
        Player player = Player.m_localPlayer;
        Vector3 position = player != null ? player.transform.position : Vector3.zero;
        string key = MuspellSettings.RequiredKey.Value;
        return $"Muspell Giant: enabled {MuspellSettings.Enabled.Value}, {MuspellSettings.ChancePerMinute.Value:0.#}% a minute, " +
            $"{MuspellSettings.CooldownMinutes.Value:0} minutes between\n" +
            $"  biome: {(player != null ? Heightmap.FindBiome(position) : Heightmap.Biome.None)} (needed: AshLands)\n" +
            $"  outside: {player != null && Outside(player)}, outside a base: {player != null && EffectArea.IsPointInsideArea(position, EffectArea.Type.PlayerBase, BaseMargin) == null}\n" +
            $"  key {(string.IsNullOrEmpty(key) ? "(none)" : key)}: {string.IsNullOrEmpty(key) || (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(key))}\n" +
            $"  Muspell Giants within {NearbyRange:0} m: {CountNear(position)}";
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
        if (!MuspellSettings.Enabled.Value || MuspellSettings.ChancePerMinute.Value <= 0f || player == null || player.IsDead() || !Outside(player))
        {
            return false;
        }

        Vector3 position = player.transform.position;
        string key = MuspellSettings.RequiredKey.Value;
        return (string.IsNullOrEmpty(key) || ZoneSystem.instance.GetGlobalKey(key))
            && Heightmap.FindBiome(position) == Heightmap.Biome.AshLands
            && EffectArea.IsPointInsideArea(position, EffectArea.Type.PlayerBase, BaseMargin) == null
            && CountNear(position) == 0;
    }

    // Out in the open: not in a dungeon or aboard a ship.
    private static bool Outside(Player player)
    {
        return !player.InInterior() && player.GetStandingOnShip() == null;
    }

    private static int CountNear(Vector3 position)
    {
        int count = 0;
        foreach (Character character in Character.GetAllCharacters())
        {
            if (character != null && !character.IsDead() && Utils.GetPrefabName(character.gameObject) == MuspellRegistry.PrefabName
                && Vector3.Distance(character.transform.position, position) < NearbyRange)
            {
                count++;
            }
        }

        return count;
    }

    private void RPC_Request(long sender)
    {
        if (!ZNet.instance.IsServer() || !MuspellSettings.Enabled.Value
            || (lastGiant.TryGetValue(sender, out float last) && Time.time - last < MuspellSettings.CooldownMinutes.Value * 60f)
            || Random.value * 100f >= MuspellSettings.ChancePerMinute.Value)
        {
            return;
        }

        lastGiant[sender] = Time.time;
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

        lastGiant[sender] = Time.time;
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, SpawnRpc);
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, ReplyRpc, "The ground trembles.");
    }

    private void RPC_Reply(long sender, string message)
    {
        if (sender == ZRoutedRpc.instance.GetServerPeerID())
        {
            Print(message);
        }
    }

    // Spawns the giant on firm ground some way off; it walks the Ashlands as the spawn system's giant did.
    private static void Spawn(Player player)
    {
        GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(MuspellRegistry.PrefabName) : null;
        if (prefab == null || !TryFindGround(player.transform.position, out Vector3 point))
        {
            Jotunn.Logger.LogInfo($"No ground for the Muspell Giant near {player.GetPlayerName()}");
            return;
        }

        Vector3 toPlayer = player.transform.position - point;
        toPlayer.y = 0f;
        CreatureStars.BeginSpawn(false);
        try
        {
            Instantiate(prefab, point, toPlayer.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toPlayer) : Quaternion.identity);
        }
        finally
        {
            CreatureStars.EndSpawn();
        }

        Jotunn.Logger.LogInfo($"A Muspell Giant walked out of the fire near {player.GetPlayerName()} at {point}");
    }

    // Ashlands ground 45 to 70 m from the player: not lava, not water and outside any base.
    private static bool TryFindGround(Vector3 origin, out Vector3 point)
    {
        ZoneSystem zones = ZoneSystem.instance;
        for (int i = 0; i < Attempts; i++)
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            Vector3 candidate = origin + new Vector3(direction.x, 0f, direction.y) * Random.Range(MinDistance, MaxDistance);
            if (!zones.GetSolidHeight(candidate, out float height) || height < zones.m_waterLevel + 1f
                || Heightmap.FindBiome(candidate) != Heightmap.Biome.AshLands || zones.IsLava(candidate)
                || EffectArea.IsPointInsideArea(candidate, EffectArea.Type.PlayerBase, BaseMargin) != null)
            {
                continue;
            }

            candidate.y = height;
            point = candidate;
            return true;
        }

        point = Vector3.zero;
        return false;
    }
}
