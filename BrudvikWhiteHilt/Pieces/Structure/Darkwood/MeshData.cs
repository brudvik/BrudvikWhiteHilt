using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Structure.Darkwood;

/// <summary>
/// A triangle mesh as plain lists, to cut and reshape a vanilla model: split it with planes, move and stretch it,
/// mirror it, pick triangles and join pieces. Only the conversion to and from <see cref="Mesh"/> needs the running
/// engine, so the tests and the offline preview use the same code as the game.
/// </summary>
public sealed class MeshData
{
    /// <summary>Vertex positions.</summary>
    public readonly List<Vector3> Vertices = new();

    /// <summary>Vertex normals; empty when the source had none.</summary>
    public readonly List<Vector3> Normals = new();

    /// <summary>Vertex tangents (w is the handedness); empty when the source had none.</summary>
    public readonly List<Vector4> Tangents = new();

    /// <summary>Texture coordinates; empty when the source had none.</summary>
    public readonly List<Vector2> Uvs = new();

    /// <summary>Vertex indices, three per triangle.</summary>
    public readonly List<int> Triangles = new();

    /// <summary>Number of triangles.</summary>
    public int TriangleCount => Triangles.Count / 3;

    private bool HasNormals => Vertices.Count > 0 && Normals.Count == Vertices.Count;

    private bool HasTangents => Vertices.Count > 0 && Tangents.Count == Vertices.Count;

    private bool HasUvs => Vertices.Count > 0 && Uvs.Count == Vertices.Count;

    /// <summary>
    /// Reads one submesh of a readable mesh, moved by a matrix (e.g. from the renderer's space into the piece's).
    /// </summary>
    /// <param name="mesh">The mesh.</param>
    /// <param name="subMesh">Submesh index.</param>
    /// <param name="matrix">Placement of the mesh.</param>
    /// <returns>The mesh data.</returns>
    public static MeshData FromMesh(Mesh mesh, int subMesh, Matrix4x4 matrix)
    {
        MeshData data = new();
        data.Vertices.AddRange(mesh.vertices);
        data.Normals.AddRange(mesh.normals);
        data.Tangents.AddRange(mesh.tangents);
        data.Uvs.AddRange(mesh.uv);
        data.Triangles.AddRange(mesh.GetTriangles(subMesh));
        return data.Select(_ => true).Transformed(matrix.GetColumn(0), matrix.GetColumn(1), matrix.GetColumn(2), matrix.GetColumn(3));
    }

    /// <summary>
    /// Builds a mesh with one submesh per part, in the order given; parts may be empty.
    /// </summary>
    /// <param name="name">Mesh name.</param>
    /// <param name="parts">The submeshes.</param>
    /// <returns>The mesh.</returns>
    public static Mesh ToMesh(string name, params MeshData[] parts)
    {
        MeshData all = new();
        List<int[]> subMeshes = new();
        foreach (MeshData part in parts)
        {
            int start = all.Vertices.Count;
            all.Append(part);
            subMeshes.Add(part.Triangles.Select(index => index + start).ToArray());
        }

        Mesh mesh = new() { name = name };
        mesh.SetVertices(all.Vertices);
        if (all.HasNormals)
        {
            mesh.SetNormals(all.Normals);
        }

        if (all.HasUvs)
        {
            mesh.SetUVs(0, all.Uvs);
        }

        if (all.HasTangents)
        {
            mesh.SetTangents(all.Tangents);
        }

        mesh.subMeshCount = subMeshes.Count;
        for (int i = 0; i < subMeshes.Count; i++)
        {
            mesh.SetTriangles(subMeshes[i], i);
        }

        if (!all.HasNormals)
        {
            mesh.RecalculateNormals();
        }

        if (!all.HasTangents && all.HasUvs)
        {
            mesh.RecalculateTangents();
        }

        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>
    /// Joins meshes into one.
    /// </summary>
    /// <param name="parts">The meshes.</param>
    /// <returns>The joined mesh.</returns>
    public static MeshData Combine(IEnumerable<MeshData> parts)
    {
        MeshData result = new();
        foreach (MeshData part in parts)
        {
            result.Append(part);
        }

        return result;
    }

    /// <summary>
    /// Centre of a triangle.
    /// </summary>
    /// <param name="triangle">Triangle index.</param>
    /// <returns>The centre.</returns>
    public Vector3 Centroid(int triangle)
    {
        return (Vertices[Triangles[triangle * 3]] + Vertices[Triangles[triangle * 3 + 1]] + Vertices[Triangles[triangle * 3 + 2]]) / 3f;
    }

    /// <summary>
    /// Texture coordinates of a triangle's corners, or an empty array when the mesh has none.
    /// </summary>
    /// <param name="triangle">Triangle index.</param>
    /// <returns>The three texture coordinates.</returns>
    public Vector2[] TriangleUvs(int triangle)
    {
        return HasUvs ? new[] { Uvs[Triangles[triangle * 3]], Uvs[Triangles[triangle * 3 + 1]], Uvs[Triangles[triangle * 3 + 2]] } : new Vector2[0];
    }

    /// <summary>
    /// The lowest and highest coordinate of the vertices that triangles use, along an axis.
    /// </summary>
    /// <param name="axis">0 for x, 1 for y, 2 for z.</param>
    /// <param name="min">Lowest.</param>
    /// <param name="max">Highest.</param>
    /// <returns>False when the mesh has no triangles.</returns>
    public bool Range(int axis, out float min, out float max)
    {
        min = float.MaxValue;
        max = float.MinValue;
        foreach (int index in Triangles)
        {
            float value = Vertices[index][axis];
            min = Mathf.Min(min, value);
            max = Mathf.Max(max, value);
        }

        return Triangles.Count > 0;
    }

    /// <summary>
    /// Keeps the triangles a test accepts.
    /// </summary>
    /// <param name="keep">Gets the triangle index.</param>
    /// <returns>The kept triangles, without unused vertices.</returns>
    public MeshData Select(Func<int, bool> keep)
    {
        MeshData result = new();
        Dictionary<int, int> map = new();
        for (int t = 0; t < TriangleCount; t++)
        {
            if (keep(t))
            {
                for (int corner = 0; corner < 3; corner++)
                {
                    result.Triangles.Add(result.Keep(this, map, Triangles[t * 3 + corner]));
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Keeps what lies on the side of a plane the normal points to: dot(normal, point) &gt;= distance. Triangles across
    /// the plane are cut, with normals, tangents and texture coordinates carried over to the new corners.
    /// </summary>
    /// <param name="normal">Plane normal, towards the side kept.</param>
    /// <param name="distance">Plane distance along the normal.</param>
    /// <returns>The kept part.</returns>
    public MeshData Clip(Vector3 normal, float distance)
    {
        const float Tolerance = 1e-5f;
        MeshData result = new();
        Dictionary<int, int> map = new();
        List<int> polygon = new(4);
        int[] corners = new int[3];
        float[] sides = new float[3];
        for (int t = 0; t < TriangleCount; t++)
        {
            bool allIn = true, allOut = true;
            for (int i = 0; i < 3; i++)
            {
                corners[i] = Triangles[t * 3 + i];
                sides[i] = Vector3.Dot(normal, Vertices[corners[i]]) - distance;
                allIn &= sides[i] >= -Tolerance;
                allOut &= sides[i] < -Tolerance;
            }

            if (allOut)
            {
                continue;
            }

            polygon.Clear();
            for (int i = 0; i < 3; i++)
            {
                int next = (i + 1) % 3;
                bool inside = sides[i] >= -Tolerance;
                if (inside)
                {
                    polygon.Add(result.Keep(this, map, corners[i]));
                }

                if (!allIn && inside != (sides[next] >= -Tolerance))
                {
                    polygon.Add(result.AddBetween(this, corners[i], corners[next], sides[i] / (sides[i] - sides[next])));
                }
            }

            for (int i = 1; i + 1 < polygon.Count; i++)
            {
                result.Triangles.Add(polygon[0]);
                result.Triangles.Add(polygon[i]);
                result.Triangles.Add(polygon[i + 1]);
            }
        }

        return result;
    }

    /// <summary>
    /// Keeps what lies between two values along an axis.
    /// </summary>
    /// <param name="axis">0 for x, 1 for y, 2 for z.</param>
    /// <param name="min">Lowest value kept.</param>
    /// <param name="max">Highest value kept.</param>
    /// <returns>The kept part.</returns>
    public MeshData Band(int axis, float min, float max)
    {
        Vector3 direction = Vector3.zero;
        direction[axis] = 1f;
        return Clip(direction, min).Clip(-direction, -max);
    }

    /// <summary>
    /// Moves every vertex by an affine map: point' = x * point.x + y * point.y + z * point.z + offset. Normals and
    /// tangents follow; a map that mirrors turns the triangles round, so their front stays outside.
    /// </summary>
    /// <param name="x">Where the x axis goes.</param>
    /// <param name="y">Where the y axis goes.</param>
    /// <param name="z">Where the z axis goes.</param>
    /// <param name="offset">Translation.</param>
    /// <returns>The moved mesh.</returns>
    public MeshData Transformed(Vector3 x, Vector3 y, Vector3 z, Vector3 offset)
    {
        // Normals go with the cofactor matrix (the inverse transpose times the determinant), which needs no inverse.
        Vector3 nx = Vector3.Cross(y, z), ny = Vector3.Cross(z, x), nz = Vector3.Cross(x, y);
        float determinant = Vector3.Dot(x, nx);
        float handedness = determinant < 0f ? -1f : 1f;

        MeshData result = new();
        foreach (Vector3 p in Vertices)
        {
            result.Vertices.Add(x * p.x + y * p.y + z * p.z + offset);
        }

        foreach (Vector3 n in Normals)
        {
            result.Normals.Add(((nx * n.x + ny * n.y + nz * n.z) * handedness).normalized);
        }

        foreach (Vector4 t in Tangents)
        {
            Vector3 direction = (x * t.x + y * t.y + z * t.z).normalized;
            result.Tangents.Add(new Vector4(direction.x, direction.y, direction.z, t.w * handedness));
        }

        result.Uvs.AddRange(Uvs);
        for (int i = 0; i < Triangles.Count; i += 3)
        {
            result.Triangles.Add(Triangles[i]);
            result.Triangles.Add(determinant < 0f ? Triangles[i + 2] : Triangles[i + 1]);
            result.Triangles.Add(determinant < 0f ? Triangles[i + 1] : Triangles[i + 2]);
        }

        return result;
    }

    /// <summary>
    /// Moves the mesh.
    /// </summary>
    /// <param name="offset">Translation.</param>
    /// <returns>The moved mesh.</returns>
    public MeshData Translated(Vector3 offset)
    {
        return Transformed(Vector3.right, Vector3.up, Vector3.forward, offset);
    }

    /// <summary>
    /// Maps one stretch along an axis linearly onto another, e.g. a panel between two heights onto a taller or lower
    /// one. A reversed target turns the stretch upside down.
    /// </summary>
    /// <param name="axis">0 for x, 1 for y, 2 for z.</param>
    /// <param name="from">Start of the stretch.</param>
    /// <param name="to">End of the stretch.</param>
    /// <param name="newFrom">Where the start goes.</param>
    /// <param name="newTo">Where the end goes.</param>
    /// <returns>The mapped mesh.</returns>
    public MeshData Remapped(int axis, float from, float to, float newFrom, float newTo)
    {
        float scale = (newTo - newFrom) / (to - from);
        Vector3[] axes = { Vector3.right, Vector3.up, Vector3.forward };
        axes[axis] *= scale;
        Vector3 offset = Vector3.zero;
        offset[axis] = newFrom - from * scale;
        return Transformed(axes[0], axes[1], axes[2], offset);
    }

    /// <summary>
    /// Mirrors the mesh in the plane x = 0.
    /// </summary>
    /// <returns>The mirrored mesh.</returns>
    public MeshData MirroredX()
    {
        return Transformed(Vector3.left, Vector3.up, Vector3.forward, Vector3.zero);
    }

    /// <summary>
    /// Splits the mesh into its loose parts: triangles joined by a shared corner (by position, so seams in the texture
    /// do not split a part) belong together, such as one hinge, one board or the ring.
    /// </summary>
    /// <returns>The parts.</returns>
    public List<MeshData> Islands()
    {
        Dictionary<(int, int, int), int> welded = new();
        int[] weld = new int[Vertices.Count];
        for (int i = 0; i < Vertices.Count; i++)
        {
            Vector3 p = Vertices[i];
            (int, int, int) key = (Mathf.RoundToInt(p.x * 10000f), Mathf.RoundToInt(p.y * 10000f), Mathf.RoundToInt(p.z * 10000f));
            if (!welded.TryGetValue(key, out int id))
            {
                id = welded.Count;
                welded[key] = id;
            }

            weld[i] = id;
        }

        int[] parent = Enumerable.Range(0, welded.Count).ToArray();
        int Find(int node)
        {
            while (parent[node] != node)
            {
                parent[node] = parent[parent[node]];
                node = parent[node];
            }

            return node;
        }

        for (int i = 0; i < Triangles.Count; i += 3)
        {
            int c = Find(weld[Triangles[i + 2]]);
            parent[Find(weld[Triangles[i]])] = c;
            parent[Find(weld[Triangles[i + 1]])] = c;
        }

        Dictionary<int, HashSet<int>> groups = new();
        for (int t = 0; t < TriangleCount; t++)
        {
            int root = Find(weld[Triangles[t * 3]]);
            if (!groups.TryGetValue(root, out HashSet<int> set))
            {
                set = new HashSet<int>();
                groups[root] = set;
            }

            set.Add(t);
        }

        return groups.Values.Select(set => Select(set.Contains)).ToList();
    }

    // Adds a part; an attribute some part lacks is dropped for the whole (the mesh then recalculates it). An empty part
    // adds nothing and lacks nothing: a floor laid where the cut leaves none of it would otherwise drop the texture.
    private void Append(MeshData part)
    {
        if (part.Vertices.Count == 0)
        {
            return;
        }

        int start = Vertices.Count;
        bool normals = (start == 0 || Normals.Count == start) && part.HasNormals;
        bool tangents = (start == 0 || Tangents.Count == start) && part.HasTangents;
        bool uvs = (start == 0 || Uvs.Count == start) && part.HasUvs;
        Vertices.AddRange(part.Vertices);
        AddOrDrop(Normals, normals, part.Normals);
        AddOrDrop(Tangents, tangents, part.Tangents);
        AddOrDrop(Uvs, uvs, part.Uvs);
        Triangles.AddRange(part.Triangles.Select(index => index + start));
    }

    private static void AddOrDrop<T>(List<T> target, bool add, List<T> source)
    {
        if (add)
        {
            target.AddRange(source);
        }
        else
        {
            target.Clear();
        }
    }

    // This mesh's copy of a source vertex, made on first use.
    private int Keep(MeshData source, Dictionary<int, int> map, int index)
    {
        if (map.TryGetValue(index, out int existing))
        {
            return existing;
        }

        Vertices.Add(source.Vertices[index]);
        if (source.HasNormals)
        {
            Normals.Add(source.Normals[index]);
        }

        if (source.HasTangents)
        {
            Tangents.Add(source.Tangents[index]);
        }

        if (source.HasUvs)
        {
            Uvs.Add(source.Uvs[index]);
        }

        map[index] = Vertices.Count - 1;
        return Vertices.Count - 1;
    }

    private int AddBetween(MeshData source, int from, int to, float t)
    {
        Vertices.Add(Vector3.Lerp(source.Vertices[from], source.Vertices[to], t));
        if (source.HasNormals)
        {
            Normals.Add(Vector3.Lerp(source.Normals[from], source.Normals[to], t).normalized);
        }

        if (source.HasTangents)
        {
            Vector4 a = source.Tangents[from];
            Vector3 direction = Vector3.Lerp(a, source.Tangents[to], t).normalized;
            Tangents.Add(new Vector4(direction.x, direction.y, direction.z, a.w));
        }

        if (source.HasUvs)
        {
            Uvs.Add(Vector2.Lerp(source.Uvs[from], source.Uvs[to], t));
        }

        return Vertices.Count - 1;
    }
}
