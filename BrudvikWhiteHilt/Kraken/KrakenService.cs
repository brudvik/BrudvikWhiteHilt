using UnityEngine;

namespace BrudvikWhiteHilt.Kraken;

/// <summary>
/// Runs on the <see cref="Game"/> object. The client that owns a ship at sea asks the server once a minute while the
/// night is calm and foggy; the server rolls the chance, keeps the world-wide cooldown and tells that client to raise
/// the Kraken. Admins can call it with whitehilt_kraken.
/// </summary>
public class KrakenService : MonoBehaviour
{
    private const string RequestRpc = "WhiteHiltKrakenRequest";
    private const string SpawnRpc = "WhiteHiltKrakenSpawn";
    private const string AdminRpc = "WhiteHiltKrakenAdmin";
    private const string ReplyRpc = "WhiteHiltKrakenReply";
    private const float CheckInterval = 60f;

    private float nextCheck;
    private float lastKraken = float.NegativeInfinity;

    /// <summary>
    /// Asks the server, as an admin, to raise the Kraken beside the local player's ship now.
    /// </summary>
    public static void Summon()
    {
        Ship ship = Ship.GetLocalShip();
        if (ZRoutedRpc.instance == null || ship == null || ship.m_nview == null || !ship.m_nview.IsValid())
        {
            Print("Board a ship at sea first.");
            return;
        }

        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), AdminRpc, ship.m_nview.GetZDO().m_uid);
    }

    /// <summary>
    /// Describes the conditions for the Kraken where the local player is, for the console.
    /// </summary>
    /// <returns>One line per condition.</returns>
    public static string Describe()
    {
        Ship ship = Ship.GetLocalShip();
        EnvSetup env = EnvMan.instance != null ? EnvMan.instance.GetCurrentEnvironment() : null;
        string key = KrakenSettings.RequiredKey.Value;
        return $"Kraken: enabled {KrakenSettings.Enabled.Value}\n" +
            $"  on a ship: {ship != null}, own the ship: {ship != null && ship.m_nview.IsOwner()}\n" +
            $"  players aboard: {(ship != null ? ship.m_players.Count : 0)}, chance per minute: {AttackChance(ship != null ? ship.m_players.Count : 0):0.##}%\n" +
            $"  ocean: {ship != null && Heightmap.FindBiome(ship.transform.position) == Heightmap.Biome.Ocean}, " +
            $"deep ({KrakenSettings.MinDepth.Value} m): {ship != null && KrakenSpawner.DeepEnough(ship.transform.position, KrakenSettings.MinDepth.Value)}\n" +
            $"  night: {EnvMan.IsNight()} (needed: {KrakenSettings.NightOnly.Value}), weather: {env?.m_name} (fog: {KrakenSettings.IsFog(env)})\n" +
            $"  wind: {(EnvMan.instance != null ? EnvMan.instance.GetWindIntensity() : 0f):0.00} (calm up to {KrakenSettings.MaxWind.Value:0.00})\n" +
            $"  key {(string.IsNullOrEmpty(key) ? "(none)" : key)}: {string.IsNullOrEmpty(key) || (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(key))}\n" +
            $"  Krakens nearby: {KrakenBody.Instances.Count}";
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
        ZRoutedRpc.instance?.Register<ZDOID, int>(RequestRpc, RPC_Request);
        ZRoutedRpc.instance?.Register<ZDOID>(SpawnRpc, RPC_Spawn);
        ZRoutedRpc.instance?.Register<ZDOID>(AdminRpc, RPC_Admin);
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
        Ship ship = Ship.GetLocalShip();
        if (Conditions(ship))
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), RequestRpc, ship.m_nview.GetZDO().m_uid, ship.m_players.Count);
        }
    }

    private static bool Conditions(Ship ship)
    {
        Player player = Player.m_localPlayer;
        if (!KrakenSettings.Enabled.Value || player == null || player.IsDead() || ship == null || ship.m_nview == null
            || !ship.m_nview.IsValid() || !ship.m_nview.IsOwner() || KrakenBody.Instances.Count > 0)
        {
            return false;
        }

        string key = KrakenSettings.RequiredKey.Value;
        Vector3 position = ship.transform.position;
        return (string.IsNullOrEmpty(key) || ZoneSystem.instance.GetGlobalKey(key))
            && (!KrakenSettings.NightOnly.Value || EnvMan.IsNight())
            && KrakenSettings.IsFog(EnvMan.instance.GetCurrentEnvironment())
            && EnvMan.instance.GetWindIntensity() <= KrakenSettings.MaxWind.Value
            && Heightmap.FindBiome(position) == Heightmap.Biome.Ocean
            && KrakenSpawner.DeepEnough(position, KrakenSettings.MinDepth.Value);
    }

    private static float AttackChance(int playersAboard)
    {
        float baseChance = KrakenSettings.ChancePerMinute.Value;
        if (playersAboard <= 0 || baseChance <= 0f)
        {
            return 0f;
        }

        return Mathf.Clamp(baseChance + (playersAboard - 1) * KrakenSettings.ChancePerExtraPlayer.Value, 0f, 100f);
    }

    private void RPC_Request(long sender, ZDOID ship, int playersAboard)
    {
        if (!ZNet.instance.IsServer() || !KrakenSettings.Enabled.Value
            || Time.time - lastKraken < KrakenSettings.CooldownMinutes.Value * 60f
            || Random.value * 100f >= AttackChance(playersAboard))
        {
            return;
        }

        lastKraken = Time.time;
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, SpawnRpc, ship);
    }

    private void RPC_Spawn(long sender, ZDOID shipId)
    {
        if (sender != ZRoutedRpc.instance.GetServerPeerID())
        {
            return;
        }

        GameObject found = ZNetScene.instance.FindInstance(shipId);
        Ship ship = found != null ? found.GetComponent<Ship>() : null;
        if (ship != null)
        {
            KrakenSpawner.Spawn(ship);
        }
    }

    private void RPC_Admin(long sender, ZDOID ship)
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

        lastKraken = Time.time;
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, SpawnRpc, ship);
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, ReplyRpc, "The Kraken rises.");
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
}
