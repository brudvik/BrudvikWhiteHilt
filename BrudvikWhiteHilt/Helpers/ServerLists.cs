using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Helpers;

/// <summary>
/// Packs the lists the server services send to each player (portals, map areas, discoveries) and the blueprints players
/// share. A large list is gzipped, since Steam connections send data as it is; a fingerprint tells whether a player
/// already has the same list. A packed list is sent in small pieces through one <see cref="SendQueue"/> per player,
/// paced so the game's own data comes first: while more than 10 KB waits on a connection the game sends no positions,
/// and other players are seen hanging in the air. Steam also refuses a message over 512 KB.
/// </summary>
public static class ServerLists
{
    /// <summary>Lists from this size up are gzipped.</summary>
    public const int CompressFrom = 1024;

    /// <summary>Most bytes a received list may unpack to.</summary>
    public const int MaxUnpackedBytes = 16 * 1024 * 1024;

    /// <summary>Most bytes of a packed list in one message: about a tenth of a second of Steam's 150 KB/s.</summary>
    public const int PieceBytes = 16 * 1024;

    /// <summary>Most pieces one list may be sent in.</summary>
    public const int MaxPieces = MaxUnpackedBytes / PieceBytes + 1;

    private const byte Plain = 0;
    private const byte Gzip = 1;

    private static readonly Dictionary<long, SendQueue> queues = new();
    private static readonly Dictionary<(long Peer, string Rpc), Assembler> receiving = new();
    private static int nextTransfer;

    /// <summary>
    /// Packs a list and queues it for one player. A newer list for the same player and call replaces an older one that
    /// has not started yet.
    /// </summary>
    /// <param name="target">The player's peer id.</param>
    /// <param name="rpcName">The routed call that receives the pieces, see <see cref="Receive"/>.</param>
    /// <param name="list">The list as written by its feature.</param>
    /// <param name="compress">False for data that is gzipped already.</param>
    public static void Send(long target, string rpcName, ZPackage list, bool compress = true)
    {
        SendPacked(target, rpcName, Pack(list, compress).GetArray());
    }

    /// <summary>
    /// Queues a list packed by <see cref="Pack"/> for one player. A call to this machine itself is made at once.
    /// </summary>
    /// <param name="target">The player's peer id.</param>
    /// <param name="rpcName">The routed call that receives the pieces, see <see cref="Receive"/>.</param>
    /// <param name="packed">The bytes of the packed list.</param>
    public static void SendPacked(long target, string rpcName, byte[] packed)
    {
        if (ZRoutedRpc.instance == null || ZNet.instance == null)
        {
            return;
        }

        List<ZPackage> pieces = Split(packed, ++nextTransfer);
        if (target == ZDOMan.GetSessionID())
        {
            pieces.ForEach(piece => ZRoutedRpc.instance.InvokeRoutedRPC(target, rpcName, piece));
            return;
        }

        if (!queues.TryGetValue(target, out SendQueue queue))
        {
            queue = new SendQueue();
            queues[target] = queue;
        }

        queue.Add(rpcName, pieces);
        if (!ReferenceEquals(queue.Driver, ZNet.instance))
        {
            queue.Driver = ZNet.instance;
            ZNet.instance.StartCoroutine(Pump(target, queue));
        }
    }

    /// <summary>
    /// Whether a list of this call for this player waits and has not started, so the next one replaces it.
    /// </summary>
    /// <param name="target">The player's peer id.</param>
    /// <param name="rpcName">The call.</param>
    /// <returns>True if one waits.</returns>
    public static bool IsWaiting(long target, string rpcName)
    {
        return queues.TryGetValue(target, out SendQueue queue) && queue.IsWaiting(rpcName);
    }

    /// <summary>
    /// Takes one received piece of a list sent by <see cref="Send"/>.
    /// </summary>
    /// <param name="sender">Who sent it; check that it is the server first.</param>
    /// <param name="rpcName">The routed call it came by.</param>
    /// <param name="piece">The piece.</param>
    /// <param name="maxPacked">Most bytes the packed list may have; pieces beyond it are dropped as they come.</param>
    /// <returns>The list, ready to read, once its last piece is in; otherwise null.</returns>
    public static ZPackage Receive(long sender, string rpcName, ZPackage piece, int maxPacked = int.MaxValue)
    {
        if (!receiving.TryGetValue((sender, rpcName), out Assembler assembler))
        {
            assembler = new Assembler();
            receiving[(sender, rpcName)] = assembler;
        }

        byte[] packed = assembler.Add(piece, maxPacked);
        return packed != null ? Unpack(new ZPackage(packed)) : null;
    }

    /// <summary>
    /// Cuts a packed list into the pieces to send.
    /// </summary>
    /// <param name="packed">The bytes of the packed list.</param>
    /// <param name="transfer">A number telling this sending apart from others of the same call.</param>
    /// <returns>The pieces, at least one.</returns>
    public static List<ZPackage> Split(byte[] packed, int transfer)
    {
        int count = Math.Max(1, (packed.Length + PieceBytes - 1) / PieceBytes);
        List<ZPackage> pieces = new(count);
        for (int index = 0; index < count; index++)
        {
            int start = index * PieceBytes;
            byte[] part = new byte[Math.Min(PieceBytes, packed.Length - start)];
            Array.Copy(packed, start, part, 0, part.Length);
            ZPackage piece = new();
            piece.Write(transfer);
            piece.Write(index);
            piece.Write(count);
            piece.Write(part);
            pieces.Add(piece);
        }

        return pieces;
    }

    /// <summary>
    /// Packs a list for sending.
    /// </summary>
    /// <param name="list">The list as written by its feature.</param>
    /// <param name="compress">False for data that is gzipped already.</param>
    /// <returns>The package to send.</returns>
    public static ZPackage Pack(ZPackage list, bool compress = true)
    {
        byte[] bytes = list.GetArray();
        ZPackage packed = new();
        if (compress && bytes.Length >= CompressFrom)
        {
            packed.Write(Gzip);
            packed.Write(Utils.Compress(bytes));
        }
        else
        {
            packed.Write(Plain);
            packed.Write(bytes);
        }

        return packed;
    }

    /// <summary>
    /// Unpacks a list made by <see cref="Pack"/>.
    /// </summary>
    /// <param name="received">The package received.</param>
    /// <returns>The list, ready to read, or null if it is broken or too large.</returns>
    public static ZPackage Unpack(ZPackage received)
    {
        try
        {
            byte format = received.ReadByte();
            byte[] bytes = received.ReadByteArray();
            byte[] list = format == Gzip ? BoundedGzip.Decompress(bytes, MaxUnpackedBytes) : format == Plain ? bytes : null;
            return list != null ? new ZPackage(list) : null;
        }
        catch (System.IO.EndOfStreamException)
        {
            return null;
        }
    }

    /// <summary>
    /// A 64-bit FNV-1a fingerprint of a list, to see whether a player already has it without keeping a copy.
    /// </summary>
    /// <param name="list">The list as written by its feature.</param>
    /// <returns>The fingerprint.</returns>
    public static ulong Fingerprint(ZPackage list)
    {
        ulong hash = 14695981039346656037UL;
        foreach (byte value in list.GetArray())
        {
            hash = (hash ^ value) * 1099511628211UL;
        }

        return hash;
    }

    // Sends one player's queue, a piece at a time as the queue allows, until it is empty or the player has left.
    private static IEnumerator Pump(long target, SendQueue queue)
    {
        while (PumpOnce(target, queue))
        {
            yield return null;
        }

        if (queues.TryGetValue(target, out SendQueue current) && current == queue)
        {
            queues.Remove(target);
        }

        queue.Driver = null;
    }

    // Sends what may go now; false when the queue is done with.
    private static bool PumpOnce(long target, SendQueue queue)
    {
        try
        {
            ZNetPeer peer = ZNet.instance != null && ZRoutedRpc.instance != null ? ZNet.instance.GetPeer(target) : null;
            if (peer == null || peer.m_socket == null || !ReferenceEquals(queue.Driver, ZNet.instance))
            {
                return false;
            }

            while (queue.TryTake(Time.unscaledTime, peer.m_socket.GetSendQueueSize(), out string rpcName, out ZPackage piece))
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(target, rpcName, piece);
            }

            return !queue.IsEmpty;
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Network: could not send a list to {target}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Puts the pieces of one list back together, in the order they were sent. A first piece starts over, so a list
    /// cut short by a newer one is dropped; pieces out of order or of another sending are ignored.
    /// </summary>
    public class Assembler
    {
        private readonly List<byte[]> parts = new();
        private int transfer;
        private int count;
        private int size;

        /// <summary>
        /// Adds a piece made by <see cref="Split"/>.
        /// </summary>
        /// <param name="piece">The piece, read from its start.</param>
        /// <param name="maxPacked">Most bytes the whole list may have.</param>
        /// <returns>The bytes of the packed list once its last piece is in; otherwise null.</returns>
        public byte[] Add(ZPackage piece, int maxPacked = int.MaxValue)
        {
            int pieceTransfer;
            int index;
            int pieceCount;
            byte[] part;
            try
            {
                pieceTransfer = piece.ReadInt();
                index = piece.ReadInt();
                pieceCount = piece.ReadInt();
                part = piece.ReadByteArray();
            }
            catch (System.IO.EndOfStreamException)
            {
                Clear();
                return null;
            }

            if (index != 0 && pieceTransfer != transfer)
            {
                return null;
            }

            if (index == 0)
            {
                Clear();
                transfer = pieceTransfer;
                count = pieceCount;
            }

            bool fits = part != null && part.Length <= PieceBytes && count >= 1 && count <= MaxPieces;
            if (!fits || pieceCount != count || index != parts.Count || size + part.Length > maxPacked)
            {
                Clear();
                return null;
            }

            parts.Add(part);
            size += part.Length;
            if (parts.Count < count)
            {
                return null;
            }

            byte[] packed = new byte[size];
            int offset = 0;
            foreach (byte[] done in parts)
            {
                Array.Copy(done, 0, packed, offset, done.Length);
                offset += done.Length;
            }

            Clear();
            return packed;
        }

        private void Clear()
        {
            parts.Clear();
            transfer = 0;
            count = 0;
            size = 0;
        }
    }
}
