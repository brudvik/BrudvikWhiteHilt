using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Runes;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Portals.RuneRack;

/// <summary>
/// A free-standing post with a hook plank for the portal runes. Runes on a post within <see cref="RunePortalRules.Range"/>
/// of a portal let that portal carry their metals. All runes on one post let it carry everything.
/// </summary>
public class RuneRack : IWhiteHiltCustomPiece
{
    /// <summary>
    /// Prefab name of the rune post.
    /// </summary>
    public const string PrefabName = "piece_whitehilt_runerack";

    private const string FullName = "Rune Post";
    private const string Description = "A post with iron hooks for portal runes. Raise it near the portal you travel from and hang runes on it to let metal through.";

    // Layout in metres, measured from the converted models: the plank is 7.146 units long with its hook tips
    // at these x positions, 0.613 up and 0.297 forward.
    private const float PostHeight = 1.6f;
    private const float PostSpacing = 0.8f;
    private const float PostThickness = 0.14f;
    private const float PlankLength = 1.5f;
    private const float PlankBottom = 1.3f;
    private const float PlankMeshLength = 7.146f;
    private const float HookTipHeight = 0.613f;
    private const float HookTipDepth = 0.297f;
    private const float RingDiameter = 0.12f;
    private static readonly float[] hookPositions = { -2.97f, -1.78f, -0.59f, 0.6f, 1.79f, 2.98f };

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
    /// Constructor for the RuneRack class. Registers the English text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public RuneRack(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
        Translations.AddEnglish("whitehilt_runerack_take", "Take a rune");
        Translations.AddEnglish("whitehilt_runerack_hang", "Hang a rune");
        Translations.AddEnglish("whitehilt_runerack_empty", "No runes");
        Translations.AddEnglish("whitehilt_runerack_runes", "Runes");
        Translations.AddEnglish("whitehilt_runerack_everything", "Every rune is hung: the portal can carry everything");
        Translations.AddEnglish("msg_whitehilt_rune_hung", "Rune hung");
        Translations.AddEnglish("msg_whitehilt_rune_already", "That rune already hangs here");
        Translations.AddEnglish("msg_whitehilt_runes_complete", "Every rune is hung!\nThe portal can now carry everything");
        Translations.AddEnglish("whitehilt_portal_runes", "Runes");
        Translations.AddEnglish("whitehilt_portal_everything", "Rune post: can carry everything");
    }

    /// <summary>
    /// Adds the rune post to the hammer.
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
                    new() { Item = "Wood", Amount = 6, Recover = true },
                    new() { Item = "FineWood", Amount = 4, Recover = true },
                    new() { Item = "Iron", Amount = 2, Recover = true }
                }
            };

            CustomPiece piece = new(PrefabName, "piece_table", pieceConfig);
            GameObject prefab = piece.PiecePrefab;
            prefab.AddComponent<RuneRackComponent>();
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

    // One box around the posts and plank, on the server as well, so the post blocks and can be hit like it looks.
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
            collider.center = new Vector3(0f, PostHeight / 2f, 0.05f);
            collider.size = new Vector3(PostSpacing * 2f + PostThickness, PostHeight, 0.3f);
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
            Transform root = piece.PiecePrefab.transform;
            VisualHelper.HideRenderers(piece.PiecePrefab);

            // The posts are copies of the vanilla wood pole, so they match the rest of the building.
            Transform pole = PrefabManager.Instance.GetPrefab("wood_pole2")?.transform.Find("New")
                ?? throw new InvalidOperationException("the vanilla wood pole was not found");
            float thickness = PostThickness / pole.localScale.y;
            foreach (float x in new[] { -PostSpacing, PostSpacing })
            {
                GameObject post = UnityEngine.Object.Instantiate(pole.gameObject, root);
                post.name = "post";
                post.transform.localPosition = new Vector3(x, PostHeight / 2f, 0f);
                post.transform.localScale = Vector3.Scale(pole.localScale, new Vector3(PostHeight / 2f, thickness, thickness));
            }

            Renderer wood = pole.GetComponent<Renderer>();
            Mesh plankMesh = ForagingAssets.LoadMesh("runerack");
            float scale = PlankLength / PlankMeshLength;
            Vector3 plankBase = new(0f, PlankBottom, PostThickness / 2f + plankMesh.bounds.extents.z * scale);
            Vector3 plankPivot = plankBase - new Vector3(plankMesh.bounds.center.x, plankMesh.bounds.min.y, plankMesh.bounds.center.z) * scale;
            VisualHelper.CreateModel(root, plankMesh, ForagingAssets.LoadTexture("runerack_albedo"), wood, plankPivot, Quaternion.identity, scale);

            AddRings(root, wood, plankPivot, scale);
            AddFullSetGlow(root, plankBase);
            PieceFragments.Apply(piece.PiecePrefab);

            // Show every rune for the icon, so the build menu shows what the post is for.
            Transform rings = root.Find(RuneRackComponent.RingsName);
            SetChildrenActive(rings, true);
            Sprite icon = VisualHelper.RenderIcon(piece.PiecePrefab);
            SetChildrenActive(rings, false);
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

    // One hidden ring per hook. The rack component shows a ring when its rune hangs on the post.
    private static void AddRings(Transform root, Renderer template, Vector3 plankPivot, float plankScale)
    {
        GameObject ringRoot = new(RuneRackComponent.RingsName);
        ringRoot.transform.SetParent(root, false);

        Mesh ringMesh = ForagingAssets.LoadMesh("runering");
        float ringScale = RingDiameter / Mathf.Max(ringMesh.bounds.size.x, ringMesh.bounds.size.z);
        Quaternion upright = Quaternion.Euler(90f, 0f, 0f);
        Vector3 meshCenter = upright * (ringMesh.bounds.center * ringScale);

        for (int i = 0; i < WhiteHiltRuneBase.Count; i++)
        {
            WhiteHiltRuneBase rune = WhiteHiltRuneBase.Get(i);
            if (rune?.RingTexture == null)
            {
                continue;
            }

            // The hook passes through the top of the ring, so the ring's centre hangs below the hook tip.
            Vector3 hookTip = plankPivot + new Vector3(hookPositions[i], HookTipHeight, HookTipDepth) * plankScale;
            Vector3 ringCenter = hookTip + new Vector3(0f, -RingDiameter * 0.35f, -0.02f);
            GameObject ring = VisualHelper.CreateModel(ringRoot.transform, ringMesh, rune.RingTexture, template, ringCenter - meshCenter, upright, ringScale);
            ring.name = $"rune_{i}";
            Material material = ring.GetComponent<MeshRenderer>().sharedMaterial;
            if (material.HasProperty("_EmissionMap"))
            {
                // Dark until the post is full; RuneRackComponent then lights each post's runes on its own.
                material.EnableKeyword("_EMISSION");
                material.SetTexture("_EmissionMap", rune.GlowTexture);
                material.SetColor("_EmissionColor", Color.black);
            }

            ring.SetActive(false);
        }
    }

    // A soft light that fades in while every rune hangs on the post. The runes themselves glow too, see RuneRackComponent.
    private static void AddFullSetGlow(Transform root, Vector3 plankBase)
    {
        GameObject glow = new(RuneRackComponent.FullSetEffectName);
        glow.transform.SetParent(root, false);
        glow.transform.localPosition = new Vector3(0f, plankBase.y, plankBase.z + 0.4f);

        Light light = glow.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.8f, 0.5f);
        light.range = 3f;
        light.intensity = 1.2f;
        light.shadows = LightShadows.None;

        // EffectFade starts the light at zero and fades it towards full when switched on.
        glow.AddComponent<EffectFade>().m_fadeDuration = 1.5f;
    }

    private static void SetChildrenActive(Transform parent, bool active)
    {
        if (parent == null)
        {
            return;
        }

        foreach (Transform child in parent)
        {
            child.gameObject.SetActive(active);
        }
    }
}
