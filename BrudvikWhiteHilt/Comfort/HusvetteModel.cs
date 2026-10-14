using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Comfort;

/// <summary>
/// The Husvette's carved figure and porridge, made in code from turned shapes: the same profiles, places and colours as
/// <c>husvette()</c> in AssetSource/Preview/build_husvette.py, so the agreed picture is what the game shows. All parts
/// are one mesh whose UVs point into a small palette texture, so the figure costs one material. The figure faces +z
/// and stands on the stump, whose top is at <see cref="Stump"/>.
/// </summary>
public static class HusvetteModel
{
    /// <summary>Height of the stump's top, where the figure stands (build_husvette.py STUMP).</summary>
    public const float Stump = 0.3f;

    private const int Segments = 24;

    // The palette, one texel per colour, in the order of the colour indices below.
    private static readonly Color32[] palette =
    {
        Rgb(0.55f, 0.38f, 0.24f), Rgb(0.38f, 0.26f, 0.17f), Rgb(0.72f, 0.55f, 0.42f), Rgb(0.66f, 0.64f, 0.6f),
        Rgb(0.62f, 0.12f, 0.09f), Rgb(0.12f, 0.1f, 0.09f), Rgb(0.9f, 0.82f, 0.6f), Rgb(0.95f, 0.8f, 0.3f)
    };

    private const int Wood = 0;
    private const int WoodDark = 1;
    private const int Face = 2;
    private const int Beard = 3;
    private const int Cap = 4;
    private const int Eyes = 5;
    private const int Porridge = 6;
    private const int Butter = 7;

    private static Mesh mesh;
    private static Texture2D texture;

    /// <summary>
    /// Adds the figure to a piece's look, with a copy of the piece's own material.
    /// </summary>
    /// <param name="parent">The piece's visual root.</param>
    /// <param name="template">A renderer of the piece, whose material and shadows are copied.</param>
    public static void Attach(Transform parent, Renderer template)
    {
        texture ??= CreatePalette();
        mesh ??= CreateMesh();
        GameObject model = VisualHelper.CreateModel(parent, mesh, texture, template, Vector3.zero, Quaternion.identity, 1f);
        model.name = "husvette";
    }

    private static Mesh CreateMesh()
    {
        Builder b = new();
        float s = Stump;
        b.Lathe(new[] { (0.15f, 0f), (0.16f, 0.1f), (0.13f, 0.32f), (0.1f, 0.42f), (0.07f, 0.45f) }, new Vector3(0, s, -0.05f), Wood);
        b.Lathe(new[] { (0.139f, 0f), (0.139f, 0.035f) }, new Vector3(0, s + 0.24f, -0.05f), WoodDark);
        b.Sphere(0.105f, new Vector3(0, s + 0.41f, -0.03f), Face);
        b.Sphere(0.028f, new Vector3(0, s + 0.43f, 0.075f), Face);
        b.Sphere(0.012f, new Vector3(-0.038f, s + 0.465f, 0.062f), Eyes);
        b.Sphere(0.012f, new Vector3(0.038f, s + 0.465f, 0.062f), Eyes);
        b.Lathe(new[] { (0.02f, 0f), (0.07f, 0.05f), (0.085f, 0.13f), (0.075f, 0.2f), (0f, 0.25f) }, new Vector3(0, s + 0.22f, 0.01f), Beard);
        b.Lathe(new[] { (0.112f, 0f), (0.108f, 0.03f), (0.08f, 0.13f), (0.04f, 0.25f), (0f, 0.34f) }, new Vector3(0, s + 0.5f, -0.04f), Cap,
            new Vector2(0f, -0.11f));
        b.Lathe(new[] { (0.116f, 0f), (0.116f, 0.03f) }, new Vector3(0, s + 0.5f, -0.04f), Beard);

        // The porridge bowl before his feet: a turned wooden bowl, the porridge and a knob of butter.
        b.Lathe(new[] { (0.045f, 0f), (0.072f, 0.02f), (0.085f, 0.055f), (0.088f, 0.07f) }, new Vector3(0, s, 0.17f), Wood);
        b.Lathe(new[] { (0.075f, 0f), (0.075f, 0.01f) }, new Vector3(0, s + 0.055f, 0.17f), Porridge);
        b.Sphere(0.018f, new Vector3(0, s + 0.064f, 0.17f), Butter, 0.6f);

        // Both sides, each with its own vertices so each gets its own normals: the game's shaders draw only the front.
        int count = b.Vertices.Count;
        List<Vector3> vertices = new(b.Vertices);
        vertices.AddRange(b.Vertices);
        List<Vector2> uvs = new(b.Uvs);
        uvs.AddRange(b.Uvs);
        List<int> triangles = new(b.Triangles);
        for (int i = 0; i < b.Triangles.Count; i += 3)
        {
            triangles.Add(b.Triangles[i] + count);
            triangles.Add(b.Triangles[i + 2] + count);
            triangles.Add(b.Triangles[i + 1] + count);
        }

        Mesh created = new() { name = "whitehilt_husvette" };
        created.SetVertices(vertices);
        created.SetUVs(0, uvs);
        created.SetTriangles(triangles, 0);
        created.RecalculateNormals();
        created.RecalculateBounds();
        return created;
    }

    private static Texture2D CreatePalette()
    {
        Texture2D created = VisualHelper.CreateTexture("whitehilt_husvette_palette", palette.Length, 1, palette);
        created.filterMode = FilterMode.Point;
        created.wrapMode = TextureWrapMode.Clamp;
        return created;
    }

    private static Color32 Rgb(float r, float g, float b) => new Color(r, g, b);

    // Collects turned shapes into one mesh; every vertex of a shape gets the UV of its colour's texel.
    private sealed class Builder
    {
        public readonly List<Vector3> Vertices = new();
        public readonly List<Vector2> Uvs = new();
        public readonly List<int> Triangles = new();

        // A turned shape from a profile of (radius, height) from the bottom up, closed at both ends; tilt bends the top
        // over by (x, z), the rings below in proportion to the square of their height.
        public void Lathe(IReadOnlyList<(float Radius, float Height)> profile, Vector3 at, int colour, Vector2 tilt = default)
        {
            int first = Vertices.Count;
            float top = profile[profile.Count - 1].Height > 0f ? profile[profile.Count - 1].Height : 1f;
            Vector2 uv = new((colour + 0.5f) / palette.Length, 0.5f);
            foreach ((float radius, float height) in profile)
            {
                float lean = height / top * (height / top);
                for (int i = 0; i < Segments; i++)
                {
                    float angle = 2f * Mathf.PI * i / Segments;
                    Vertices.Add(at + new Vector3(radius * Mathf.Cos(angle) + tilt.x * lean, height, radius * Mathf.Sin(angle) + tilt.y * lean));
                    Uvs.Add(uv);
                }
            }

            for (int ring = 0; ring < profile.Count - 1; ring++)
            {
                for (int i = 0; i < Segments; i++)
                {
                    int a = first + ring * Segments + i;
                    int bb = first + ring * Segments + (i + 1) % Segments;
                    Triangles.AddRange(new[] { a, bb, a + Segments, bb, bb + Segments, a + Segments });
                }
            }

            Cap(first, profile[0].Height, 0, top, at, tilt, uv, down: true);
            Cap(first, profile[profile.Count - 1].Height, profile.Count - 1, top, at, tilt, uv, down: false);
        }

        // A ball standing on its point at "at", as the preview's sphere does: its bottom, not its centre, is there.
        public void Sphere(float radius, Vector3 at, int colour, float squash = 1f)
        {
            List<(float, float)> profile = new();
            for (int k = 0; k <= 10; k++)
            {
                profile.Add((radius * Mathf.Sin(Mathf.PI * k / 10f), radius * squash * (1f - Mathf.Cos(Mathf.PI * k / 10f))));
            }

            Lathe(profile, at, colour);
        }

        private void Cap(int first, float height, int ring, float top, Vector3 at, Vector2 tilt, Vector2 uv, bool down)
        {
            float lean = height / top * (height / top);
            int centre = Vertices.Count;
            Vertices.Add(at + new Vector3(tilt.x * lean, height, tilt.y * lean));
            Uvs.Add(uv);
            for (int i = 0; i < Segments; i++)
            {
                int a = first + ring * Segments + i;
                int bb = first + ring * Segments + (i + 1) % Segments;
                Triangles.AddRange(down ? new[] { centre, a, bb } : new[] { centre, bb, a });
            }
        }
    }
}
