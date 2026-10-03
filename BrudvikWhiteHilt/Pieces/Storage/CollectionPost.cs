using BrudvikWhiteHilt.Chests.Collection;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Storage;

/// <summary>A carved collection post with a wicker basket, connected to a workbench.</summary>
public sealed class CollectionPost : IWhiteHiltCustomPiece
{
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
        Translations.AddEnglishNameAndDescription(Id, DisplayName, "A carved post and wicker basket. Connect it to a workbench to gather loose items into White Hilt chests, sorted by kind, with the Everlasting Chest taking the rest.");
    }

    /// <inheritdoc/>
    public void Add()
    {
        try
        {
            var piece = new CustomPiece(Id, "piece_workbench_ext3", new PieceConfig
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
            prefab.GetComponent<StationExtension>().m_maxStationDistance = CollectionSettings.StationDistance.Value;
            prefab.AddComponent<CollectionPostComponent>();
            foreach (var collider in prefab.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
            var postBox = prefab.AddComponent<BoxCollider>();
            postBox.center = new Vector3(0f, 1.1f, 0f);
            postBox.size = new Vector3(0.5f, 2.2f, 0.46f);
            var basketBox = prefab.AddComponent<BoxCollider>();
            basketBox.center = new Vector3(0f, 0.08f, 0.65f);
            basketBox.size = new Vector3(0.94f, 0.16f, 0.744f);
            if (!VisualHelper.IsHeadless)
            {
                var workbench = PrefabManager.Instance.GetPrefab("piece_workbench").GetComponent<CraftingStation>();
                var marker = UnityEngine.Object.Instantiate(workbench.m_areaMarker, prefab.transform);
                marker.name = "CollectionArea";
                marker.SetActive(false);
            }
            ApplyVisual(piece);
            manager.AddPiece(piece);
            Jotunn.Logger.LogInfo("Collection Post added!");
        }
        catch (Exception exception) { Jotunn.Logger.LogError("Collection Post failed to load!"); Jotunn.Logger.LogError(exception); }
    }

    private static void ApplyVisual(CustomPiece piece)
    {
        if (VisualHelper.IsHeadless) return;
        try
        {
            var prefab = piece.PiecePrefab;
            var template = PrefabManager.Instance.GetPrefab("wood_pole2").transform.Find("New").GetComponent<MeshRenderer>();
            VisualHelper.HideRenderers(prefab);
            var post = ForagingAssets.LoadMesh("muninpost");
            float scale = 2.2f / post.bounds.size.y;
            var pivot = -new Vector3(post.bounds.center.x, post.bounds.min.y, post.bounds.center.z) * scale;
            VisualHelper.CreateModel(prefab.transform, post, ForagingAssets.LoadTexture("muninpost_albedo"), template, pivot, Quaternion.identity, scale);
            VisualHelper.CreateModel(prefab.transform, ForagingAssets.LoadMesh("dogbed"), ForagingAssets.LoadTexture("dogbed_albedo"), template,
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