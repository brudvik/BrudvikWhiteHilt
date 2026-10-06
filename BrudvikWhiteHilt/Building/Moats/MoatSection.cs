using Jotunn.Entities;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Building.Moats;

/// <summary>
/// A dug stretch of moat in the world: an invisible network object that remembers where the ditch runs, so creatures
/// down in it can be slowed and made to slide on every machine, and that fills a wet moat with water.
/// </summary>
public class MoatSection : MonoBehaviour
{
    /// <summary>Prefab name of the section object.</summary>
    public const string PrefabName = "WhiteHiltMoatSection";

    private const int Version = 1;
    private const float NoWater = -10000f;
    private const float InsideMargin = 0.3f;
    private const float BelowEdge = 0.6f;
    private const float WaterAbove = 1f;
    private const float WaterBelow = 0.5f;

    private static readonly int DataKey = "whitehilt_moat_section".GetStableHashCode();
    private static readonly List<MoatSection> sections = new();

    private static Material waterMaterial;
    private static int waterLayer = -1;
    private static int surfaceLayer = -1;

    private ZNetView nview;
    private bool loaded;
    private Vector2[] points;
    private Vector2[] outward;
    private float[] factor;
    private float[] top;
    private float width;
    private float waterLevel = NoWater;
    private float bottom;
    private Rect area;
    private Mesh waterMesh;
    private MeshRenderer waterRenderer;

    /// <summary>
    /// Creates the section prefab. Call when the vanilla prefabs are available.
    /// </summary>
    public static void CreatePrefab()
    {
        GameObject prefab = PrefabManager.Instance.CreateEmptyPrefab(PrefabName);
        foreach (Component part in prefab.GetComponents<Component>())
        {
            if (part is MeshRenderer || part is MeshFilter || part is Collider)
            {
                DestroyImmediate(part);
            }
        }

        ZNetView view = prefab.GetComponent<ZNetView>();
        view.m_persistent = true;
        prefab.AddComponent<MoatSection>();
        PrefabManager.Instance.AddPrefab(new CustomPrefab(prefab, false));
    }

    /// <summary>
    /// Places a section in the world.
    /// </summary>
    /// <param name="run">The run the section is part of.</param>
    /// <param name="first">First sample of the section.</param>
    /// <param name="last">Last sample of the section; one more is kept so the water reaches the next section.</param>
    /// <param name="top">Height of the ground at the edges of the ditch, per sample from first.</param>
    /// <param name="spec">The moat's sizes.</param>
    /// <param name="waterLevel">Height of the water, or null for a dry ditch.</param>
    /// <param name="bottom">Height of a wet moat's bottom.</param>
    public static void Spawn(MoatRun run, int first, int last, float[] top, MoatSpec spec, float? waterLevel, float bottom)
    {
        GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(PrefabName) : null;
        if (prefab == null)
        {
            return;
        }

        int end = run.Closed || last < run.Count - 1 ? last + 1 : last;
        ZPackage package = new();
        package.Write(Version);
        package.Write(spec.Width);
        package.Write(waterLevel ?? NoWater);
        package.Write(bottom);
        package.Write(end - first + 1);
        for (int i = first; i <= end; i++)
        {
            int index = run.Wrap(i);
            package.Write(run.Points[index].x);
            package.Write(run.Points[index].y);
            package.Write(run.Outward[index].x);
            package.Write(run.Outward[index].y);
            package.Write(run.Factor[index]);
            package.Write(top[Mathf.Min(i - first, top.Length - 1)]);
        }

        Vector2 at = run.Points[run.Wrap(first)];
        GameObject section = Instantiate(prefab, new Vector3(at.x, top[0], at.y), Quaternion.identity);
        ZNetView view = section.GetComponent<ZNetView>();
        if (view != null && view.IsValid())
        {
            view.GetZDO().Set(DataKey, package.GetArray());
        }
    }

    /// <summary>
    /// True if a point lies down in a dug ditch.
    /// </summary>
    /// <param name="position">The point.</param>
    /// <returns>True if so.</returns>
    public static bool IsInDitch(Vector3 position)
    {
        Vector2 at = new(position.x, position.z);
        foreach (MoatSection section in sections)
        {
            if (section != null && section.area.Contains(at) && section.Contains(at, position.y))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Removes the sections that reach into a rectangle, e.g. when the ground there is put back.
    /// </summary>
    /// <param name="min">Lowest corner (x, z).</param>
    /// <param name="max">Highest corner (x, z).</param>
    public static void RemoveInside(Vector2 min, Vector2 max)
    {
        Rect rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        foreach (MoatSection section in sections.ToArray())
        {
            if (section == null || section.nview == null || !section.nview.IsValid() || !section.area.Overlaps(rect))
            {
                continue;
            }

            foreach (Vector2 point in section.points)
            {
                if (rect.Contains(point))
                {
                    section.nview.ClaimOwnership();
                    ZNetScene.instance.Destroy(section.gameObject);
                    break;
                }
            }
        }
    }

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
    }

    // Reads the section's shape from its network object once it has arrived, then shows the water if it is a wet moat.
    // Runs until loaded, as the data may arrive after the object.
    private void Update()
    {
        if (loaded)
        {
            enabled = false;
            return;
        }

        if (nview == null || !nview.IsValid())
        {
            return;
        }

        byte[] data = nview.GetZDO().GetByteArray(DataKey);
        if (data == null || data.Length == 0)
        {
            return;
        }

        try
        {
            Load(new ZPackage(data));
            loaded = true;
            sections.Add(this);
            if (waterLevel > NoWater + 1f && !Helpers.VisualHelper.IsHeadless)
            {
                BuildWater();
            }
        }
        catch (System.Exception ex)
        {
            Jotunn.Logger.LogWarning($"Could not read a moat section: {ex.Message}");
            loaded = true;
        }
    }

    private void OnDestroy()
    {
        sections.Remove(this);
        if (waterMesh != null)
        {
            Destroy(waterMesh);
        }

        // The water volumes give the renderer its own material copy for their wave time.
        if (waterRenderer != null && waterRenderer.sharedMaterial != null && waterRenderer.sharedMaterial != waterMaterial)
        {
            Destroy(waterRenderer.sharedMaterial);
        }
    }

    // Reads the section's shape: width, water level, bottom and every sample with its outward direction, depth factor
    // and bank height.
    private void Load(ZPackage package)
    {
        package.ReadInt();
        width = package.ReadSingle();
        waterLevel = package.ReadSingle();
        bottom = package.ReadSingle();
        int count = package.ReadInt();
        points = new Vector2[count];
        outward = new Vector2[count];
        factor = new float[count];
        top = new float[count];
        Vector2 min = new(float.MaxValue, float.MaxValue);
        Vector2 max = new(float.MinValue, float.MinValue);
        for (int i = 0; i < count; i++)
        {
            points[i] = new Vector2(package.ReadSingle(), package.ReadSingle());
            outward[i] = new Vector2(package.ReadSingle(), package.ReadSingle());
            factor[i] = package.ReadSingle();
            top[i] = package.ReadSingle();
            min = Vector2.Min(min, points[i]);
            max = Vector2.Max(max, points[i]);
        }

        float reach = width / 2f + 1f;
        area = Rect.MinMaxRect(min.x - reach, min.y - reach, max.x + reach, max.y + reach);
    }

    // Whether a point lies in the ditch: within the half-width of its nearest sample and below the bank. Causeways (a
    // factor of 0) are not ditch.
    private bool Contains(Vector2 at, float height)
    {
        int nearest = -1;
        float best = float.MaxValue;
        for (int i = 0; i < points.Length; i++)
        {
            float distance = (points[i] - at).sqrMagnitude;
            if (distance < best)
            {
                best = distance;
                nearest = i;
            }
        }

        if (nearest < 0 || factor[nearest] < 0.5f || height > top[nearest] - BelowEdge)
        {
            return false;
        }

        return Mathf.Abs(Vector2.Dot(at - points[nearest], outward[nearest])) <= width / 2f - InsideMargin;
    }

    // The water is a flat strip over the ditch at the water level, and a trigger box per metre with the game's own water
    // volume, so swimming, floating and wet feet work as at sea. Where the strip reaches under the banks, the ground hides it.
    private void BuildWater()
    {
        if (!FindWaterTemplate())
        {
            return;
        }

        GameObject surface = new("water_surface") { layer = surfaceLayer };
        surface.transform.SetParent(transform, false);
        surface.transform.position = new Vector3(transform.position.x, waterLevel, transform.position.z);
        List<Vector3> vertices = new();
        List<Vector2> uvs = new();
        List<int> triangles = new();
        float half = width / 2f + 0.3f;
        for (int i = 0; i + 1 < points.Length; i++)
        {
            if (factor[i] < 0.5f || factor[i + 1] < 0.5f)
            {
                continue;
            }

            int index = vertices.Count;
            foreach (int j in new[] { i, i + 1 })
            {
                foreach (float side in new[] { -half, half })
                {
                    Vector2 point = points[j] + outward[j] * side;
                    vertices.Add(surface.transform.InverseTransformPoint(new Vector3(point.x, waterLevel, point.y)));
                    uvs.Add(point / 8f);
                }
            }

            AddUpward(triangles, vertices, index, index + 1, index + 2);
            AddUpward(triangles, vertices, index + 1, index + 3, index + 2);
            AddTrigger(i);
        }

        if (triangles.Count == 0)
        {
            Destroy(surface);
            return;
        }

        waterMesh = new Mesh { name = "moat_water" };
        waterMesh.SetVertices(vertices);
        waterMesh.SetUVs(0, uvs);
        waterMesh.SetTriangles(triangles, 0);
        waterMesh.RecalculateNormals();
        waterMesh.RecalculateBounds();
        surface.AddComponent<MeshFilter>().sharedMesh = waterMesh;
        MeshRenderer renderer = surface.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = waterMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        waterRenderer = renderer;

        foreach (WaterVolume volume in GetComponentsInChildren<WaterVolume>())
        {
            volume.m_waterSurface = renderer;
        }
    }

    // Unity draws a triangle's front where its corners run clockwise seen from above; the water is seen from above.
    private static void AddUpward(List<int> triangles, List<Vector3> vertices, int a, int b, int c)
    {
        if (Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]).y < 0f)
        {
            (b, c) = (c, b);
        }

        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);
    }

    // A box of water along one stretch: a trigger with the game's own WaterVolume, so swimming, floating and wet feet
    // work in the moat as in any lake.
    private void AddTrigger(int i)
    {
        Vector2 a = points[i];
        Vector2 b = points[i + 1];
        Vector2 middle = (a + b) / 2f;
        Vector2 along = b - a;
        float length = along.magnitude;
        if (length < 0.01f)
        {
            return;
        }

        float low = bottom - WaterBelow;
        float high = waterLevel + WaterAbove;
        GameObject trigger = new("water_volume") { layer = waterLayer };
        trigger.SetActive(false);
        trigger.transform.SetParent(transform, false);
        trigger.transform.position = new Vector3(middle.x, waterLevel, middle.y);
        trigger.transform.rotation = Quaternion.LookRotation(new Vector3(along.x, 0f, along.y));
        BoxCollider box = trigger.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(width, high - low, length + 0.1f);
        box.center = new Vector3(0f, (high + low) / 2f - waterLevel, 0f);
        WaterVolume volume = trigger.AddComponent<WaterVolume>();
        volume.m_forceDepth = 0f;
        volume.m_useGlobalWind = false;
        volume.m_heightmap = null;
        trigger.SetActive(true);
    }

    // Borrows the material and layers of the game's water from the zone prefab, so the moat's water looks and behaves
    // like the sea. Without it moats stay dry.
    private static bool FindWaterTemplate()
    {
        if (waterMaterial != null)
        {
            return true;
        }

        WaterVolume template = ZoneSystem.instance != null && ZoneSystem.instance.m_zonePrefab != null
            ? ZoneSystem.instance.m_zonePrefab.GetComponentInChildren<WaterVolume>(true)
            : null;
        if (template == null || template.m_waterSurface == null)
        {
            Jotunn.Logger.LogWarning("The game's water was not found; moats stay dry.");
            return false;
        }

        waterMaterial = template.m_waterSurface.sharedMaterial;
        waterLayer = template.gameObject.layer;
        surfaceLayer = template.m_waterSurface.gameObject.layer;
        return waterMaterial != null;
    }
}
