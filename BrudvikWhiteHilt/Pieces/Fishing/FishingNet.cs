using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Fishing;

/// <summary>
/// A 10 m fishing net set out on the water between two stakes, tied to a Net Winch on the shore.
/// </summary>
public class FishingNet : IWhiteHiltCustomPiece
{
    /// <summary>Prefab name of the net.</summary>
    public const string PrefabName = "piece_whitehilt_fishingnet";

    /// <summary>Length of the net in metres, along the piece's x axis.</summary>
    public const float NetLength = 10f;

    /// <summary>How far outside the net ends the stakes stand.</summary>
    public const float StakeOffset = 0.12f;

    /// <summary>How far the stakes reach above the water.</summary>
    public const float StakeTop = 1f;

    /// <summary>Name of the visual child, which the component keeps at the water line.</summary>
    public const string VisualName = "New";

    /// <summary>Name of the net model under the visual child, which rides the waves.</summary>
    public const string NetName = "net";

    /// <summary>Name of the hover collider child, which the component keeps at the water line.</summary>
    public const string ColliderName = "collider";

    private const string FullName = "Shore Net";
    private const string Description = "A net to set out on the water, at most {0} metres from a Net Winch on the shore. It catches where the water is at least {1} metres deep, and the fish end up in the winch's barrel.";

    // fishnet.glb (made by AssetSource/Tools/make_fishing_net.py) spans 2.65 m from the stones at -2.52 to the floats.
    private const float ModelHeight = 2.65f;
    private const float ModelBottom = -2.52f;
    private const float StakeDepth = 3.2f;
    private const float StakeThickness = 0.16f;

    // Vanilla places a water piece 3 m above the water; the parts start there and are moved to the water line.
    private const float PlacementLift = 3f;
    private const int ExtraPlacementDistance = 10;

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
    /// Registers the English text and binds the config.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public FishingNet(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
        Translations.AddEnglish("whitehilt_net_nowinch", "No Net Winch within {0} m");
        Translations.AddEnglish("whitehilt_net_spare", "The winch already has all the nets it can take");
        Translations.AddEnglish("whitehilt_net_shallow", "The water is too shallow here");
        Translations.AddEnglish("whitehilt_net_catching", "Catching: {0}%");
        Translations.AddEnglish("msg_whitehilt_net_nowinch", "Needs a Net Winch within {0} m");
        Translations.AddEnglish("msg_whitehilt_net_winchfull", "The Net Winch has no room for another net");
        Translations.AddEnglish("msg_whitehilt_net_shallow", "The water is too shallow for a net here");
        FishingNetSettings.Initialize();
    }

    /// <summary>
    /// The vanilla wood pole's renderer, whose material the winch, the net and the rope copy.
    /// </summary>
    /// <returns>The renderer, or null.</returns>
    public static Renderer WoodTemplate()
    {
        return PrefabManager.Instance.GetPrefab("wood_pole2")?.transform.Find("New")?.GetComponent<Renderer>();
    }

    /// <summary>
    /// Adds the net to the hammer.
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
                    new() { Item = "LeatherScraps", Amount = 12, Recover = true },
                    new() { Item = "Resin", Amount = 4, Recover = true },
                    new() { Item = "Stone", Amount = 6, Recover = true }
                }
            };

            // The pole brings the health and network parts; its look and colliders are replaced.
            CustomPiece piece = new(PrefabName, "wood_pole2", pieceConfig);
            GameObject prefab = piece.PiecePrefab;
            Strip(prefab);
            AddCollider(prefab.transform);
            GameObject visual = new(VisualName) { layer = prefab.layer };
            visual.transform.SetParent(prefab.transform, false);
            visual.transform.localPosition = new Vector3(0f, -PlacementLift, 0f);
            GameObject net = new(NetName) { layer = prefab.layer };
            net.transform.SetParent(visual.transform, false);
            SetUpPiece(piece.Piece);
            SetUpWearNTear(prefab, visual);
            prefab.AddComponent<FishingNetComponent>();
            TryApplyVisual(piece, visual.transform, net.transform);

            instance.AddPiece(piece);
            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    private static void Strip(GameObject prefab)
    {
        foreach (Transform child in prefab.transform.Cast<Transform>().ToList())
        {
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        foreach (Component component in prefab.GetComponents<Collider>().Cast<Component>().Concat(prefab.GetComponents<LODGroup>()))
        {
            UnityEngine.Object.DestroyImmediate(component);
        }
    }

    // A thin box along the floats, so the net can be hovered and hit, while boats and swimmers pass through it.
    private static void AddCollider(Transform root)
    {
        GameObject collider = new(ColliderName) { layer = LayerMask.NameToLayer("piece_nonsolid") };
        collider.transform.SetParent(root, false);
        collider.transform.localPosition = new Vector3(0f, -PlacementLift, 0f);
        BoxCollider box = collider.AddComponent<BoxCollider>();
        box.size = new Vector3(NetLength + 2f * StakeOffset, 0.6f, 0.6f);
    }

    private static void SetUpPiece(Piece piece)
    {
        piece.m_waterPiece = true;
        piece.m_noInWater = false;
        piece.m_groundOnly = false;
        piece.m_groundPiece = false;
        piece.m_clipEverything = true;
        piece.m_extraPlacementDistance = ExtraPlacementDistance;
        piece.m_comfort = 0;
    }

    // Nothing holds the net up but the water: it needs no support, and wet weather does not wear it.
    private static void SetUpWearNTear(GameObject prefab, GameObject visual)
    {
        WearNTear wearNTear = prefab.GetComponent<WearNTear>() ?? throw new InvalidOperationException("the pole has no WearNTear.");
        wearNTear.m_new = visual;
        wearNTear.m_worn = visual;
        wearNTear.m_broken = visual;
        wearNTear.m_wet = null;
        wearNTear.m_snow = null;
        wearNTear.m_snowWorn = null;
        wearNTear.m_snowBroken = null;
        wearNTear.m_fragmentRoots = null;
        wearNTear.m_noSupportWear = false;
        wearNTear.m_noRoofWear = false;
        wearNTear.m_supports = false;
    }

    private static void TryApplyVisual(CustomPiece piece, Transform visual, Transform net)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            Renderer template = WoodTemplate() ?? throw new InvalidOperationException("the vanilla wood pole was not found");
            Mesh mesh = ForagingAssets.LoadMesh("fishnet");
            float scale = ModelHeight / mesh.bounds.size.y;
            Vector3 pivot = -new Vector3(mesh.bounds.center.x, mesh.bounds.min.y, mesh.bounds.center.z) * scale + new Vector3(0f, ModelBottom, 0f);
            VisualHelper.CreateModel(net, mesh, ForagingAssets.LoadTexture("fishnet_albedo"), template, pivot, Quaternion.identity, scale);

            MeshFilter pole = template.GetComponent<MeshFilter>();
            foreach (float side in new[] { -1f, 1f })
            {
                GameObject stake = new("stake") { layer = template.gameObject.layer };
                stake.transform.SetParent(visual, false);
                stake.transform.localPosition = new Vector3(side * (NetLength / 2f + StakeOffset), (StakeTop - StakeDepth) / 2f, 0f);

                // The vanilla pole is a unit cube turned upright, its length along local x.
                stake.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                stake.transform.localScale = new Vector3(StakeTop + StakeDepth, StakeThickness, StakeThickness);
                stake.AddComponent<MeshFilter>().sharedMesh = pole.sharedMesh;
                MeshRenderer renderer = stake.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = template.sharedMaterials;
                renderer.shadowCastingMode = template.shadowCastingMode;
            }

            Sprite icon = VisualHelper.RenderIcon(piece.PiecePrefab);
            if (icon != null)
            {
                piece.Piece.m_icon = icon;
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: no custom look: {ex.Message}");
        }
    }
}
