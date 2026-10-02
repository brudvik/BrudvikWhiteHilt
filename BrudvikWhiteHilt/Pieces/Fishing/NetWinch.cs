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
/// A winch and a fish barrel on the shore. Shore Nets set out in the water nearby fill the barrel with fish.
/// </summary>
public class NetWinch : IWhiteHiltCustomPiece
{
    /// <summary>Prefab name of the winch.</summary>
    public const string PrefabName = "piece_whitehilt_netwinch";

    /// <summary>Slots in each row of the barrel; fixed, since saved items outside the width are lost on loading.</summary>
    public const int BarrelWidth = 4;

    /// <summary>Where the rope leaves the winch drum, in piece space.</summary>
    public static readonly Vector3 RopePoint = new(WinchX, 0.86f, 0.12f);

    private const string FullName = "Net Winch";
    private const string Description = "A winch and a fish barrel for the shore. Set out up to {0} Shore Nets within {1} metres, and the fish they catch end up in the barrel. Put bait in the barrel to catch faster.";

    // Models (fishwinch.glb CC BY Max Wittig, fishbarrel.glb CC BY Lakin) side by side; the winch drum runs along x.
    private const float WinchX = 0.55f;
    private const float WinchHeight = 1.1f;
    private const float BarrelX = -0.55f;
    private const float BarrelHeight = 0.8f;
    private const float ColliderWidth = 2.1f;
    private const float ColliderDepth = 0.9f;

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
    public NetWinch(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
        Translations.AddEnglish("whitehilt_netwinch_nonets", "No Shore Net within {0} m");
        Translations.AddEnglish("whitehilt_netwinch_nets", "Nets: {0}/{1}, about {2} fish an hour");
        Translations.AddEnglish("whitehilt_netwinch_bait", "Bait: {0}, the nets catch faster");
        Translations.AddEnglish("whitehilt_netwinch_full", "The barrel is full");
        Translations.AddEnglish("whitehilt_netwinch_torn", "The nets are torn and catch nothing");
        Translations.AddEnglish("whitehilt_netwinch_wear", "Net wear: {0}/{1} fish");
        Translations.AddEnglish("whitehilt_netwinch_mend", "Mend the nets");
        Translations.AddEnglish("whitehilt_netwinch_mend_cost", "Mend the nets ({0})");
        Translations.AddEnglish("whitehilt_netwinch_or", " or ");
        Translations.AddEnglish("msg_whitehilt_netwinch_whole", "The nets are whole");
        Translations.AddEnglish("msg_whitehilt_netwinch_need", "You need {0} to mend the nets");
        Translations.AddEnglish("msg_whitehilt_netwinch_mended", "The nets are mended");
        FishingNetSettings.Initialize();
    }

    /// <summary>
    /// Adds the winch to the hammer.
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
                    new() { Item = "Wood", Amount = 10, Recover = true },
                    new() { Item = "FineWood", Amount = 4, Recover = true },
                    new() { Item = "Bronze", Amount = 2, Recover = true }
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
            container.m_width = BarrelWidth;
            container.m_height = FishingNetSettings.BarrelRows.Value;
            container.m_open = null;
            container.m_closed = null;
            prefab.AddComponent<NetWinchComponent>();
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

    // One box around winch and barrel, on the server as well, so it blocks, can be hit and opened like it looks.
    private static void AddCollider(Transform root)
    {
        GameObject collider = new("collider") { layer = LayerMask.NameToLayer("piece") };
        collider.transform.SetParent(root, false);
        BoxCollider box = collider.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, WinchHeight / 2f, 0f);
        box.size = new Vector3(ColliderWidth, WinchHeight, ColliderDepth);
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
            Renderer template = FishingNet.WoodTemplate() ?? throw new InvalidOperationException("the vanilla wood pole was not found");
            AddModel(visual, "fishbarrel", template, new Vector3(BarrelX, 0f, 0f), Quaternion.identity, BarrelHeight);
            AddModel(visual, "fishwinch", template, new Vector3(WinchX, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), WinchHeight);

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

    // Stands the model on its base at the position, centred, scaled to the height.
    private static void AddModel(Transform parent, string name, Renderer template, Vector3 position, Quaternion rotation, float height)
    {
        Mesh mesh = ForagingAssets.LoadMesh(name);
        float scale = height / mesh.bounds.size.y;
        Vector3 pivot = -new Vector3(mesh.bounds.center.x, mesh.bounds.min.y, mesh.bounds.center.z) * scale;
        VisualHelper.CreateModel(parent, mesh, ForagingAssets.LoadTexture($"{name}_albedo"), template, position + rotation * pivot, rotation, scale);
    }
}
