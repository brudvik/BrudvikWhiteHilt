using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Fishing;

/// <summary>
/// On a Shore Net: measures how much of the net hangs in deep enough water, lets the net ride the waves, and draws
/// the rope to its Net Winch. The catch itself is taken by the winch.
/// </summary>
public class FishingNetComponent : MonoBehaviour, Hoverable
{
    private const int DepthSamples = 8;
    private const float DepthCacheSeconds = 10f;
    private const float RopeCheckSeconds = 2f;
    private const float GhostRopeCheckSeconds = 0.25f;
    private const float MaxBob = 1f;
    private const float BobSpeed = 2f;
    private const int RopeSegments = 16;
    private const int RopeSides = 6;
    private const float RopeRadius = 0.025f;

    private static readonly List<FishingNetComponent> all = new();
    private static Material ropeMaterial;

    private ZNetView nview;
    private Piece piece;
    private bool ghost;
    private Transform visual;
    private Transform net;
    private Transform hoverCollider;
    private GameObject rope;
    private NetWinchComponent ropeWinch;
    private Vector3 ropeFrom;
    private float nextRopeCheck;
    private float depthShare;
    private float depthCheckedAt = float.MinValue;

    /// <summary>
    /// Every placed net in the loaded world.
    /// </summary>
    public static IReadOnlyList<FishingNetComponent> All => all;

    private static float BaseWaterLevel => ZoneSystem.instance != null ? ZoneSystem.instance.m_waterLevel : 30f;

    /// <summary>
    /// The winch that takes this net's catch: the nearest one in range.
    /// </summary>
    /// <returns>The winch, or null.</returns>
    public NetWinchComponent Winch()
    {
        return NetWinchComponent.Nearest(transform.position);
    }

    /// <summary>
    /// How well the net catches, 0 to 1: the share of it in deep enough water, divided between crowded nets.
    /// </summary>
    /// <returns>The share.</returns>
    public float Catching()
    {
        return DepthShare() * CrowdShare();
    }

    /// <summary>
    /// Share of the net that hangs in water at least <see cref="FishingNetSettings.MinDepth"/> deep.
    /// </summary>
    /// <returns>The share, 0 to 1.</returns>
    public float DepthShare()
    {
        if (!ghost && Time.time - depthCheckedAt < DepthCacheSeconds)
        {
            return depthShare;
        }

        float halfLength = FishingNet.NetLength / 2f;
        float water = BaseWaterLevel;
        int deep = 0;
        for (int i = 0; i < DepthSamples; i++)
        {
            Vector3 point = transform.TransformPoint(new Vector3(Mathf.Lerp(-halfLength, halfLength, (i + 0.5f) / DepthSamples), 0f, 0f));
            if (water - GroundHeight(point) >= FishingNetSettings.MinDepth.Value)
            {
                deep++;
            }
        }

        depthShare = (float)deep / DepthSamples;
        depthCheckedAt = Time.time;
        return depthShare;
    }

    /// <summary>
    /// Why the net cannot be placed where its ghost is, or null when it can.
    /// </summary>
    /// <returns>A message token with its values filled in, or null.</returns>
    public string PlacementProblem()
    {
        NetWinchComponent winch = Winch();
        if (winch == null)
        {
            return string.Format(Localization.instance.Localize("$msg_whitehilt_net_nowinch"), Translations.Number(FishingNetSettings.Range.Value));
        }

        if (!winch.HasRoom())
        {
            return Localization.instance.Localize("$msg_whitehilt_net_winchfull");
        }

        return DepthShare() <= 0f ? Localization.instance.Localize("$msg_whitehilt_net_shallow") : null;
    }

    /// <inheritdoc/>
    public string GetHoverText()
    {
        string name = GetHoverName();
        NetWinchComponent winch = Winch();
        string status;
        if (winch == null)
        {
            status = string.Format(Localization.instance.Localize("$whitehilt_net_nowinch"), Translations.Number(FishingNetSettings.Range.Value));
        }
        else if (!winch.Nets().Contains(this))
        {
            status = Localization.instance.Localize("$whitehilt_net_spare");
        }
        else if (DepthShare() <= 0f)
        {
            status = Localization.instance.Localize("$whitehilt_net_shallow");
        }
        else
        {
            status = string.Format(Localization.instance.Localize("$whitehilt_net_catching"), Translations.Percent(Catching()));
        }

        return $"{name}\n<color=#a0a0a0>{status}</color>";
    }

    /// <inheritdoc/>
    public string GetHoverName()
    {
        return Localization.instance.Localize(piece != null ? piece.m_name : Translations.Token(FishingNet.PrefabName));
    }

    /// <inheritdoc/>
    public float GetHoverOffset()
    {
        return 0f;
    }

    private static float GroundHeight(Vector3 point)
    {
        if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(point, out float height))
        {
            return height;
        }

        return WorldGenerator.instance != null ? WorldGenerator.instance.GetHeight(point.x, point.z) : BaseWaterLevel;
    }

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        piece = GetComponent<Piece>();
        ghost = nview == null || nview.GetZDO() == null;
        visual = transform.Find(FishingNet.VisualName);
        net = visual != null ? visual.Find(FishingNet.NetName) : null;
        hoverCollider = transform.Find(FishingNet.ColliderName);
        if (!ghost)
        {
            all.Add(this);
        }

        PlaceAtWaterLevel();
    }

    private void OnDestroy()
    {
        all.Remove(this);
    }

    // The share of a catch this net gets when other nets lie near: one over the number of nets within the radius, so
    // packing nets together does not multiply the catch.
    private float CrowdShare()
    {
        float radius = FishingNetSettings.CrowdRadius.Value;
        if (radius <= 0f)
        {
            return 1f;
        }

        int crowd = 1;
        foreach (FishingNetComponent other in all)
        {
            if (other != null && other != this && (other.transform.position - transform.position).sqrMagnitude < radius * radius)
            {
                crowd++;
            }
        }

        return 1f / crowd;
    }

    // The root stands above the water (water pieces are placed so), so the parts are put at the water line.
    private void PlaceAtWaterLevel()
    {
        float water = BaseWaterLevel;
        foreach (Transform part in new[] { visual, hoverCollider })
        {
            if (part != null)
            {
                Vector3 position = part.position;
                part.position = new Vector3(position.x, water, position.z);
            }
        }
    }

    // Keeps a ghost on the water's surface while it is being placed, bobs the net on the waves and redraws the rope to
    // its winch now and then.
    private void LateUpdate()
    {
        if (ghost)
        {
            PlaceAtWaterLevel();
        }

        if (VisualHelper.IsHeadless || visual == null)
        {
            return;
        }

        Bob();
        if (Time.time >= nextRopeCheck)
        {
            nextRopeCheck = Time.time + (ghost ? GhostRopeCheckSeconds : RopeCheckSeconds);
            UpdateRope();
        }
    }

    private void Bob()
    {
        if (net == null)
        {
            return;
        }

        float water = BaseWaterLevel;
        float level = Floating.GetLiquidLevel(net.position);
        float target = level < water - 10f ? water : Mathf.Clamp(level, water - MaxBob, water + MaxBob);
        Vector3 position = net.position;
        position.y = Mathf.Lerp(position.y, target, 1f - Mathf.Exp(-BobSpeed * Time.deltaTime));
        net.position = position;
    }

    // Rebuilds the rope to the winch only when the winch or the net has moved, as building the mesh is not free.
    private void UpdateRope()
    {
        NetWinchComponent winch = Winch();
        Vector3 from = RopeStart(winch);
        if (winch == ropeWinch && (from - ropeFrom).sqrMagnitude < 0.01f && (rope != null) == (winch != null))
        {
            return;
        }

        ropeWinch = winch;
        ropeFrom = from;
        if (rope != null)
        {
            Destroy(rope.GetComponent<MeshFilter>().sharedMesh);
            Destroy(rope);
            rope = null;
        }

        if (winch != null)
        {
            rope = CreateRope(from, winch.RopePoint);
        }
    }

    // The top of the stake on the side facing the winch.
    private Vector3 RopeStart(NetWinchComponent winch)
    {
        float end = FishingNet.NetLength / 2f + FishingNet.StakeOffset;
        if (winch != null && transform.InverseTransformPoint(winch.transform.position).x < 0f)
        {
            end = -end;
        }

        Vector3 point = transform.TransformPoint(new Vector3(end, 0f, 0f));
        point.y = BaseWaterLevel + FishingNet.StakeTop - 0.1f;
        return point;
    }

    private GameObject CreateRope(Vector3 from, Vector3 to)
    {
        Material material = RopeMaterial();
        if (material == null)
        {
            return null;
        }

        GameObject created = new("rope") { layer = gameObject.layer };
        created.transform.SetParent(transform, false);
        created.AddComponent<MeshFilter>().sharedMesh = BuildRopeMesh(from, to);
        MeshRenderer renderer = created.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        return created;
    }

    // A rope as a tube along a hanging curve between two points: a parabola that sags more the longer it is, with a
    // ring of vertices at each step.
    private Mesh BuildRopeMesh(Vector3 from, Vector3 to)
    {
        float sag = 0.15f + 0.03f * Vector3.Distance(from, to);
        Vector3[] centres = new Vector3[RopeSegments + 1];
        for (int i = 0; i <= RopeSegments; i++)
        {
            float t = (float)i / RopeSegments;
            centres[i] = Vector3.Lerp(from, to, t) + Vector3.down * (sag * 4f * t * (1f - t));
        }

        List<Vector3> vertices = new();
        List<Vector2> uvs = new();
        List<int> triangles = new();
        for (int i = 0; i <= RopeSegments; i++)
        {
            Vector3 ahead = (centres[Mathf.Min(i + 1, RopeSegments)] - centres[Mathf.Max(i - 1, 0)]).normalized;
            Vector3 side = Vector3.Cross(ahead, Vector3.up);
            side = side.sqrMagnitude < 1e-6f ? Vector3.right : side.normalized;
            Vector3 up = Vector3.Cross(side, ahead);
            for (int k = 0; k < RopeSides; k++)
            {
                float angle = 2f * Mathf.PI * k / RopeSides;
                vertices.Add(transform.InverseTransformPoint(centres[i] + (side * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * RopeRadius));
                uvs.Add(new Vector2((float)k / RopeSides, (float)i / RopeSegments));
            }
        }

        for (int i = 0; i < RopeSegments; i++)
        {
            for (int k = 0; k < RopeSides; k++)
            {
                int a = i * RopeSides + k;
                int b = i * RopeSides + (k + 1) % RopeSides;
                int c = a + RopeSides;
                int d = b + RopeSides;
                triangles.AddRange(new[] { a, c, b, b, c, d });
            }
        }

        Mesh mesh = new() { name = "whitehilt_netrope" };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // A plain rope-coloured material on the vanilla wood shader, made once and shared by all ropes.
    private static Material RopeMaterial()
    {
        if (ropeMaterial != null)
        {
            return ropeMaterial;
        }

        Renderer template = FishingNet.WoodTemplate();
        if (template == null)
        {
            return null;
        }

        Color32[] pixels = new Color32[16];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color32(128, 104, 72, 255);
        }

        ropeMaterial = VisualHelper.CreateTexturedMaterial(template.sharedMaterial, VisualHelper.CreateTexture("whitehilt_netrope", 4, 4, pixels), "whitehilt_netrope");
        return ropeMaterial;
    }
}
