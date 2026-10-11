namespace BrudvikWhiteHilt.Helpers;

/// <summary>
/// Tells the server which connection a routed RPC came in on. The game routes an RPC with the sender id the sending
/// client wrote into it and never checks it, so a client could pretend to be another player. A handler that acts on
/// who sent it (deleting their blueprint, say) asks <see cref="Verify"/> instead of trusting the id.
/// </summary>
public static class RoutedRpcSender
{
    /// <summary>The connection of the routed RPC being handled on the server, or null outside one.</summary>
    public static ZRpc Current { get; set; }

    /// <summary>
    /// Checks a routed RPC's sender and finds who they are.
    /// </summary>
    /// <param name="sender">The sender id the RPC carries.</param>
    /// <param name="name">The player's name, or null.</param>
    /// <returns>The sender's platform user id, or null if the sender cannot be confirmed.</returns>
    public static string Verify(long sender, out string name)
    {
        name = null;
        if (ZNet.instance == null)
        {
            return null;
        }

        if (Current != null)
        {
            ZNetPeer peer = ZNet.instance.GetPeer(Current);
            if (peer == null || peer.m_uid != sender || peer.m_socket == null)
            {
                return null;
            }

            name = peer.m_playerName;
            return peer.m_socket.GetHostName();
        }

        // Not from the network: the host, calling its own server.
        if (sender == ZDOMan.GetSessionID() && !ZNet.instance.IsDedicated())
        {
            name = Game.instance != null ? Game.instance.GetPlayerProfile()?.GetName() : null;
            return UserInfo.GetLocalUser().UserId.ToString();
        }

        return null;
    }

    /// <summary>
    /// The platform user id of a connected peer, or of the host for its own session id.
    /// </summary>
    /// <param name="uid">The peer's id.</param>
    /// <returns>The id, or null.</returns>
    public static string IdentityOf(long uid)
    {
        if (ZNet.instance == null)
        {
            return null;
        }

        ZNetPeer peer = ZNet.instance.GetPeer(uid);
        if (peer != null)
        {
            return peer.m_socket?.GetHostName();
        }

        return uid == ZDOMan.GetSessionID() && !ZNet.instance.IsDedicated() ? UserInfo.GetLocalUser().UserId.ToString() : null;
    }
}
