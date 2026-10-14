using BrudvikWhiteHilt.Helpers;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// The trading posts' part on the game object: on the server it answers the admin commands (build a post where the
/// admin stands, remove the nearest, list them); on a client it prints the server's answers.
/// </summary>
public class TradingPostService : MonoBehaviour
{
    private const string AdminRpc = "WhiteHiltTradePostAdmin";
    private const string ReplyRpc = "WhiteHiltTradePostReply";

    private const int ActionHere = 0;
    private const int ActionRemove = 1;
    private const int ActionList = 2;

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
        string id = TradingPostBuilder.Build(origin, yaw);
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
            text.Append($"\n  {at.x:0} {at.z:0}, {Utils.DistanceXZ(at, position):0} m away");
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
}
