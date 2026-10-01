using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ranching.FeedingTrough;

/// <summary>
/// A hollowed log on stones that holds food. Hungry animals nearby walk over and eat from it,
/// so food no longer has to be thrown on the ground.
/// </summary>
public class FeedingTrough : IWhiteHiltCustomPiece
{
    /// <summary>
    /// Prefab name of the feeding trough.
    /// </summary>
    public const string PrefabName = "piece_whitehilt_feedingtrough";

    private const string FullName = "Feeding Trough";
    private const string Description = "A hollowed log on stones. Fill it with food, and hungry animals within {0} metres come and eat from it.";

    // Size in metres. The model is 6.35 long and 2.36 wide for a height of 1, lying along x.
    private const float Length = 2f;
    private const float Height = Length / 6.35f;
    private const float Width = 2.36f * Height;
    private const int InventoryWidth = 4;
    private const int InventoryHeight = 2;

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
    /// Constructor for the FeedingTrough class. Registers the English text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public FeedingTrough(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
    }

    /// <summary>
    /// Adds the feeding trough to the hammer.
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
                    new() { Item = "Wood", Amount = 8, Recover = true },
                    new() { Item = "Stone", Amount = 4, Recover = true }
                }
            };

            // The wooden chest brings the inventory, health and network parts; its look and colliders are replaced.
            CustomPiece piece = new(PrefabName, "piece_chest_wood", pieceConfig);
            GameObject prefab = piece.PiecePrefab;
            Strip(prefab);
            AddCollider(prefab.transform);

            GameObject visual = new("New") { layer = prefab.layer };
            visual.transform.SetParent(prefab.transform, false);
            SetUpContainer(prefab);
            SetUpWearNTear(prefab, visual);
            prefab.AddComponent<FeedingTroughComponent>();
            TryApplyVisual(piece, visual.transform);
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

    // One box around the trough, on the server as well, so it blocks, can be hit and opened like it looks.
    private static void AddCollider(Transform root)
    {
        GameObject collider = new("collider") { layer = LayerMask.NameToLayer("piece") };
        collider.transform.SetParent(root, false);
        BoxCollider box = collider.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, Height / 2f, 0f);
        box.size = new Vector3(Length, Height, Width);
    }

    private static void SetUpContainer(GameObject prefab)
    {
        Container container = prefab.GetComponent<Container>() ?? throw new InvalidOperationException("the chest has no Container.");
        container.m_name = Translations.Token(PrefabName);
        container.m_width = InventoryWidth;
        container.m_height = InventoryHeight;
        container.m_open = null;
        container.m_closed = null;
    }

    private static void SetUpWearNTear(GameObject prefab, GameObject visual)
    {
        WearNTear wearNTear = prefab.GetComponent<WearNTear>() ?? throw new InvalidOperationException("the chest has no WearNTear.");
        wearNTear.m_new = visual;
        wearNTear.m_worn = visual;
        wearNTear.m_broken = visual;
        wearNTear.m_wet = null;
        wearNTear.m_snow = null;
        wearNTear.m_snowWorn = null;
        wearNTear.m_snowBroken = null;
        wearNTear.m_fragmentRoots = null;
    }

    private static void TryApplyVisual(CustomPiece piece, Transform visual)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            // A vanilla wood material, so the trough is lit and weathered like the rest of the building.
            Renderer template = PrefabManager.Instance.GetPrefab("wood_pole2")?.transform.Find("New")?.GetComponent<Renderer>()
                ?? throw new InvalidOperationException("the vanilla wood pole was not found");

            Mesh mesh = ForagingAssets.LoadMesh("trough");
            float scale = Length / mesh.bounds.size.x;
            Vector3 pivot = -new Vector3(mesh.bounds.center.x, mesh.bounds.min.y, mesh.bounds.center.z) * scale;
            VisualHelper.CreateModel(visual, mesh, ForagingAssets.LoadTexture("trough_albedo"), template, pivot, Quaternion.identity, scale);

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
