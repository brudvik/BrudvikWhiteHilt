using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Tools;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships.Skidbladnir;

/// <summary>Marker restricting a compact vanilla-equivalent workshop to Skidbladnir.</summary>
public sealed class ShipWorkshop : MonoBehaviour
{
    // The ray starts a little inside the ghost, so a ghost sunk into a slope still finds its floor.
    private const float Lift = 0.3f;
    // More than the lower deck's headroom.
    private const float FloorSearch = 3f;

    /// <summary>Tests the ship's still-building rule at this workshop's position.</summary>
    /// <returns>True only aboard an available sailing home.</returns>
    public bool CanPlace() => ShipFurniture.Below(transform.position)?.CanBuild ?? false;

    /// <summary>Lowers the placement ghost onto the surface beneath it, so aiming at a wall cannot hang it there.</summary>
    public void SettleOnFloor()
    {
        Vector3 up = transform.up;
        RaycastHit[] hits = Physics.RaycastAll(transform.position + up * Lift, -up, Lift + FloorSearch,
            LayerMask.GetMask("vehicle", "piece", "piece_nonsolid"), QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform)) continue;
            transform.position -= up * (hit.distance - Lift);
            return;
        }
    }
}

/// <summary>Registers compact ship stations without changing vanilla recipe station identifiers.</summary>
public abstract class ShipWorkshopBase : IWhiteHiltCustomPiece
{
    private readonly PieceManager instance;
    private readonly string source;
    private readonly string model;

    /// <inheritdoc/>
    public bool Enabled => true;
    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Start;
    /// <inheritdoc/>
    public string Id { get; }
    /// <inheritdoc/>
    public string DisplayName { get; }
    /// <inheritdoc/>
    public string NameToken => Translations.Token(Id);
    /// <inheritdoc/>
    public string GatedPrefabName => Id;

    /// <summary>Captures the vanilla station and compact model.</summary>
    /// <param name="instance">Piece manager.</param>
    /// <param name="id">Stable prefab identifier.</param>
    /// <param name="name">Display name.</param>
    /// <param name="source">Vanilla crafting station.</param>
    /// <param name="model">Bundled mesh name.</param>
    protected ShipWorkshopBase(PieceManager instance, string id, string name, string source, string model)
    {
        this.instance = instance;
        this.source = source;
        this.model = model;
        Id = id;
        DisplayName = name;
        Translations.AddEnglishNameAndDescription(id, name,
            "A compact shipboard workshop. Functions like its land-based counterpart; can only be built aboard Skidbladnir.");
    }

    /// <summary>Clones vanilla functionality and costs, replacing only the footprint and build table.</summary>
    public void Add()
    {
        try
        {
            Piece vanilla = PrefabManager.Instance.GetPrefab(source).GetComponent<Piece>();
            CustomPiece piece = new(Id, source, new PieceConfig
            {
                Name = NameToken,
                Description = Translations.Token(Id + "_description"),
                PieceTable = WhiteHiltShipHammer.TableName,
                Category = PieceCategories.Crafting,
                Requirements = vanilla.m_resources.Select(requirement => new RequirementConfig
                {
                    Item = requirement.m_resItem.name,
                    Amount = requirement.m_amount,
                    Recover = requirement.m_recover
                }).ToArray()
            });
            GameObject prefab = piece.PiecePrefab;
            CraftingStation station = prefab.GetComponent<CraftingStation>();
            Renderer template = prefab.GetComponentsInChildren<MeshRenderer>(true).First();
            foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            foreach (LODGroup group in prefab.GetComponentsInChildren<LODGroup>(true)) UnityEngine.Object.DestroyImmediate(group);
            foreach (Collider collider in prefab.GetComponentsInChildren<Collider>(true))
                if (!collider.isTrigger && collider != station.m_effectAreaCollider) UnityEngine.Object.DestroyImmediate(collider);
            prefab.AddComponent<ShipWorkshop>();
            piece.Piece.m_groundOnly = false;
            piece.Piece.m_notOnWood = false;
            piece.Piece.m_spaceRequirement = 0f;
            GameObject visual = new("ShipWorkshopVisual") { layer = LayerMask.NameToLayer("piece") };
            visual.transform.SetParent(prefab.transform, false);
            WearNTear wear = prefab.GetComponent<WearNTear>();
            wear.m_new = visual;
            wear.m_worn = visual;
            wear.m_broken = visual;
            wear.m_fragmentRoots = null;
            BoxCollider box = visual.AddComponent<BoxCollider>();
            box.center = new Vector3(0, 0.425f, 0);
            box.size = new Vector3(1.1f, 0.85f, 0.62f);
            // The vanilla stonecutter has no connection point.
            if (station.m_connectionPoint != null) station.m_connectionPoint.localPosition = new Vector3(0, 0.85f, 0);
            if (station.m_roofCheckPoint != null) station.m_roofCheckPoint.localPosition = new Vector3(0, 1.2f, 0);
            if (!VisualHelper.IsHeadless)
            {
                AddModel(visual.transform, model, 1.1f, Vector3.zero, template, true);
                if (model == "shipforge") AddModel(visual.transform, "repairanvil", 0.36f, new Vector3(-0.25f, 0.85f, 0.02f), template, false);
                Sprite icon = VisualHelper.RenderIcon(prefab);
                if (icon != null) piece.Piece.m_icon = icon;
            }
            instance.AddPiece(piece);
        }
        catch (Exception exception)
        {
            Jotunn.Logger.LogError($"{DisplayName} failed to load: {exception}");
        }
    }

    private static void AddModel(Transform parent, string name, float dimension, Vector3 position, Renderer template, bool width)
    {
        Mesh mesh = ForagingAssets.LoadMesh(name);
        float scale = dimension / (width ? mesh.bounds.size.x : mesh.bounds.size.y);
        Vector3 pivot = -new Vector3(mesh.bounds.center.x, mesh.bounds.min.y, mesh.bounds.center.z) * scale;
        VisualHelper.CreateModel(parent, mesh, ForagingAssets.LoadTexture(name + "_albedo"), template,
            position + pivot, Quaternion.identity, scale);
    }
}

/// <summary>Compact shipboard workbench.</summary>
public sealed class ShipWorkbench : ShipWorkshopBase
{
    /// <summary>Creates the workbench descriptor.</summary>
    /// <param name="instance">Piece manager.</param>
    public ShipWorkbench(PieceManager instance) : base(instance, "piece_whitehilt_shipworkbench", "Ship Workbench", "piece_workbench", "shipworkbench") { }
}

/// <summary>Compact shipboard forge.</summary>
public sealed class ShipForge : ShipWorkshopBase
{
    /// <summary>Creates the forge descriptor.</summary>
    /// <param name="instance">Piece manager.</param>
    public ShipForge(PieceManager instance) : base(instance, "piece_whitehilt_shipforge", "Ship Forge", "forge", "shipforge") { }
}

/// <summary>Compact shipboard stonecutter.</summary>
public sealed class ShipStonecutter : ShipWorkshopBase
{
    /// <summary>Creates the stonecutter descriptor.</summary>
    /// <param name="instance">Piece manager.</param>
    public ShipStonecutter(PieceManager instance) : base(instance, "piece_whitehilt_shipstonecutter", "Ship Stonecutter", "piece_stonecutter", "shipstonecutter") { }
}