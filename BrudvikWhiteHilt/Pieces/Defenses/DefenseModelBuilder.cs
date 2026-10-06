using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace BrudvikWhiteHilt.Pieces.Defenses;

/// <summary>
/// Turns a piece of the defence layout into combined meshes. Every group (the piece itself, each gate leaf) gets one
/// mesh per material, so a piece of fifty logs and stakes costs a handful of draw calls. Small details are left out of
/// the far level of detail and cast no shadows.
/// </summary>
public static class DefenseModelBuilder
{
    // Screen heights where the details disappear and where the whole piece is culled.
    private const float DetailScreenHeight = 0.15f;
    private const float CullScreenHeight = 0.015f;
    private const string TorchMesh = "piece_walltorch";

    private static readonly Dictionary<(Material, Color), Material> tinted = new();

    /// <summary>
    /// Flattens a piece, including its nested templates, into mesh placements in the piece's space.
    /// </summary>
    /// <param name="layout">The layout.</param>
    /// <param name="piece">The piece.</param>
    /// <returns>The placements.</returns>
    public static List<Placement> Flatten(DefenseLayout layout, DefensePieceData piece)
    {
        List<Placement> placements = new();
        Flatten(layout, piece, Matrix4x4.identity, placements, 0);
        return placements;
    }

    /// <summary>
    /// Builds the combined meshes, the level of detail and the torch flames.
    /// </summary>
    /// <param name="visualRoot">Object for the piece's own meshes, at the piece origin.</param>
    /// <param name="groups">Moving groups by name, direct children of the piece root.</param>
    /// <param name="placements">Mesh placements from <see cref="Flatten"/>.</param>
    /// <param name="levelOfDetail">Add a LOD group. Off for a worn item, which the character's own LOD group takes in.</param>
    public static void Build(Transform visualRoot, IDictionary<string, Transform> groups, List<Placement> placements, bool levelOfDetail = true)
    {
        List<Renderer> details = new();
        List<Renderer> mains = new();
        foreach (IGrouping<string, Placement> group in placements.GroupBy(placement => placement.Group ?? string.Empty))
        {
            Transform parent = group.Key.Length == 0 ? visualRoot : groups[group.Key];
            Matrix4x4 toParent = parent == visualRoot ? Matrix4x4.identity : Matrix4x4.TRS(parent.localPosition, parent.localRotation, parent.localScale).inverse;
            BuildGroup(parent, toParent, group.ToList(), details, mains);
        }

        if (levelOfDetail)
        {
            LODGroup lodGroup = visualRoot.gameObject.AddComponent<LODGroup>();
            lodGroup.SetLODs(new[]
            {
                new LOD(DetailScreenHeight, mains.Concat(details).ToArray()),
                new LOD(CullScreenHeight, mains.ToArray())
            });
            lodGroup.RecalculateBounds();
        }

        AddTorchFlames(visualRoot, placements.Where(placement => placement.Mesh == TorchMesh));
    }

    // Combines the placements of one group into as few meshes as possible: one per material, and apart for small
    // details, which cast no shadow. Fewer renderers means fewer draw calls, which matters for walls built of many
    // vanilla parts.
    private static void BuildGroup(Transform parent, Matrix4x4 toParent, List<Placement> placements, List<Renderer> details, List<Renderer> mains)
    {
        Dictionary<(Material, bool), List<CombineInstance>> batches = new();
        foreach (Placement placement in placements)
        {
            foreach (VanillaMeshLibrary.MeshSource source in VanillaMeshLibrary.Get(placement.Mesh))
            {
                Material material = placement.Texture != null ? VanillaMeshLibrary.MaterialWithTexture(placement.Texture) : source.Material;
                if (placement.Tint.HasValue)
                {
                    material = Tinted(material, placement.Tint.Value);
                }

                (Material, bool) key = (material, placement.Detail);
                if (!batches.TryGetValue(key, out List<CombineInstance> batch))
                {
                    batch = new List<CombineInstance>();
                    batches[key] = batch;
                }

                batch.Add(new CombineInstance
                {
                    mesh = source.Mesh,
                    subMeshIndex = source.SubMesh,
                    transform = toParent * placement.Matrix * source.Matrix
                });
            }
        }

        foreach (KeyValuePair<(Material Material, bool Detail), List<CombineInstance>> batch in batches)
        {
            Mesh mesh = new() { name = $"{parent.name}_{batch.Key.Material.name}", indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(batch.Value.ToArray(), mergeSubMeshes: true, useMatrices: true);
            mesh.RecalculateBounds();

            GameObject model = new(mesh.name) { layer = parent.gameObject.layer };
            model.transform.SetParent(parent, false);
            model.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = model.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = batch.Key.Material;
            renderer.shadowCastingMode = batch.Key.Detail ? ShadowCastingMode.Off : ShadowCastingMode.On;
            (batch.Key.Detail ? details : mains).Add(renderer);
        }
    }

    // The vanilla wall torch's flame, light and crackle, without its warmth, lit at every torch mesh.
    private static void AddTorchFlames(Transform visualRoot, IEnumerable<Placement> torches)
    {
        Transform flame = PrefabManager.Instance.GetPrefab(TorchMesh)?.transform.Find("_enabled");
        if (flame == null)
        {
            return;
        }

        Matrix4x4 flameOffset = Matrix4x4.TRS(flame.localPosition, flame.localRotation, flame.localScale);
        foreach (Placement torch in torches)
        {
            Matrix4x4 matrix = torch.Matrix * flameOffset;
            GameObject copy = new("torch_flame");
            copy.transform.SetParent(visualRoot, false);
            copy.transform.localPosition = matrix.GetColumn(3);
            copy.transform.localRotation = matrix.rotation;
            foreach (Transform child in flame)
            {
                if (child.name == "FireWarmth")
                {
                    continue;
                }

                GameObject effect = Object.Instantiate(child.gameObject, copy.transform);
                effect.name = child.name;
                effect.transform.localPosition = child.localPosition;
                effect.transform.localRotation = child.localRotation;
                effect.transform.localScale = child.localScale;
            }
        }
    }

    private static Material Tinted(Material source, Color tint)
    {
        if (!tinted.TryGetValue((source, tint), out Material material))
        {
            material = new Material(source) { name = $"{source.name}_tinted" };
            if (material.HasProperty("_Color"))
            {
                material.color = source.color * tint;
            }

            tinted[(source, tint)] = material;
        }

        return material;
    }

    // Turns a layout piece into a flat list of placements, following pieces made of other pieces. The depth limit stops
    // a layout that refers to itself from recursing forever.
    private static void Flatten(DefenseLayout layout, DefensePieceData piece, Matrix4x4 parent, List<Placement> placements, int depth)
    {
        foreach (DefensePartData part in piece.parts)
        {
            Matrix4x4 matrix = parent * Matrix4x4.TRS(ToVector(part.position, Vector3.zero), Quaternion.Euler(ToVector(part.rotation, Vector3.zero)), ToVector(part.scale, Vector3.one));
            if (!string.IsNullOrEmpty(part.piece))
            {
                if (depth < 4)
                {
                    Flatten(layout, layout.Get(part.piece), matrix, placements, depth + 1);
                }

                continue;
            }

            Color? tint = part.tint != null && part.tint.Length >= 3 ? new Color(part.tint[0], part.tint[1], part.tint[2]) : null;
            placements.Add(new Placement(part.mesh, matrix, tint, string.IsNullOrEmpty(part.texture) ? null : part.texture,
                string.IsNullOrEmpty(part.group) ? null : part.group, part.detail));
        }
    }

    /// <summary>
    /// Reads a float triple from the layout.
    /// </summary>
    /// <param name="values">The floats, or null.</param>
    /// <param name="fallback">Value when there are none.</param>
    /// <returns>The vector.</returns>
    public static Vector3 ToVector(float[] values, Vector3 fallback)
    {
        return values != null && values.Length >= 3 ? new Vector3(values[0], values[1], values[2]) : fallback;
    }

    /// <summary>
    /// One vanilla mesh key placed in a piece.
    /// </summary>
    public readonly struct Placement
    {
        /// <summary>Mesh key.</summary>
        public readonly string Mesh;

        /// <summary>Placement in the piece's space.</summary>
        public readonly Matrix4x4 Matrix;

        /// <summary>Colour multiplied into the material, or null.</summary>
        public readonly Color? Tint;

        /// <summary>Texture name of a vanilla material to use instead, or null.</summary>
        public readonly string Texture;

        /// <summary>Moving group, or null.</summary>
        public readonly string Group;

        /// <summary>Dropped at a distance.</summary>
        public readonly bool Detail;

        /// <summary>
        /// Creates a placement.
        /// </summary>
        /// <param name="mesh">Mesh key.</param>
        /// <param name="matrix">Placement in the piece's space.</param>
        /// <param name="tint">Colour multiplied into the material, or null.</param>
        /// <param name="texture">Texture name of a vanilla material to use instead, or null.</param>
        /// <param name="group">Moving group, or null.</param>
        /// <param name="detail">Dropped at a distance.</param>
        public Placement(string mesh, Matrix4x4 matrix, Color? tint, string texture, string group, bool detail)
        {
            Mesh = mesh;
            Matrix = matrix;
            Tint = tint;
            Texture = texture;
            Group = group;
            Detail = detail;
        }
    }
}
