using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Farming;

/// <summary>
/// A wooden bin that turns kitchen and field waste into compost. Crops near a bin with compost in it grow faster.
/// </summary>
public class CompostBin : IWhiteHiltCustomPiece
{
    /// <summary>Prefab name of the bin.</summary>
    public const string PrefabName = "piece_whitehilt_compostbin";

    /// <summary>Prefab name of the compost item.</summary>
    public const string CompostName = "WhiteHiltCompost";

    private const string FullName = "Compost Bin";
    private const string Description = "Put kitchen and field waste in it; every few minutes five pieces turn into compost. Crops within 12 metres of a bin with compost grow 30% faster, and the bin uses one compost a day.";
    private const string CompostKey = "item_whitehiltcompost";

    // Height in metres; the model (compostbin.glb, CC BY Pants85) is 1.1 wide and 1.06 deep for a height of 1.
    private const float Height = 1.2f;
    private const float Width = 1.1f * Height;
    private const float Depth = 1.06f * Height;

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
    public CompostBin(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
        Translations.AddEnglishNameAndDescription(CompostKey, "Compost", "Rich black soil from rotted waste. Crops near a Compost Bin with compost in it grow faster.");
        Translations.AddEnglish("whitehilt_compost_fertile", "Feeding the soil within {0} m");
        Translations.AddEnglish("whitehilt_compost_waiting", "Needs {0} pieces of waste");
        CompostSettings.Initialize();
    }

    /// <summary>
    /// Adds the compost and the bin.
    /// </summary>
    public void Add()
    {
        try
        {
            AddCompost();
            PieceConfig pieceConfig = new()
            {
                Name = Translations.Token(PrefabName),
                Description = Translations.Token($"{PrefabName}_description"),
                PieceTable = PieceTables.Hammer,
                Category = PieceCategories.Misc,
                CraftingStation = CraftingStations.Workbench,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "Wood", Amount = 10, Recover = true },
                    new() { Item = "Stone", Amount = 2, Recover = true }
                }
            };

            // The wooden chest brings the inventory, health and network parts; its look and colliders are replaced.
            CustomPiece piece = new(PrefabName, "piece_chest_wood", pieceConfig);
            GameObject prefab = piece.PiecePrefab;
            Strip(prefab);
            AddCollider(prefab.transform);
            GameObject visual = new("New") { layer = prefab.layer };
            visual.transform.SetParent(prefab.transform, false);
            SetUpWearNTear(prefab, visual);

            Container container = prefab.GetComponent<Container>() ?? throw new InvalidOperationException("the chest has no Container.");
            container.m_name = Translations.Token(PrefabName);
            container.m_width = 4;
            container.m_height = 2;
            container.m_open = null;
            container.m_closed = null;
            prefab.AddComponent<CompostBinComponent>();
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

    private static void AddCompost()
    {
        CustomItem item = new(CompostName, "Coal");
        ItemDrop.ItemData.SharedData shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = Translations.Token(CompostKey);
        shared.m_description = Translations.Token($"{CompostKey}_description");
        shared.m_itemType = ItemDrop.ItemData.ItemType.Material;
        shared.m_maxStackSize = 50;
        shared.m_weight = 1f;
        shared.m_value = 0;
        shared.m_teleportable = true;
        if (!VisualHelper.IsHeadless)
        {
            VisualHelper.Recolor(item.ItemPrefab, color => new Color32(
                (byte)Mathf.Min(255, color.r + 70), (byte)Mathf.Min(255, color.g + 45), (byte)Mathf.Min(255, color.b + 20), color.a));
            Sprite icon = VisualHelper.RenderIcon(item.ItemPrefab);
            if (icon != null)
            {
                shared.m_icons = new[] { icon };
            }
        }

        ItemManager.Instance.AddItem(item);
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

    // One box around the bin, on the server as well, so it blocks, can be hit and opened like it looks.
    private static void AddCollider(Transform root)
    {
        GameObject collider = new("collider") { layer = LayerMask.NameToLayer("piece") };
        collider.transform.SetParent(root, false);
        BoxCollider box = collider.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, Height / 2f, 0f);
        box.size = new Vector3(Width, Height, Depth);
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
            // A vanilla wood material, so the bin is lit and weathered like the rest of the building.
            Renderer template = PrefabManager.Instance.GetPrefab("wood_pole2")?.transform.Find("New")?.GetComponent<Renderer>()
                ?? throw new InvalidOperationException("the vanilla wood pole was not found");

            Mesh mesh = ForagingAssets.LoadMesh("compostbin");
            float scale = Height / mesh.bounds.size.y;
            Vector3 pivot = -new Vector3(mesh.bounds.center.x, mesh.bounds.min.y, mesh.bounds.center.z) * scale;
            VisualHelper.CreateModel(visual, mesh, ForagingAssets.LoadTexture("compostbin_albedo"), template, pivot, Quaternion.identity, scale);

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
