using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Companions;

/// <summary>
/// Shared set-up for the dog's pieces: clones a vanilla piece, removes its look and colliders, adds a "New" visual
/// root that <see cref="WearNTear"/> shows in every state, and puts a bundle model in it.
/// </summary>
public abstract class DogPieceBase : IWhiteHiltCustomPiece
{
    private readonly PieceManager instance;

    /// <summary>
    /// Registers the English text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    protected DogPieceBase(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
    }

    /// <summary>
    /// Prefab name of the piece.
    /// </summary>
    protected abstract string PrefabName { get; }

    /// <summary>
    /// English name.
    /// </summary>
    protected abstract string FullName { get; }

    /// <summary>
    /// English description.
    /// </summary>
    protected abstract string Description { get; }

    /// <summary>
    /// Vanilla piece to clone.
    /// </summary>
    protected abstract string BasePrefab { get; }

    /// <summary>
    /// Crafting station needed nearby, or null for none.
    /// </summary>
    protected abstract string CraftingStation { get; }

    /// <summary>
    /// Build cost.
    /// </summary>
    protected abstract RequirementConfig[] Requirements { get; }

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
    /// Adds the piece to the hammer.
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
                CraftingStation = CraftingStation,
                Requirements = Requirements
            };

            CustomPiece piece = new(PrefabName, BasePrefab, pieceConfig);
            GameObject prefab = piece.PiecePrefab;
            prefab.GetComponent<Piece>().m_comfort = 0;

            GameObject visual = new("New") { layer = prefab.layer };
            KeepBeforeStrip(prefab, visual.transform);
            Strip(prefab, visual.transform);
            visual.transform.SetParent(prefab.transform, false);
            SetUpWearNTear(prefab, visual);
            Configure(prefab);
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

    /// <summary>
    /// Moves vanilla children worth keeping under <paramref name="visual"/> before the rest is removed.
    /// </summary>
    /// <param name="prefab">The cloned prefab.</param>
    /// <param name="visual">The new visual root, not yet parented.</param>
    protected virtual void KeepBeforeStrip(GameObject prefab, Transform visual)
    {
    }

    /// <summary>
    /// Adds colliders and components. Runs on the server as well.
    /// </summary>
    /// <param name="prefab">The cloned prefab.</param>
    protected abstract void Configure(GameObject prefab);

    /// <summary>
    /// Adds the bundle model under <paramref name="visual"/>. Never runs on a dedicated server.
    /// </summary>
    /// <param name="visual">The visual root.</param>
    protected abstract void ApplyVisual(Transform visual);

    /// <summary>
    /// Adds a box collider on a child object.
    /// </summary>
    /// <param name="prefab">The piece prefab.</param>
    /// <param name="layer">Layer name; "piece" blocks characters, "piece_nonsolid" does not.</param>
    /// <param name="center">Centre of the box.</param>
    /// <param name="size">Size of the box.</param>
    protected static void AddBox(GameObject prefab, string layer, Vector3 center, Vector3 size)
    {
        GameObject collider = new("collider") { layer = LayerMask.NameToLayer(layer) };
        collider.transform.SetParent(prefab.transform, false);
        BoxCollider box = collider.AddComponent<BoxCollider>();
        box.center = center;
        box.size = size;
    }

    /// <summary>
    /// Keeps the vanilla wolf rug's meshes (when cloning <c>rug_wolf</c>) as a small pelt under <paramref name="visual"/>.
    /// </summary>
    /// <param name="prefab">The cloned rug.</param>
    /// <param name="visual">The new visual root.</param>
    /// <param name="position">Where the pelt lies.</param>
    /// <param name="yaw">Turn of the pelt in degrees.</param>
    /// <param name="scale">Size of the pelt compared to the rug.</param>
    protected static void KeepPelt(GameObject prefab, Transform visual, Vector3 position, float yaw, float scale)
    {
        Transform pelt = new GameObject("pelt").transform;
        pelt.SetParent(visual, false);
        foreach (Transform child in prefab.transform.Cast<Transform>().Where(child => child.GetComponent<MeshRenderer>() != null).ToList())
        {
            child.SetParent(pelt, false);
        }

        pelt.localPosition = position;
        pelt.localRotation = Quaternion.Euler(0f, yaw, 0f);
        pelt.localScale = Vector3.one * scale;
    }

    /// <summary>
    /// A vanilla renderer to copy the material from, so the model is lit and weathered like the building around it.
    /// </summary>
    /// <param name="prefabName">Vanilla piece.</param>
    /// <param name="childPath">Path of the child with the renderer.</param>
    /// <returns>The renderer.</returns>
    protected static Renderer Template(string prefabName, string childPath)
    {
        return PrefabManager.Instance.GetPrefab(prefabName)?.transform.Find(childPath)?.GetComponentInChildren<Renderer>()
            ?? throw new InvalidOperationException($"the vanilla template {prefabName}/{childPath} was not found");
    }

    private static void Strip(GameObject prefab, Transform visual)
    {
        foreach (Transform child in prefab.transform.Cast<Transform>().Where(child => child != visual).ToList())
        {
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        foreach (Component component in prefab.GetComponents<Collider>().Cast<Component>().Concat(prefab.GetComponents<LODGroup>()))
        {
            UnityEngine.Object.DestroyImmediate(component);
        }
    }

    private static void SetUpWearNTear(GameObject prefab, GameObject visual)
    {
        WearNTear wearNTear = prefab.GetComponent<WearNTear>();
        if (wearNTear == null)
        {
            return;
        }

        wearNTear.m_new = visual;
        wearNTear.m_worn = visual;
        wearNTear.m_broken = visual;
        wearNTear.m_wet = null;
        wearNTear.m_snow = null;
        wearNTear.m_snowWorn = null;
        wearNTear.m_snowBroken = null;
        wearNTear.m_fragmentRoots = null;
    }

    private void TryApplyVisual(CustomPiece piece, Transform visual)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            ApplyVisual(visual);
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
