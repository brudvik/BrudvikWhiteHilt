using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Smithing.RepairAnvil;

/// <summary>
/// An anvil on a stump that repairs everything the player wears in one go, free unless the server sets a cost.
/// </summary>
public class RepairAnvil : IWhiteHiltCustomPiece
{
    /// <summary>
    /// Prefab name of the repair anvil.
    /// </summary>
    public const string PrefabName = "piece_whitehilt_repairanvil";

    private const string FullName = "Repair Anvil";
    private const string Description = "An anvil on a stump. Use it to repair everything you wear at once, whatever station made it.";
    private const string ModelName = "repairanvil";

    // Height in metres; the footprint follows the model.
    private const float Height = 1f;

    private readonly PieceManager instance;

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.BlackForest;

    /// <inheritdoc/>
    public string Id => PrefabName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(PrefabName);

    /// <inheritdoc/>
    public string GatedPrefabName => PrefabName;

    /// <summary>
    /// Constructor for the RepairAnvil class. Registers the English text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public RepairAnvil(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
    }

    /// <summary>
    /// Adds the repair anvil to the hammer.
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
                Category = PieceCategories.Crafting,
                CraftingStation = CraftingStations.Forge,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "Bronze", Amount = 5, Recover = true },
                    new() { Item = "Wood", Amount = 4, Recover = true }
                }
            };

            // The wooden chest brings the health and network parts; its inventory, look and colliders are removed.
            CustomPiece piece = new(PrefabName, "piece_chest_wood", pieceConfig);
            GameObject prefab = piece.PiecePrefab;
            Strip(prefab);

            GameObject visual = new("New") { layer = prefab.layer };
            visual.transform.SetParent(prefab.transform, false);
            SetUpWearNTear(prefab, visual);
            prefab.AddComponent<RepairAnvilComponent>();
            Vector2 footprint = TryApplyVisual(piece, visual.transform);
            AddCollider(prefab.transform, footprint);
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

        foreach (Component component in prefab.GetComponents<Collider>().Cast<Component>()
            .Concat(prefab.GetComponents<LODGroup>()).Concat(prefab.GetComponents<Container>()))
        {
            UnityEngine.Object.DestroyImmediate(component);
        }
    }

    // One box around the anvil and stump, on the server as well, so it blocks, can be hit and used like it looks.
    private static void AddCollider(Transform root, Vector2 footprint)
    {
        GameObject collider = new("collider") { layer = LayerMask.NameToLayer("piece") };
        collider.transform.SetParent(root, false);
        BoxCollider box = collider.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, Height / 2f, 0f);
        box.size = new Vector3(footprint.x, Height, footprint.y);
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
        wearNTear.m_materialType = WearNTear.MaterialType.Iron;
    }

    // Returns the model's footprint in metres (x, z); a server or a failed look keeps an anvil-sized box.
    private static Vector2 TryApplyVisual(CustomPiece piece, Transform visual)
    {
        Vector2 fallback = new(0.8f, 0.6f);
        if (VisualHelper.IsHeadless)
        {
            return fallback;
        }

        try
        {
            // A vanilla material, so the anvil is lit like the rest of the building; the texture is the model's own.
            Renderer template = PrefabManager.Instance.GetPrefab("wood_pole2")?.transform.Find("New")?.GetComponent<Renderer>()
                ?? throw new InvalidOperationException("the vanilla wood pole was not found");

            Mesh mesh = ForagingAssets.LoadMesh(ModelName);
            float scale = Height / mesh.bounds.size.y;
            Vector3 pivot = -new Vector3(mesh.bounds.center.x, mesh.bounds.min.y, mesh.bounds.center.z) * scale;
            VisualHelper.CreateModel(visual, mesh, ForagingAssets.LoadTexture($"{ModelName}_albedo"), template, pivot, Quaternion.identity, scale);

            Sprite icon = VisualHelper.RenderIcon(piece.PiecePrefab);
            if (icon != null)
            {
                piece.Piece.m_icon = icon;
            }

            return new Vector2(mesh.bounds.size.x, mesh.bounds.size.z) * scale;
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: no custom look: {ex.Message}");
            return fallback;
        }
    }
}
