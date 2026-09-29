using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Defenses;

/// <summary>
/// Base class for the palisade defences. Each piece is a vanilla piece stripped of its look and colliders, rebuilt from
/// the embedded defence layout: box colliders, snap points and ladders on every machine, combined meshes on clients.
/// One piece is one network object with one health bar, much lighter than building the same from vanilla pieces.
/// </summary>
public abstract class DefensePieceBase : IWhiteHiltCustomPiece
{
    private const string SnapTag = "snappoint";

    private readonly PieceManager instance;

    /// <summary>
    /// Name of the piece in the defence layout; also the end of the prefab name.
    /// </summary>
    protected abstract string LayoutName { get; }

    /// <summary>
    /// Name shown to players, in English. Other languages come from the embedded translation files.
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
    /// Health of the piece.
    /// </summary>
    protected abstract float Health { get; }

    /// <inheritdoc/>
    public abstract ProgressionTier DefaultTier { get; }

    /// <summary>
    /// Hammer category of the piece.
    /// </summary>
    protected virtual string Category => PieceCategories.Building;

    /// <inheritdoc/>
    public virtual bool Enabled => true;

    /// <inheritdoc/>
    public string Id => PrefabName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(PrefabName);

    /// <inheritdoc/>
    public string GatedPrefabName => PrefabName;

    /// <summary>
    /// Prefab name of the piece.
    /// </summary>
    public string PrefabName => $"piece_whitehilt_{LayoutName}";

    /// <summary>
    /// Constructor for the DefensePieceBase class. Registers the English text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    protected DefensePieceBase(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
    }

    /// <summary>
    /// Adds the piece to the hammer.
    /// </summary>
    public void Add()
    {
        try
        {
            DefenseLayout layout = DefenseLayout.Load();
            DefensePieceData data = layout.Get(LayoutName);
            PieceConfig pieceConfig = new()
            {
                Name = Translations.Token(PrefabName),
                Description = Translations.Token($"{PrefabName}_description"),
                PieceTable = PieceTables.Hammer,
                Category = Category,
                CraftingStation = CraftingStations.Workbench,
                Requirements = Requirements
            };

            CustomPiece piece = new(PrefabName, data.@base, pieceConfig);
            GameObject prefab = piece.PiecePrefab;
            Strip(prefab, data.keep ?? Array.Empty<string>());
            Dictionary<string, Transform> groups = AddGroups(prefab.transform, data);
            AddColliders(prefab.transform, data, groups);
            AddSnapPoints(prefab.transform, data);
            AddLadders(prefab.transform, data);

            GameObject visual = new("New") { layer = prefab.layer };
            visual.transform.SetParent(prefab.transform, false);
            if (!VisualHelper.IsHeadless)
            {
                DefenseModelBuilder.Build(visual.transform, groups, DefenseModelBuilder.Flatten(layout, data));
            }

            SetUpWearNTear(prefab, visual);
            CustomizePrefab(prefab, data, groups);

            Sprite icon = VisualHelper.RenderIcon(prefab);
            if (icon != null)
            {
                piece.Piece.m_icon = icon;
                CraftingStation station = prefab.GetComponent<CraftingStation>();
                if (station != null)
                {
                    station.m_icon = icon;
                }
            }

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
    /// Changes the prefab after it is built, e.g. to hook up a gate.
    /// </summary>
    /// <param name="prefab">The piece prefab.</param>
    /// <param name="data">The piece in the layout.</param>
    /// <param name="groups">Moving groups by name.</param>
    protected virtual void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
    }

    /// <summary>
    /// Reads a layout box as position, rotation and size.
    /// </summary>
    /// <param name="data">The box.</param>
    /// <param name="position">Centre.</param>
    /// <param name="rotation">Rotation.</param>
    /// <param name="size">Size.</param>
    protected static void ReadBox(DefenseBoxData data, out Vector3 position, out Quaternion rotation, out Vector3 size)
    {
        position = DefenseModelBuilder.ToVector(data.center, Vector3.zero);
        rotation = Quaternion.Euler(DefenseModelBuilder.ToVector(data.rotation, Vector3.zero));
        size = DefenseModelBuilder.ToVector(data.size, Vector3.one);
    }

    // Keeps the network, piece and health components, and only the children the layout asks for.
    private static void Strip(GameObject prefab, string[] keep)
    {
        foreach (Transform child in prefab.transform.Cast<Transform>().ToList())
        {
            if (!keep.Contains(child.name))
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        foreach (Component component in prefab.GetComponents<Collider>().Cast<Component>().Concat(prefab.GetComponents<LODGroup>()))
        {
            UnityEngine.Object.DestroyImmediate(component);
        }
    }

    private static Dictionary<string, Transform> AddGroups(Transform root, DefensePieceData data)
    {
        Dictionary<string, Transform> groups = new();
        foreach (DefenseGroupData group in data.groups ?? Array.Empty<DefenseGroupData>())
        {
            GameObject pivot = new(group.name) { layer = root.gameObject.layer };
            pivot.transform.SetParent(root, false);
            pivot.transform.localPosition = DefenseModelBuilder.ToVector(group.pivot, Vector3.zero);
            groups[group.name] = pivot.transform;
        }

        return groups;
    }

    private static void AddColliders(Transform root, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        int layer = LayerMask.NameToLayer("piece");
        foreach (DefenseBoxData box in data.colliders ?? Array.Empty<DefenseBoxData>())
        {
            ReadBox(box, out Vector3 position, out Quaternion rotation, out Vector3 size);
            Transform parent = !string.IsNullOrEmpty(box.group) && groups.TryGetValue(box.group, out Transform group) ? group : root;
            GameObject collider = new("collider") { layer = layer };
            collider.transform.SetParent(parent, false);
            collider.transform.localPosition = parent == root ? position : position - parent.localPosition;
            collider.transform.localRotation = rotation;
            collider.AddComponent<BoxCollider>().size = size;
        }
    }

    private static void AddSnapPoints(Transform root, DefensePieceData data)
    {
        float[] points = data.snapPoints ?? Array.Empty<float>();
        for (int i = 0; i + 2 < points.Length; i += 3)
        {
            GameObject snap = new("$hud_snappoint") { tag = SnapTag };
            snap.transform.SetParent(root, false);
            snap.transform.localPosition = new Vector3(points[i], points[i + 1], points[i + 2]);
            snap.SetActive(false);
        }
    }

    private static void AddLadders(Transform root, DefensePieceData data)
    {
        int layer = LayerMask.NameToLayer("piece");
        foreach (DefenseLadderData ladder in data.ladders ?? Array.Empty<DefenseLadderData>())
        {
            GameObject climb = new("ladder") { layer = layer };
            climb.transform.SetParent(root, false);
            climb.transform.localPosition = DefenseModelBuilder.ToVector(ladder.center, Vector3.zero);
            climb.AddComponent<BoxCollider>().size = DefenseModelBuilder.ToVector(ladder.size, Vector3.one);
            float[] stops = ladder.stops ?? Array.Empty<float>();
            climb.AddComponent<DefenseLadder>().m_stops = Enumerable.Range(0, stops.Length / 3)
                .Select(i => new Vector3(stops[i * 3], stops[i * 3 + 1], stops[i * 3 + 2]))
                .ToArray();
        }
    }

    // The combined meshes are the new, worn and broken look alike; the vanilla snow and wet looks are gone.
    private void SetUpWearNTear(GameObject prefab, GameObject visual)
    {
        WearNTear wearNTear = prefab.GetComponent<WearNTear>() ?? throw new InvalidOperationException($"{prefab.name} has no WearNTear.");
        wearNTear.m_new = visual;
        wearNTear.m_worn = visual;
        wearNTear.m_broken = visual;
        wearNTear.m_wet = null;
        wearNTear.m_snow = null;
        wearNTear.m_snowWorn = null;
        wearNTear.m_snowBroken = null;
        wearNTear.m_fragmentRoots = null;
        wearNTear.m_health = Health;
    }
}
