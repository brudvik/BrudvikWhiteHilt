using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Building;

/// <summary>
/// Places pieces without the ghost, for the stamp and redo tools, with the same costs and rules as normal building.
/// </summary>
public static class BuildPlacer
{
    private const float MinExtent = 0.05f;

    private static readonly List<Piece> nearbyPieces = new();
    private static readonly List<Transform> snapPoints = new();

    /// <summary>True while a redo places its piece, so it does not clear what can be redone.</summary>
    public static bool Redoing { get; private set; }

    /// <summary>
    /// Places a copy of the last piece right next to it, on the side the camera looks towards.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Stamp(Player player)
    {
        Piece last = BuildUndo.LastPlaced;
        Piece prefab = last != null ? ZNetScene.instance.GetPrefab(Utils.GetPrefabName(last.gameObject))?.GetComponent<Piece>() : null;
        if (prefab == null || GameCamera.instance == null)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_build_stamp_none");
            return;
        }

        Transform piece = last.transform;
        Vector3 look = GameCamera.instance.transform.forward;
        Vector3 bestAxis = Vector3.forward;
        float bestDot = float.NegativeInfinity;
        foreach (Vector3 axis in new[] { Vector3.forward, Vector3.back, Vector3.right, Vector3.left, Vector3.up, Vector3.down })
        {
            float dot = Vector3.Dot(piece.TransformDirection(axis), look);
            if (dot > bestDot)
            {
                bestDot = dot;
                bestAxis = axis;
            }
        }

        float extent = Extent(last, bestAxis);
        if (extent < MinExtent)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_build_stamp_unsupported");
            return;
        }

        TryPlace(player, prefab, piece.position + piece.TransformDirection(bestAxis) * extent, piece.rotation, redo: false);
    }

    /// <summary>
    /// Places a piece at a given spot if the player may build it there and can pay for it.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="piece">The piece prefab.</param>
    /// <param name="position">Where to place it.</param>
    /// <param name="rotation">How to turn it.</param>
    /// <param name="redo">True when putting back an undone piece.</param>
    /// <returns>True if the piece was placed.</returns>
    public static bool TryPlace(Player player, Piece piece, Vector3 position, Quaternion rotation, bool redo)
    {
        if (!CanPlaceWithoutGhost(player, piece))
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_build_stamp_unsupported");
            return false;
        }

        if (!player.m_noPlacementCost && !player.HaveRequirements(piece, Player.RequirementMode.CanBuild))
        {
            player.Message(MessageHud.MessageType.Center, "$msg_missingrequirement");
            return false;
        }

        ItemDrop.ItemData tool = player.GetRightItem();
        if (tool != null && !player.HaveStamina(tool.m_shared.m_attack.m_attackStamina))
        {
            Hud.instance.StaminaBarEmptyFlash();
            return false;
        }

        string problem = CheckSpot(piece, position, rotation);
        if (problem != null)
        {
            player.Message(MessageHud.MessageType.Center, problem);
            return false;
        }

        Redoing = redo;
        try
        {
            player.PlacePiece(piece, position, rotation);
        }
        finally
        {
            Redoing = false;
        }

        if (!player.m_noPlacementCost && !ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey()))
        {
            player.ConsumeResources(piece.m_resources, 0);
        }

        player.UseStamina(player.GetBuildStamina());
        if (tool != null && tool.m_shared.m_useDurability)
        {
            tool.m_durability -= player.GetPlaceDurability(tool) * Game.m_durabilityRate;
        }

        return true;
    }

    /// <summary>
    /// True if the piece can be placed without vanilla's ghost: it is in the held tool and does not depend on the ground,
    /// water or terrain under it.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="piece">The piece prefab.</param>
    /// <returns>True if it can.</returns>
    public static bool CanPlaceWithoutGhost(Player player, Piece piece)
    {
        return player.m_buildPieces != null && player.m_buildPieces.m_pieces.Contains(piece.gameObject)
            && !piece.m_groundOnly && !piece.m_groundPiece && !piece.m_cultivatedGroundOnly && !piece.m_waterPiece
            && piece.GetComponent<TerrainModifier>() == null && piece.GetComponent<TerrainOp>() == null;
    }

    // Why a piece may not be placed at a spot, or null if it may: a place where building is not allowed, someone else's
    // ward, the wrong biome, or the same piece already there facing the same way.
    private static string CheckSpot(Piece piece, Vector3 position, Quaternion rotation)
    {
        if (Location.IsInsideNoBuildLocation(position))
        {
            return "$msg_nobuildzone";
        }

        PrivateArea ward = piece.GetComponent<PrivateArea>();
        if (!PrivateArea.CheckAccess(position, ward != null ? ward.m_radius : 0f, true, ward != null))
        {
            return "$msg_privatezone";
        }

        if (piece.m_onlyInBiome != Heightmap.Biome.None && (Heightmap.FindBiome(position) & piece.m_onlyInBiome) == 0)
        {
            return "$msg_wrongbiome";
        }

        nearbyPieces.Clear();
        Piece.GetAllPiecesInRadius(position, 0.1f, nearbyPieces);
        string name = piece.gameObject.name;
        foreach (Piece other in nearbyPieces)
        {
            if (Utils.GetPrefabName(other.gameObject) == name && Quaternion.Angle(other.transform.rotation, rotation) < 10f)
            {
                return "$msg_whitehilt_build_occupied";
            }
        }

        return null;
    }

    private static float Extent(Piece piece, Vector3 localAxis)
    {
        Range(piece, localAxis, out float min, out float max);
        return max - min;
    }

    /// <summary>
    /// How far a piece reaches along one of its own axes, measured from its pivot. Snap points are used when there are
    /// any, since they mark where pieces join; otherwise the mesh bounds.
    /// </summary>
    /// <param name="piece">The piece or piece prefab.</param>
    /// <param name="localAxis">The axis in the piece's own space.</param>
    /// <param name="min">Lowest reach along the axis.</param>
    /// <param name="max">Highest reach along the axis.</param>
    public static void Range(Piece piece, Vector3 localAxis, out float min, out float max)
    {
        snapPoints.Clear();
        piece.GetSnapPoints(snapPoints);
        Span(snapPoints.ConvertAll(point => piece.transform.InverseTransformPoint(point.position)), localAxis, out min, out max);
        if (max - min >= MinExtent)
        {
            return;
        }

        List<Vector3> corners = new();
        foreach (MeshFilter filter in piece.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.sharedMesh == null)
            {
                continue;
            }

            Bounds bounds = filter.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                corners.Add(piece.transform.InverseTransformPoint(filter.transform.TransformPoint(corner)));
            }
        }

        Span(corners, localAxis, out min, out max);
    }

    // How far a set of points reaches along an axis, from its lowest to its highest.
    private static void Span(List<Vector3> points, Vector3 axis, out float min, out float max)
    {
        min = 0f;
        max = 0f;
        if (points.Count < 2)
        {
            return;
        }

        min = float.PositiveInfinity;
        max = float.NegativeInfinity;
        foreach (Vector3 point in points)
        {
            float along = Vector3.Dot(point, axis);
            min = Mathf.Min(min, along);
            max = Mathf.Max(max, along);
        }
    }
}
