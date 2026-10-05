using BrudvikWhiteHilt.Pieces.Defenses;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Helpers;

/// <summary>
/// Gives the White Hilt models made of flat colour swatches (the wall drawer and the ship workshops) the surfaces of
/// Valheim's own wood, iron and stone. Their faces only point at the middle of a swatch in a small palette texture, so
/// they have no texture coordinates to show grain with: each face gets coordinates in metres, projected along its
/// facing, and each kind of surface its own vanilla material, the light pine look chosen for them.
/// </summary>
public static class PaletteSurfaces
{
    /// <summary>A kind of surface: the vanilla texture, its tint and how many metres one repeat covers.</summary>
    public readonly struct Surface
    {
        /// <summary>Name of the vanilla texture.</summary>
        public readonly string Texture;

        /// <summary>Colour multiplied into the material.</summary>
        public readonly Color Tint;

        /// <summary>Metres one repeat of the texture covers.</summary>
        public readonly float Metres;

        /// <summary>
        /// Creates a surface.
        /// </summary>
        /// <param name="texture">Name of the vanilla texture.</param>
        /// <param name="tint">Colour multiplied into the material.</param>
        /// <param name="metres">Metres one repeat covers.</param>
        public Surface(string texture, Color tint, float metres)
        {
            Texture = texture;
            Tint = tint;
            Metres = metres;
        }
    }

    /// <summary>Light worn pine, for table tops and drawer fronts.</summary>
    public static readonly Surface Top = new("wood", new Color(0.82f, 0.74f, 0.62f), 0.9f);

    /// <summary>Plain plank wood, for frames and legs.</summary>
    public static readonly Surface Frame = new("Planks5c_low", new Color(1.25f, 1.15f, 1f), 0.8f);

    /// <summary>Dark worn iron, for bands, brackets and tools.</summary>
    public static readonly Surface Iron = new("metalwall", new Color(0.42f, 0.42f, 0.45f), 0.4f);

    /// <summary>Stone, for the stonecutter's block.</summary>
    public static readonly Surface Stone = new("stone", new Color(0.8f, 0.8f, 0.8f), 0.7f);

    private static readonly Dictionary<(string, Color), Material> materials = new();
    private static readonly Dictionary<(Mesh, Vector3), Mesh> meshes = new();

    /// <summary>
    /// Replaces a palette model's mesh and material with surfaced ones.
    /// </summary>
    /// <param name="model">The model, with a mesh filter and renderer.</param>
    /// <param name="swatches">The surface of each swatch, by the u coordinate of its middle in the palette.</param>
    /// <param name="scale">The model's scale in its parent, for coordinates in metres.</param>
    public static void Apply(GameObject model, IReadOnlyList<(float U, Surface Surface)> swatches, Vector3 scale)
    {
        MeshFilter filter = model.GetComponent<MeshFilter>();
        MeshRenderer renderer = model.GetComponent<MeshRenderer>();
        if (filter == null || renderer == null || filter.sharedMesh == null || !filter.sharedMesh.isReadable)
        {
            return;
        }

        List<Surface> used = new();
        Mesh source = filter.sharedMesh;
        if (!meshes.TryGetValue((source, scale), out Mesh surfaced))
        {
            surfaced = Build(source, swatches, scale, used);
            meshes[(source, scale)] = surfaced;
        }
        else
        {
            used.AddRange(Used(source, swatches));
        }

        Material[] shared = used.Select(surface => MaterialFor(surface, renderer.sharedMaterial)).ToArray();
        filter.sharedMesh = surfaced;
        renderer.sharedMaterials = shared;
    }

    private static Mesh Build(Mesh source, IReadOnlyList<(float U, Surface Surface)> swatches, Vector3 scale, List<Surface> used)
    {
        Vector3[] vertices = source.vertices;
        Vector3[] normals = source.normals;
        Vector2[] uvs = source.uv;
        int[] triangles = source.triangles;
        bool hasNormals = normals.Length == vertices.Length;

        Dictionary<int, List<int>> bySwatch = new();
        List<Vector3> outVertices = new();
        List<Vector3> outNormals = new();
        List<Vector2> outUvs = new();
        for (int i = 0; i + 2 < triangles.Length; i += 3)
        {
            int swatch = Nearest(swatches, uvs.Length > triangles[i] ? uvs[triangles[i]].x : 0f);
            Surface surface = swatches[swatch].Surface;
            Vector3 a = Vector3.Scale(vertices[triangles[i]], scale);
            Vector3 b = Vector3.Scale(vertices[triangles[i + 1]], scale);
            Vector3 c = Vector3.Scale(vertices[triangles[i + 2]], scale);
            Vector3 facing = Vector3.Cross(b - a, c - a);
            int axis = Mathf.Abs(facing.x) >= Mathf.Abs(facing.y) && Mathf.Abs(facing.x) >= Mathf.Abs(facing.z) ? 0
                : Mathf.Abs(facing.y) >= Mathf.Abs(facing.z) ? 1 : 2;
            if (!bySwatch.TryGetValue(swatch, out List<int> list))
            {
                list = new List<int>();
                bySwatch[swatch] = list;
            }

            for (int corner = 0; corner < 3; corner++)
            {
                int index = triangles[i + corner];
                Vector3 metres = Vector3.Scale(vertices[index], scale);
                list.Add(outVertices.Count);
                outVertices.Add(vertices[index]);
                outNormals.Add(hasNormals ? normals[index] : facing.normalized);
                Vector2 projected = axis == 0 ? new Vector2(metres.z, metres.y) : axis == 1 ? new Vector2(metres.x, metres.z) : new Vector2(metres.x, metres.y);
                outUvs.Add(projected / surface.Metres);
            }
        }

        Mesh mesh = new() { name = $"{source.name}_surfaced" };
        mesh.indexFormat = outVertices.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(outVertices);
        mesh.SetNormals(outNormals);
        mesh.SetUVs(0, outUvs);
        List<int> order = bySwatch.Keys.OrderBy(key => key).ToList();
        mesh.subMeshCount = order.Count;
        for (int sub = 0; sub < order.Count; sub++)
        {
            mesh.SetTriangles(bySwatch[order[sub]], sub);
            used.Add(swatches[order[sub]].Surface);
        }

        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    // The surfaces a mesh uses, in its sub-mesh order, for a mesh already surfaced.
    private static IEnumerable<Surface> Used(Mesh source, IReadOnlyList<(float U, Surface Surface)> swatches)
    {
        Vector2[] uvs = source.uv;
        int[] triangles = source.triangles;
        return Enumerable.Range(0, triangles.Length / 3)
            .Select(t => Nearest(swatches, uvs.Length > triangles[t * 3] ? uvs[triangles[t * 3]].x : 0f))
            .Distinct().OrderBy(swatch => swatch).Select(swatch => swatches[swatch].Surface);
    }

    private static int Nearest(IReadOnlyList<(float U, Surface Surface)> swatches, float u)
    {
        int best = 0;
        for (int i = 1; i < swatches.Count; i++)
        {
            if (Mathf.Abs(swatches[i].U - u) < Mathf.Abs(swatches[best].U - u))
            {
                best = i;
            }
        }

        return best;
    }

    // The vanilla material with the surface's texture, tinted, repeating once per unit of the new coordinates; the
    // model's own material should the vanilla one be missing.
    private static Material MaterialFor(Surface surface, Material fallback)
    {
        if (materials.TryGetValue((surface.Texture, surface.Tint), out Material cached))
        {
            return cached;
        }

        Material source;
        try
        {
            source = VanillaMeshLibrary.MaterialWithTexture(surface.Texture);
        }
        catch (InvalidOperationException exception)
        {
            // Never the palette itself: its swatches stretched over coordinates in metres show as stripes. The plank
            // wood in the surface's own tint instead.
            Jotunn.Logger.LogWarning($"{exception.Message} Using the plank wood instead.");
            if (surface.Texture == Frame.Texture)
            {
                return fallback;
            }

            Material planks = MaterialFor(new Surface(Frame.Texture, surface.Tint, surface.Metres), fallback);
            materials[(surface.Texture, surface.Tint)] = planks;
            return planks;
        }

        Material material = new(source) { name = $"{source.name}_whitehilt" };
        if (material.HasProperty("_Color"))
        {
            material.color = source.color * surface.Tint;
        }

        material.mainTextureScale = Vector2.one;
        material.mainTextureOffset = Vector2.zero;
        materials[(surface.Texture, surface.Tint)] = material;
        return material;
    }
}
