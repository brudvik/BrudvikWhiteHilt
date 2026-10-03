using BepInEx.Bootstrap;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Patches.Portals;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Portals.WhiteHiltPortal;

/// <summary>
/// Base class of the White Hilt portals. Each is the wooden portal stripped of its pairing, look and colliders, with a
/// <see cref="WhiteHiltPortalComponent"/>, box colliders and a model from the asset bundle.
/// </summary>
public abstract class WhiteHiltPortalPieceBase : IWhiteHiltCustomPiece
{
    private const string BasePrefab = "portal_wood";

    private readonly PieceManager instance;

    /// <summary>
    /// Prefab name of the portal.
    /// </summary>
    public abstract string PrefabName { get; }

    /// <summary>
    /// Name shown to players, in English.
    /// </summary>
    protected abstract string FullName { get; }

    /// <summary>
    /// Description, in English.
    /// </summary>
    protected abstract string Description { get; }

    /// <summary>
    /// Building costs.
    /// </summary>
    protected abstract RequirementConfig[] Requirements { get; }

    /// <summary>
    /// True for the portal that lies on the ground.
    /// </summary>
    protected abstract bool Ground { get; }

    /// <inheritdoc/>
    public virtual bool Enabled => true;

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
    /// Constructor for the WhiteHiltPortalPieceBase class. Registers the English text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    protected WhiteHiltPortalPieceBase(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
    }

    /// <summary>
    /// Adds the portal to the hammer.
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
                Requirements = Requirements
            };

            CustomPiece piece = new(PrefabName, BasePrefab, pieceConfig);
            GameObject prefab = piece.PiecePrefab;
            GameObject swirl = Strip(prefab);
            prefab.AddComponent<WhiteHiltPortalComponent>().Ground = Ground;
            AddColliders(prefab.transform);
            if (!VisualHelper.IsHeadless)
            {
                TryApplyVisual(piece, swirl);
            }
            else if (swirl != null)
            {
                UnityEngine.Object.DestroyImmediate(swirl);
            }

            instance.AddPiece(piece);
            OnAdded(prefab);
            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    /// <summary>
    /// Adds the box colliders, in the portal's own space. They exist on the server too.
    /// </summary>
    /// <param name="root">The portal prefab.</param>
    protected abstract void AddColliders(Transform root);

    /// <summary>
    /// Builds the look: the model, and the vanilla portal's swirl if the portal keeps it.
    /// </summary>
    /// <param name="prefab">The portal prefab.</param>
    /// <param name="template">A vanilla renderer whose material the model copies.</param>
    /// <param name="swirl">The vanilla portal's swirl effect.</param>
    protected abstract void ApplyVisual(GameObject prefab, Renderer template, GameObject swirl);

    /// <summary>
    /// Called after the piece is registered.
    /// </summary>
    /// <param name="prefab">The portal prefab.</param>
    protected virtual void OnAdded(GameObject prefab)
    {
    }

    /// <summary>
    /// Adds a box collider on the piece layer.
    /// </summary>
    /// <param name="root">The portal prefab.</param>
    /// <param name="center">Centre in the portal's space.</param>
    /// <param name="size">Size.</param>
    protected static void AddBox(Transform root, Vector3 center, Vector3 size)
    {
        GameObject collider = new("collider") { layer = LayerMask.NameToLayer("piece") };
        collider.transform.SetParent(root, false);
        collider.transform.localPosition = center;
        collider.AddComponent<BoxCollider>().size = size;
    }

    // Removes the pairing, the trigger and the colliders; returns the swirl effect the pairing used to switch on.
    private static GameObject Strip(GameObject prefab)
    {
        GameObject swirl = null;
        foreach (TeleportWorld teleport in prefab.GetComponentsInChildren<TeleportWorld>(true))
        {
            swirl ??= teleport.m_target_found != null ? teleport.m_target_found.gameObject : null;
            UnityEngine.Object.DestroyImmediate(teleport);
        }

        foreach (TeleportWorldTrigger trigger in prefab.GetComponentsInChildren<TeleportWorldTrigger>(true))
        {
            UnityEngine.Object.DestroyImmediate(trigger.gameObject);
        }

        // The PlayerBase area keeps its sphere: EffectArea.Awake needs a collider on its own object.
        foreach (Collider collider in prefab.GetComponentsInChildren<Collider>(true).Where(collider => collider.GetComponent<EffectArea>() == null))
        {
            UnityEngine.Object.DestroyImmediate(collider);
        }

        return swirl;
    }

    private void TryApplyVisual(CustomPiece piece, GameObject swirl)
    {
        try
        {
            VisualHelper.HideRenderers(piece.PiecePrefab);

            // The portal's own wood, not a mesh of the swirl, is the material the model copies; the swirl stays visible.
            Renderer template = null;
            foreach (Renderer renderer in piece.PiecePrefab.GetComponentsInChildren<Renderer>(true))
            {
                bool inSwirl = swirl != null && renderer.transform.IsChildOf(swirl.transform);
                if (inSwirl)
                {
                    renderer.enabled = true;
                }
                else if (template == null && renderer is MeshRenderer)
                {
                    template = renderer;
                }
            }

            ApplyVisual(piece.PiecePrefab, template ?? throw new InvalidOperationException("the portal has no mesh renderer"), swirl);
            Sprite icon = VisualHelper.RenderIcon(piece.PiecePrefab);
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
}

/// <summary>
/// The standing White Hilt portal: a weathered stone arch on a round, stepped base, with the portal swirl in the arch.
/// </summary>
public class WhiteHiltPortal : WhiteHiltPortalPieceBase
{
    private const float Height = 3.6f;
    private const float SwirlScale = 0.55f;

    // The arch spans the model's z axis; turned a quarter it spans x and is walked through along z, like the vanilla portal.
    private static readonly Quaternion turn = Quaternion.Euler(0f, 90f, 0f);

    // Middle of the opening, measured on the converted model (height 1, x negated as in the game).
    private static readonly Vector3 openingInMesh = new(0.05f, 0.45f, -0.1f);

    /// <summary>
    /// Constructor for the WhiteHiltPortal class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public WhiteHiltPortal(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    public override string PrefabName => "piece_whitehilt_portal";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Portal";

    /// <inheritdoc/>
    protected override string Description => "A stone arch that leads to every other White Hilt portal. Use it to open the travel map; Shift + Use names it.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = 30, Recover = true },
        new() { Item = "FineWood", Amount = 10, Recover = true },
        new() { Item = "SurtlingCore", Amount = 2, Recover = true },
        new() { Item = "GreydwarfEye", Amount = 10, Recover = true }
    };

    /// <inheritdoc/>
    protected override bool Ground => false;

    /// <inheritdoc/>
    protected override void AddColliders(Transform root)
    {
        AddMeshBox(root, new Vector3(0f, 0.045f, 0f), new Vector3(0.85f, 0.09f, 0.85f));
        AddMeshBox(root, new Vector3(0.05f, 0.5f, -0.37f), new Vector3(0.3f, 0.75f, 0.16f));
        AddMeshBox(root, new Vector3(0.05f, 0.5f, 0.17f), new Vector3(0.3f, 0.75f, 0.16f));
        AddMeshBox(root, new Vector3(0.05f, 0.89f, -0.1f), new Vector3(0.3f, 0.2f, 0.7f));
    }

    /// <inheritdoc/>
    protected override void ApplyVisual(GameObject prefab, Renderer template, GameObject swirl)
    {
        VisualHelper.CreateModel(prefab.transform, ForagingAssets.LoadMesh("portal"), ForagingAssets.LoadTexture("portal_albedo"), template, Vector3.zero, turn, Height);
        if (swirl != null)
        {
            swirl.transform.SetParent(prefab.transform, false);
            swirl.transform.localPosition = turn * (openingInMesh * Height);
            swirl.transform.localScale = Vector3.one * SwirlScale;
            swirl.SetActive(true);
        }
    }

    // A box measured on the model, turned and scaled into the portal's space.
    private static void AddMeshBox(Transform root, Vector3 centerInMesh, Vector3 sizeInMesh)
    {
        AddBox(root, turn * (centerInMesh * Height), new Vector3(sizeInMesh.z, sizeInMesh.y, sizeInMesh.x) * Height);
    }
}

/// <summary>
/// The White Hilt portal that lies on the ground: a flat rune circle. Stations of the Portal Stations mod become these.
/// </summary>
public class WhiteHiltGroundPortal : WhiteHiltPortalPieceBase
{
    /// <summary>
    /// Prefab names of the Portal Stations stations; the ones already built carry on as ground portals.
    /// </summary>
    public static readonly string[] PortalStationsPrefabs =
    {
        "portalstation", "portalStationOne", "portalPlatform", "portalStationDoor", "PortalStation_Stone", "PortalStation_Wood", "PortalStation_Blue"
    };

    private const float Diameter = 4f;
    private const float Lift = 0.01f;

    // The stone base; make_portal_base.py builds the model with the same sizes.
    private const float BaseRadius = 2.4f;
    private const float BaseBevel = 0.18f;
    private const float BaseHeight = 0.12f;
    private const float BaseSkirt = 0.3f;
    private const float BaseGlossiness = 0.7f;
    private const int ColliderSides = 24;

    private static readonly Color runeGlow = new(0.45f, 0.8f, 1f);

    // Middle of the rune ring at a diameter of 4 m: the rune stone at its front moves the model's own centre off it.
    private static readonly Vector3 ringCentre = new(0f, 0f, -0.16f);

    /// <summary>
    /// Constructor for the WhiteHiltGroundPortal class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public WhiteHiltGroundPortal(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    public override string PrefabName => "piece_whitehilt_portalground";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Rune Circle";

    /// <inheritdoc/>
    protected override string Description => "A circle of glowing runes on the ground that leads to every other White Hilt portal. Step on it and use it to open the travel map; Shift + Use names it.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = 20, Recover = true },
        new() { Item = "Bronze", Amount = 2, Recover = true },
        new() { Item = "SurtlingCore", Amount = 2, Recover = true },
        new() { Item = "GreydwarfEye", Amount = 10, Recover = true }
    };

    /// <inheritdoc/>
    protected override bool Ground => true;

    /// <inheritdoc/>
    protected override void AddColliders(Transform root)
    {
        // Flat top, a slope down the rim and on into the ground, so players walk straight onto the stone.
        (float radius, float height)[] rings = { (BaseRadius - BaseBevel, BaseHeight), (BaseRadius, 0f), (BaseRadius, -BaseSkirt) };
        List<Vector3> vertices = new();
        foreach ((float radius, float height) in rings)
        {
            for (int i = 0; i < ColliderSides; i++)
            {
                float angle = i * 2f * Mathf.PI / ColliderSides;
                vertices.Add(ringCentre + new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius));
            }
        }

        List<int> triangles = new();
        for (int ring = 0; ring < rings.Length - 1; ring++)
        {
            for (int i = 0; i < ColliderSides; i++)
            {
                int a = ring * ColliderSides + i;
                int b = ring * ColliderSides + (i + 1) % ColliderSides;
                triangles.AddRange(new[] { a, b, a + ColliderSides, b, b + ColliderSides, a + ColliderSides });
            }
        }

        int bottom = (rings.Length - 1) * ColliderSides;
        for (int i = 1; i < ColliderSides - 1; i++)
        {
            triangles.AddRange(new[] { 0, i + 1, i, bottom, bottom + i, bottom + i + 1 });
        }

        Mesh mesh = new() { name = "portalbase_collider" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();

        GameObject collider = new("collider") { layer = LayerMask.NameToLayer("piece") };
        collider.transform.SetParent(root, false);
        MeshCollider meshCollider = collider.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
        meshCollider.convex = true;
    }

    /// <inheritdoc/>
    protected override void ApplyVisual(GameObject prefab, Renderer template, GameObject swirl)
    {
        if (swirl != null)
        {
            UnityEngine.Object.DestroyImmediate(swirl);
        }

        // The converted model is 1 high from the bottom of its skirt, which reaches into the ground.
        GameObject stone = VisualHelper.CreateModel(prefab.transform, ForagingAssets.LoadMesh("portalbase"), ForagingAssets.LoadTexture("portalbase_albedo"), template,
            ringCentre + Vector3.down * BaseSkirt, Quaternion.identity, BaseHeight + BaseSkirt);
        Material material = stone.GetComponent<MeshRenderer>().sharedMaterial;
        if (material.HasProperty("_EmissionMap"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetTexture("_EmissionMap", ForagingAssets.LoadTexture("portalbase_emission"));
            material.SetColor("_EmissionColor", PortalBaseGlow.GlowColor * PortalBaseGlow.FarStrength);
        }

        if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Glossiness", BaseGlossiness);
        }

        prefab.AddComponent<PortalBaseGlow>();
        AddRuneCircle(prefab.transform, template, Diameter, BaseHeight + Lift);
    }

    /// <summary>
    /// Adds the glowing rune circle model and its light.
    /// </summary>
    /// <param name="parent">Transform the circle lies on.</param>
    /// <param name="template">Renderer whose material the model copies.</param>
    /// <param name="diameter">Diameter of the circle, in metres.</param>
    /// <param name="lift">Height above the parent, so it does not flicker with the surface below.</param>
    public static void AddRuneCircle(Transform parent, Renderer template, float diameter, float lift)
    {
        Mesh mesh = ForagingAssets.LoadMesh("portalground");
        float scale = diameter / Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.z);
        GameObject circle = VisualHelper.CreateModel(parent, mesh, ForagingAssets.LoadTexture("portalground_albedo"), template, Vector3.up * lift, Quaternion.identity, scale);
        Material material = circle.GetComponent<MeshRenderer>().sharedMaterial;
        if (material.HasProperty("_EmissionMap"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetTexture("_EmissionMap", ForagingAssets.LoadTexture("portalground_emission"));
            material.SetColor("_EmissionColor", runeGlow * 2f);
        }

        GameObject light = new("WhiteHiltRuneLight");
        light.transform.SetParent(parent, false);
        light.transform.localPosition = Vector3.up * 0.6f;
        Light glow = light.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = runeGlow;
        glow.range = diameter;
        glow.intensity = 1.2f;
        glow.shadows = LightShadows.None;
    }

    /// <summary>
    /// Registers the Portal Stations prefab names as copies of this portal, unless that mod is installed.
    /// </summary>
    /// <param name="prefab">The portal prefab.</param>
    protected override void OnAdded(GameObject prefab)
    {
        if (Chainloader.PluginInfos.ContainsKey(PortalStationsRunePatch.ModGuid))
        {
            Jotunn.Logger.LogWarning("Portal Stations is installed, so its stations are not taken over by White Hilt portals.");
            return;
        }

        foreach (string name in PortalStationsPrefabs)
        {
            // The clone copies Jotunn's mock workbench and resource items; without fixReference they are never resolved.
            PrefabManager.Instance.AddPrefab(new CustomPrefab(PrefabManager.Instance.CreateClonedPrefab(name, prefab), fixReference: true));
        }
    }
}
