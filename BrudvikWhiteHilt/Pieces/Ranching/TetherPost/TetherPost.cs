using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ranching.TetherPost;

/// <summary>
/// A wooden post. Tame animals tethered to it wander around the post instead of drifting away.
/// </summary>
public class TetherPost : IWhiteHiltCustomPiece
{
    /// <summary>
    /// Prefab name of the tether post.
    /// </summary>
    public const string PrefabName = "piece_whitehilt_tetherpost";

    private const string FullName = "Tether Post";
    private const string Description = "A post to tie animals to. Use it to keep the tame animals within {0} metres wandering around it; use it again with the alternative key to set them free.";

    // Layout in mesh units of the post (1 high), measured from the converted model with x negated for Unity:
    // the trunk stands at (0.042, -0.046) and leans towards +z; at 0.75 up its front is at z 0.072.
    private const float PostHeight = 1.6f;
    private const float ColliderWidth = 0.3f;
    private const float ChainLength = 0.25f;
    private static readonly Vector3 trunkBase = new(0.042f, 0f, -0.046f);
    private static readonly Vector3 chainBase = new(0f, 0.75f - ChainLength, 0.085f);

    private readonly PieceManager instance;

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
    /// Constructor for the TetherPost class. Registers the English text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public TetherPost(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
        Translations.AddEnglish("whitehilt_tether_animals", "Tether the tame animals nearby");
        Translations.AddEnglish("whitehilt_tether_release", "Set them free");
        Translations.AddEnglish("whitehilt_tether_count", "Tethered here");
        Translations.AddEnglish("msg_whitehilt_tether_none", "No tame animals nearby");
        Translations.AddEnglish("msg_whitehilt_tethered", "animals tethered");
        Translations.AddEnglish("msg_whitehilt_released", "animals set free");
    }

    /// <summary>
    /// Adds the tether post to the hammer.
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
                    new() { Item = "Wood", Amount = 4, Recover = true },
                    new() { Item = "LeatherScraps", Amount = 2, Recover = true }
                }
            };

            // The table is a plain ground piece; it gets our post and chain below.
            CustomPiece piece = new(PrefabName, "piece_table", pieceConfig);
            piece.PiecePrefab.AddComponent<TetherPostComponent>();
            piece.Piece.m_comfort = 0;
            FitColliders(piece.PiecePrefab.transform);
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

    // One box around the post, on the server as well, so it blocks and can be hit like it looks.
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
            collider.center = new Vector3(0f, PostHeight / 2f, 0f);
            collider.size = new Vector3(ColliderWidth, PostHeight, ColliderWidth);
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
            Renderer template = prefab.transform.Find("new/high")?.GetComponent<MeshRenderer>()
                ?? throw new InvalidOperationException("the table's renderer new/high was not found");

            // The model leans; its trunk stands on the piece's origin.
            Mesh mesh = ForagingAssets.LoadMesh("tetherpost");
            float scale = PostHeight / mesh.bounds.size.y;
            GameObject post = VisualHelper.CreateModel(prefab.transform, mesh, ForagingAssets.LoadTexture("tetherpost_albedo"), template,
                -trunkBase * scale, Quaternion.identity, scale);

            Texture2D chains = VisualHelper.RecolorTexture(ForagingAssets.LoadTexture("chains_albedo"), _ => new Color32(95, 95, 100, 255));
            VisualHelper.AddMesh(post, ForagingAssets.LoadMesh("chains"), chains, chainBase, ChainLength, Quaternion.Euler(0f, 90f, 0f));

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
