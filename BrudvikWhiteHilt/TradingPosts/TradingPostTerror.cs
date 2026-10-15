using UnityEngine;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// The plague on a trading post: now and then a named monster of the biome (<see cref="TerrorDefinition"/>) makes its
/// lair a few hundred metres inland of a post, comes down to the yard at night and keeps people away, so the trader asks
/// more and pays less until it is gone. The trader hands out a free bounty notice with a circle on the map where the
/// monster was last seen; its head, brought back, is paid for well, and the trader gives the bringer half off the post's
/// garb. Whether a post is plagued is decided by the post's owner when a player comes, once a game day, and the plague
/// lasts a few days if nobody ends it. Everything is kept in the marker's network data.
/// </summary>
public static class TradingPostTerror
{
    // A player this close to the landing has come to the post.
    private const float ArriveRange = 45f;

    // How long one answer holds, in world seconds: a game day.
    private const double Hold = 1800.0;

    // How long a plague lasts, in game days.
    private const int Days = 3;

    // How far inland of the post the lair is, in metres.
    private const float LairMin = 300f;
    private const float LairMax = 800f;
    private const int LairAttempts = 80;

    /// <summary>The radius of the search circle on the bounty notice.</summary>
    public const float CircleRadius = 150f;

    // Where the monster stands when it comes down to the post at night, from the post's origin.
    private const float VisitDistance = 38f;

    // A monster this far from the post is in its own ground.
    private const float AwayDistance = 150f;

    /// <summary>How much more the trader asks, and less pays, while the post is plagued.</summary>
    public const float PlaguePenalty = 0.2f;

    /// <summary>The share off the post's garb the bringer of the head gets.</summary>
    public const float Favour = 0.5f;

    private const string FavourPrefix = "whitehilt_garbfavour_";

    /// <summary>
    /// How long a plague lasts, in world seconds.
    /// </summary>
    public static double Duration => Days * Hold;

    /// <summary>
    /// Whether a plague is on, by its end time.
    /// </summary>
    /// <param name="until">The world time it lasts until; 0 for none.</param>
    /// <param name="now">The world time now.</param>
    /// <returns>True while it lasts.</returns>
    public static bool Lasts(long until, double now) => until > 0 && now < until;

    /// <summary>
    /// The plague on a post whose marker this machine has, or null.
    /// </summary>
    /// <param name="postId">The post's id.</param>
    /// <returns>The biome's plague while the post is plagued.</returns>
    public static TerrorDefinition PlagueOn(string postId)
    {
        ZDO marker = TradingPostSite.Marker(postId);
        if (marker == null || ZNet.instance == null || !Lasts(marker.GetLong(TradingPostKeys.TerrorUntil), ZNet.instance.GetTimeSeconds()))
        {
            return null;
        }

        return TerrorDefinition.Of(TradingPostGoods.IndexOf(TraderLooks.Get((Heightmap.Biome)marker.GetInt(TradingPostKeys.Biome)).biome));
    }

    /// <summary>
    /// On the post's owner, every few seconds: when a player has come to the post, decides once a game day whether a
    /// monster plagues it, ends a plague that has lasted its time, and at night brings the monster down to the yard.
    /// </summary>
    /// <param name="post">The post's marker.</param>
    /// <param name="marker">The marker's network data.</param>
    /// <param name="biome">The post's biome index.</param>
    public static void Check(Transform post, ZDO marker, int biome)
    {
        TerrorDefinition terror = TerrorDefinition.Of(biome);
        if (terror == null || ZNet.instance == null || ZDOMan.instance == null || !Player.IsPlayerInRange(post.position, ArriveRange))
        {
            return;
        }

        double now = ZNet.instance.GetTimeSeconds();
        long until = marker.GetLong(TradingPostKeys.TerrorUntil);
        if (until > 0 && !Lasts(until, now))
        {
            End(marker);
            until = 0;
        }

        if (until == 0)
        {
            long decided = marker.GetLong(TradingPostKeys.TerrorDecided, long.MinValue);
            if (decided != long.MinValue && now - decided < Hold && now >= decided)
            {
                return;
            }

            marker.Set(TradingPostKeys.TerrorDecided, (long)now);
            string key = TradingPostGoods.OpenKey(biome);
            bool open = string.IsNullOrEmpty(key) || (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(key));
            if (open && Random.Range(0, 100) < TradingPostSettings.TerrorChance.Value)
            {
                Begin(post, marker, biome, terror, now);
            }

            return;
        }

        Visit(post, marker, terror);
    }

    /// <summary>
    /// Ends the plague on a post when its monster's head is brought: the bringer gets half off the post's garb.
    /// </summary>
    /// <param name="postId">The post's id.</param>
    /// <param name="player">The bringer.</param>
    public static void Deliver(string postId, Player player)
    {
        ZDO marker = TradingPostSite.Marker(postId);
        if (marker != null)
        {
            marker.SetOwner(ZDOMan.GetSessionID());
            End(marker);
        }

        if (player != null)
        {
            player.m_customData[FavourPrefix + postId] = "1";
        }
    }

    /// <summary>
    /// Whether a player has half off a post's garb for bringing its monster's head.
    /// </summary>
    /// <param name="player">The player, or null.</param>
    /// <param name="postId">The post's id.</param>
    /// <returns>True with the favour.</returns>
    public static bool HasFavour(Player player, string postId) => player != null && postId != null && player.m_customData.ContainsKey(FavourPrefix + postId);

    /// <summary>
    /// Uses up a player's favour at a post, once they have bought a piece of its garb.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="postId">The post's id.</param>
    public static void UseFavour(Player player, string postId)
    {
        if (player != null && postId != null)
        {
            player.m_customData.Remove(FavourPrefix + postId);
        }
    }

    /// <summary>
    /// Writes what a bounty notice shows: the post, its plague, the search circle and the plague's end.
    /// </summary>
    /// <param name="notice">The notice.</param>
    /// <param name="postId">The plagued post's id.</param>
    /// <returns>True if the post is loaded and plagued.</returns>
    public static bool Fill(ItemDrop.ItemData notice, string postId)
    {
        ZDO marker = TradingPostSite.Marker(postId);
        if (notice == null || marker == null)
        {
            return false;
        }

        long until = marker.GetLong(TradingPostKeys.TerrorUntil);
        int biome = TradingPostGoods.IndexOf(TraderLooks.Get((Heightmap.Biome)marker.GetInt(TradingPostKeys.Biome)).biome);
        TerrorNotice.Write(notice, new TerrorNotice.Data
        {
            Post = postId,
            Biome = biome,
            Centre = marker.GetVec3(TradingPostKeys.TerrorCentre, Vector3.zero),
            Lair = marker.GetVec3(TradingPostKeys.TerrorLair, Vector3.zero),
            Until = until
        });
        return true;
    }

    // A plague begins: a lair inland of the post, the monster put out in it, saved, and the search circle near it.
    private static void Begin(Transform post, ZDO marker, int biome, TerrorDefinition terror, double now)
    {
        GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(terror.PrefabName) : null;
        if (prefab == null || !FindLair(post, biome, out Vector3 lair))
        {
            return;
        }

        long until = (long)(now + Duration);
        int hash = prefab.name.GetStableHashCode();
        ZNetView template = prefab.GetComponent<ZNetView>();
        ZDO monster = ZDOMan.instance.CreateNewZDO(lair, hash);
        monster.Persistent = template.m_persistent;
        monster.Type = template.m_type;
        monster.Distant = template.m_distant;
        monster.SetPrefab(hash);
        monster.SetRotation(Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        monster.Set(ZDOVars.s_level, TradingPostTerrors.Level);
        monster.Set(ZDOVars.s_spawnPoint, lair);
        monster.Set(TradingPostKeys.TerrorPost, marker.GetString(TradingPostKeys.PostId));
        monster.Set(TradingPostKeys.TerrorHome, lair);
        monster.Set(TradingPostKeys.TerrorLeaves, until);

        Vector2 offset = Random.insideUnitCircle * (CircleRadius * 0.6f);
        marker.Set(TradingPostKeys.TerrorUntil, until);
        marker.Set(TradingPostKeys.TerrorLair, lair);
        marker.Set(TradingPostKeys.TerrorCentre, lair + new Vector3(offset.x, 0f, offset.y));
        marker.Set(TradingPostKeys.TerrorUser, monster.m_uid.UserID);
        marker.Set(TradingPostKeys.TerrorObject, (long)monster.m_uid.ID);
        marker.Set(TradingPostKeys.TerrorVisited, -1);
        Jotunn.Logger.LogInfo($"Trading posts: {terror.EnglishName} plagues the post at {post.position.x:0} {post.position.z:0}, its lair at {lair.x:0} {lair.z:0}.");
    }

    // A plague ends: its monster, if it still lives, leaves the land, and the post is free.
    private static void End(ZDO marker)
    {
        ZDO monster = Monster(marker);
        if (monster != null && ZNetScene.instance != null && ZNetScene.instance.FindInstance(monster) == null)
        {
            monster.SetOwner(ZDOMan.GetSessionID());
            ZDOMan.instance.DestroyZDO(monster);
        }
        else if (monster != null)
        {
            // A loaded monster leaves on its own (TerrorBehaviour), now its time is up.
            monster.SetOwner(ZDOMan.GetSessionID());
            monster.Set(TradingPostKeys.TerrorLeaves, 1L);
        }

        marker.Set(TradingPostKeys.TerrorUntil, 0L);
        marker.Set(TradingPostKeys.TerrorUser, 0L);
        marker.Set(TradingPostKeys.TerrorObject, 0L);
    }

    // At night, once a night, the monster comes down from its lair to the post's yard, if nobody is fighting it where it
    // is; it goes back by day (TerrorBehaviour).
    private static void Visit(Transform post, ZDO marker, TerrorDefinition terror)
    {
        if (EnvMan.instance == null || !EnvMan.IsNight() || ZoneSystem.instance == null)
        {
            return;
        }

        int night = EnvMan.instance.GetDay();
        ZDO monster = Monster(marker);
        if (monster == null || marker.GetInt(TradingPostKeys.TerrorVisited, -1) == night
            || Utils.DistanceXZ(monster.GetPosition(), post.position) < AwayDistance || ZNetScene.instance.FindInstance(monster) != null)
        {
            return;
        }

        Vector3 lair = marker.GetVec3(TradingPostKeys.TerrorLair, monster.GetPosition());
        Vector3 inland = lair - post.position;
        inland.y = 0f;
        Vector3 spot = post.position + (inland.sqrMagnitude > 1f ? inland.normalized : -post.forward) * VisitDistance;
        if (!ZoneSystem.instance.GetSolidHeight(spot, out float height))
        {
            return;
        }

        spot.y = height + 0.5f;
        marker.Set(TradingPostKeys.TerrorVisited, night);
        monster.SetOwner(ZDOMan.GetSessionID());
        monster.SetPosition(spot);
        monster.Set(ZDOVars.s_spawnPoint, spot);
        monster.Set(ZDOVars.s_huntPlayer, true);
        Monsters.ArrivalSound.Mark(monster);
        Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
            Localization.instance.Localize("$msg_whitehilt_terror_comes", Localization.instance.Localize("$" + terror.NameKey)));
    }

    // The plague's monster, if it is still in the world.
    private static ZDO Monster(ZDO marker)
    {
        long user = marker.GetLong(TradingPostKeys.TerrorUser);
        uint id = (uint)marker.GetLong(TradingPostKeys.TerrorObject);
        return user == 0L && id == 0u ? null : ZDOMan.instance.GetZDO(new ZDOID(user, id));
    }

    // A dry spot of the post's biome a few hundred metres inland, or of any land if the biome has none there.
    private static bool FindLair(Transform post, int biome, out Vector3 lair)
    {
        WorldGenerator world = WorldGenerator.instance;
        float water = ZoneSystem.instance != null ? ZoneSystem.instance.m_waterLevel : 30f;
        Heightmap.Biome wanted = (Heightmap.Biome)System.Enum.Parse(typeof(Heightmap.Biome), TradingPostGoods.Biomes[biome]);
        Vector3 fallback = Vector3.zero;
        bool any = false;
        for (int i = 0; world != null && i < LairAttempts; i++)
        {
            float angle = Random.Range(-100f, 100f) + 180f;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * post.forward;
            Vector3 point = post.position + direction * Random.Range(LairMin, LairMax);
            float height = world.GetHeight(point.x, point.z);
            if (height < water + 2f)
            {
                continue;
            }

            point.y = height + 0.5f;
            if (world.GetBiome(point.x, point.z) == wanted)
            {
                lair = point;
                return true;
            }

            if (!any)
            {
                fallback = point;
                any = true;
            }
        }

        lair = fallback;
        return any;
    }
}
