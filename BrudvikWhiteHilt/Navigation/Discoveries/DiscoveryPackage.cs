using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Navigation.Discoveries;

/// <summary>
/// One kind of discovery as the server sends it: its key and group, plus what the client works out from it.
/// </summary>
public class DiscoveryKind
{
    /// <summary>
    /// Kind key, see <see cref="DiscoveryCatalog"/>.
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// Group the kind is shown under.
    /// </summary>
    public DiscoveryGroup Group { get; set; }

    /// <summary>
    /// How many of the kind were found in all, filled in by the client.
    /// </summary>
    public int Total { get; set; }
}

/// <summary>
/// One marker: a location, or the plants or deposits of one kind within one square.
/// </summary>
public class DiscoveryEntry
{
    /// <summary>
    /// Index of the kind in the list sent with it.
    /// </summary>
    public int Kind { get; set; }

    /// <summary>
    /// Centre of what it stands for.
    /// </summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// How many it stands for.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// How many of them can be picked now, or -1 for things that are not picked.
    /// </summary>
    public int Ripe { get; set; } = -1;
}

/// <summary>
/// Reads and writes the list of discoveries the server sends. The list is compressed, since a well-explored world can
/// hold thousands of markers.
/// </summary>
public static class DiscoveryPackage
{
    private const int Version = 1;

    /// <summary>
    /// Writes kinds and entries into a package holding the compressed list.
    /// </summary>
    /// <param name="kinds">The kinds the entries point to.</param>
    /// <param name="entries">The entries.</param>
    /// <returns>The package.</returns>
    public static ZPackage Write(List<DiscoveryKind> kinds, List<DiscoveryEntry> entries)
    {
        ZPackage inner = new();
        inner.Write(Version);
        inner.Write(kinds.Count);
        foreach (DiscoveryKind kind in kinds)
        {
            inner.Write(kind.Key);
            inner.Write((byte)kind.Group);
        }

        inner.Write(entries.Count);
        foreach (DiscoveryEntry entry in entries)
        {
            inner.Write(entry.Kind);
            inner.Write(entry.Position.x);
            inner.Write(entry.Position.z);
            inner.Write(entry.Count);
            inner.Write(entry.Ripe);
        }

        ZPackage package = new();
        package.Write(Utils.Compress(inner.GetArray()));
        return package;
    }

    /// <summary>
    /// Reads a package made by <see cref="Write"/>.
    /// </summary>
    /// <param name="package">The package, read from its start.</param>
    /// <param name="kinds">The kinds.</param>
    /// <param name="entries">The entries.</param>
    /// <returns>False if the package is from another version of the mod.</returns>
    public static bool Read(ZPackage package, out List<DiscoveryKind> kinds, out List<DiscoveryEntry> entries)
    {
        kinds = new List<DiscoveryKind>();
        entries = new List<DiscoveryEntry>();
        ZPackage inner = new(Utils.Decompress(package.ReadByteArray()));
        if (inner.ReadInt() != Version)
        {
            return false;
        }

        int kindCount = inner.ReadInt();
        for (int i = 0; i < kindCount; i++)
        {
            kinds.Add(new DiscoveryKind { Key = inner.ReadString(), Group = (DiscoveryGroup)inner.ReadByte() });
        }

        int entryCount = inner.ReadInt();
        for (int i = 0; i < entryCount; i++)
        {
            int kind = inner.ReadInt();
            float x = inner.ReadSingle();
            float z = inner.ReadSingle();
            int count = inner.ReadInt();
            int ripe = inner.ReadInt();
            if (kind >= 0 && kind < kindCount)
            {
                entries.Add(new DiscoveryEntry { Kind = kind, Position = new Vector3(x, 0f, z), Count = count, Ripe = ripe });
                kinds[kind].Total += count;
            }
        }

        return true;
    }
}
