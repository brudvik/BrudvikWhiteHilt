using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Portals.PortalMap;

/// <summary>
/// One portal or ship on the shared map, as the server sends it to a player.
/// </summary>
public class PortalMapEntry
{
    /// <summary>
    /// <see cref="Kind"/> of a portal or Portal Stations station.
    /// </summary>
    public const byte PortalKind = 0;

    /// <summary>
    /// <see cref="Kind"/> of a ship.
    /// </summary>
    public const byte ShipKind = 1;

    /// <summary>
    /// Privacy value of a public Portal Stations station, and of every ordinary portal and ship.
    /// </summary>
    public const int Public = 0;

    /// <summary>
    /// Privacy value of a private Portal Stations station.
    /// </summary>
    public const int Private = 1;

    /// <summary>
    /// <see cref="PortalKind"/> or <see cref="ShipKind"/>.
    /// </summary>
    public byte Kind { get; set; }

    /// <summary>
    /// Name the players gave the portal, or empty.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Position of the portal or ship.
    /// </summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// Portal Stations privacy: 0 public, 1 private, 2 guild, 3 group, 4 guild and group.
    /// </summary>
    public int Privacy { get; set; }

    /// <summary>
    /// Player ID of the builder. The server uses it for privacy and the builder's name; it is not sent.
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
    /// Prefab hash of a ship, for its icon and type name.
    /// </summary>
    public int Prefab { get; set; }

    /// <summary>
    /// Name of the player who built the ship, if they are online, or empty.
    /// </summary>
    public string Builder { get; set; } = string.Empty;

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
            package.Write(entry.Kind);
            package.Write(entry.Name);
            package.Write(entry.Position);
            package.Write(entry.Privacy);
            package.Write(entry.RuneMask);
            package.Write(entry.Everything);
            package.Write(entry.Prefab);
            package.Write(entry.Builder);
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
                Kind = package.ReadByte(),
                Name = package.ReadString(),
                Position = package.ReadVector3(),
                Privacy = package.ReadInt(),
                RuneMask = package.ReadInt(),
                Everything = package.ReadBool(),
                Prefab = package.ReadInt(),
                Builder = package.ReadString()
            });
        }

        return entries;
    }

    /// <summary>
    /// True if both entries show the same thing, so a pin can move instead of being made again.
    /// </summary>
    /// <param name="other">The other entry.</param>
    /// <returns>True if only the position or the runes may differ.</returns>
    public bool SameMarker(PortalMapEntry other)
    {
        return Kind == other.Kind && Prefab == other.Prefab && Name == other.Name && Privacy == other.Privacy;
    }
}
