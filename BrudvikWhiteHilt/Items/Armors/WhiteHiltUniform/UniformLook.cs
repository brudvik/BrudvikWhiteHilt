using BrudvikWhiteHilt.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace BrudvikWhiteHilt.Items.Armors.WhiteHiltUniform;

/// <summary>
/// The black and gold look of the White Hilt uniforms: recoloured vanilla clothes, the logo badge on the chest and the
/// uniform cape.
/// </summary>
public static class UniformLook
{
    private const string CapeTrimResource = "BrudvikWhiteHilt.Assets.Uniform.CapeTrim.png";
    private const string EmptyBodyTexture = "player_armor_none";
    private const int CapeTextureSize = 512;
    private const int BadgeTextureSize = 256;
    private const int BadgeSegments = 40;
    private const float BadgeDiameter = 0.11f;

    private static readonly Color black = new(0.075f, 0.072f, 0.08f);
    private static readonly Color gold = new(0.86f, 0.66f, 0.27f);

    private static Mesh badgeMesh;
    private static Texture2D badgeTexture;

    // Where a ray along the body's centre line meets the cape at 1.27 m; the cape's UVs run about 0.5 per metre, so the
    // logo is 0.46 m across.
    /// <summary>
    /// Centre of the logo on the back of the troll hide cape, in the worn cape's UVs.
    /// </summary>
    public static Vector2 CapeLogoCentre => new(0.174f, 0.63f);

    /// <summary>
    /// Half the logo's width and height on the troll hide cape, in the worn cape's UVs.
    /// </summary>
    public static Vector2 CapeLogoRadius => new(0.115f, 0.117f);

    // A flat disc facing local -z, like Unity's quad, with the logo across it.
    private static Mesh BadgeMesh
    {
        get
        {
            if (badgeMesh != null)
            {
                return badgeMesh;
            }

            Vector3[] vertices = new Vector3[BadgeSegments + 1];
            Vector2[] uvs = new Vector2[BadgeSegments + 1];
            int[] triangles = new int[BadgeSegments * 3];
            vertices[0] = Vector3.zero;
            uvs[0] = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < BadgeSegments; i++)
            {
                float angle = 2f * Mathf.PI * i / BadgeSegments;
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                vertices[i + 1] = direction * (BadgeDiameter / 2f);
                uvs[i + 1] = new Vector2(0.5f, 0.5f) + direction * 0.5f;
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = 1 + (i + 1) % BadgeSegments;
                triangles[i * 3 + 2] = 1 + i;
            }

            badgeMesh = new Mesh { name = "whitehilt_badge", vertices = vertices, uv = uvs, triangles = triangles };
            badgeMesh.normals = Enumerable.Repeat(Vector3.back, vertices.Length).ToArray();
            badgeMesh.RecalculateBounds();
            badgeMesh.RecalculateTangents();
            return badgeMesh;
        }
    }

    // The logo on black, so the disc's edge does not show the logo's transparent corners.
    private static Texture2D BadgeTexture
    {
        get
        {
            if (badgeTexture == null)
            {
                Color32[] pixels = Enumerable.Repeat((Color32)black, BadgeTextureSize * BadgeTextureSize).ToArray();
                WhiteHiltLogo.Paint(pixels, BadgeTextureSize, BadgeTextureSize, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                badgeTexture = VisualHelper.CreateTexture("whitehilt_badge", BadgeTextureSize, BadgeTextureSize, pixels);
                badgeTexture.wrapMode = TextureWrapMode.Clamp;
            }

            return badgeTexture;
        }
    }

    /// <summary>
    /// Returns a recolouring that turns cloth black, keeping its grain, and yellow or orange trim gold.
    /// </summary>
    /// <param name="lightToGold">Also turns light, unsaturated embroidery gold.</param>
    /// <param name="goldMinValue">Least brightness of a yellow pixel that becomes gold.</param>
    /// <param name="blackLevel">Brightness multiplier of the black cloth.</param>
    /// <returns>The recolouring.</returns>
    public static Func<Color32, Color32> BlackGold(bool lightToGold, float goldMinValue = 0.5f, float blackLevel = 1f)
    {
        return pixel =>
        {
            Color.RGBToHSV(pixel, out float hue, out float saturation, out float value);
            bool trim = (hue > 0.05f && hue < 0.18f && saturation > 0.45f && value > goldMinValue)
                || (lightToGold && saturation < 0.3f && value > 0.68f);
            Color result = trim
                ? gold * Mathf.Clamp(0.55f + 0.6f * value, 0f, 1.15f)
                : black * ((0.55f + 0.9f * value) * blackLevel);
            result.a = pixel.a / 255f;
            return result;
        };
    }

    /// <summary>
    /// Recolours every material of the item, worn and on the ground, and the textures it puts on the body.
    /// </summary>
    /// <param name="prefab">Item prefab.</param>
    /// <param name="shared">Item data, whose body material is replaced by a recoloured copy.</param>
    /// <param name="recolor">The recolouring.</param>
    public static void Recolor(GameObject prefab, ItemDrop.ItemData.SharedData shared, Func<Color32, Color32> recolor)
    {
        Dictionary<Texture, Texture2D> recoloured = new();
        Texture2D RecolorOnce(Texture source)
        {
            if (!recoloured.TryGetValue(source, out Texture2D texture))
            {
                texture = VisualHelper.RecolorTexture(source, recolor);
                recoloured[source] = texture;
            }

            return texture;
        }

        foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true).Where(IsMeshRenderer))
        {
            renderer.sharedMaterials = renderer.sharedMaterials
                .Select(source => source == null || source.mainTexture == null
                    ? source
                    : new Material(source) { mainTexture = RecolorOnce(source.mainTexture) })
                .ToArray();
        }

        if (shared.m_armorMaterial == null)
        {
            return;
        }

        Material body = new(shared.m_armorMaterial) { name = $"{shared.m_armorMaterial.name}_whitehilt" };
        foreach (string property in new[] { "_ChestTex", "_LegsTex" })
        {
            Texture texture = body.HasProperty(property) ? body.GetTexture(property) : null;
            if (texture != null && texture.name != EmptyBodyTexture)
            {
                body.SetTexture(property, RecolorOnce(texture));
            }
        }

        shared.m_armorMaterial = body;
    }

    /// <summary>
    /// Puts a round logo badge on the wearer's left breast. VisEquipment hangs a child named attach_&lt;bone&gt; on that
    /// bone, at the bone's position and rotation and in world scale, so offsets are in metres along the bone's axes.
    /// </summary>
    /// <param name="prefab">Item prefab of a chest piece.</param>
    /// <param name="male">Badge centre and outward normal on the male body.</param>
    /// <param name="female">Badge centre and outward normal on the female body.</param>
    public static void AddChestBadge(GameObject prefab, UniformBadge.Placement male, UniformBadge.Placement female)
    {
        Renderer template = prefab.GetComponentsInChildren<Renderer>(true).Where(IsMeshRenderer).FirstOrDefault()
            ?? throw new InvalidOperationException($"{prefab.name} has no renderer to copy a material from.");

        GameObject attach = new("attach_Spine2") { layer = template.gameObject.layer };
        attach.transform.SetParent(prefab.transform, false);
        attach.SetActive(false);

        GameObject badge = new("WhiteHiltBadge") { layer = attach.layer };
        badge.transform.SetParent(attach.transform, false);
        badge.AddComponent<MeshFilter>().sharedMesh = BadgeMesh;
        MeshRenderer renderer = badge.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = VisualHelper.CreateTexturedMaterial(template.sharedMaterial, BadgeTexture, "whitehilt_badge");
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        UniformBadge placement = badge.AddComponent<UniformBadge>();
        placement.male = male;
        placement.female = female;
        placement.Apply(male);
    }

    /// <summary>
    /// Creates the worn uniform cape's material: black hide in the cape mesh's own UVs, a gold trim just inside its
    /// edge and the logo on the back.
    /// </summary>
    /// <param name="source">Material of the vanilla troll hide cape, whose texture is tiled onto the cape.</param>
    /// <returns>The new material.</returns>
    public static Material CreateCapeMaterial(Material source)
    {
        Texture hide = source.mainTexture;
        Vector2 scale = source.mainTextureScale;
        Vector2 offset = source.mainTextureOffset;
        Color32[] hidePixels = VisualHelper.ReadPixels(hide, hide.width, hide.height);
        Texture2D trim = AssetUtilsExtended.LoadTextureFromEmbeddedResource(CapeTrimResource);
        Color32[] trimPixels = trim.GetPixels32();

        Color32[] pixels = new Color32[CapeTextureSize * CapeTextureSize];
        for (int y = 0; y < CapeTextureSize; y++)
        {
            float v = (y + 0.5f) / CapeTextureSize;
            int hideY = Mathf.Clamp((int)(Mathf.Repeat(v * scale.y + offset.y, 1f) * hide.height), 0, hide.height - 1);
            int trimY = Mathf.Min(trim.height - 1, (int)(v * trim.height));
            for (int x = 0; x < CapeTextureSize; x++)
            {
                float u = (x + 0.5f) / CapeTextureSize;
                int hideX = Mathf.Clamp((int)(Mathf.Repeat(u * scale.x + offset.x, 1f) * hide.width), 0, hide.width - 1);
                int trimX = Mathf.Min(trim.width - 1, (int)(u * trim.width));
                Color.RGBToHSV(hidePixels[hideY * hide.width + hideX], out _, out _, out float grain);
                Color color = trimPixels[trimY * trim.width + trimX].r > 127
                    ? gold * (0.85f + 0.3f * grain)
                    : black * (0.7f + 0.55f * grain);
                color.a = 1f;
                pixels[y * CapeTextureSize + x] = color;
            }
        }

        UnityEngine.Object.Destroy(trim);
        WhiteHiltLogo.Paint(pixels, CapeTextureSize, CapeTextureSize, CapeLogoCentre, CapeLogoRadius);
        Texture2D texture = VisualHelper.CreateTexture("whitehilt_uniformcape", CapeTextureSize, CapeTextureSize, pixels, hide);
        Material material = VisualHelper.CreateTexturedMaterial(source, texture, "whitehilt_uniformcape");
        material.mainTextureScale = Vector2.one;
        material.mainTextureOffset = Vector2.zero;
        return material;
    }

    /// <summary>
    /// Renders a new inventory icon, keeping one entry per vanilla variant.
    /// </summary>
    /// <param name="prefab">Item prefab.</param>
    /// <param name="shared">Item data.</param>
    public static void RenderIcon(GameObject prefab, ItemDrop.ItemData.SharedData shared)
    {
        Sprite icon = VisualHelper.RenderIcon(prefab);
        if (icon != null)
        {
            // GetIcon indexes m_icons by the saved m_variant.
            shared.m_icons = Enumerable.Repeat(icon, Math.Max(1, shared.m_icons?.Length ?? 0)).ToArray();
        }
    }

    private static bool IsMeshRenderer(Renderer renderer)
    {
        return renderer is MeshRenderer || renderer is SkinnedMeshRenderer;
    }
}
