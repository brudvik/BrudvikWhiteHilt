using BrudvikWhiteHilt.Helpers;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// The trading posts' part on the game object: on the server it answers the admin commands (build a post where the
/// admin stands, remove the nearest, list them, search the coasts again); on a client it prints the server's answers.
/// </summary>
public class TradingPostService : MonoBehaviour
{
    private const string AdminRpc = "WhiteHiltTradePostAdmin";
    private const string ReplyRpc = "WhiteHiltTradePostReply";
    private const string ChartRpc = "WhiteHiltTradePostChart";
    private const string ChartReplyRpc = "WhiteHiltTradePostChartReply";

    private const int ActionHere = 0;
    private const int ActionRemove = 1;
    private const int ActionList = 2;
    private const int ActionPlace = 3;

    // How far from the admin a post may be to be removed by "remove".
    private const float RemoveRange = 80f;

    // The landing's deck is this much above the ground the admin stands on.
    private const float DeckAboveGround = 0.1f;

    /// <summary>
    /// Asks the server to build a post where the local player stands, its jetty running out the way they look.
    /// </summary>
    public static void RequestHere() => Send(ActionHere);

    /// <summary>
    /// Asks the server to remove the post nearest the local player.
    /// </summary>
    public static void RequestRemove() => Send(ActionRemove);

    /// <summary>
    /// Asks the server for a list of the posts.
    /// </summary>
    public static void RequestList() => Send(ActionList);

    /// <summary>
    /// Asks the server to search the coasts now and build the posts each biome still lacks.
    /// </summary>
    public static void RequestPlace() => Send(ActionPlace);

    /// <summary>
    /// Asks the server where a biome's posts stand, for a chart the local player reads.
    /// </summary>
    /// <param name="biome">The biome index (<see cref="TradingPostGoods.Biomes"/>).</param>
    public static void RequestChart(int biome)
    {
        ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), ChartRpc, biome);
    }

    private static void Send(int action)
    {
        Player player = Player.m_localPlayer;
        if (player == null || ZRoutedRpc.instance == null)
        {
            Print("Join a world first.");
            return;
        }

        Vector3 forward = player.GetLookDir();
        float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), AdminRpc, player.transform.position, yaw, action);
    }

    private static void Print(string message)
    {
        global::Console.instance?.Print(message);
        Jotunn.Logger.LogInfo($"Trading posts: {message}");
    }

    private void Start()
    {
        ZRoutedRpc.instance?.Register<Vector3, float, int>(AdminRpc, RPC_Admin);
        ZRoutedRpc.instance?.Register<string>(ReplyRpc, RPC_Reply);
        ZRoutedRpc.instance?.Register<int>(ChartRpc, RPC_Chart);
        ZRoutedRpc.instance?.Register<ZPackage>(ChartReplyRpc, RPC_ChartReply);
    }

    // On the server: an admin's request. Every request is checked: the sender must be an admin, and building must be on.
    private void RPC_Admin(long sender, Vector3 position, float yaw, int action)
    {
        if (ZNet.instance == null || !ZNet.instance.IsServer())
        {
            return;
        }

        string identity = RoutedRpcSender.Verify(sender, out _);
        if (identity == null || (sender != ZRoutedRpc.instance.m_id && !ZNet.instance.IsAdmin(identity)))
        {
            Reply(sender, "Only admins can do that.");
            return;
        }

        switch (action)
        {
            case ActionHere:
                BuildHere(sender, position, yaw);
                break;
            case ActionRemove:
                RemoveNearest(sender, position);
                break;
            case ActionList:
                Reply(sender, Describe(position));
                break;
            case ActionPlace:
                Reply(sender, TradingPostPlacer.PlaceNow()
                    ? "Searching the coasts for trading posts; the log says what was placed."
                    : "A search is already running.");
                break;
        }
    }

    private static void BuildHere(long sender, Vector3 position, float yaw)
    {
        if (!TradingPostSettings.Enabled.Value)
        {
            Reply(sender, "Trading posts are off ([TradingPosts] Enabled).");
            return;
        }

        Vector3 origin = position + Vector3.up * DeckAboveGround;
        string id = TradingPostBuilder.Build(origin, yaw, WorldGenerator.instance.GetBiome(origin));
        Reply(sender, id == null
            ? "No trading post could be built; see the log."
            : $"A trading post is built here, its jetty running out {yaw.ToString("0", CultureInfo.InvariantCulture)}° from north.");
    }

    private static void RemoveNearest(long sender, Vector3 position)
    {
        ZDO nearest = TradingPostBuilder.Markers()
            .OrderBy(zdo => Utils.DistanceXZ(zdo.GetPosition(), position))
            .FirstOrDefault(zdo => Utils.DistanceXZ(zdo.GetPosition(), position) <= RemoveRange);
        if (nearest == null)
        {
            Reply(sender, $"No trading post within {RemoveRange:0} m.");
            return;
        }

        int removed = TradingPostBuilder.Remove(nearest.GetString(TradingPostKeys.PostId));
        Reply(sender, $"The trading post is removed ({removed} objects).");
    }

    private static string Describe(Vector3 position)
    {
        StringBuilder text = new();
        ZDO[] markers = TradingPostBuilder.Markers().OrderBy(zdo => Utils.DistanceXZ(zdo.GetPosition(), position)).ToArray();
        text.Append($"{markers.Length} trading post(s).");
        foreach (ZDO zdo in markers)
        {
            Vector3 at = zdo.GetPosition();
            text.Append($"\n  {(Heightmap.Biome)zdo.GetInt(TradingPostKeys.Biome)} at {at.x:0} {at.z:0}, {Utils.DistanceXZ(at, position):0} m away");
        }

        return text.ToString();
    }

    private static void Reply(long sender, string message)
    {
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, ReplyRpc, message);
    }

    private void RPC_Reply(long sender, string message)
    {
        Print(message);
    }

    // On the server: where the posts of a biome stand, sent back to the reader. Anyone may ask: the posts are no secret,
    // only hard to find, and the chart that asks costs coins.
    private void RPC_Chart(long sender, int biome)
    {
        if (ZNet.instance == null || !ZNet.instance.IsServer() || biome < 0 || biome >= TradingPostGoods.Biomes.Length)
        {
            return;
        }

        string name = TradingPostGoods.Biomes[biome];
        ZPackage reply = new();
        ZDO[] markers = TradingPostBuilder.Markers()
            .Where(zdo => string.Equals(((Heightmap.Biome)zdo.GetInt(TradingPostKeys.Biome)).ToString(), name, System.StringComparison.OrdinalIgnoreCase))
            .ToArray();
        reply.Write(markers.Length);
        foreach (ZDO zdo in markers)
        {
            reply.Write(zdo.GetPosition());
        }

        ZRoutedRpc.instance.InvokeRoutedRPC(sender, ChartReplyRpc, reply);
    }

    private void RPC_ChartReply(long sender, ZPackage reply)
    {
        int count = reply.ReadInt();
        System.Collections.Generic.List<Vector3> positions = new();
        for (int i = 0; i < count; i++)
        {
            positions.Add(reply.ReadVector3());
        }

        TradingPostCharts.Mark(positions);
    }
}
