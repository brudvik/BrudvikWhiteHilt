using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Darkness;

/// <summary>
/// Runs on the <see cref="Game"/> object. The server remembers when each place below ground was first gone into, and
/// forgets it once the place has stood empty a while; the clients ask while they are inside and keep the answer, so
/// everyone in a place counts its darkness from the same moment. It also answers the admins' darkness commands.
/// </summary>
public class DarknessService : MonoBehaviour
{
    private const string InsideRpc = "WhiteHiltDarkInside";
    private const string StartRpc = "WhiteHiltDarkStart";
    private const string AdminRpc = "WhiteHiltDarkAdmin";
    private const string ReplyRpc = "WhiteHiltDarkReply";

    /// <summary>Seconds a place stands empty before its darkness starts over.</summary>
    public const float ForgetSeconds = 300f;

    private static readonly Dictionary<string, double> starts = new();
    private static readonly Dictionary<string, Place> places = new();

    /// <summary>
    /// When the place was first gone into, as the server said.
    /// </summary>
    /// <param name="key">The place.</param>
    /// <param name="start">World time.</param>
    /// <returns>Whether the server has said.</returns>
    public static bool TryGetStart(string key, out double start)
    {
        return starts.TryGetValue(key ?? string.Empty, out start);
    }

    /// <summary>
    /// Tells the server the local player is in a place; it answers with when the place was first gone into.
    /// </summary>
    /// <param name="key">The place.</param>
    public static void Inside(string key)
    {
        if (ZRoutedRpc.instance != null && !string.IsNullOrEmpty(key))
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), InsideRpc, key);
        }
    }

    /// <summary>
    /// Sends an admin command for a place to the server.
    /// </summary>
    /// <param name="command">start or stop.</param>
    /// <param name="key">The place.</param>
    public static void SendAdmin(string command, string key)
    {
        if (ZRoutedRpc.instance == null)
        {
            Print("Join a world first.");
            return;
        }

        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), AdminRpc, command ?? string.Empty, key ?? string.Empty);
    }

    /// <summary>
    /// The start a place gets: the one it has while someone has been there lately, else now.
    /// </summary>
    /// <param name="known">Whether the place is known.</param>
    /// <param name="start">Its start.</param>
    /// <param name="lastSeen">When someone was last there.</param>
    /// <param name="now">World time.</param>
    /// <returns>The start.</returns>
    public static double StartFor(bool known, double start, double lastSeen, double now)
    {
        return known && now - lastSeen <= ForgetSeconds ? start : now;
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
        starts.Clear();
        places.Clear();
        ZRoutedRpc.instance?.Register<string>(InsideRpc, RPC_Inside);
        ZRoutedRpc.instance?.Register<string, double>(StartRpc, RPC_Start);
        ZRoutedRpc.instance?.Register<string, string>(AdminRpc, RPC_Admin);
        ZRoutedRpc.instance?.Register<string>(ReplyRpc, RPC_Reply);
    }

    // On the server: someone is in a place.
    private void RPC_Inside(long sender, string key)
    {
        if (ZNet.instance == null || !ZNet.instance.IsServer() || string.IsNullOrEmpty(key))
        {
            return;
        }

        double now = ZNet.instance.GetTimeSeconds();
        bool known = places.TryGetValue(key, out Place place);
        double start = StartFor(known, place.Start, place.LastSeen, now);
        places[key] = new Place(start, now);
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, StartRpc, key, start);
    }

    // On a client: when a place was first gone into.
    private void RPC_Start(long sender, string key, double start)
    {
        if (sender == ZRoutedRpc.instance.GetServerPeerID())
        {
            starts[key] = start;
        }
    }

    // On the server: an admin's command to bring the darkness to a place now, or to lift it.
    private void RPC_Admin(long sender, string command, string key)
    {
        if (ZNet.instance == null || !ZNet.instance.IsServer())
        {
            return;
        }

        if (!IsAdmin(sender))
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(sender, ReplyRpc, "Only admins can do that.");
            return;
        }

        if (string.IsNullOrEmpty(key))
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(sender, ReplyRpc, "Go below ground first.");
            return;
        }

        double now = ZNet.instance.GetTimeSeconds();
        bool stop = command?.ToLowerInvariant() == "stop";
        double start = stop ? now : now - DarknessSchedule.FirstDarkness(DarknessSchedule.Seed(key), DarknessClient.Timing());
        places[key] = new Place(start, now);
        foreach (ZNetPeer peer in ZNet.instance.GetPeers())
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, StartRpc, key, start);
        }

        if (!ZNet.instance.IsDedicated())
        {
            starts[key] = start;
        }

        ZRoutedRpc.instance.InvokeRoutedRPC(sender, ReplyRpc, stop ? "The darkness lifts and keeps away a while." : "The darkness creeps in.");
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

    private readonly struct Place
    {
        public Place(double start, double lastSeen)
        {
            Start = start;
            LastSeen = lastSeen;
        }

        public double Start { get; }

        public double LastSeen { get; }
    }
}
