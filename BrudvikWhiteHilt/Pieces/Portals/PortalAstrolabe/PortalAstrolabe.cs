using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Portals.PortalMap;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Portals.PortalAstrolabe;

/// <summary>
/// A map table extension: a floating armillary with an amethyst heart. While one stands within
/// <see cref="PortalMapService.ActivationRange"/> of a map table, every portal shows on everyone's map.
/// </summary>
public class PortalAstrolabe : IWhiteHiltCustomPiece
{
    /// <summary>
    /// Prefab name of the Portal Astrolabe.
    /// </summary>
    public const string PrefabName = "piece_whitehilt_portalastrolabe";

    private const string FullName = "Portal Astrolabe";
    private const string Description = "A floating armillary around an amethyst that senses every portal. Place it next to a map table, and every portal shows on the map for everyone, with the runes it carries.";

    // Size in metres. The model is 0.70 wide and 0.76 deep for a height of 1.
    private const float Height = 1.4f;
    private const float Width = 0.70f * Height;
    private const float Depth = 0.76f * Height;

    private static readonly Color glowColor = new(0.7f, 0.45f, 1f);

    private readonly PieceManager instance;

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Swamp;

    /// <inheritdoc/>
    public string Id => PrefabName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(PrefabName);

    /// <inheritdoc/>
    public string GatedPrefabName => PrefabName;

    /// <summary>
    /// Constructor for the PortalAstrolabe class. Registers the English text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public PortalAstrolabe(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
        Translations.AddEnglish("whitehilt_astrolabe_active", "Every portal shows on the map");
        Translations.AddEnglish("whitehilt_mapextension_inactive", "Place it within {0} m of a map table");
        Translations.AddEnglish("whitehilt_portalmap_unnamed", "Unnamed portal");
        Translations.AddEnglish("whitehilt_portalmap_private", "private");
        Translations.AddEnglish("whitehilt_portalmap_guild", "guild");
        Translations.AddEnglish("whitehilt_portalmap_group", "group");
    }

    /// <summary>
    /// Adds the Portal Astrolabe to the hammer.
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
                Category = PieceCategories.Misc,
                CraftingStation = CraftingStations.Workbench,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "FineWood", Amount = 6, Recover = true },
                    new() { Item = "Bronze", Amount = 4, Recover = true },
                    new() { Item = "SurtlingCore", Amount = 1, Recover = true }
                }
            };

            // The table is a plain ground piece; it gets our look below.
            CustomPiece piece = new(PrefabName, "piece_table", pieceConfig);
            GameObject prefab = piece.PiecePrefab;
            prefab.AddComponent<MapTableExtensionComponent>().ActiveToken = "$whitehilt_astrolabe_active";
            piece.Piece.m_comfort = 0;
            WearNTear wearNTear = prefab.GetComponent<WearNTear>();
            if (wearNTear != null)
            {
                // Without this it would break into table fragments.
                wearNTear.m_fragmentRoots = Array.Empty<GameObject>();
            }

            FitColliders(prefab.transform);
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

    // One box around the armillary, on the server as well, so it blocks and can be hit like it looks.
    private static void FitColliders(Transform root)
    {
        foreach (BoxCollider collider in root.GetComponentsInChildren<BoxCollider>(true))
        {
            if (collider.transform.parent != root)
            {
                continue;
            }

            collider.transform.localPosition = Vector3.zero;
            collider.transform.localRotation = Quaternion.identity;
            collider.transform.localScale = Vector3.one;
            collider.center = new Vector3(0f, Height / 2f, 0f);
            collider.size = new Vector3(Width, Height, Depth);
        }
    }

    private static void TryApplyVisual(CustomPiece piece)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            GameObject prefab = piece.PiecePrefab;
            VisualHelper.HideRenderers(prefab);
            // The table top's material; the first renderer is the table's snow cover, which draws nothing.
            Renderer template = prefab.transform.Find("new/high")?.GetComponent<MeshRenderer>()
                ?? throw new InvalidOperationException("the table's renderer new/high was not found");

            Mesh mesh = ForagingAssets.LoadMesh("portalastrolabe");
            Texture2D albedo = ForagingAssets.LoadTexture("portalastrolabe_albedo");
            float scale = Height / mesh.bounds.size.y;
            Vector3 pivot = -new Vector3(mesh.bounds.center.x, mesh.bounds.min.y, mesh.bounds.center.z) * scale;
            GameObject model = VisualHelper.CreateModel(prefab.transform, mesh, albedo, template, pivot, Quaternion.identity, scale);

            Material material = model.GetComponent<MeshRenderer>().sharedMaterial;
            if (material.HasProperty("_EmissionMap"))
            {
                // Only the amethyst glows: the clearly purple pixels of the albedo.
                Texture2D glow = VisualHelper.RecolorTexture(albedo, pixel =>
                {
                    Color.RGBToHSV(pixel, out float hue, out float saturation, out float value);
                    return hue > 0.68f && hue < 0.86f && saturation > 0.35f ? pixel : new Color32(0, 0, 0, 255);
                });
                material.EnableKeyword("_EMISSION");
                material.SetTexture("_EmissionMap", glow);
                material.SetColor("_EmissionColor", Color.white * 1.5f);
            }

            AddLight(prefab.transform);

            Sprite icon = VisualHelper.RenderIcon(prefab);
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

    private static void AddLight(Transform root)
    {
        GameObject glow = new("AmethystGlow");
        glow.transform.SetParent(root, false);
        glow.transform.localPosition = new Vector3(0f, Height * 0.6f, 0f);

        Light light = glow.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = glowColor;
        light.range = 3f;
        light.intensity = 0.8f;
        light.shadows = LightShadows.None;
    }
}
