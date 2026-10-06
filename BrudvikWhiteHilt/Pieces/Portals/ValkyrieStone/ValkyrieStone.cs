using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Portals.ValkyrieStone;

/// <summary>
/// A carved runestone. For one Surtling Core a valkyrie carries you to where you last fell, once per death.
/// </summary>
public class ValkyrieStone : IWhiteHiltCustomPiece
{
    /// <summary>
    /// Prefab name of the Valkyrie Stone.
    /// </summary>
    public const string PrefabName = "piece_whitehilt_valkyriestone";

    private const string FullName = "Valkyrie Stone";
    private const string Description = "A carved runestone that calls a valkyrie. For {0} she carries you to where you last fell{1}.";

    // Size in metres. The model is 0.74 wide and 0.44 deep for a height of 1, with the carved side facing +z.
    private const float StoneHeight = 1.8f;
    private const float StoneWidth = 0.74f * StoneHeight;
    private const float StoneDepth = 0.44f * StoneHeight;

    private static readonly Color glowColor = new(1f, 0.82f, 0.45f);

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
    public ValkyrieStone(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
        Translations.AddEnglish("whitehilt_valkyrie_travel", "Travel to where you fell");
        Translations.AddEnglish("whitehilt_valkyrie_cost", "1 Surtling Core");
        Translations.AddEnglish("whitehilt_valkyrie_confirm", "Let the valkyrie carry you to where you last fell?\nIt costs {0}.");
        Translations.AddEnglish("whitehilt_valkyrie_none", "You have not fallen in this world");
        Translations.AddEnglish("whitehilt_valkyrie_used", "The valkyrie has already carried you to your last fall");
        Translations.AddEnglish("msg_whitehilt_valkyrie_nocore", "The valkyrie asks for a Surtling Core");
        Translations.AddEnglish("msg_whitehilt_valkyrie_travel", "The valkyrie carries you to where you fell");
    }

    /// <summary>
    /// Adds the Valkyrie Stone to the hammer.
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
                    new() { Item = "Stone", Amount = 20, Recover = true },
                    new() { Item = "SurtlingCore", Amount = 2, Recover = true },
                    new() { Item = "WitheredBone", Amount = 5, Recover = true }
                }
            };

            // The table is a plain ground piece; it gets stone behaviour and our look below.
            CustomPiece piece = new(PrefabName, "piece_table", pieceConfig);
            GameObject prefab = piece.PiecePrefab;
            prefab.AddComponent<ValkyrieStoneComponent>();
            MakeStone(piece, StoneHeight / 2f);
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

    /// <summary>
    /// Makes a cloned piece behave like stone: stone health, hit and break effects, no fragments of the vanilla piece.
    /// </summary>
    /// <param name="piece">The cloned piece.</param>
    /// <param name="centreHeight">Height of the middle of the stone above its base.</param>
    internal static void MakeStone(CustomPiece piece, float centreHeight)
    {
        GameObject pillar = PrefabManager.Instance.GetPrefab("stone_pillar");
        piece.Piece.m_comfort = 0;

        WearNTear wearNTear = piece.PiecePrefab.GetComponent<WearNTear>();
        if (wearNTear == null)
        {
            return;
        }

        // Without this the stone would break into table fragments.
        wearNTear.m_fragmentRoots = Array.Empty<GameObject>();
        wearNTear.m_materialType = WearNTear.MaterialType.Stone;
        wearNTear.m_noRoofWear = false;
        // Stone needs 100 support; a centre of mass on the floor's surface reckons it sideways and it breaks off.
        wearNTear.m_comOffset = Vector3.up * centreHeight;
        WearNTear stone = pillar?.GetComponent<WearNTear>();
        if (stone != null)
        {
            wearNTear.m_health = stone.m_health;
            wearNTear.m_hitEffect = stone.m_hitEffect;
            wearNTear.m_destroyedEffect = stone.m_destroyedEffect;
        }

        Piece stonePiece = pillar?.GetComponent<Piece>();
        if (stonePiece != null)
        {
            piece.Piece.m_placeEffect = stonePiece.m_placeEffect;
        }
    }

    // One box around the stone, on the server as well, so it blocks and can be hit like it looks.
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
            collider.center = new Vector3(0f, StoneHeight / 2f, 0f);
            collider.size = new Vector3(StoneWidth, StoneHeight, StoneDepth);
        }
    }

    // Replaces the cloned table's look with the Valkyrie stone model, with its knotwork and runes glowing gold from the
    // model's own glow map. Should anything be missing, the vanilla look stays.
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
            // The table top's material. The first renderer of both the table and the pillar is their snow cover, which drew nothing.
            Renderer template = prefab.transform.Find("new/high")?.GetComponent<MeshRenderer>()
                ?? throw new InvalidOperationException("the table's renderer new/high was not found");

            Mesh mesh = ForagingAssets.LoadMesh("valkyriestone");
            float scale = StoneHeight / mesh.bounds.size.y;
            Vector3 pivot = -new Vector3(mesh.bounds.center.x, mesh.bounds.min.y, mesh.bounds.center.z) * scale;
            GameObject stone = VisualHelper.CreateModel(prefab.transform, mesh, ForagingAssets.LoadTexture("valkyriestone_albedo"), template, pivot, Quaternion.identity, scale);

            Material material = stone.GetComponent<MeshRenderer>().sharedMaterial;
            if (material.HasProperty("_EmissionMap"))
            {
                // The model's glow map outlines the knotwork in red and the runes in cyan; both become gold.
                Texture2D glow = VisualHelper.RecolorTexture(ForagingAssets.LoadTexture("valkyriestone_emission"), pixel =>
                {
                    float strength = Mathf.Max(pixel.r, pixel.g, pixel.b) / 255f;
                    return strength < 0.1f ? new Color32(0, 0, 0, 255) : (Color32)(glowColor * strength);
                });
                material.EnableKeyword("_EMISSION");
                material.SetTexture("_EmissionMap", glow);
                material.SetColor("_EmissionColor", Color.white * 1.5f);
            }

            AddLight(prefab.transform);

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

    private static void AddLight(Transform root)
    {
        GameObject glow = new("ValkyrieGlow");
        glow.transform.SetParent(root, false);
        glow.transform.localPosition = new Vector3(0f, StoneHeight * 0.6f, StoneDepth / 2f + 0.4f);

        Light light = glow.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = glowColor;
        light.range = 3f;
        light.intensity = 0.8f;
        light.shadows = LightShadows.None;
    }
}
