using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Portals.PortalMap;

/// <summary>
/// One portal on the shared portal map, as the server sends it to a player.
/// </summary>
public class PortalMapEntry
{
    /// <summary>
    /// Privacy value of a public Portal Stations station, and of every ordinary portal.
    /// </summary>
    public const int Public = 0;

    /// <summary>
    /// Privacy value of a private Portal Stations station.
    /// </summary>
    public const int Private = 1;

    /// <summary>
    /// Name the players gave the portal, or empty.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Position of the portal.
    /// </summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// Portal Stations privacy: 0 public, 1 private, 2 guild, 3 group, 4 guild and group.
    /// </summary>
    public int Privacy { get; set; }

    /// <summary>
    /// Player ID of the station's builder, used by the server to decide who sees a non-public station. Not sent.
    /// </summary>
    public long Creator { get; set; }

    /// <summary>
    /// Bit mask of the runes on rune posts near the portal.
    /// </summary>
    public int RuneMask { get; set; }

    /// <summary>
    /// True if one rune post near the portal holds every rune.
    /// </summary>
    public bool Everything { get; set; }

    /// <summary>
    /// Writes a list of entries to a package.
    /// </summary>
    /// <param name="entries">Entries to write.</param>
    /// <returns>The package.</returns>
    public static ZPackage Write(IReadOnlyList<PortalMapEntry> entries)
    {
        ZPackage package = new();
        package.Write(entries.Count);
        foreach (PortalMapEntry entry in entries)
        {
            package.Write(entry.Name);
            package.Write(entry.Position);
            package.Write(entry.Privacy);
            package.Write(entry.RuneMask);
            package.Write(entry.Everything);
        }

        return package;
    }

    /// <summary>
    /// Reads a list of entries written by <see cref="Write"/>.
    /// </summary>
    /// <param name="package">The package.</param>
    /// <returns>The entries.</returns>
    public static List<PortalMapEntry> Read(ZPackage package)
    {
        int count = package.ReadInt();
        List<PortalMapEntry> entries = new(count);
        for (int i = 0; i < count; i++)
        {
            entries.Add(new PortalMapEntry
            {
                Name = package.ReadString(),
                Position = package.ReadVector3(),
                Privacy = package.ReadInt(),
                RuneMask = package.ReadInt(),
                Everything = package.ReadBool()
            });
        }

        return entries;
    }
}
