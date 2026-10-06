using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Waste;

/// <summary>
/// A stone well to throw rubbish into: what is put in is gone a few seconds after it is closed. Switched on, it also
/// collects items that have lain on the ground nearby for a while.
/// </summary>
public class WasteWell : IWhiteHiltCustomPiece
{
    /// <summary>
    /// Prefab name of the waste well.
    /// </summary>
    public const string PrefabName = "piece_whitehilt_wastewell";

    private const string FullName = "Waste Well";
    private const string Description = "A deep stone well for rubbish. What you throw in is gone a few seconds after you close it. Shift + Use makes it collect items that have lain on the ground nearby for a while.";
    private const string ModelName = "wastewell";

    // Footprint of the model in metres; the height follows the model (the Stone Well is about 1.35 times as high).
    private const float Diameter = 2f;
    private const int InventoryWidth = 6;
    private const int InventoryHeight = 3;

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
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run. Registers the English text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public WasteWell(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
    }

    /// <summary>
    /// Adds the waste well to the hammer.
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
                    new() { Item = "Wood", Amount = 6, Recover = true }
                }
            };

            // The wooden chest brings the inventory, health and network parts; its look and colliders are replaced.
            CustomPiece piece = new(PrefabName, "piece_chest_wood", pieceConfig);
            GameObject prefab = piece.PiecePrefab;
            Strip(prefab);

            GameObject visual = new("New") { layer = prefab.layer };
            visual.transform.SetParent(prefab.transform, false);
            SetUpContainer(prefab);
            SetUpWearNTear(prefab, visual);
            prefab.AddComponent<WasteWellComponent>();
            float height = TryApplyVisual(piece, visual.transform);
            AddCollider(prefab.transform, height);
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

    // One box around the stone ring and posts (the roof overhangs it), on the server as well, so it blocks, can be hit
    // and opened like it looks.
    private static void AddCollider(Transform root, float height)
    {
        GameObject collider = new("collider") { layer = LayerMask.NameToLayer("piece") };
        collider.transform.SetParent(root, false);
        BoxCollider box = collider.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, height / 2f, 0f);
        box.size = new Vector3(Diameter * 0.8f, height, Diameter * 0.8f);
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
        wearNTear.m_materialType = WearNTear.MaterialType.Stone;
    }

    // Returns the model's height in metres; a server or a failed look keeps a well-sized box.
    private static float TryApplyVisual(CustomPiece piece, Transform visual)
    {
        const float fallbackHeight = 2.7f;
        if (VisualHelper.IsHeadless)
        {
            return fallbackHeight;
        }

        try
        {
            // A vanilla stone material, so the well is lit and weathered like the rest of the building.
            Renderer template = PrefabManager.Instance.GetPrefab("stone_wall_1x1")?.transform.Find("new")?.GetComponentInChildren<Renderer>()
                ?? PrefabManager.Instance.GetPrefab("wood_pole2")?.transform.Find("New")?.GetComponent<Renderer>()
                ?? throw new InvalidOperationException("no vanilla template material was found");

            Mesh mesh = ForagingAssets.LoadMesh(ModelName);
            float scale = Diameter / Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.z);
            Vector3 pivot = -new Vector3(mesh.bounds.center.x, mesh.bounds.min.y, mesh.bounds.center.z) * scale;
            VisualHelper.CreateModel(visual, mesh, ForagingAssets.LoadTexture($"{ModelName}_albedo"), template, pivot, Quaternion.identity, scale);

            Sprite icon = VisualHelper.RenderIcon(piece.PiecePrefab);
            if (icon != null)
            {
                piece.Piece.m_icon = icon;
            }

            return mesh.bounds.size.y * scale;
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: no custom look: {ex.Message}");
            return fallbackHeight;
        }
    }
}
