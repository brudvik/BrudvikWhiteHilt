using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// The invisible marker at a trading post's origin, turned the way the post faces. It carries the dome on every
/// machine that loads it, is a location nothing may be built in, and levels the land behind the landing (both set up on
/// the prefab, <see cref="TradingPostRegistry"/>). Its owner clears trees, rocks and bushes off the post. The trader and
/// the post's other behaviour come here in later phases.
/// </summary>
public class TradingPostSite : MonoBehaviour
{
    // When to look for vegetation after the post comes to life: a zone generated as the player arrives gets its trees a
    // moment after the post's objects load, so the post looks again a little later.
    private static readonly float[] clearDelays = { 1f, 5f, 15f };

    // Vegetation is cleared this far inside the dome's edge, so the trees just outside it stay.
    private const float ClearMargin = 4f;

    private ZNetView view;
    private int clearStep;

    /// <summary>
    /// Gets the post's id, the same on all its pieces.
    /// </summary>
    public string PostId => view != null && view.IsValid() ? view.GetZDO().GetString(TradingPostKeys.PostId) : null;

    private void Awake()
    {
        view = GetComponent<ZNetView>();
        if (view == null || !view.IsValid())
        {
            return;
        }

        TradingPostLayout layout = TradingPostLayout.Load();
        TradingPostDome.Attach(transform, TradingPostLayout.ToVector(layout.dome.centre), layout.dome.radius);
        Invoke(nameof(ClearVegetation), clearDelays[0]);
    }

    // On the marker's owner: removes the trees, logs, rocks, bushes and pickables of the world's vegetation that stand
    // inside the post. Things players built or dropped, creatures and the post's own pieces are left alone.
    private void ClearVegetation()
    {
        if (view == null || !view.IsValid() || ZNetScene.instance == null)
        {
            return;
        }

        if (view.IsOwner())
        {
            TradingPostLayout layout = TradingPostLayout.Load();
            Vector3 centre = transform.TransformPoint(TradingPostLayout.ToVector(layout.dome.centre));
            float range = layout.dome.radius - ClearMargin;
            List<ZNetView> found = ZNetScene.instance.m_instances.Values
                .Where(other => other != null && other != view && IsVegetation(other) && Utils.DistanceXZ(other.transform.position, centre) <= range)
                .ToList();
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

        clearStep++;
        if (clearStep < clearDelays.Length)
        {
            Invoke(nameof(ClearVegetation), clearDelays[clearStep] - clearDelays[clearStep - 1]);
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
