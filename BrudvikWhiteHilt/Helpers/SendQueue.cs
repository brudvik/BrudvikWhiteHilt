using System;
using System.Collections.Generic;

namespace BrudvikWhiteHilt.Helpers;

/// <summary>
/// The lists waiting for one player, see <see cref="ServerLists"/>. Pieces go out in the order their lists were queued,
/// one at a time, and only while the player's connection is nearly empty and the lists' share of the line allows: the
/// game stops sending positions and other world data while more than 10 KB waits, so a list must never fill the queue
/// for long. A newer list of a call replaces an older one only while that has not started; one being sent is finished,
/// so a list that changes often still arrives.
/// </summary>
public sealed class SendQueue
{
    /// <summary>
    /// A piece is sent only while less than this waits on the connection, below the 8 KB at which the game sends its
    /// own world data (<c>ZDOMan.SendZDOs</c>).
    /// </summary>
    public const int QueueLimit = 6 * 1024;

    /// <summary>Most bytes per second the lists may take of one player's connection; Steam sends 150 KB/s.</summary>
    public const float BytesPerSecond = 64 * 1024;

    // Enough for one whole piece with its header, so a piece is never held back for good.
    private const float Burst = ServerLists.PieceBytes + 1024;

    private readonly List<Item> items = new();
    private float allowance = Burst;
    private float lastTime = float.NaN;

    /// <summary>True when nothing waits.</summary>
    public bool IsEmpty => items.Count == 0;

    /// <summary>Whatever drives this queue; the driver is started again when it has gone, say with a new world.</summary>
    public object Driver { get; set; }

    /// <summary>
    /// Queues a list. A list of the same call that has not started is dropped, since this one is newer.
    /// </summary>
    /// <param name="rpcName">The call that receives the pieces.</param>
    /// <param name="pieces">The pieces, see <see cref="ServerLists.Split"/>.</param>
    public void Add(string rpcName, List<ZPackage> pieces)
    {
        if (pieces == null || pieces.Count == 0)
        {
            return;
        }

        items.RemoveAll(item => item.Rpc == rpcName && item.Next == 0);
        items.Add(new Item(rpcName, pieces));
    }

    /// <summary>
    /// Whether a list of this call waits and has not started yet.
    /// </summary>
    /// <param name="rpcName">The call.</param>
    /// <returns>True if it waits.</returns>
    public bool IsWaiting(string rpcName)
    {
        return items.Exists(item => item.Rpc == rpcName && item.Next == 0);
    }

    /// <summary>
    /// The next piece to send now, if the connection and the lists' share allow it.
    /// </summary>
    /// <param name="now">Seconds, from any steady clock.</param>
    /// <param name="queued">Bytes waiting on the player's connection.</param>
    /// <param name="rpcName">The call to send the piece with.</param>
    /// <param name="piece">The piece.</param>
    /// <returns>True if a piece should be sent now.</returns>
    public bool TryTake(float now, int queued, out string rpcName, out ZPackage piece)
    {
        if (!float.IsNaN(lastTime))
        {
            allowance = Math.Min(Burst, allowance + Math.Max(0f, now - lastTime) * BytesPerSecond);
        }

        lastTime = now;
        rpcName = null;
        piece = null;
        if (items.Count == 0 || queued > QueueLimit)
        {
            return false;
        }

        Item first = items[0];
        ZPackage next = first.Pieces[first.Next];
        if (allowance < next.Size())
        {
            return false;
        }

        allowance -= next.Size();
        first.Next++;
        if (first.Next >= first.Pieces.Count)
        {
            items.RemoveAt(0);
        }

        rpcName = first.Rpc;
        piece = next;
        return true;
    }

    private sealed class Item
    {
        public Item(string rpc, List<ZPackage> pieces)
        {
            Rpc = rpc;
            Pieces = pieces;
        }

        public string Rpc { get; }

        public List<ZPackage> Pieces { get; }

        public int Next { get; set; }
    }
}
