using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Structure.Darkwood;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Structure.Planking;

/// <summary>
/// A plank wall the vanilla wood walls do not have, made from the vanilla 2 × 2 m wood wall (woodwall) by
/// <see cref="WallCutting"/> when the game starts: no model of its own, and nothing of the game's shipped. Keeps the
/// vanilla wall's material, strength and wear, and sits in the hammer right after it.
/// </summary>
public abstract class PlankWallPieceBase : IWhiteHiltCustomPiece
{
    /// <summary>The vanilla 2 × 2 m wood wall.</summary>
    protected const string Wall = "woodwall";

    private const string SnapTag = "snappoint";

    // The vanilla wall is 0.3 m thick.
    private const float Depth = 0.3f;

    private static readonly string[] states = { "New", "Worn", "Broken" };
    private static readonly List<PlankWallPieceBase> all = new();
    private static WallSource source;
    private static bool sourceRead;
    private static bool orderHooked;

    private readonly PieceManager instance;

    /// <summary>Prefab name of the new piece.</summary>
    protected abstract string PrefabName { get; }

    /// <summary>Name shown to players.</summary>
    protected abstract string FullName { get; }

    /// <summary>Description, in English.</summary>
    protected abstract string Description { get; }

    /// <summary>Width in metres, a multiple of 2.</summary>
    protected abstract int Width { get; }

    /// <summary>Height in metres, a multiple of 2.</summary>
    protected abstract int Height { get; }

    /// <summary>The vanilla piece the new one is a clone of, for its components.</summary>
    protected virtual string CopyFrom => Wall;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Start;

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public string Id => PrefabName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(PrefabName);

    /// <inheritdoc/>
    public string GatedPrefabName => PrefabName;

    /// <summary>
    /// Registers the English text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    protected PlankWallPieceBase(PieceManager instance)
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
            GameObject wall = PrefabManager.Instance.GetPrefab(Wall) ?? throw new InvalidOperationException($"{Wall} not found");
            GameObject copyFrom = PrefabManager.Instance.GetPrefab(CopyFrom) ?? throw new InvalidOperationException($"{CopyFrom} not found");
            PieceConfig config = new()
            {
                Name = Translations.Token(PrefabName),
                Description = Translations.Token($"{PrefabName}_description"),
                PieceTable = PieceTables.Hammer,
                Requirements = Requirements(wall, copyFrom)
            };

            CustomPiece piece = new(PrefabName, CopyFrom, config);
            WallSource models = ReadWall(wall);
            Assemble(piece.PiecePrefab, models, copyFrom);
            WearNTear vanilla = wall.GetComponent<WearNTear>();
            WearNTear wearNTear = piece.PiecePrefab.GetComponent<WearNTear>();
            wearNTear.m_health = vanilla.m_health * Width * Height / 4f;
            wearNTear.m_materialType = vanilla.m_materialType;

            Sprite icon = VisualHelper.RenderIcon(piece.PiecePrefab);
            if (icon != null)
            {
                piece.Piece.m_icon = icon;
            }

            instance.AddPiece(piece);
            all.Add(this);
            if (!orderHooked)
            {
                orderHooked = true;
                PieceManager.OnPiecesRegistered += PlaceAfterTheWall;
            }

            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    /// <summary>The wall's model in one wear state, from the vanilla wall's model of that state.</summary>
    /// <param name="tile">The vanilla 2 × 2 m wall's model.</param>
    /// <returns>The model.</returns>
    protected abstract MeshData Shape(MeshData tile);

    /// <summary>Box colliders, centre and size, in the piece's space.</summary>
    /// <returns>The boxes.</returns>
    protected virtual IEnumerable<(Vector3 Center, Vector3 Size)> Colliders()
    {
        yield return (Vector3.zero, new Vector3(Width, Height, Depth));
    }

    /// <summary>Adds what the clone keeps of its own, such as a window's shutter.</summary>
    /// <param name="prefab">The new piece.</param>
    /// <param name="copyFrom">The vanilla piece it is a clone of.</param>
    /// <param name="layer">The piece layer.</param>
    protected virtual void Finish(GameObject prefab, GameObject copyFrom, int layer)
    {
    }

    /// <summary>Children of the clone that stay, such as a window's shutter.</summary>
    /// <param name="child">A child of the clone.</param>
    /// <returns>Whether it stays.</returns>
    protected virtual bool Keeps(Transform child)
    {
        return false;
    }

    /// <summary>The recipe: the vanilla wall's for the area, rounded, at least 1 of each.</summary>
    /// <param name="wall">The vanilla wall.</param>
    /// <param name="copyFrom">The vanilla piece the new one is a clone of.</param>
    /// <returns>The recipe.</returns>
    protected virtual RequirementConfig[] Requirements(GameObject wall, GameObject copyFrom)
    {
        return Scaled(wall, Width * Height / 4f).ToArray();
    }

    /// <summary>A vanilla piece's recipe times a factor, rounded, at least 1 of each.</summary>
    /// <param name="prefab">The vanilla piece.</param>
    /// <param name="factor">The factor.</param>
    /// <returns>The requirements.</returns>
    protected static IEnumerable<RequirementConfig> Scaled(GameObject prefab, float factor)
    {
        return prefab.GetComponent<Piece>().m_resources
            .Where(requirement => requirement.m_resItem != null)
            .Select(requirement => new RequirementConfig
            {
                Item = requirement.m_resItem.name,
                Amount = Mathf.Max(1, Mathf.RoundToInt(requirement.m_amount * factor)),
                Recover = requirement.m_recover
            });
    }

    // The vanilla wall's model in each wear state with its material, read once for all the walls. A dedicated server
    // draws nothing, so it reads nothing and builds only colliders and snap points.
    private static WallSource ReadWall(GameObject wall)
    {
        if (sourceRead)
        {
            return source;
        }

        sourceRead = true;
        if (VisualHelper.IsHeadless)
        {
            return null;
        }

        Transform root = wall.transform;
        WallSource read = new();
        foreach (string state in states)
        {
            Transform holder = root.Find(state) ?? throw new InvalidOperationException($"{Wall} has no {state}");
            MeshFilter filter = holder.Cast<Transform>()
                .Where(child => child.gameObject.activeSelf && child.name.StartsWith("_Combined"))
                .Select(child => child.GetComponent<MeshFilter>())
                .FirstOrDefault(found => found != null && found.sharedMesh != null);
            Material material = filter != null ? filter.GetComponent<MeshRenderer>()?.sharedMaterial : null;
            if (filter == null || material == null || !filter.sharedMesh.isReadable)
            {
                throw new InvalidOperationException($"{Wall}: {state} has no readable model");
            }

            Matrix4x4 matrix = root.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            read.Models[state] = MeshData.FromMesh(filter.sharedMesh, 0, matrix);
            read.Materials[state] = material;
        }

        source = read;
        return source;
    }

    // Replaces the clone's look, colliders and snap points; keeps its network, piece, wear and what Keeps names.
    private void Assemble(GameObject prefab, WallSource models, GameObject copyFrom)
    {
        Transform root = prefab.transform;
        int layer = LayerMask.NameToLayer("piece");
        foreach (Transform child in root.Cast<Transform>().Where(child => !Keeps(child)).ToList())
        {
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        foreach (Component component in prefab.GetComponents<Collider>().Cast<Component>().Concat(prefab.GetComponents<LODGroup>()))
        {
            UnityEngine.Object.DestroyImmediate(component);
        }

        Dictionary<string, GameObject> holders = new();
        foreach (string state in states)
        {
            GameObject holder = new(state) { layer = layer };
            holder.transform.SetParent(root, false);
            holder.SetActive(state == "New");
            holders[state] = holder;
            if (models == null)
            {
                continue;
            }

            GameObject model = new("model") { layer = layer };
            model.transform.SetParent(holder.transform, false);
            model.AddComponent<MeshFilter>().sharedMesh = MeshData.ToMesh($"{PrefabName}_{state}", Shape(models.Models[state]));
            model.AddComponent<MeshRenderer>().sharedMaterial = models.Materials[state];
        }

        foreach ((Vector3 center, Vector3 size) in Colliders())
        {
            GameObject box = new("collider") { layer = layer };
            box.transform.SetParent(root, false);
            box.transform.localPosition = center;
            box.AddComponent<BoxCollider>().size = size;
        }

        foreach (Vector3 point in WallCutting.SnapPoints(Width, Height))
        {
            GameObject snap = new("$hud_snappoint") { tag = SnapTag };
            snap.transform.SetParent(root, false);
            snap.transform.localPosition = point;
            snap.SetActive(false);
        }

        Finish(prefab, copyFrom, layer);

        WearNTear wearNTear = prefab.GetComponent<WearNTear>() ?? throw new InvalidOperationException($"{prefab.name} has no WearNTear.");
        wearNTear.m_new = holders["New"];
        wearNTear.m_worn = holders["Worn"];
        wearNTear.m_broken = holders["Broken"];
        wearNTear.m_wet = null;
        wearNTear.m_snow = null;
        wearNTear.m_snowWorn = null;
        wearNTear.m_snowBroken = null;
        wearNTear.m_fragmentRoots = null;
    }

    // The new walls follow the vanilla 2 × 2 m wall in the hammer, in the order they were added.
    private static void PlaceAfterTheWall()
    {
        PieceTable table = PieceManager.Instance.GetPieceTable(PieceTables.Hammer);
        if (table == null)
        {
            return;
        }

        List<GameObject> pieces = table.m_pieces;
        List<GameObject> ours = all.Select(entry => pieces.FirstOrDefault(prefab => prefab != null && prefab.name == entry.PrefabName))
            .Where(prefab => prefab != null)
            .ToList();
        if (ours.Count == 0)
        {
            return;
        }

        pieces.RemoveAll(ours.Contains);
        int after = pieces.FindIndex(prefab => prefab != null && prefab.name == Wall);
        pieces.InsertRange(after < 0 ? pieces.Count : after + 1, ours);
    }

    // The vanilla wall's models and materials by wear state.
    private sealed class WallSource
    {
        public readonly Dictionary<string, MeshData> Models = new();
        public readonly Dictionary<string, Material> Materials = new();
    }
}

/// <summary>A plank wall 4 m wide and 2 m high.</summary>
public sealed class PlankWall4x2 : PlankWallPieceBase
{
    /// <summary>Registers the piece's text.</summary>
    /// <param name="instance">The piece manager.</param>
    public PlankWall4x2(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string PrefabName => "piece_whitehilt_woodwall_4x2";
    /// <inheritdoc/>
    protected override string FullName => "Wood Wall 4×2";
    /// <inheritdoc/>
    protected override string Description => "Two vanilla wood walls side by side as one piece, 4 m wide and 2 m high. Snaps every 2 m along its edges.";
    /// <inheritdoc/>
    protected override int Width => 4;
    /// <inheritdoc/>
    protected override int Height => 2;
    /// <inheritdoc/>
    protected override MeshData Shape(MeshData tile) => WallCutting.Lay(tile, Width, Height);
}

/// <summary>A plank wall 4 m wide and 4 m high.</summary>
public sealed class PlankWall4x4 : PlankWallPieceBase
{
    /// <summary>Registers the piece's text.</summary>
    /// <param name="instance">The piece manager.</param>
    public PlankWall4x4(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string PrefabName => "piece_whitehilt_woodwall_4x4";
    /// <inheritdoc/>
    protected override string FullName => "Wood Wall 4×4";
    /// <inheritdoc/>
    protected override string Description => "Four vanilla wood walls as one piece, 4 m wide and 4 m high. Snaps every 2 m along its edges.";
    /// <inheritdoc/>
    protected override int Width => 4;
    /// <inheritdoc/>
    protected override int Height => 4;
    /// <inheritdoc/>
    protected override MeshData Shape(MeshData tile) => WallCutting.Lay(tile, Width, Height);
}

/// <summary>
/// The vanilla 2 × 2 m wood wall with a window sawn through it and the vanilla window's shutter hung in it. A clone of
/// the vanilla window (wood_window), so its door opens and closes the shutter.
/// </summary>
public sealed class PlankWallWindow : PlankWallPieceBase
{
    private const string Window = "wood_window";
    private const string Shutter = "door";

    /// <summary>Registers the piece's text.</summary>
    /// <param name="instance">The piece manager.</param>
    public PlankWallWindow(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string PrefabName => "piece_whitehilt_woodwall_window";
    /// <inheritdoc/>
    protected override string FullName => "Wood Wall with Window";
    /// <inheritdoc/>
    protected override string Description => "A 2 × 2 m wood wall with a window 1 m wide from 0.5 m to 1.5 m above the floor, and the vanilla window's shutter on its post.";
    /// <inheritdoc/>
    protected override int Width => 2;
    /// <inheritdoc/>
    protected override int Height => 2;
    /// <inheritdoc/>
    protected override string CopyFrom => Window;
    /// <inheritdoc/>
    protected override MeshData Shape(MeshData tile) => WallCutting.CutOpening(tile, WallCutting.Window);

    // Beside, under and over the opening.
    /// <inheritdoc/>
    protected override IEnumerable<(Vector3 Center, Vector3 Size)> Colliders()
    {
        Rect o = WallCutting.Window;
        const float depth = 0.3f;
        yield return (new Vector3((-1f + o.xMin) / 2f, 0f, 0f), new Vector3(o.xMin + 1f, 2f, depth));
        yield return (new Vector3((1f + o.xMax) / 2f, 0f, 0f), new Vector3(1f - o.xMax, 2f, depth));
        yield return (new Vector3(o.center.x, (-1f + o.yMin) / 2f, 0f), new Vector3(o.width, o.yMin + 1f, depth));
        yield return (new Vector3(o.center.x, (1f + o.yMax) / 2f, 0f), new Vector3(o.width, 1f - o.yMax, depth));
    }

    // The vanilla window and the wall it replaces.
    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements(GameObject wall, GameObject copyFrom)
    {
        return Scaled(wall, 1f).Concat(Scaled(copyFrom, 1f))
            .GroupBy(requirement => requirement.Item)
            .Select(group => new RequirementConfig { Item = group.Key, Amount = group.Sum(requirement => requirement.Amount), Recover = group.First().Recover })
            .ToArray();
    }

    /// <inheritdoc/>
    protected override bool Keeps(Transform child)
    {
        return child.name == Shutter;
    }

    // The shutter hangs on the opening's right side, as the vanilla window hangs on its own post; the far model goes,
    // since the clone has no LOD group to switch it.
    /// <inheritdoc/>
    protected override void Finish(GameObject prefab, GameObject copyFrom, int layer)
    {
        Transform shutter = prefab.transform.Find(Shutter) ?? throw new InvalidOperationException($"{Window} has no {Shutter}");
        shutter.localPosition = new Vector3(WallCutting.Window.xMax, WallCutting.Window.center.y, 0f);
        foreach (Transform far in shutter.Cast<Transform>().Where(child => child.name == "Low").ToList())
        {
            UnityEngine.Object.DestroyImmediate(far.gameObject);
        }

        // The vanilla window's post, with the shutter, in each wear state.
        foreach (string state in new[] { "New", "Worn", "Broken" })
        {
            Transform from = copyFrom.transform.Find(state) ?? copyFrom.transform.Find("New");
            Transform holder = prefab.transform.Find(state);
            if (from == null || holder == null)
            {
                continue;
            }

            foreach (MeshRenderer renderer in from.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.gameObject.activeSelf))
            {
                GameObject post = UnityEngine.Object.Instantiate(renderer.gameObject, holder.transform);
                post.name = "post";
                post.layer = layer;
                Matrix4x4 local = copyFrom.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                post.transform.localPosition = (Vector3)local.GetColumn(3) + shutter.localPosition;
                post.transform.localRotation = local.rotation;
                post.transform.localScale = local.lossyScale;
                foreach (Collider collider in post.GetComponentsInChildren<Collider>(true))
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }
            }
        }
    }
}
