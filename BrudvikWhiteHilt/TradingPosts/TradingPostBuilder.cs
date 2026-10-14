using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// Builds and removes trading posts on the server. A post is made only in the world's network data, the way the game
/// places vegetation in zones nobody is in: a marker at its origin and every piece of <see cref="TradingPostLayout"/>,
/// each carrying the post's id. They come to life when a player comes near.
/// </summary>
public static class TradingPostBuilder
{
    // build_water.py PILE_LENGTH: how far one long pile reaches down from its top.
    private const float PileLength = 6f;

    // A long pile is only needed where the bed lies this much below where the jetty's own piles end.
    private const float PileMargin = 0.2f;

    // Never more long piles stacked under one place than this, so a post on the edge of a deep trench stays sane.
    private const int MaxPilesPerPlace = 4;

    /// <summary>
    /// Builds a post. Call on the server.
    /// </summary>
    /// <param name="origin">The middle of the landing at deck height.</param>
    /// <param name="yaw">Which way the post faces, in degrees: its jetty runs out along this direction.</param>
    /// <param name="biome">The biome the post serves.</param>
    /// <returns>The new post's id, or null if a prefab is missing.</returns>
    public static string Build(Vector3 origin, float yaw, Heightmap.Biome biome)
    {
        TradingPostLayout layout = TradingPostLayout.Load();
        Quaternion turn = Quaternion.Euler(0f, yaw, 0f);
        string id = Guid.NewGuid().ToString("N");

        GameObject marker = ZNetScene.instance.GetPrefab(TradingPostKeys.MarkerPrefabName);
        if (marker == null)
        {
            Jotunn.Logger.LogWarning("Trading posts: the marker prefab is missing, no post can be built");
            return null;
        }

        ZDO site = Create(marker, origin, turn);
        site.Set(TradingPostKeys.PostId, id);
        site.Set(TradingPostKeys.Biome, (int)biome);
        int built = 1;
        List<string> missing = new();
        foreach (TradingPostPart part in layout.parts)
        {
            GameObject prefab = ZNetScene.instance.GetPrefab(part.prefab);
            if (prefab == null)
            {
                missing.Add(part.prefab);
                continue;
            }

            Vector3 position = origin + turn * TradingPostLayout.ToVector(part.position);
            if (part.ground)
            {
                position.y = LevelledHeight(layout, origin, turn, position);
            }

            Create(prefab, position, turn * Quaternion.Euler(0f, part.yaw, 0f)).Set(TradingPostKeys.PostId, id);
            built++;
        }

        built += BuildPiles(layout, origin, turn, id);
        if (missing.Count > 0)
        {
            Jotunn.Logger.LogWarning($"Trading posts: missing prefabs, left out: {string.Join(", ", missing.Distinct())}");
        }

        Jotunn.Logger.LogInfo($"Trading posts: built post {id} ({biome}) at {origin.x:0} {origin.z:0} facing {yaw:0}° ({built} objects).");
        return id;
    }

    /// <summary>
    /// Removes every object of a post. Call on the server.
    /// </summary>
    /// <param name="id">The post's id.</param>
    /// <returns>How many objects were removed.</returns>
    public static int Remove(string id)
    {
        List<ZDO> parts = ZDOMan.instance.m_objectsByID.Values.Where(zdo => zdo.GetString(TradingPostKeys.PostId) == id).ToList();
        foreach (ZDO zdo in parts)
        {
            zdo.SetOwner(ZDOMan.GetSessionID());
            ZDOMan.instance.DestroyZDO(zdo);
        }

        return parts.Count;
    }

    /// <summary>
    /// Every post's marker the server knows.
    /// </summary>
    /// <returns>The markers' network data.</returns>
    public static IEnumerable<ZDO> Markers()
    {
        return ZDOMan.instance.m_objectsByID.Values.Where(zdo => zdo.GetPrefab() == TradingPostKeys.MarkerHash);
    }

    // Stands long piles under the jetty's own piles wherever the bed lies deeper than they reach, stacked down to
    // the bed.
    private static int BuildPiles(TradingPostLayout layout, Vector3 origin, Quaternion turn, string id)
    {
        GameObject pile = ZNetScene.instance.GetPrefab(TradingPostPile.PilePrefabName);
        if (pile == null || layout.piles == null)
        {
            return 0;
        }

        int built = 0;
        foreach (TradingPostSpot spot in layout.piles)
        {
            Vector3 top = origin + turn * TradingPostLayout.ToVector(spot.position);
            float bed = GroundHeight(top);
            for (int i = 0; i < MaxPilesPerPlace && bed < top.y - PileMargin; i++)
            {
                Create(pile, top, turn).Set(TradingPostKeys.PostId, id);
                top.y -= PileLength;
                built++;
            }
        }

        return built;
    }

    // The ground a thing on the land stands on: inside a levelled patch the patch's height, which the post's terrain
    // modifiers make the ground when it loads; elsewhere the ground itself.
    private static float LevelledHeight(TradingPostLayout layout, Vector3 origin, Quaternion turn, Vector3 position)
    {
        foreach (TradingPostLevel level in layout.level ?? new TradingPostLevel[0])
        {
            Vector3 centre = origin + turn * TradingPostLayout.ToVector(level.centre);
            if (Utils.DistanceXZ(centre, position) <= level.radius)
            {
                return centre.y;
            }
        }

        return GroundHeight(position);
    }

    // The height of the ground: the terrain where the zone is loaded, else the generated world's.
    private static float GroundHeight(Vector3 position)
    {
        if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(position, out float height))
        {
            return height;
        }

        return WorldGenerator.instance.GetHeight(position.x, position.z);
    }

    // Creates an object in the world's network data only; nobody owns it until a player comes near.
    private static ZDO Create(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        ZNetView view = prefab.GetComponent<ZNetView>();
        int hash = prefab.name.GetStableHashCode();
        ZDO zdo = ZDOMan.instance.CreateNewZDO(position, hash);
        zdo.Persistent = view.m_persistent;
        zdo.Type = view.m_type;
        zdo.Distant = view.m_distant;
        zdo.SetPrefab(hash);
        zdo.SetRotation(rotation);
        zdo.SetOwnerInternal(0L);
        return zdo;
    }
}
