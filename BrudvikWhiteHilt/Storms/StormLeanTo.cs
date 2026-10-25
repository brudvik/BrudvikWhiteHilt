using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces;
using BrudvikWhiteHilt.Pieces.Defenses;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// The vindskjul: a lean-to of logs on two posts, open to the front, the roof sloping to the ground behind. Quick and
/// cheap to put up when a storm is coming, anywhere and without a workbench. Under it counts as under a roof; with a
/// fire burning in front of the opening it is a krypinn that keeps you alive.
/// </summary>
public class StormLeanTo : IWhiteHiltCustomPiece
{
    /// <summary>Prefab name of the lean-to.</summary>
    public const string PrefabName = "piece_whitehilt_leanto";

    private const string FullName = "Storm Lean-to";
    private const string Description = "A lean-to of logs, open to the front. Quick to put up when a storm is coming, anywhere. Under it you are under a roof; light a fire in front of the opening to ride out the storm.";
    private const string BasePrefab = "wood_pole2";
    private const string PoleMesh = "wood_pole2";
    private const string LogMesh = "wood_beam";

    // The shape, in metres: the opening faces +z.
    private const float HalfWidth = 1.2f;
    private const float Front = 0.9f;
    private const float Back = -1.35f;
    private const float FrontHeight = 2f;
    private const float BackHeight = 0.12f;
    private const int Logs = 9;

    private readonly PieceManager instance;

    /// <summary>
    /// Creates the definition and registers the English text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StormLeanTo(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
    }

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
    /// The height of the roof's underside above a point of the lean-to's floor.
    /// </summary>
    /// <param name="z">Metres forward from the centre, from <see cref="Back"/> to <see cref="Front"/>.</param>
    /// <returns>The height.</returns>
    public static float RoofHeight(float z)
    {
        return Mathf.Lerp(BackHeight, FrontHeight, Mathf.InverseLerp(Back, Front, z));
    }

    /// <summary>
    /// Whether a point in the lean-to's own space is under its roof, with room to sit or crouch.
    /// </summary>
    /// <param name="local">The point (the feet), relative to the lean-to.</param>
    /// <returns>True if under the roof.</returns>
    public static bool Covers(Vector3 local)
    {
        return Mathf.Abs(local.x) <= HalfWidth && local.z >= Back && local.z <= Front
            && local.y >= -0.6f && RoofHeight(local.z) - local.y >= 0.6f;
    }

    /// <summary>
    /// Adds the lean-to to the hammer.
    /// </summary>
    public void Add()
    {
        try
        {
            CustomPiece piece = new(PrefabName, BasePrefab, new PieceConfig
            {
                Name = Translations.Token(PrefabName),
                Description = Translations.Token($"{PrefabName}_description"),
                PieceTable = PieceTables.Hammer,
                Category = PieceCategories.Building,
                CraftingStation = string.Empty,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "Wood", Amount = 12, Recover = true }
                }
            });

            GameObject prefab = piece.PiecePrefab;
            Strip(prefab);
            GameObject visual = new("New") { layer = prefab.layer };
            visual.transform.SetParent(prefab.transform, false);
            if (!VisualHelper.IsHeadless)
            {
                DefenseModelBuilder.Build(visual.transform, new Dictionary<string, Transform>(), Placements());
            }

            AddColliders(prefab.transform);
            SetUp(prefab, visual, piece.Piece);
            prefab.AddComponent<StormLeanToArea>();
            Sprite icon = VisualHelper.RenderIcon(prefab);
            if (icon != null)
            {
                piece.Piece.m_icon = icon;
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

    // Two posts and a crossbar at the front, two rafters to the ground behind, and the roof of logs laid on them.
    private static List<DefenseModelBuilder.Placement> Placements()
    {
        List<DefenseModelBuilder.Placement> placements = new();
        foreach (float side in new[] { -1f, 1f })
        {
            float x = side * (HalfWidth - 0.1f);
            placements.Add(Span(PoleMesh, new Vector3(x, 0f, Front), new Vector3(x, FrontHeight + 0.08f, Front), 1f));
            placements.Add(Span(PoleMesh, new Vector3(x, FrontHeight, Front + 0.05f), new Vector3(x, 0f, Back - 0.05f), 1f));
        }

        placements.Add(Span(PoleMesh, new Vector3(-HalfWidth - 0.05f, FrontHeight, Front), new Vector3(HalfWidth + 0.05f, FrontHeight, Front), 1f));
        for (int i = 0; i < Logs; i++)
        {
            float x = Mathf.Lerp(-HalfWidth + 0.05f, HalfWidth - 0.05f, i / (Logs - 1f));
            placements.Add(Span(LogMesh, new Vector3(x, FrontHeight + 0.12f, Front + 0.1f), new Vector3(x, BackHeight + 0.05f, Back), 0.75f));
        }

        return placements;
    }

    // A vanilla mesh stretched along its longest side from one point to another, its thickness scaled by a share.
    internal static DefenseModelBuilder.Placement Span(string mesh, Vector3 from, Vector3 to, float thickness)
    {
        Bounds bounds = MeshBounds(mesh);
        Vector3 size = bounds.size;
        int axis = size.x >= size.y && size.x >= size.z ? 0 : size.y >= size.z ? 1 : 2;
        Vector3 along = axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;
        Vector3 scale = Vector3.one * thickness;
        scale[axis] = Vector3.Distance(from, to) / Mathf.Max(0.01f, size[axis]);
        Matrix4x4 matrix = Matrix4x4.TRS((from + to) / 2f, Quaternion.FromToRotation(along, (to - from).normalized), scale)
            * Matrix4x4.Translate(-bounds.center);
        return new DefenseModelBuilder.Placement(mesh, matrix, null, null, null, false);
    }

    private static Bounds MeshBounds(string key)
    {
        Bounds bounds = default;
        bool first = true;
        foreach (VanillaMeshLibrary.MeshSource source in VanillaMeshLibrary.Get(key))
        {
            Bounds mesh = source.Mesh.bounds;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = source.Matrix.MultiplyPoint3x4(mesh.center + Vector3.Scale(mesh.extents,
                    new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f)));
                if (first)
                {
                    bounds = new Bounds(point, Vector3.zero);
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(point);
                }
            }
        }

        return bounds;
    }

    internal static void Strip(GameObject prefab)
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

    // The sloping roof and the two front posts, solid like any piece.
    private static void AddColliders(Transform root)
    {
        int layer = LayerMask.NameToLayer("piece");
        Vector3 top = new(0f, FrontHeight + 0.12f, Front + 0.1f);
        Vector3 bottom = new(0f, BackHeight + 0.05f, Back);
        GameObject roof = new("roof") { layer = layer };
        roof.transform.SetParent(root, false);
        roof.transform.localPosition = (top + bottom) / 2f;
        roof.transform.localRotation = Quaternion.LookRotation(top - bottom, Vector3.up);
        BoxCollider roofBox = roof.AddComponent<BoxCollider>();
        roofBox.size = new Vector3(HalfWidth * 2f + 0.1f, 0.2f, Vector3.Distance(top, bottom));

        foreach (float side in new[] { -1f, 1f })
        {
            GameObject post = new("post") { layer = layer };
            post.transform.SetParent(root, false);
            post.transform.localPosition = new Vector3(side * (HalfWidth - 0.1f), FrontHeight / 2f, Front);
            post.AddComponent<BoxCollider>().size = new Vector3(0.2f, FrontHeight, 0.2f);
        }
    }

    private static void SetUp(GameObject prefab, GameObject visual, Piece piece)
    {
        WearNTear wear = prefab.GetComponent<WearNTear>() ?? throw new InvalidOperationException("the wood pole has no WearNTear");
        wear.m_new = visual;
        wear.m_worn = visual;
        wear.m_broken = visual;
        wear.m_wet = null;
        wear.m_snow = null;
        wear.m_snowWorn = null;
        wear.m_snowBroken = null;
        wear.m_fragmentRoots = null;
        wear.m_noSupportWear = false;
        wear.m_noRoofWear = true;
        wear.m_supports = true;
        wear.m_health = 250f;
        wear.m_materialType = WearNTear.MaterialType.Wood;
        piece.m_groundPiece = false;
        piece.m_groundOnly = false;
        piece.m_allowedInDungeons = false;
        piece.m_noInWater = true;
        piece.m_comfort = 0;
        piece.m_clipGround = true;
    }
}

/// <summary>
/// Marks a built lean-to, so the storm knows who sits under one.
/// </summary>
public class StormLeanToArea : MonoBehaviour
{
    private static readonly List<StormLeanToArea> all = new();

    /// <summary>
    /// Whether a point (the feet) is under a lean-to's roof.
    /// </summary>
    /// <param name="point">The point in the world.</param>
    /// <returns>True if under one.</returns>
    public static bool IsUnder(Vector3 point)
    {
        foreach (StormLeanToArea area in all)
        {
            // The ghost shown while placing has no valid network view and shelters no one.
            if (area != null && area.view != null && area.view.IsValid() && (area.transform.position - point).sqrMagnitude < 9f
                && StormLeanTo.Covers(area.transform.InverseTransformPoint(point)))
            {
                return true;
            }
        }

        return false;
    }

    private ZNetView view;

    private void OnEnable()
    {
        view = GetComponent<ZNetView>();
        all.Add(this);
    }

    private void OnDisable()
    {
        all.Remove(this);
    }
}
