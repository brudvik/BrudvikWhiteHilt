using BrudvikWhiteHilt.Helpers;
using UnityEngine;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// The hunter who now and then sits on the chair behind a trading post's fish stall and sells the trophies of the post's
/// biome (<see cref="TradingPostTrophies"/>). Whether the hunter is there is decided by the post's owner when a player
/// comes near, with <see cref="TradingPostSettings.HunterChance"/>, and holds for a game day, so walking away and back
/// does not ask again. The hunter is a network object that is not saved: when nobody is near the post any more the game
/// lets it go, and the post puts it out again while the day's answer holds.
/// </summary>
public static class TradingPostHunter
{
    /// <summary>Prefab name of the hunter.</summary>
    public const string PrefabName = "WhiteHilt_TradingPostHunter";

    /// <summary>Translation key of the hunter's name.</summary>
    public const string NameKey = "npc_whitehilt_hunter";

    // A player this close to the landing has come to the post.
    private const float ArriveRange = 45f;

    // How long one answer holds, in world seconds: a game day.
    private const double Hold = 1800.0;

    // The hunter's look: a woodsman in troll leather with a deer hide cape and the Huntsman's bow on the back.
    private const string Hair = "Hair8";
    private const string Beard = "Beard6";
    private static readonly Vector3 hairColour = new(0.32f, 0.22f, 0.14f);
    private const string Chest = "ArmorTrollLeatherChest";
    private const string Legs = "ArmorTrollLeatherLegs";
    private const string Cape = "CapeDeerHide";
    private const string Bow = "BowHuntsman";

    /// <summary>
    /// Registers the hunter's name and lines. Call from the plugin's Awake, before the game loads its languages.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish(NameKey, "Ulf the Hunter");
        TradingPostTrader.Dialogue("hunter_greet", "Sit, sit. Want a look at my trophies?", "Every beast of this land, I have hunted.");
        TradingPostTrader.Dialogue("hunter_bye", "Good hunting!", "Mind the woods on your way.");
        TradingPostTrader.Dialogue("hunter_talk", "That one nearly had me. Nearly.", "I am only here a day, then back to the woods.",
            "A trophy on your wall tells a story. Buy one, tell it as yours.", "Sharp arrows and a quiet step, that is all it takes.");
        TradingPostTrader.Dialogue("hunter_start", "Have a look, friend.", "Each one taken with my own bow.");
        TradingPostTrader.Dialogue("hunter_buy", "Hang it high.", "A fine trophy for a fine hall.");
        TradingPostTrader.Dialogue("hunter_sell", "I only sell, friend.", "Keep your trophies; they are yours to show.");
    }

    /// <summary>
    /// Writes the hunter's look into its network data, where the game's own equipment display reads it on every machine.
    /// </summary>
    /// <param name="zdo">The hunter's network data.</param>
    public static void WriteLook(ZDO zdo)
    {
        zdo.Set(ZDOVars.s_modelIndex, 0);
        zdo.Set(ZDOVars.s_skinColor, Vector3.one);
        zdo.Set(ZDOVars.s_hairColor, hairColour);
        zdo.Set(ZDOVars.s_hairItem, Hair.GetStableHashCode());
        zdo.Set(ZDOVars.s_beardItem, Beard.GetStableHashCode());
        zdo.Set(ZDOVars.s_chestItem, Chest.GetStableHashCode());
        zdo.Set(ZDOVars.s_legItem, Legs.GetStableHashCode());
        zdo.Set(ZDOVars.s_shoulderItem, Cape.GetStableHashCode());
        zdo.Set(ZDOVars.s_shoulderItemVariant, 0);
        zdo.Set(ZDOVars.s_shoulderItemQuality, 1);
        zdo.Set(ZDOVars.s_leftBackItem, Bow.GetStableHashCode());
    }

    /// <summary>
    /// On the post's owner, every few seconds: when a player has come to the post, decides whether the hunter is there
    /// (once a game day), and puts the hunter out or sends the hunter away to match.
    /// </summary>
    /// <param name="post">The post's marker.</param>
    /// <param name="marker">The marker's network data.</param>
    /// <param name="biome">The post's biome index.</param>
    public static void Check(Transform post, ZDO marker, int biome)
    {
        if (ZNet.instance == null || ZDOMan.instance == null || !TradingPostTrophies.HasAny(biome)
            || !Player.IsPlayerInRange(post.position, ArriveRange))
        {
            return;
        }

        double now = ZNet.instance.GetTimeSeconds();
        bool here = marker.GetBool(TradingPostKeys.HunterHere);
        long decided = marker.GetLong(TradingPostKeys.HunterDecided, long.MinValue);
        if (decided == long.MinValue || now - decided >= Hold || now < decided)
        {
            here = Random.Range(0, 100) < TradingPostSettings.HunterChance.Value;
            marker.Set(TradingPostKeys.HunterDecided, (long)now);
            marker.Set(TradingPostKeys.HunterHere, here);
        }

        ZDO hunter = Find(marker);
        if (here && hunter == null)
        {
            PutOut(post, marker);
        }
        else if (!here && hunter != null)
        {
            hunter.SetOwner(ZDOMan.GetSessionID());
            ZDOMan.instance.DestroyZDO(hunter);
        }
    }

    // The hunter put out for the post, if it is still in the world.
    private static ZDO Find(ZDO marker)
    {
        long user = marker.GetLong(TradingPostKeys.HunterUser);
        uint id = (uint)marker.GetLong(TradingPostKeys.HunterObject);
        return user == 0L && id == 0u ? null : ZDOMan.instance.GetZDO(new ZDOID(user, id));
    }

    // Creates the hunter's network data on the chair, with the look already in it, owned by this machine and not saved.
    private static void PutOut(Transform post, ZDO marker)
    {
        GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(PrefabName) : null;
        TradingPostSpot spot = TradingPostLayout.Load().hunter;
        if (prefab == null || spot == null)
        {
            return;
        }

        Vector3 position = post.TransformPoint(TradingPostLayout.ToVector(spot.position));
        Quaternion rotation = post.rotation * Quaternion.Euler(0f, spot.yaw, 0f);
        int hash = prefab.name.GetStableHashCode();
        ZNetView template = prefab.GetComponent<ZNetView>();
        ZDO hunter = ZDOMan.instance.CreateNewZDO(position, hash);
        hunter.Persistent = template.m_persistent;
        hunter.Type = template.m_type;
        hunter.Distant = template.m_distant;
        hunter.SetPrefab(hash);
        hunter.SetRotation(rotation);
        hunter.Set(TradingPostKeys.PostId, marker.GetString(TradingPostKeys.PostId));
        hunter.Set(TradingPostKeys.Biome, marker.GetInt(TradingPostKeys.Biome));
        WriteLook(hunter);
        marker.Set(TradingPostKeys.HunterUser, hunter.m_uid.UserID);
        marker.Set(TradingPostKeys.HunterObject, (long)hunter.m_uid.ID);
        Jotunn.Logger.LogInfo($"Trading posts: a hunter sits at the post at {position.x:0} {position.z:0}.");
    }
}
