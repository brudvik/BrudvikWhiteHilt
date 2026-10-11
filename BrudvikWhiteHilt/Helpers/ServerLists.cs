namespace BrudvikWhiteHilt.Helpers;

/// <summary>
/// Packs the lists the server services send to each player (portals, map areas, discoveries). A large list is gzipped,
/// since Steam connections send data as it is; a fingerprint tells whether a player already has the same list.
/// </summary>
public static class ServerLists
{
    /// <summary>Lists from this size up are gzipped.</summary>
    public const int CompressFrom = 1024;

    /// <summary>Most bytes a received list may unpack to.</summary>
    public const int MaxUnpackedBytes = 16 * 1024 * 1024;

    private const byte Plain = 0;
    private const byte Gzip = 1;

    /// <summary>
    /// Packs a list for sending.
    /// </summary>
    /// <param name="list">The list as written by its feature.</param>
    /// <returns>The package to send.</returns>
    public static ZPackage Pack(ZPackage list)
    {
        byte[] bytes = list.GetArray();
        ZPackage packed = new();
        if (bytes.Length >= CompressFrom)
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
}
