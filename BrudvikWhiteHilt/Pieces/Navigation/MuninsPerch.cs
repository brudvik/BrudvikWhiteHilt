using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Portals.PortalMap;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Navigation;

/// <summary>
/// A map table extension: a carved post with a raven on top. While one stands within
/// <see cref="PortalMapService.ActivationRange"/> of a map table, the caves, settlements, plants, resources and
/// landmarks uncovered on that table's map can be shown on everyone's map (see Navigation/Discoveries).
/// </summary>
public class MuninsPerch : IWhiteHiltCustomPiece
{
    /// <summary>
    /// Prefab name of Munin's Perch.
    /// </summary>
    public const string PrefabName = "piece_whitehilt_muninsperch";

    private const string FullName = "Munin's Perch";
    private const string Description = "A carved post where Odin's raven of memory rests. Place it next to a map table, and what everyone has found on that table's map can show on your own: caves, settlements, berries, resources and landmarks. Pick what to show on the large map.";

    // The post is 2.2 m tall, 0.5 wide and 0.46 deep; the raven, 0.44 m tall, stands on its rounded top.
    private const float Height = 2.2f;
    private const float Width = 0.5f;
    private const float Depth = 0.46f;
    private const float RavenBase = 0.985f;
    private const float RavenHeight = 0.2f;
    private const float RavenYaw = 60f;
    private const float ColliderHeight = Height + RavenHeight * Height;

    private static readonly Color eyeGlow = new(1f, 0.78f, 0.35f);

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
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run. Registers the English text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public MuninsPerch(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
        Translations.AddEnglish("whitehilt_perch_active", "Munin remembers what is on this map table");
    }

    /// <summary>
    /// Adds Munin's Perch to the hammer.
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
                    new() { Item = "FineWood", Amount = 8, Recover = true },
                    new() { Item = "Iron", Amount = 2, Recover = true },
                    new() { Item = "Feathers", Amount = 6, Recover = true }
                }
            };

            // The table is a plain ground piece; it gets our look below.
            CustomPiece piece = new(PrefabName, "piece_table", pieceConfig);
            GameObject prefab = piece.PiecePrefab;
            prefab.AddComponent<MapTableExtensionComponent>().ActiveToken = "$whitehilt_perch_active";
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

    // One box around the post and the raven, on the server as well, so it blocks and can be hit like it looks.
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
            collider.center = new Vector3(0f, ColliderHeight / 2f, 0f);
            collider.size = new Vector3(Width, ColliderHeight, Depth);
        }
    }

    // Builds the perch's look: the post model with the raven on top, its eyes recoloured to glow gold. Should anything
    // be missing, the vanilla look stays.
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

            Mesh post = ForagingAssets.LoadMesh("muninpost");
            float scale = Height / post.bounds.size.y;
            Vector3 pivot = -new Vector3(post.bounds.center.x, post.bounds.min.y, post.bounds.center.z) * scale;
            GameObject model = VisualHelper.CreateModel(prefab.transform, post, ForagingAssets.LoadTexture("muninpost_albedo"), template, pivot,
                Quaternion.identity, scale);

            GameObject raven = VisualHelper.AddMesh(model, ForagingAssets.LoadMesh("muninraven"), ForagingAssets.LoadTexture("muninraven_albedo"),
                new Vector3(0f, RavenBase, 0f), RavenHeight, Quaternion.Euler(0f, RavenYaw, 0f));
            Material material = raven.GetComponent<MeshRenderer>().sharedMaterial;
            Texture2D eyes = ForagingAssets.LoadTexture("muninraven_emission");
            if (eyes != null && material.HasProperty("_EmissionMap"))
            {
                // The model's red eyes glow gold instead.
                material.EnableKeyword("_EMISSION");
                material.SetTexture("_EmissionMap", VisualHelper.RecolorTexture(eyes, pixel =>
                {
                    float value = Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b)) / 255f;
                    return (Color32)(eyeGlow * value);
                }));
                material.SetColor("_EmissionColor", Color.white * 1.5f);
            }

            PieceFragments.Apply(prefab);
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
