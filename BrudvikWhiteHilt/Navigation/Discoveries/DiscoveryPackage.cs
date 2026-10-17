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
/// What changed in the list of discoveries from one version to the next. Markers are told apart by kind and place;
/// removed and changed ones are named by their index in the old list.
/// </summary>
public class DiscoveryChanges
{
    /// <summary>Version of the list the changes apply to.</summary>
    public int BaseVersion { get; set; }

    /// <summary>Version of the list after the changes.</summary>
    public int Version { get; set; }

    /// <summary>The whole table of kinds after the changes; it only ever grows, so old markers keep their kind.</summary>
    public List<DiscoveryKind> Kinds { get; set; } = new();

    /// <summary>Indices in the old list of the markers that are gone, from low to high.</summary>
    public List<int> Removed { get; set; } = new();

    /// <summary>Indices in the old list of markers with a new count or ripeness, and the new values.</summary>
    public List<(int Index, int Count, int Ripe)> Changed { get; set; } = new();

    /// <summary>New markers, added at the end.</summary>
    public List<DiscoveryEntry> Added { get; set; } = new();

    /// <summary>True if nothing changed.</summary>
    public bool IsEmpty => Removed.Count == 0 && Changed.Count == 0 && Added.Count == 0;
}

/// <summary>
/// Reads and writes the discoveries the server sends: the whole list to a player who has none, or else what changed
/// since the version they have. A well-explored world holds thousands of markers, and plants ripen all the time, so
/// sending the whole list every time would fill the players' connections. Server and client keep the list in the same
/// order (<see cref="Apply"/>), so a change can name a marker by its index. <see cref="Helpers.ServerLists"/> gzips both.
/// </summary>
public static class DiscoveryPackage
{
    private const int Format = 2;
    private const byte Whole = 0;
    private const byte Delta = 1;

    /// <summary>
    /// Writes the whole list.
    /// </summary>
    /// <param name="version">The list's version.</param>
    /// <param name="kinds">The kinds the entries point to.</param>
    /// <param name="entries">The entries.</param>
    /// <returns>The package.</returns>
    public static ZPackage WriteWhole(int version, List<DiscoveryKind> kinds, List<DiscoveryEntry> entries)
    {
        ZPackage package = new();
        package.Write(Format);
        package.Write(Whole);
        package.Write(version);
        WriteKinds(package, kinds);
        package.Write(entries.Count);
        entries.ForEach(entry => WriteEntry(package, entry));
        return package;
    }

    /// <summary>
    /// Writes the changes from one version to the next.
    /// </summary>
    /// <param name="changes">The changes.</param>
    /// <returns>The package.</returns>
    public static ZPackage WriteChanges(DiscoveryChanges changes)
    {
        ZPackage package = new();
        package.Write(Format);
        package.Write(Delta);
        package.Write(changes.Version);
        package.Write(changes.BaseVersion);
        WriteKinds(package, changes.Kinds);
        package.Write(changes.Removed.Count);
        changes.Removed.ForEach(package.Write);
        package.Write(changes.Changed.Count);
        foreach ((int index, int count, int ripe) in changes.Changed)
        {
            package.Write(index);
            package.Write(count);
            package.Write(ripe);
        }

        package.Write(changes.Added.Count);
        changes.Added.ForEach(entry => WriteEntry(package, entry));
        return package;
    }

    /// <summary>
    /// Reads a package made by <see cref="WriteWhole"/> or <see cref="WriteChanges"/>.
    /// </summary>
    /// <param name="package">The package, read from its start.</param>
    /// <param name="version">The list's version.</param>
    /// <param name="kinds">The kinds, for the whole list; null for changes.</param>
    /// <param name="entries">The entries, for the whole list; null for changes.</param>
    /// <param name="changes">The changes, or null for the whole list.</param>
    /// <returns>False if the package is from another version of the mod.</returns>
    public static bool Read(ZPackage package, out int version, out List<DiscoveryKind> kinds, out List<DiscoveryEntry> entries,
        out DiscoveryChanges changes)
    {
        version = 0;
        kinds = null;
        entries = null;
        changes = null;
        if (package.ReadInt() != Format)
        {
            return false;
        }

        byte type = package.ReadByte();
        version = package.ReadInt();
        if (type == Whole)
        {
            kinds = ReadKinds(package);
            entries = ReadEntries(package);
            return true;
        }

        if (type != Delta)
        {
            return false;
        }

        changes = new DiscoveryChanges { Version = version, BaseVersion = package.ReadInt(), Kinds = ReadKinds(package) };
        int removed = package.ReadInt();
        for (int i = 0; i < removed; i++)
        {
            changes.Removed.Add(package.ReadInt());
        }

        int changed = package.ReadInt();
        for (int i = 0; i < changed; i++)
        {
            changes.Changed.Add((package.ReadInt(), package.ReadInt(), package.ReadInt()));
        }

        changes.Added = ReadEntries(package);
        return true;
    }

    /// <summary>
    /// Works out what changed between two lists whose entries point into the same table of kinds.
    /// </summary>
    /// <param name="old">The list the player has.</param>
    /// <param name="current">The new list.</param>
    /// <returns>The changes, or null if two markers share a kind and place and cannot be told apart.</returns>
    public static DiscoveryChanges Diff(List<DiscoveryEntry> old, List<DiscoveryEntry> current)
    {
        Dictionary<(int, float, float), DiscoveryEntry> byPlace = new(current.Count);
        foreach (DiscoveryEntry entry in current)
        {
            if (!byPlace.ContainsKey(Place(entry)))
            {
                byPlace[Place(entry)] = entry;
            }
            else
            {
                return null;
            }
        }

        DiscoveryChanges changes = new();
        HashSet<(int, float, float)> kept = new();
        for (int index = 0; index < old.Count; index++)
        {
            DiscoveryEntry entry = old[index];
            if (!byPlace.TryGetValue(Place(entry), out DiscoveryEntry now) || !kept.Add(Place(entry)))
            {
                changes.Removed.Add(index);
            }
            else if (now.Count != entry.Count || now.Ripe != entry.Ripe)
            {
                changes.Changed.Add((index, now.Count, now.Ripe));
            }
        }

        changes.Added.AddRange(current.FindAll(entry => !kept.Contains(Place(entry))));
        return changes;
    }

    /// <summary>
    /// Makes the new list from the old one and the changes, the same way on the server and the client: the markers
    /// kept stay in their order, and the new ones follow.
    /// </summary>
    /// <param name="old">The old list; it is not changed.</param>
    /// <param name="changes">The changes.</param>
    /// <returns>The new list, or null if the changes do not fit the old list.</returns>
    public static List<DiscoveryEntry> Apply(List<DiscoveryEntry> old, DiscoveryChanges changes)
    {
        bool[] removed = new bool[old.Count];
        foreach (int index in changes.Removed)
        {
            if (index < 0 || index >= old.Count)
            {
                return null;
            }

            removed[index] = true;
        }

        Dictionary<int, (int Count, int Ripe)> changed = new();
        foreach ((int index, int count, int ripe) in changes.Changed)
        {
            if (index < 0 || index >= old.Count || removed[index])
            {
                return null;
            }

            changed[index] = (count, ripe);
        }

        List<DiscoveryEntry> result = new(old.Count - changes.Removed.Count + changes.Added.Count);
        for (int index = 0; index < old.Count; index++)
        {
            if (removed[index])
            {
                continue;
            }

            DiscoveryEntry entry = old[index];
            (int count, int ripe) = changed.TryGetValue(index, out (int Count, int Ripe) value) ? value : (entry.Count, entry.Ripe);
            result.Add(new DiscoveryEntry { Kind = entry.Kind, Position = entry.Position, Count = count, Ripe = ripe });
        }

        result.AddRange(changes.Added);
        return result.TrueForAll(entry => entry.Kind >= 0 && entry.Kind < changes.Kinds.Count) ? result : null;
    }

    /// <summary>
    /// The lists for the map: only the kinds in use, numbered afresh, with their totals counted.
    /// </summary>
    /// <param name="kinds">The table of kinds.</param>
    /// <param name="entries">The entries.</param>
    /// <param name="shownKinds">The kinds in use.</param>
    /// <param name="shownEntries">Copies of the entries, pointing into <paramref name="shownKinds"/>.</param>
    public static void ForMap(List<DiscoveryKind> kinds, List<DiscoveryEntry> entries, out List<DiscoveryKind> shownKinds,
        out List<DiscoveryEntry> shownEntries)
    {
        Dictionary<int, int> used = new();
        shownKinds = new List<DiscoveryKind>();
        shownEntries = new List<DiscoveryEntry>(entries.Count);
        foreach (DiscoveryEntry entry in entries)
        {
            if (entry.Kind < 0 || entry.Kind >= kinds.Count)
            {
                continue;
            }

            if (!used.TryGetValue(entry.Kind, out int index))
            {
                index = shownKinds.Count;
                used[entry.Kind] = index;
                shownKinds.Add(new DiscoveryKind { Key = kinds[entry.Kind].Key, Group = kinds[entry.Kind].Group });
            }

            shownKinds[index].Total += entry.Count;
            shownEntries.Add(new DiscoveryEntry { Kind = index, Position = entry.Position, Count = entry.Count, Ripe = entry.Ripe });
        }
    }

    // A marker is known by its kind and where it stands; only the ground position is sent.
    private static (int, float, float) Place(DiscoveryEntry entry) => (entry.Kind, entry.Position.x, entry.Position.z);

    private static void WriteKinds(ZPackage package, List<DiscoveryKind> kinds)
    {
        package.Write(kinds.Count);
        foreach (DiscoveryKind kind in kinds)
        {
            package.Write(kind.Key);
            package.Write((byte)kind.Group);
        }
    }

    private static List<DiscoveryKind> ReadKinds(ZPackage package)
    {
        int count = package.ReadInt();
        List<DiscoveryKind> kinds = new(count);
        for (int i = 0; i < count; i++)
        {
            kinds.Add(new DiscoveryKind { Key = package.ReadString(), Group = (DiscoveryGroup)package.ReadByte() });
        }

        return kinds;
    }

    private static void WriteEntry(ZPackage package, DiscoveryEntry entry)
    {
        package.Write(entry.Kind);
        package.Write(entry.Position.x);
        package.Write(entry.Position.z);
        package.Write(entry.Count);
        package.Write(entry.Ripe);
    }

    private static List<DiscoveryEntry> ReadEntries(ZPackage package)
    {
        int count = package.ReadInt();
        List<DiscoveryEntry> entries = new(count);
        for (int i = 0; i < count; i++)
        {
            int kind = package.ReadInt();
            float x = package.ReadSingle();
            float z = package.ReadSingle();
            entries.Add(new DiscoveryEntry { Kind = kind, Position = new Vector3(x, 0f, z), Count = package.ReadInt(), Ripe = package.ReadInt() });
        }

        return entries;
    }
}
