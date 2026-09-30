using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Banners;

/// <summary>
/// A vanilla banner with white or black cloth and the White Hilt logo, in the normal size or half again as big.
/// </summary>
public abstract class WhiteHiltBannerBase : IWhiteHiltCustomPiece
{
    private const string CopyFrom = "piece_banner01";
    private const float LargeScale = 1.5f;
    private const int TextureSize = 512;

    // The vanilla cloth is two mirrored halves on one UV strip, which would mirror the logo's text. The copy gets u across
    // its whole width instead: the cloth lies in the y-z plane, 1.18 m wide around z 0, with v running up from the tip
    // at 3.114 m per unit. Seen from +x the text reads the right way.
    private const float MetresPerUv = 3.114f;
    private const float HalfWidthU = 0.59f / MetresPerUv;
    private const float TopV = 0.9677f;
    private const float BorderWidth = 0.012f;
    private const float LogoCentreV = 0.62f;
    private const float LogoRadius = 0.16f;

    private static readonly Color whiteCloth = new(0.94f, 0.91f, 0.84f);
    private static readonly Color blackCloth = new(0.07f, 0.065f, 0.06f);
    private static readonly Color gold = Color.HSVToRGB(0.11f, 0.7f, 0.85f);
    private static readonly Dictionary<bool, Texture2D> textures = new();
    private static readonly Dictionary<Mesh, Mesh> meshes = new();

    private readonly PieceManager instance;

    /// <summary>
    /// Prefab name of the banner.
    /// </summary>
    protected abstract string PrefabName { get; }

    /// <summary>
    /// Name shown to players.
    /// </summary>
    protected abstract string FullName { get; }

    /// <summary>
    /// Description shown to players.
    /// </summary>
    protected abstract string Description { get; }

    /// <summary>
    /// True for black cloth, false for white.
    /// </summary>
    protected abstract bool Black { get; }

    /// <summary>
    /// True for the banner half again as big.
    /// </summary>
    protected abstract bool Large { get; }

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Start;

    /// <inheritdoc/>
    public string Id => PrefabName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(PrefabName);

    /// <inheritdoc/>
    public string GatedPrefabName => PrefabName;

    /// <summary>
    /// Registers the English text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    protected WhiteHiltBannerBase(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
    }

    /// <summary>
    /// Adds the banner to the hammer.
    /// </summary>
    public void Add()
    {
        try
        {
            PieceConfig pieceConfig = new()
            {
                Name = Translations.Token(PrefabName),
                Description = Translations.Token($"{PrefabName}_description"),
                PieceTable = PieceTables.Hammer,
                Category = PieceCategories.Furniture,
                CraftingStation = CraftingStations.Workbench,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "LeatherScraps", Amount = 6, Recover = true },
                    new() { Item = "FineWood", Amount = 2, Recover = true },
                    new() { Item = "BoneFragments", Amount = 2, Recover = true }
                }
            };

            CustomPiece piece = new(PrefabName, CopyFrom, pieceConfig);
            if (Large)
            {
                piece.PiecePrefab.transform.localScale *= LargeScale;
            }

            TryApplyVisual(piece);
            instance.AddPiece(piece);

            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    private void TryApplyVisual(CustomPiece piece)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            Texture2D texture = GetTexture(Black);
            Renderer[] cloths = piece.PiecePrefab.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.sharedMaterial != null && renderer.sharedMaterial.mainTexture != null
                    && renderer.sharedMaterial.mainTexture.name.StartsWith("Banner"))
                .ToArray();
            if (cloths.Length == 0)
            {
                throw new InvalidOperationException("no banner cloth found");
            }

            foreach (Renderer cloth in cloths)
            {
                MeshFilter filter = cloth.GetComponent<MeshFilter>() ?? throw new InvalidOperationException("the banner cloth has no mesh filter");
                filter.sharedMesh = GetMesh(filter.sharedMesh);
                cloth.sharedMaterial = new Material(cloth.sharedMaterial) { name = $"{PrefabName}_cloth", mainTexture = texture };
            }

            Sprite icon = VisualHelper.RenderIcon(piece.PiecePrefab);
            if (icon != null)
            {
                piece.Piece.m_icon = icon;
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: keeping the vanilla look: {ex.Message}");
        }
    }

    private static Mesh GetMesh(Mesh source)
    {
        if (meshes.TryGetValue(source, out Mesh cached) && cached != null)
        {
            return cached;
        }

        if (!source.isReadable)
        {
            throw new InvalidOperationException($"the banner mesh {source.name} is not readable");
        }

        Mesh mesh = UnityEngine.Object.Instantiate(source);
        mesh.name = "whitehilt_banner";
        Vector3[] vertices = mesh.vertices;
        Vector2[] uvs = mesh.uv;
        for (int i = 0; i < uvs.Length; i++)
        {
            uvs[i].x = 0.5f + vertices[i].z / MetresPerUv;
        }

        mesh.uv = uvs;
        meshes[source] = mesh;
        return mesh;
    }

    // The normal and large banner of a colour share one texture.
    private static Texture2D GetTexture(bool black)
    {
        if (textures.TryGetValue(black, out Texture2D cached) && cached != null)
        {
            return cached;
        }

        Color cloth = black ? blackCloth : whiteCloth;
        Color32[] pixels = new Color32[TextureSize * TextureSize];
        for (int y = 0; y < TextureSize; y++)
        {
            float v = (y + 0.5f) / TextureSize;
            for (int x = 0; x < TextureSize; x++)
            {
                float edge = HalfWidthU - Mathf.Abs((x + 0.5f) / TextureSize - 0.5f);
                bool border = (edge >= 0f && edge < BorderWidth) || (v <= TopV && v > TopV - BorderWidth);
                float grain = 0.94f + 0.06f * Mathf.PerlinNoise(x * 0.35f, y * 0.08f);
                Color color = (border ? gold : cloth) * grain;
                color.a = 1f;
                pixels[y * TextureSize + x] = color;
            }
        }

        WhiteHiltLogo.Paint(pixels, TextureSize, TextureSize, new Vector2(0.5f, LogoCentreV), Vector2.one * LogoRadius);
        Texture2D texture = VisualHelper.CreateTexture(black ? "whitehilt_banner_black" : "whitehilt_banner_white", TextureSize, TextureSize, pixels);
        textures[black] = texture;
        return texture;
    }
}
