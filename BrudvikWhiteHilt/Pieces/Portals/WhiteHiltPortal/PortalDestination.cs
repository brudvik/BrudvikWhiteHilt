using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Portals.WhiteHiltPortal;

/// <summary>
/// A White Hilt portal a player can travel to, as the server sends it.
/// </summary>
public class PortalDestination
{
    /// <summary>
    /// Stable id of the portal; the Portal Stations id for stations taken over from that mod.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Name the players gave the portal, or empty.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Position of the portal.
    /// </summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// Rotation of the portal around the vertical axis, in degrees.
    /// </summary>
    public float Yaw { get; set; }

    /// <summary>
    /// True for a portal that lies on the ground; travellers arrive on top of it instead of in front.
    /// </summary>
    public bool Ground { get; set; }

    /// <summary>
    /// True if only its builder may travel to it.
    /// </summary>
    public bool Private { get; set; }

    /// <summary>
    /// True if the receiving player built it.
    /// </summary>
    public bool Own { get; set; }

    /// <summary>
    /// Player ID of the builder. The server uses it; it is not sent.
    /// </summary>
    public long Creator { get; set; }

    /// <summary>
    /// Where a traveller arrives.
    /// </summary>
    /// <returns>The arrival point.</returns>
    public Vector3 ArrivalPoint()
    {
        Quaternion rotation = Quaternion.Euler(0f, Yaw, 0f);
        return Ground ? Position + Vector3.up * 0.4f : Position + rotation * Vector3.forward * 1.8f + Vector3.up;
    }

    /// <summary>
    /// Writes a list of destinations to a package.
    /// </summary>
    /// <param name="destinations">Destinations to write.</param>
    /// <returns>The package.</returns>
    public static ZPackage Write(IReadOnlyList<PortalDestination> destinations)
    {
        ZPackage package = new();
        package.Write(destinations.Count);
        foreach (PortalDestination destination in destinations)
        {
            package.Write(destination.Id);
            package.Write(destination.Name);
            package.Write(destination.Position);
            package.Write(destination.Yaw);
            package.Write(destination.Ground);
            package.Write(destination.Private);
            package.Write(destination.Own);
        }

        return package;
    }

    /// <summary>
    /// Reads a list written by <see cref="Write"/>.
    /// </summary>
    /// <param name="package">The package.</param>
    /// <returns>The destinations.</returns>
    public static List<PortalDestination> Read(ZPackage package)
    {
        int count = package.ReadInt();
        List<PortalDestination> destinations = new(count);
        for (int i = 0; i < count; i++)
        {
            destinations.Add(new PortalDestination
            {
                Id = package.ReadString(),
                Name = package.ReadString(),
                Position = package.ReadVector3(),
                Yaw = package.ReadSingle(),
                Ground = package.ReadBool(),
                Private = package.ReadBool(),
                Own = package.ReadBool()
            });
        }

        return destinations;
    }
}
