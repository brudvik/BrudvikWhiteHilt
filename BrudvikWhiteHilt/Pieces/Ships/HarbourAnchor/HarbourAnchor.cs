using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Portals.PortalMap;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships.HarbourAnchor;

/// <summary>
/// A map table extension: a standing iron anchor. While one stands within <see cref="PortalMapService.ActivationRange"/>
/// of a map table, every ship shows on everyone's map.
/// </summary>
public class HarbourAnchor : IWhiteHiltCustomPiece
{
    /// <summary>
    /// Prefab name of the Harbour Anchor.
    /// </summary>
    public const string PrefabName = "piece_whitehilt_harbouranchor";

    private const string FullName = "Harbour Anchor";
    private const string Description = "A heavy iron anchor that remembers where every ship lies. Place it next to a map table, and every ship shows on the map for everyone.";

    // Size in metres. The model is 0.70 wide and 0.16 deep for a height of 1, with its broad side facing +z.
    private const float Height = 1.8f;
    private const float Width = 0.70f * Height;
    private const float Depth = 0.16f * Height;

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
    /// Constructor for the HarbourAnchor class. Registers the English text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public HarbourAnchor(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
        Translations.AddEnglish("whitehilt_anchor_active", "Every ship shows on the map");
        Translations.AddEnglish("whitehilt_shipmap_ship", "Ship");
        Translations.AddEnglish("whitehilt_shipmap_builder", "Built by");
    }

    /// <summary>
    /// Adds the Harbour Anchor to the hammer.
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
                    new() { Item = "Iron", Amount = 2, Recover = true },
                    new() { Item = "Chain", Amount = 2, Recover = true },
                    new() { Item = "FineWood", Amount = 4, Recover = true }
                }
            };

            // The table is a plain ground piece; it gets our look below.
            CustomPiece piece = new(PrefabName, "piece_table", pieceConfig);
            GameObject prefab = piece.PiecePrefab;
            prefab.AddComponent<MapTableExtensionComponent>().ActiveToken = "$whitehilt_anchor_active";
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

    // One box around the anchor, on the server as well, so it blocks and can be hit like it looks.
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

            Mesh mesh = ForagingAssets.LoadMesh("shipanchor");
            float scale = Height / mesh.bounds.size.y;
            Vector3 pivot = -new Vector3(mesh.bounds.center.x, mesh.bounds.min.y, mesh.bounds.center.z) * scale;
            VisualHelper.CreateModel(prefab.transform, mesh, ForagingAssets.LoadTexture("shipanchor_albedo"), template, pivot, Quaternion.identity, scale);

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
}
