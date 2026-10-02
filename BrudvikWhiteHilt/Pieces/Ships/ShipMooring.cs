using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships;

/// <summary>
/// A ship's mooring to a Mooring Post. The post is kept in the ship's ZDO; while it is set, the ship's owner holds the
/// ship where it lies, like the drift anchor (it still rocks on the waves), and every client draws the rope. Added to
/// every ship when it wakes.
/// </summary>
public class ShipMooring : MonoBehaviour
{
    private const string MoorRpc = "WhiteHiltMoor";
    private const string PostKey = "whitehilt_moor_post";
    private const float PostCheckInterval = 2f;
    private const int RopePoints = 16;
    private const float RopeWidth = 0.05f;

    private const RigidbodyConstraints MooredConstraints =
        RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationY;

    private static readonly List<ShipMooring> instances = new();
    private static readonly Color ropeColour = new(0.5f, 0.4f, 0.27f, 1f);
    private static Material ropeMaterial;

    private ZNetView nview;
    private Ship ship;
    private Rigidbody body;
    private LineRenderer rope;
    private bool frozen;
    private float nextPostCheck;

    /// <summary>The ship.</summary>
    public Ship Ship => ship;

    /// <summary>The post the ship is moored to, or none.</summary>
    public ZDOID Post => nview != null && nview.IsValid() ? nview.GetZDO().GetZDOID(PostKey) : ZDOID.None;

    /// <summary>True while the ship is moored.</summary>
    public bool IsMoored => !Post.IsNone();

    /// <summary>
    /// Adds the mooring to a ship. Safe to call more than once.
    /// </summary>
    /// <param name="ship">The ship.</param>
    public static void Attach(Ship ship)
    {
        if (ship.GetComponent<ShipMooring>() == null)
        {
            ship.gameObject.AddComponent<ShipMooring>();
        }
    }

    /// <summary>
    /// The loaded ship moored to a post.
    /// </summary>
    /// <param name="post">The post.</param>
    /// <returns>The ship's mooring, or null.</returns>
    public static ShipMooring MooredTo(ZDOID post)
    {
        return post.IsNone() ? null : instances.Find(mooring => mooring != null && mooring.Post == post);
    }

    /// <summary>
    /// The nearest loaded ship that is not moored.
    /// </summary>
    /// <param name="position">Where to look from.</param>
    /// <param name="range">How far, in metres.</param>
    /// <returns>The ship's mooring, or null.</returns>
    public static ShipMooring Nearest(Vector3 position, float range)
    {
        ShipMooring best = null;
        float bestDistance = range;
        foreach (ShipMooring mooring in instances)
        {
            if (mooring == null || mooring.ship == null || mooring.IsMoored)
            {
                continue;
            }

            float distance = Utils.DistanceXZ(position, mooring.transform.position);
            if (distance <= bestDistance)
            {
                best = mooring;
                bestDistance = distance;
            }
        }

        return best;
    }

    /// <summary>
    /// Moors the ship to a post, or casts it off with <see cref="ZDOID.None"/>. Sent to the ship's owner.
    /// </summary>
    /// <param name="post">The post.</param>
    public void Moor(ZDOID post)
    {
        if (nview != null && nview.IsValid())
        {
            nview.InvokeRPC(MoorRpc, post);
        }
    }

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        ship = GetComponent<Ship>();
        body = GetComponent<Rigidbody>();
        instances.Add(this);
        if (nview != null && nview.GetZDO() != null)
        {
            nview.Register<ZDOID>(MoorRpc, RPC_Moor);
        }
    }

    private void OnDestroy()
    {
        instances.Remove(this);
        if (rope != null)
        {
            Destroy(rope.gameObject);
        }
    }

    private void FixedUpdate()
    {
        if (nview == null || !nview.IsValid() || !nview.IsOwner() || body == null)
        {
            return;
        }

        bool moored = IsMoored;
        if (moored && Time.time >= nextPostCheck)
        {
            nextPostCheck = Time.time + PostCheckInterval;
            // A post that is gone from the world (not just out of reach) lets the ship go.
            ZDOID post = Post;
            if (ZNetScene.instance.FindInstance(post) == null && ZDOMan.instance.GetZDO(post) == null)
            {
                nview.GetZDO().Set(PostKey, ZDOID.None);
                moored = false;
            }
        }

        if (moored)
        {
            // Re-applied every update: raising the drift anchor puts back the constraints it found.
            if ((body.constraints & MooredConstraints) != MooredConstraints)
            {
                body.constraints |= MooredConstraints;
            }

            frozen = true;
        }
        else if (frozen)
        {
            frozen = false;
            if (!(GetComponent<WhiteHiltShip.WhiteHiltShipUpgrades>()?.IsAnchored ?? false))
            {
                body.constraints &= ~MooredConstraints;
            }
        }
    }

    private void LateUpdate()
    {
        GameObject post = IsMoored && ZNetScene.instance != null ? ZNetScene.instance.FindInstance(Post) : null;
        MooringPostComponent component = post != null ? post.GetComponent<MooringPostComponent>() : null;
        if (component == null)
        {
            if (rope != null && rope.gameObject.activeSelf)
            {
                rope.gameObject.SetActive(false);
            }

            return;
        }

        if (rope == null)
        {
            rope = CreateRope();
            if (rope == null)
            {
                return;
            }
        }

        if (!rope.gameObject.activeSelf)
        {
            rope.gameObject.SetActive(true);
        }

        Vector3 from = component.RopePoint;
        Vector3 to = ShipEnd(from);
        float sag = 0.15f + 0.03f * Vector3.Distance(from, to);
        for (int i = 0; i < RopePoints; i++)
        {
            float t = (float)i / (RopePoints - 1);
            rope.SetPosition(i, Vector3.Lerp(from, to, t) + Vector3.down * (sag * 4f * t * (1f - t)));
        }
    }

    // The bow or stern, whichever is nearer the post, at the top of the hull.
    private Vector3 ShipEnd(Vector3 post)
    {
        BoxCollider box = ship != null ? ship.m_floatCollider : null;
        if (box == null)
        {
            return transform.position;
        }

        Vector3 top = box.center + Vector3.up * (box.size.y / 2f);
        Vector3 bow = box.transform.TransformPoint(top + Vector3.forward * (box.size.z * 0.45f));
        Vector3 stern = box.transform.TransformPoint(top - Vector3.forward * (box.size.z * 0.45f));
        return (bow - post).sqrMagnitude < (stern - post).sqrMagnitude ? bow : stern;
    }

    private LineRenderer CreateRope()
    {
        if (ropeMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                return null;
            }

            ropeMaterial = new Material(shader) { name = "whitehilt_mooringrope", color = ropeColour };
        }

        GameObject line = new("WhiteHiltMooringRope");
        LineRenderer renderer = line.AddComponent<LineRenderer>();
        renderer.sharedMaterial = ropeMaterial;
        renderer.positionCount = RopePoints;
        renderer.startWidth = RopeWidth;
        renderer.endWidth = RopeWidth;
        renderer.useWorldSpace = true;
        renderer.numCapVertices = 2;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return renderer;
    }

    private void RPC_Moor(long sender, ZDOID post)
    {
        if (nview.IsOwner())
        {
            nview.GetZDO().Set(PostKey, post);
        }
    }
}
