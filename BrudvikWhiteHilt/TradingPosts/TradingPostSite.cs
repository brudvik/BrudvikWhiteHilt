using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// The invisible marker at a trading post's origin, turned the way the post faces. It carries the dome on every
/// machine that loads it, is a location nothing may be built in, and levels the land behind the landing (both set up on
/// the prefab, <see cref="TradingPostRegistry"/>). Its owner clears trees, rocks and bushes off the post and puts the
/// post's trader out once; every machine dyes the post's stalls and pennant in its biome's colours.
/// </summary>
public class TradingPostSite : MonoBehaviour
{
    // When to look again after the post comes to life: a zone generated as the player arrives gets its trees a moment
    // after the post's objects load, and the post's own pieces load around the same time.
    private static readonly float[] passDelays = { 1f, 5f, 15f };

    // Vegetation is cleared this far inside the dome's edge, so the trees just outside it stay.
    private const float ClearMargin = 4f;

    private static readonly List<TradingPostSite> loaded = new();

    private ZNetView view;
    private int pass;

    /// <summary>
    /// Gets the network id of the post's marker.
    /// </summary>
    public ZDOID Id => view != null && view.IsValid() ? view.GetZDO().m_uid : ZDOID.None;

    /// <summary>
    /// Gets the post's id, the same on all its pieces.
    /// </summary>
    public string PostId => view != null && view.IsValid() ? view.GetZDO().GetString(TradingPostKeys.PostId) : null;

    /// <summary>
    /// Gets the trader of the post's biome.
    /// </summary>
    public TraderLook Look => TraderLooks.Get((Heightmap.Biome)view.GetZDO().GetInt(TradingPostKeys.Biome));

    /// <summary>
    /// The loaded post whose dome a point is under, or null.
    /// </summary>
    /// <param name="position">The point, in world space.</param>
    /// <returns>The post, or null.</returns>
    public static TradingPostSite Within(Vector3 position)
    {
        TradingPostLayout layout = TradingPostLayout.Load();
        foreach (TradingPostSite site in loaded)
        {
            if (site != null && site.view != null && site.view.IsValid()
                && Utils.DistanceXZ(site.transform.TransformPoint(TradingPostLayout.ToVector(layout.dome.centre)), position) <= layout.dome.radius)
            {
                return site;
            }
        }

        return null;
    }

    private void OnEnable()
    {
        loaded.Add(this);
    }

    private void OnDisable()
    {
        loaded.Remove(this);
    }

    private void Awake()
    {
        view = GetComponent<ZNetView>();
        if (view == null || !view.IsValid())
        {
            return;
        }

        TradingPostLayout layout = TradingPostLayout.Load();
        TradingPostDome.Attach(transform, TradingPostLayout.ToVector(layout.dome.centre), layout.dome.radius);
        Invoke(nameof(Pass), passDelays[0]);
    }

    private void Pass()
    {
        if (view == null || !view.IsValid() || ZNetScene.instance == null)
        {
            return;
        }

        List<ZNetView> near = NearbyObjects();
        if (view.IsOwner())
        {
            ClearVegetation(near);
            PutOutTrader();
        }

        Dye(near);
        pass++;
        if (pass < passDelays.Length)
        {
            Invoke(nameof(Pass), passDelays[pass] - passDelays[pass - 1]);
        }
    }

    // The loaded objects within the post's dome, found once per pass.
    private List<ZNetView> NearbyObjects()
    {
        TradingPostLayout layout = TradingPostLayout.Load();
        Vector3 centre = transform.TransformPoint(TradingPostLayout.ToVector(layout.dome.centre));
        float range = layout.dome.radius;
        return ZNetScene.instance.m_instances.Values
            .Where(other => other != null && other != view && Utils.DistanceXZ(other.transform.position, centre) <= range)
            .ToList();
    }

    // Removes the trees, logs, rocks, bushes and pickables of the world's vegetation that stand inside the post. Things
    // players built or dropped, creatures and the post's own pieces are left alone.
    private void ClearVegetation(List<ZNetView> near)
    {
        TradingPostLayout layout = TradingPostLayout.Load();
        Vector3 centre = transform.TransformPoint(TradingPostLayout.ToVector(layout.dome.centre));
        float range = layout.dome.radius - ClearMargin;
        List<ZNetView> found = near.Where(other => IsVegetation(other) && Utils.DistanceXZ(other.transform.position, centre) <= range).ToList();
        foreach (ZNetView other in found)
        {
            other.ClaimOwnership();
            ZNetScene.instance.Destroy(other.gameObject);
        }

        if (found.Count > 0)
        {
            Jotunn.Logger.LogInfo($"Trading posts: cleared {found.Count} trees, rocks and bushes off a post.");
        }
    }

    // Creates the trader's network data with its look already in it, so the equipment display finds hair, beard and
    // clothes when the trader first comes to life and never picks random ones. Posts built before the traders came get
    // theirs the same way.
    private void PutOutTrader()
    {
        ZDO site = view.GetZDO();
        if (site.GetBool(TradingPostKeys.HasTrader))
        {
            return;
        }

        GameObject prefab = ZNetScene.instance.GetPrefab(TradingPostTrader.PrefabName);
        if (prefab == null)
        {
            return;
        }

        TradingPostSpot spot = TradingPostLayout.Load().trader;
        Vector3 position = transform.TransformPoint(TradingPostLayout.ToVector(spot.position));
        Quaternion rotation = transform.rotation * Quaternion.Euler(0f, spot.yaw, 0f);
        int hash = prefab.name.GetStableHashCode();
        ZNetView template = prefab.GetComponent<ZNetView>();
        ZDO trader = ZDOMan.instance.CreateNewZDO(position, hash);
        trader.Persistent = template.m_persistent;
        trader.Type = template.m_type;
        trader.Distant = template.m_distant;
        trader.SetPrefab(hash);
        trader.SetRotation(rotation);
        trader.Set(TradingPostKeys.PostId, PostId);
        trader.Set(TradingPostKeys.Biome, site.GetInt(TradingPostKeys.Biome));
        Look.WriteTo(trader);
        site.Set(TradingPostKeys.HasTrader, true);
        Jotunn.Logger.LogInfo($"Trading posts: {Look.name} came to the post at {position.x:0} {position.z:0}.");
    }

    private void Dye(List<ZNetView> near)
    {
        string id = PostId;
        TraderLook look = Look;
        foreach (ZNetView piece in near)
        {
            ZDO zdo = piece.GetZDO();
            if (zdo != null && zdo.GetString(TradingPostKeys.PostId) == id)
            {
                TradingPostColours.Apply(piece, look);
            }
        }
    }

    private static bool IsVegetation(ZNetView other)
    {
        ZDO zdo = other.GetZDO();
        if (zdo == null || zdo.GetLong(ZDOVars.s_creator) != 0L || TradingPostKeys.IsPostPart(zdo))
        {
            return false;
        }

        GameObject target = other.gameObject;
        if (target.GetComponent<Character>() != null || target.GetComponent<Container>() != null || target.GetComponent<ItemDrop>() != null)
        {
            return false;
        }

        return target.GetComponent<TreeBase>() != null || target.GetComponent<TreeLog>() != null || target.GetComponent<MineRock>() != null
            || target.GetComponent<MineRock5>() != null || target.GetComponent<Pickable>() != null || target.GetComponent<Destructible>() != null;
    }
}
