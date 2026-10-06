using BrudvikWhiteHilt.Chests.Collection;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Storage;

/// <summary>A carved collection post with a wicker basket that sorts loose and dropped-in items into White Hilt chests.</summary>
public sealed class CollectionPost : IWhiteHiltCustomPiece
{
    // Fits a whole player inventory; never shrink it, saved items outside the grid would be lost.
    private const int BasketWidth = 8;
    private const int BasketHeight = 4;

    private readonly PieceManager manager;

    /// <inheritdoc/>
    public bool Enabled => true;
    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.BlackForest;
    /// <inheritdoc/>
    public string Id => "piece_whitehilt_collectionpost";
    /// <inheritdoc/>
    public string DisplayName => "Collection Post";
    /// <inheritdoc/>
    public string NameToken => Translations.Token(Id);
    /// <inheritdoc/>
    public string GatedPrefabName => Id;

    /// <summary>Registers the name and description.</summary>
    /// <param name="manager">The piece manager.</param>
    public CollectionPost(PieceManager manager)
    {
        this.manager = manager;
        Translations.AddEnglishNameAndDescription(Id, DisplayName, "A carved post and wicker basket. Gathers loose items nearby into White Hilt chests, sorted by kind, with the Everlasting Chest taking the rest. Put items in the basket and they are sorted into the chests the same way.");
    }

    /// <inheritdoc/>
    public void Add()
    {
        try
        {
            // The wooden chest brings the basket inventory, health and network parts; its look and colliders are replaced.
            var piece = new CustomPiece(Id, "piece_chest_wood", new PieceConfig
            {
                Name = NameToken,
                Description = Translations.Token(Id + "_description"),
                PieceTable = PieceTables.Hammer,
                Category = "Chests",
                CraftingStation = CraftingStations.Workbench,
                Requirements = new[]
                {
                    new RequirementConfig { Item = "FineWood", Amount = 10, Recover = true },
                    new RequirementConfig { Item = "Bronze", Amount = 5, Recover = true },
                    new RequirementConfig { Item = "SurtlingCore", Amount = 2, Recover = true }
                }
            });
            var prefab = piece.PiecePrefab;
            foreach (Transform child in prefab.transform.Cast<Transform>().ToList()) UnityEngine.Object.DestroyImmediate(child.gameObject);
            foreach (Component component in prefab.GetComponents<Collider>().Cast<Component>().Concat(prefab.GetComponents<LODGroup>()))
                UnityEngine.Object.DestroyImmediate(component);

            var visual = new GameObject("New") { layer = prefab.layer };
            visual.transform.SetParent(prefab.transform, false);
            var container = prefab.GetComponent<Container>();
            container.m_name = NameToken;
            container.m_width = BasketWidth;
            container.m_height = BasketHeight;
            container.m_open = null;
            container.m_closed = null;
            var wear = prefab.GetComponent<WearNTear>();
            wear.m_new = visual;
            wear.m_worn = visual;
            wear.m_broken = visual;
            wear.m_wet = null;
            wear.m_snow = null;
            wear.m_snowWorn = null;
            wear.m_snowBroken = null;
            wear.m_fragmentRoots = null;

            AddBox(prefab.transform, new Vector3(0f, 1.1f, 0f), new Vector3(0.5f, 2.2f, 0.46f));
            AddBox(prefab.transform, new Vector3(0f, 0.08f, 0.65f), new Vector3(0.94f, 0.16f, 0.744f));
            prefab.AddComponent<CollectionPostComponent>();
            if (!VisualHelper.IsHeadless)
            {
                var workbench = PrefabManager.Instance.GetPrefab("piece_workbench").GetComponent<CraftingStation>();
                var marker = UnityEngine.Object.Instantiate(workbench.m_areaMarker, prefab.transform);
                marker.name = "CollectionArea";
                marker.SetActive(false);
            }
            ApplyVisual(piece, visual.transform);
            manager.AddPiece(piece);
            Jotunn.Logger.LogInfo("Collection Post added!");
        }
        catch (Exception exception) { Jotunn.Logger.LogError("Collection Post failed to load!"); Jotunn.Logger.LogError(exception); }
    }

    private static void AddBox(Transform root, Vector3 center, Vector3 size)
    {
        var collider = new GameObject("collider") { layer = LayerMask.NameToLayer("piece") };
        collider.transform.SetParent(root, false);
        var box = collider.AddComponent<BoxCollider>();
        box.center = center;
        box.size = size;
    }

    // Builds the post's look: the post model with a small bed beside it and a warm light, so it can be found at night.
    private static void ApplyVisual(CustomPiece piece, Transform visual)
    {
        if (VisualHelper.IsHeadless) return;
        try
        {
            var prefab = piece.PiecePrefab;
            var template = PrefabManager.Instance.GetPrefab("wood_pole2").transform.Find("New").GetComponent<MeshRenderer>();
            var post = ForagingAssets.LoadMesh("muninpost");
            float scale = 2.2f / post.bounds.size.y;
            var pivot = -new Vector3(post.bounds.center.x, post.bounds.min.y, post.bounds.center.z) * scale;
            VisualHelper.CreateModel(visual, post, ForagingAssets.LoadTexture("muninpost_albedo"), template, pivot, Quaternion.identity, scale);
            VisualHelper.CreateModel(visual, ForagingAssets.LoadMesh("dogbed"), ForagingAssets.LoadTexture("dogbed_albedo"), template,
                new Vector3(0f, 0f, 0.65f), Quaternion.identity, 0.16f);
            var lightObject = new GameObject("CollectionGlow");
            lightObject.transform.SetParent(prefab.transform, false);
            lightObject.transform.localPosition = new Vector3(0f, 1.7f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.76f, 0.3f);
            light.range = 2f;
            light.intensity = 0.5f;
            PieceFragments.Apply(prefab);
            var icon = VisualHelper.RenderIcon(prefab);
            if (icon != null) piece.Piece.m_icon = icon;
        }
        catch (Exception exception) { Jotunn.Logger.LogWarning("Collection Post visual: " + exception.Message); }
    }
}
