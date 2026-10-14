using BrudvikWhiteHilt.Helpers;
using UnityEngine;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// The long swallow-tailed pennant on a trading post's flag pole: a mesh made in code, 2.4 m long and 0.9 m high at
/// the hoist, flying along +x, with both sides drawn (Valheim's shaders cull back faces). The same shape as
/// <c>pennant_mesh</c> in AssetSource/Preview/build_trading_post.py.
/// </summary>
public static class TradingPostPennant
{
    private const int TextureWidth = 128;
    private const int TextureHeight = 64;
    private const int StripeHeight = 16;

    private static readonly Color32 red = new(158, 41, 33, 255);
    private static readonly Color32 cream = new(230, 222, 204, 255);

    private static Mesh mesh;
    private static Texture2D texture;

    /// <summary>
    /// Gets the pennant's mesh, made once.
    /// </summary>
    public static Mesh Mesh => mesh ??= CreateMesh();

    /// <summary>
    /// Gets the pennant's striped texture in the market stall's red, made once; a post dyes it in its biome's colour
    /// (<see cref="TradingPostColours"/>).
    /// </summary>
    public static Texture2D Texture => texture ??= CreateTexture(red);

    private static Mesh CreateMesh()
    {
        Vector3[] corners =
        {
            new(0f, 0f, 0f), new(0f, 0.9f, 0f), new(2.4f, 0.75f, 0.15f), new(1.9f, 0.45f, 0.1f), new(2.4f, 0.15f, 0.15f), new(1.2f, 0.45f, 0.05f)
        };
        Vector2[] uvs = { new(0f, 0f), new(0f, 1f), new(1f, 0.85f), new(0.8f, 0.5f), new(1f, 0.15f), new(0.5f, 0.5f) };
        int[] front = { 0, 1, 5, 1, 2, 5, 5, 2, 3, 5, 3, 4, 0, 5, 4 };

        // Each side gets its own vertices, so its normals face its own way.
        Vector3[] vertices = new Vector3[corners.Length * 2];
        Vector2[] allUvs = new Vector2[corners.Length * 2];
        for (int i = 0; i < corners.Length; i++)
        {
            vertices[i] = vertices[i + corners.Length] = corners[i];
            allUvs[i] = allUvs[i + corners.Length] = uvs[i];
        }

        int[] triangles = new int[front.Length * 2];
        for (int i = 0; i < front.Length; i += 3)
        {
            triangles[i] = front[i];
            triangles[i + 1] = front[i + 1];
            triangles[i + 2] = front[i + 2];
            triangles[front.Length + i] = front[i] + corners.Length;
            triangles[front.Length + i + 1] = front[i + 2] + corners.Length;
            triangles[front.Length + i + 2] = front[i + 1] + corners.Length;
        }

        Mesh created = new() { name = "whitehilt_pennant", vertices = vertices, uv = allUvs, triangles = triangles };
        created.RecalculateNormals();
        created.RecalculateBounds();
        return created;
    }

    /// <summary>
    /// Makes the pennant's texture: stripes along it in the given colour and cream, with a faint weave so it does not
    /// look flat.
    /// </summary>
    /// <param name="colour">The stripes' colour.</param>
    /// <returns>The texture.</returns>
    public static Texture2D CreateTexture(Color colour)
    {
        Color32 coloured = colour;
        Color32[] pixels = new Color32[TextureWidth * TextureHeight];
        for (int y = 0; y < TextureHeight; y++)
        {
            Color32 stripe = y / StripeHeight % 2 == 0 ? coloured : cream;
            for (int x = 0; x < TextureWidth; x++)
            {
                float shade = 0.92f + 0.08f * ((x * 7 + y * 13) % 5) / 4f;
                pixels[y * TextureWidth + x] = new Color32((byte)(stripe.r * shade), (byte)(stripe.g * shade), (byte)(stripe.b * shade), 255);
            }
        }

        return VisualHelper.CreateTexture("whitehilt_pennant", TextureWidth, TextureHeight, pixels);
    }
}
