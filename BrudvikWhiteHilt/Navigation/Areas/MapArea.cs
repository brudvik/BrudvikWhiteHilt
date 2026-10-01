using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Navigation.Areas;

/// <summary>
/// What kind of place a map area is.
/// </summary>
public enum MapAreaKind : byte
{
    /// <summary>Buildings without a bed and fire, workbench or portal.</summary>
    Building,

    /// <summary>Buildings with a workbench or a portal.</summary>
    Outpost,

    /// <summary>Buildings with a bed and a fire.</summary>
    Base,

    /// <summary>Planted crops.</summary>
    Field,

    /// <summary>Tamed animals.</summary>
    Pasture,
}

/// <summary>
/// One area on the map: a group of 16 m cells with buildings, crops or tamed animals.
/// </summary>
public sealed class MapArea
{
    /// <summary>Side of a cell in metres.</summary>
    public const float CellSize = 16f;

    /// <summary>The kind of place.</summary>
    public MapAreaKind Kind { get; set; }

    /// <summary>Player ID of whoever built most of it; 0 for fields and pastures.</summary>
    public long Creator { get; set; }

    /// <summary>Name of that player, if known.</summary>
    public string Builder { get; set; } = string.Empty;

    /// <summary>Name from a sign starting with #, or empty.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Number of pieces, plants or animals.</summary>
    public int Count { get; set; }

    /// <summary>Cells as (x, z) indexes: world position divided by <see cref="CellSize"/>, rounded down.</summary>
    public List<Vector2Int> Cells { get; set; } = new();

    /// <summary>
    /// The middle of the area in world space.
    /// </summary>
    /// <returns>The centre of its cells.</returns>
    public Vector3 Centre()
    {
        Vector2 sum = Vector2.zero;
        foreach (Vector2Int cell in Cells)
        {
            sum += cell;
        }

        sum = sum / Mathf.Max(1, Cells.Count) + new Vector2(0.5f, 0.5f);
        return new Vector3(sum.x * CellSize, 0f, sum.y * CellSize);
    }
}

/// <summary>
/// A ward and how far it reaches.
/// </summary>
public sealed class MapWard
{
    /// <summary>World position.</summary>
    public Vector3 Position { get; set; }

    /// <summary>Reach in metres.</summary>
    public float Radius { get; set; }

    /// <summary>Whether it is switched on.</summary>
    public bool Enabled { get; set; }

    /// <summary>Player ID of whoever placed it.</summary>
    public long Creator { get; set; }
}

/// <summary>
/// The areas and wards as sent from the server.
/// </summary>
public static class MapAreaPackage
{
    private const int Version = 1;
    private const int CellOffset = 32768;

    /// <summary>
    /// Writes areas and wards.
    /// </summary>
    /// <param name="areas">The areas.</param>
    /// <param name="wards">The wards.</param>
    /// <returns>The package.</returns>
    public static ZPackage Write(List<MapArea> areas, List<MapWard> wards)
    {
        ZPackage package = new();
        package.Write(Version);
        package.Write(areas.Count);
        foreach (MapArea area in areas)
        {
            package.Write((int)area.Kind);
            package.Write(area.Creator);
            package.Write(area.Builder ?? string.Empty);
            package.Write(area.Name ?? string.Empty);
            package.Write(area.Count);
            package.Write(area.Cells.Count);
            foreach (Vector2Int cell in area.Cells)
            {
                package.Write(((cell.x + CellOffset) << 16) | ((cell.y + CellOffset) & 0xffff));
            }
        }

        package.Write(wards.Count);
        foreach (MapWard ward in wards)
        {
            package.Write(ward.Position.x);
            package.Write(ward.Position.z);
            package.Write(ward.Radius);
            package.Write(ward.Enabled);
            package.Write(ward.Creator);
        }

        return package;
    }

    /// <summary>
    /// Reads areas and wards.
    /// </summary>
    /// <param name="package">The package, positioned at its start.</param>
    /// <param name="areas">The areas read.</param>
    /// <param name="wards">The wards read.</param>
    /// <returns>False if the package is of another version.</returns>
    public static bool Read(ZPackage package, out List<MapArea> areas, out List<MapWard> wards)
    {
        areas = new List<MapArea>();
        wards = new List<MapWard>();
        if (package.ReadInt() != Version)
        {
            return false;
        }

        int areaCount = package.ReadInt();
        for (int i = 0; i < areaCount; i++)
        {
            MapArea area = new()
            {
                Kind = (MapAreaKind)package.ReadInt(),
                Creator = package.ReadLong(),
                Builder = package.ReadString(),
                Name = package.ReadString(),
                Count = package.ReadInt(),
            };
            int cellCount = package.ReadInt();
            for (int c = 0; c < cellCount; c++)
            {
                int packed = package.ReadInt();
                area.Cells.Add(new Vector2Int(((packed >> 16) & 0xffff) - CellOffset, (packed & 0xffff) - CellOffset));
            }

            areas.Add(area);
        }

        int wardCount = package.ReadInt();
        for (int i = 0; i < wardCount; i++)
        {
            float x = package.ReadSingle();
            float z = package.ReadSingle();
            wards.Add(new MapWard
            {
                Position = new Vector3(x, 0f, z),
                Radius = package.ReadSingle(),
                Enabled = package.ReadBool(),
                Creator = package.ReadLong(),
            });
        }

        return true;
    }
}
