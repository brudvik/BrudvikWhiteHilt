using BrudvikWhiteHilt.Helpers;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Defenses;

/// <summary>
/// Finds the vanilla meshes and materials the defence layout refers to. The keys match AssetSource/Preview/export_vanilla.py.
/// A few keys are models from the White Hilt asset bundle instead, shown with the workbench's material.
/// </summary>
public static class VanillaMeshLibrary
{
    private const string BundleMaterialPrefab = "piece_workbench";
    private const string BundleMaterialChild = "New/high";

    // Keys that are one child of a prefab; any other key is a whole prefab of that name.
    private static readonly Dictionary<string, (string Prefab, string Child)> children = new()
    {
        ["stake"] = ("stake_wall", "New/high/Stake (3)"),
        ["stake_support"] = ("stake_wall", "New/high/top_support"),
        ["rock"] = ("fire_pit", "New/Rock_4 (1)"),
        ["shield_wood"] = ("ShieldWood", null),
        ["trophy_deer"] = ("TrophyDeer", null),
        ["trophy_skull"] = ("TrophySkeleton", null),
        ["gem_ruby"] = ("Ruby", null),
        ["gem_amber"] = ("Amber", null)
    };

    // Keys that are models in the White Hilt asset bundle.
    private static readonly HashSet<string> bundleModels = new() { "cartodesk", "sextant", "mapscroll", "seachart", "amulet", "shipanchor", "chains", "fishnet", "rushlight", "paintbucket" };

    /// <summary>The texture key in the layout for the slate of the White Hilt slate roofs, as on the stone tower's roof.</summary>
    public const string SlateTexture = "roof_slate_albedo";

    /// <summary>
    /// The key for a plain box in the stone of the vanilla stone wall: 12 triangles where the vanilla block has some 430,
    /// for the thin stones (string courses, coping, corbels) whose bevels nobody sees. Matches build_stone_defenses.py.
    /// </summary>
    public const string StoneBox = "stonebox";
    private const string StoneBoxMaterialFrom = "stone_wall_1x1";

    // The part of the vanilla stone texture the block's flat front shows, which every face of the box shows too.
    private static readonly Rect stoneBoxUv = Rect.MinMaxRect(0.21f, 0.03f, 0.345f, 0.17f);

    // Prefabs whose materials can be borrowed by texture name.
    private static readonly string[] materialSources = { "iron_grate", "blackmarble_1x1", "Piece_grausten_floor_2x2", "wood_beam", "stone_wall_1x1", "VikingShip" };

    private static readonly Dictionary<string, List<MeshSource>> meshes = new();
    private static readonly Dictionary<string, Material> textureMaterials = new();

    /// <summary>
    /// Returns the meshes of a key, each with its material and its placement in the key's space.
    /// </summary>
    /// <param name="key">Mesh key from the layout.</param>
    /// <returns>The meshes.</returns>
    public static IReadOnlyList<MeshSource> Get(string key)
    {
        if (meshes.TryGetValue(key, out List<MeshSource> cached))
        {
            return cached;
        }

        if (key == StoneBox)
        {
            meshes[key] = new List<MeshSource> { new(StoneBoxMesh(), 0, Get(StoneBoxMaterialFrom)[0].Material, Matrix4x4.identity) };
            return meshes[key];
        }

        if (bundleModels.Contains(key))
        {
            meshes[key] = new List<MeshSource> { new(ForagingAssets.LoadMesh(key), 0, BundleMaterial(key), Matrix4x4.identity) };
            return meshes[key];
        }

        (string prefabName, string childPath) = children.TryGetValue(key, out var child) ? child : (key, null);
        GameObject prefab = PrefabManager.Instance.GetPrefab(prefabName) ?? throw new InvalidOperationException($"Vanilla prefab '{prefabName}' not found.");
        Transform root = prefab.transform;
        IEnumerable<MeshRenderer> renderers;
        if (childPath != null)
        {
            root = root.Find(childPath) ?? throw new InvalidOperationException($"{prefabName}/{childPath} not found.");
            renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        }
        else
        {
            Transform prefabRoot = root;
            renderers = root.GetComponentsInChildren<MeshRenderer>(true).Where(renderer => renderer.enabled && IsActiveBelow(renderer.transform, prefabRoot));
        }

        List<MeshSource> sources = new();
        foreach (MeshRenderer renderer in renderers)
        {
            Mesh mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
            if (mesh == null)
            {
                continue;
            }

            Matrix4x4 matrix = root.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
            Material[] materials = renderer.sharedMaterials;
            for (int subMesh = 0; subMesh < Math.Min(mesh.subMeshCount, materials.Length); subMesh++)
            {
                Material material = materials[subMesh];
                if (material != null && !material.name.ToLowerInvariant().Contains("snow"))
                {
                    sources.Add(new MeshSource(mesh, subMesh, material, matrix));
                }
            }
        }

        if (sources.Count == 0)
        {
            throw new InvalidOperationException($"'{key}' has no meshes.");
        }

        meshes[key] = sources;
        return sources;
    }

    /// <summary>
    /// Returns a vanilla material by the name of its main texture, e.g. the iron of the iron grate, or the White Hilt
    /// slate of the slate roofs for <see cref="SlateTexture"/>.
    /// </summary>
    /// <param name="texture">Texture name.</param>
    /// <returns>The material.</returns>
    public static Material MaterialWithTexture(string texture)
    {
        if (textureMaterials.TryGetValue(texture, out Material cached))
        {
            return cached;
        }

        if (texture == SlateTexture)
        {
            textureMaterials[texture] = Roofs.RoofCatalog.SlateMaterial();
            return textureMaterials[texture];
        }

        Material material = materialSources
            .Select(name => PrefabManager.Instance.GetPrefab(name))
            .Where(prefab => prefab != null)
            .SelectMany(prefab => prefab.GetComponentsInChildren<Renderer>(true))
            .SelectMany(renderer => renderer.sharedMaterials)
            .FirstOrDefault(candidate => HasTexture(candidate, texture))
            // A texture of a model no prefab draws, such as the longship's unused dragon head: any loaded material with
            // it, or else the texture itself on a copy of a wooden piece's material.
            ?? Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(candidate => HasTexture(candidate, texture))
            ?? OnWoodMaterial(Resources.FindObjectsOfTypeAll<Texture2D>().FirstOrDefault(candidate => candidate.name == texture))
            ?? throw new InvalidOperationException($"No vanilla material with the texture '{texture}'.");
        textureMaterials[texture] = material;
        return material;
    }

    private static bool HasTexture(Material material, string texture)
    {
        return material != null && material.mainTexture != null && material.mainTexture.name == texture;
    }

    private static Material OnWoodMaterial(Texture2D texture)
    {
        Material wood = texture == null ? null
            : PrefabManager.Instance.GetPrefab("wood_beam")?.GetComponentsInChildren<Renderer>(true).Select(renderer => renderer.sharedMaterial).FirstOrDefault(candidate => candidate != null);
        return wood == null ? null : new Material(wood) { name = $"{texture.name}_on_wood", mainTexture = texture };
    }

    // Unity's unit cube, centred on its origin, with every face showing the same flat stone of the stone wall's texture.
    private static Mesh StoneBoxMesh()
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh mesh = UnityEngine.Object.Instantiate(cube.GetComponent<MeshFilter>().sharedMesh);
        UnityEngine.Object.DestroyImmediate(cube);
        mesh.name = StoneBox;
        mesh.uv = mesh.uv.Select(uv => new Vector2(Mathf.Lerp(stoneBoxUv.xMin, stoneBoxUv.xMax, uv.x), Mathf.Lerp(stoneBoxUv.yMin, stoneBoxUv.yMax, uv.y))).ToArray();
        return mesh;
    }

    // The bundle model's albedo on a copy of the workbench's material, so it is lit like the vanilla pieces.
    private static Material BundleMaterial(string key)
    {
        Material template = PrefabManager.Instance.GetPrefab(BundleMaterialPrefab)?.transform.Find(BundleMaterialChild)?.GetComponent<MeshRenderer>()?.sharedMaterial
            ?? throw new InvalidOperationException($"{BundleMaterialPrefab}/{BundleMaterialChild} has no material.");
        return VisualHelper.CreateTexturedMaterial(template, ForagingAssets.LoadTexture($"{key}_albedo"), $"{key}_material");
    }

    // activeInHierarchy is always false inside Jotunn's disabled prefab container, so walk up to the root instead.
    private static bool IsActiveBelow(Transform transform, Transform root)
    {
        for (Transform current = transform; current != null && current != root; current = current.parent)
        {
            if (!current.gameObject.activeSelf)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// One vanilla sub-mesh with its material and its placement.
    /// </summary>
    public readonly struct MeshSource
    {
        /// <summary>The mesh.</summary>
        public readonly Mesh Mesh;

        /// <summary>Sub-mesh index.</summary>
        public readonly int SubMesh;

        /// <summary>The material of the sub-mesh.</summary>
        public readonly Material Material;

        /// <summary>Placement of the mesh in the key's space.</summary>
        public readonly Matrix4x4 Matrix;

        /// <summary>
        /// Creates a mesh source.
        /// </summary>
        /// <param name="mesh">The mesh.</param>
        /// <param name="subMesh">Sub-mesh index.</param>
        /// <param name="material">The material of the sub-mesh.</param>
        /// <param name="matrix">Placement of the mesh in the key's space.</param>
        public MeshSource(Mesh mesh, int subMesh, Material material, Matrix4x4 matrix)
        {
            Mesh = mesh;
            SubMesh = subMesh;
            Material = material;
            Matrix = matrix;
        }
    }
}
