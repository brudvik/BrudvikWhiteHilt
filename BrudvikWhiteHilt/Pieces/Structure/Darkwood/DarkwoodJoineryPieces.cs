using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Defenses;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Structure.Darkwood;

/// <summary>
/// A darkwood door, wall, gable wall or double gate, cut from the vanilla darkwood gate by <see cref="GateJoinery"/>
/// when the game starts: no model of its own in the asset bundle, and nothing of the game's shipped. Doors and the
/// double gate are clones of the gate, so its Door component, animation and sounds open and close them; the vanilla
/// leaf stays as an invisible driver and the cut leaves follow its swing (<see cref="GateLeafDriver"/>), as on the log
/// house doors. Walls are clones of the darkwood lattice wall, so they build, wear and break as darkwood.
/// </summary>
public abstract class DarkwoodJoineryBase : IWhiteHiltCustomPiece
{
    private const string Gate = "darkwood_gate";
    private const string Lattice = "darkwood_decowall";
    private const string SnapTag = "snappoint";

    private static readonly List<DarkwoodJoineryBase> all = new();
    private static readonly Dictionary<GateSlot, Material> materials = new();
    private static GateSource source;
    private static bool sourceRead;
    private static bool orderHooked;

    private readonly PieceManager instance;

    /// <summary>Prefab name of the new piece.</summary>
    protected abstract string PrefabName { get; }

    /// <summary>Name shown to players.</summary>
    protected abstract string FullName { get; }

    /// <summary>Description, in English.</summary>
    protected abstract string Description { get; }

    /// <summary>What the piece is.</summary>
    protected abstract JoineryKind Kind { get; }

    /// <summary>Height of a door or wall, or the rise of a gable over its 2 m.</summary>
    protected abstract float Height { get; }

    /// <summary>Building costs.</summary>
    protected abstract RequirementConfig[] Requirements { get; }

    /// <summary>The crafting station it is built at: the forge for what has iron hinges, as the vanilla gate.</summary>
    protected virtual string Station => Kind == JoineryKind.Door || Kind == JoineryKind.DoubleGate ? CraftingStations.Forge : CraftingStations.Workbench;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Plains;

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
    protected DarkwoodJoineryBase(PieceManager instance)
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
            GameObject gate = PrefabManager.Instance.GetPrefab(Gate) ?? throw new InvalidOperationException($"{Gate} not found");
            string copyFrom = Kind == JoineryKind.Door || Kind == JoineryKind.DoubleGate ? Gate : Lattice;
            if (PrefabManager.Instance.GetPrefab(copyFrom) == null)
            {
                throw new InvalidOperationException($"{copyFrom} not found");
            }

            PieceConfig config = new()
            {
                Name = Translations.Token(PrefabName),
                Description = Translations.Token($"{PrefabName}_description"),
                PieceTable = PieceTables.Hammer,
                CraftingStation = Station,
                Requirements = Requirements
            };

            CustomPiece piece = new(PrefabName, copyFrom, config);
            GateSource models = ReadGate(gate);
            Assemble(piece.PiecePrefab, gate, GateJoinery.Build(Kind, Height, models), models);

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
                PieceManager.OnPiecesRegistered += PlaceAfterTheGate;
            }

            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    // The gate's models with their materials, read once for all the pieces. A dedicated server draws nothing, so it
    // reads nothing and builds only colliders and snap points.
    private static GateSource ReadGate(GameObject gate)
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

        Transform root = gate.transform;
        Transform door = root.Find("door") ?? throw new InvalidOperationException($"{Gate} has no door");
        GateSource read = new() { DoorPivot = door.localPosition };
        foreach ((GateSlot slot, Transform holder, string child) in new[]
        {
            (GateSlot.Leaf, door, "high"), (GateSlot.LeafLow, door, "low"),
            (GateSlot.New, root.Find("New"), "high"), (GateSlot.NewLow, root.Find("New"), "low"),
            (GateSlot.Worn, root.Find("Worn"), "high"), (GateSlot.WornLow, root.Find("Worn"), "low")
        })
        {
            Transform part = holder != null ? holder.Find(child) : null;
            Mesh mesh = part != null ? part.GetComponent<MeshFilter>()?.sharedMesh : null;
            Material material = part != null ? part.GetComponent<MeshRenderer>()?.sharedMaterial : null;
            if (mesh == null || material == null || !mesh.isReadable)
            {
                throw new InvalidOperationException($"{Gate}: {holder?.name}/{child} is missing or not readable");
            }

            // The leaf stays in its own space (the cuts are measured there); the frame comes into the gate's.
            bool leaf = slot == GateSlot.Leaf || slot == GateSlot.LeafLow;
            Matrix4x4 matrix = leaf ? Matrix4x4.identity : root.worldToLocalMatrix * part.localToWorldMatrix;
            read.Meshes[slot] = MeshData.FromMesh(mesh, 0, matrix);
            materials[slot] = material;
            if (slot == GateSlot.Leaf)
            {
                read.LeafOffset = part.localPosition;
            }
        }

        source = read;
        return source;
    }

    // Replaces the clone's look, colliders and snap points with the cut ones; keeps its network, piece, wear and (for
    // doors) door components.
    private void Assemble(GameObject prefab, GameObject gate, JoineryModel model, GateSource models)
    {
        Transform root = prefab.transform;
        int layer = LayerMask.NameToLayer("piece");
        foreach (Transform child in root.Cast<Transform>().Where(child => child.name != "door").ToList())
        {
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        foreach (Component component in prefab.GetComponents<Collider>().Cast<Component>().Concat(prefab.GetComponents<LODGroup>()))
        {
            UnityEngine.Object.DestroyImmediate(component);
        }

        List<Renderer> high = new();
        List<Renderer> low = new();
        GameObject fresh = Holder(root, "New", model.Static["New"], model.Static["NewLow"], layer, high, low);
        GameObject worn = Holder(root, "Worn", model.Static["Worn"], model.Static["WornLow"], layer, high, low);
        worn.SetActive(false);

        AddLeaves(prefab, model, layer, high, low);
        foreach (JoineryBox box in model.Boxes)
        {
            AddBox(root, "collider", box.Center, Quaternion.identity, box.Size, layer);
        }

        if (Kind == JoineryKind.DoubleGate)
        {
            CopyGateColliders(gate.transform, root, layer);
        }

        if (model.Prism != null)
        {
            GameObject prism = new("collider") { layer = layer };
            prism.transform.SetParent(root, false);
            MeshCollider collider = prism.AddComponent<MeshCollider>();
            collider.sharedMesh = PrismMesh(model.Prism, GateJoinery.FrameDepth);
            collider.convex = true;
        }

        foreach (Vector3 point in model.SnapPoints)
        {
            GameObject snap = new("$hud_snappoint") { tag = SnapTag };
            snap.transform.SetParent(root, false);
            snap.transform.localPosition = point;
            snap.SetActive(false);
        }

        if (models != null)
        {
            LinkLods(prefab, gate, high, low);
        }

        WearNTear wearNTear = prefab.GetComponent<WearNTear>() ?? throw new InvalidOperationException($"{prefab.name} has no WearNTear.");
        wearNTear.m_new = fresh;
        wearNTear.m_worn = worn;
        wearNTear.m_broken = worn;
        wearNTear.m_wet = null;
        wearNTear.m_snow = null;
        wearNTear.m_snowWorn = null;
        wearNTear.m_snowBroken = null;
        wearNTear.m_fragmentRoots = null;
    }

    // One wear state: its near and far model, each one mesh with a submesh per part.
    private static GameObject Holder(Transform root, string name, List<JoineryPart> near, List<JoineryPart> far, int layer, List<Renderer> high, List<Renderer> low)
    {
        GameObject holder = new(name) { layer = layer };
        holder.transform.SetParent(root, false);
        AddModel(holder.transform, "high", near, layer, high);
        AddModel(holder.transform, "low", far, layer, low);
        return holder;
    }

    private static void AddModel(Transform parent, string name, List<JoineryPart> parts, int layer, List<Renderer> renderers)
    {
        if (parts.Count == 0)
        {
            return;
        }

        GameObject model = new(name) { layer = layer };
        model.transform.SetParent(parent, false);
        model.AddComponent<MeshFilter>().sharedMesh = MeshData.ToMesh($"{parent.name}_{name}", parts.Select(part => part.Mesh).ToArray());
        MeshRenderer renderer = model.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = parts.Select(part => materials[part.Slot]).ToArray();
        renderers.Add(renderer);
    }

    // The vanilla door keeps opening, closing and syncing, unseen; each cut leaf turns on its own hinge with it.
    private static void AddLeaves(GameObject prefab, JoineryModel model, int layer, List<Renderer> high, List<Renderer> low)
    {
        Transform door = prefab.transform.Find("door");
        if (model.Leaves.Count == 0)
        {
            if (door != null)
            {
                UnityEngine.Object.DestroyImmediate(door.gameObject);
            }

            return;
        }

        if (door == null)
        {
            throw new InvalidOperationException($"{prefab.name}: the vanilla door was not found");
        }

        foreach (Transform child in door.Cast<Transform>().ToList())
        {
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        foreach (Collider collider in door.GetComponents<Collider>())
        {
            UnityEngine.Object.DestroyImmediate(collider);
        }

        GateLeafDriver driver = prefab.AddComponent<GateLeafDriver>();
        driver.m_door = door;
        foreach (JoineryLeaf leaf in model.Leaves)
        {
            GameObject pivot = new(leaf.Mirrored ? "leaf_left" : "leaf") { layer = layer };
            pivot.transform.SetParent(prefab.transform, false);
            pivot.transform.localPosition = leaf.Pivot;
            Rigidbody body = pivot.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            AddModel(pivot.transform, "high", leaf.High, layer, high);
            AddModel(pivot.transform, "low", leaf.Low, layer, low);
            AddBox(pivot.transform, "collider", leaf.BoxCenter, Quaternion.identity, leaf.BoxSize, layer);
            if (leaf.Mirrored)
            {
                driver.m_mirroredLeaf = pivot.transform;
            }
            else
            {
                driver.m_leaf = pivot.transform;
            }
        }
    }

    private static void AddBox(Transform parent, string name, Vector3 center, Quaternion rotation, Vector3 size, int layer)
    {
        GameObject box = new(name) { layer = layer };
        box.transform.SetParent(parent, false);
        box.transform.localPosition = center;
        box.transform.localRotation = rotation;
        box.AddComponent<BoxCollider>().size = size;
    }

    // The double gate keeps the vanilla gate's own colliders for the posts and the arch: once moved right, once
    // mirrored and moved left.
    private static void CopyGateColliders(Transform gate, Transform root, int layer)
    {
        foreach (BoxCollider box in gate.Cast<Transform>().Select(child => child.GetComponent<BoxCollider>()).Where(box => box != null))
        {
            Transform t = box.transform;
            Vector3 size = Vector3.Scale(box.size, t.localScale);
            Vector3 center = t.localPosition + t.localRotation * Vector3.Scale(box.center, t.localScale);
            Vector3 euler = t.localRotation.eulerAngles;
            AddBox(root, "collider", center + Vector3.right, t.localRotation, size, layer);
            AddBox(root, "collider", new Vector3(-center.x - 1f, center.y, center.z), Quaternion.Euler(euler.x, -euler.y, -euler.z), size, layer);
        }
    }

    // A triangular prism (a gable), convex, for the collider and the ghost's placement.
    private static Mesh PrismMesh(Vector2[] corners, float depth)
    {
        List<Vector3> vertices = new();
        foreach (float z in new[] { -depth / 2f, depth / 2f })
        {
            vertices.AddRange(corners.Select(corner => new Vector3(corner.x, corner.y, z)));
        }

        Mesh mesh = new() { name = "gable_collider" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(new[] { 0, 1, 2, 3, 5, 4, 0, 3, 4, 0, 4, 1, 1, 4, 5, 1, 5, 2, 2, 5, 3, 2, 3, 0 }, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    // Near and far models swap at the gate's own distances.
    private static void LinkLods(GameObject prefab, GameObject gate, List<Renderer> high, List<Renderer> low)
    {
        LOD[] template = gate.GetComponent<LODGroup>()?.GetLODs();
        float nearTo = template != null && template.Length > 0 ? template[0].screenRelativeTransitionHeight : 0.3f;
        float farTo = template != null && template.Length > 1 ? template[1].screenRelativeTransitionHeight : 0.01f;
        LODGroup group = prefab.AddComponent<LODGroup>();
        group.SetLODs(new[] { new LOD(nearTo, high.ToArray()), new LOD(farTo, low.ToArray()) });
        group.RecalculateBounds();
    }

    // The darkwood pieces follow the vanilla darkwood gate in the hammer.
    private static void PlaceAfterTheGate()
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
        int after = pieces.FindIndex(prefab => prefab != null && prefab.name == Gate);
        pieces.InsertRange(after < 0 ? pieces.Count : after + 1, ours);
    }
}

/// <summary>A darkwood door 3 m high: the gate's leaf with three straight panels, on a post under a lintel.</summary>
public sealed class DarkwoodDoor3 : DarkwoodJoineryBase
{
    /// <summary>Registers the piece's text.</summary>
    /// <param name="instance">The piece manager.</param>
    public DarkwoodDoor3(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string PrefabName => "piece_whitehilt_darkwood_door_3";
    /// <inheritdoc/>
    protected override string FullName => "Darkwood Door 3 m";
    /// <inheritdoc/>
    protected override string Description => "A door of the darkwood gate with three straight panels, its hinges and ring, on a post under a lintel. 2 m wide and 3 m high, for the walls of a house.";
    /// <inheritdoc/>
    protected override JoineryKind Kind => JoineryKind.Door;
    /// <inheritdoc/>
    protected override float Height => 3f;
    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 12, Recover = true },
        new() { Item = "Iron", Amount = 3, Recover = true },
        new() { Item = "Tar", Amount = 2, Recover = true }
    };
}

/// <summary>A darkwood door 4 m high, as high as the darkwood gate it stands beside.</summary>
public sealed class DarkwoodDoor4 : DarkwoodJoineryBase
{
    /// <summary>Registers the piece's text.</summary>
    /// <param name="instance">The piece manager.</param>
    public DarkwoodDoor4(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string PrefabName => "piece_whitehilt_darkwood_door_4";
    /// <inheritdoc/>
    protected override string FullName => "Darkwood Door 4 m";
    /// <inheritdoc/>
    protected override string Description => "A door of the darkwood gate with three straight panels, its hinges and ring, on a post under a lintel. 2 m wide and 4 m high, as high as the darkwood gate it can stand beside.";
    /// <inheritdoc/>
    protected override JoineryKind Kind => JoineryKind.Door;
    /// <inheritdoc/>
    protected override float Height => 4f;
    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 16, Recover = true },
        new() { Item = "Iron", Amount = 4, Recover = true },
        new() { Item = "Tar", Amount = 2, Recover = true }
    };
}

/// <summary>The darkwood gate with both its leaves as one piece, 4 m wide.</summary>
public sealed class DarkwoodDoubleGate : DarkwoodJoineryBase
{
    /// <summary>Registers the piece's text.</summary>
    /// <param name="instance">The piece manager.</param>
    public DarkwoodDoubleGate(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string PrefabName => "piece_whitehilt_darkwood_gate_double";
    /// <inheritdoc/>
    protected override string FullName => "Darkwood Double Gate";
    /// <inheritdoc/>
    protected override string Description => "The darkwood gate with both leaves under one arch, built as one piece, so the two halves need not be placed against each other. 4 m wide and 4 m high; both leaves open together.";
    /// <inheritdoc/>
    protected override JoineryKind Kind => JoineryKind.DoubleGate;
    /// <inheritdoc/>
    protected override float Height => 4f;
    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 32, Recover = true },
        new() { Item = "Iron", Amount = 8, Recover = true },
        new() { Item = "Tar", Amount = 4, Recover = true }
    };
}

/// <summary>A darkwood wall: a post and a fixed leaf of the gate, without iron, under a lintel.</summary>
public abstract class DarkwoodWallBase : DarkwoodJoineryBase
{
    /// <summary>Registers the piece's text.</summary>
    /// <param name="instance">The piece manager.</param>
    protected DarkwoodWallBase(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override JoineryKind Kind => JoineryKind.Wall;

    /// <inheritdoc/>
    protected override string FullName => $"Darkwood Wall 2×{Height:0}";

    /// <inheritdoc/>
    protected override string Description => $"A post and a panelled leaf of the darkwood gate, without iron, under a lintel. 2 m wide and {Height:0} m high; its rails line up with a darkwood door of the same height.";

    /// <inheritdoc/>
    protected override string PrefabName => $"piece_whitehilt_darkwood_wall_2x{Height:0}";

    // Wood and tar for the area, as the gate costs per square metre.
    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = Mathf.RoundToInt(4f * Height), Recover = true },
        new() { Item = "Tar", Amount = Mathf.CeilToInt(Height / 2f), Recover = true }
    };
}

/// <summary>A darkwood wall 2 m wide and 1 m high.</summary>
public sealed class DarkwoodWall1 : DarkwoodWallBase
{
    /// <summary>Registers the piece's text.</summary>
    /// <param name="instance">The piece manager.</param>
    public DarkwoodWall1(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override float Height => 1f;
}

/// <summary>A darkwood wall 2 m wide and 2 m high.</summary>
public sealed class DarkwoodWall2 : DarkwoodWallBase
{
    /// <summary>Registers the piece's text.</summary>
    /// <param name="instance">The piece manager.</param>
    public DarkwoodWall2(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override float Height => 2f;
}

/// <summary>A darkwood wall 2 m wide and 3 m high, beside the 3 m door.</summary>
public sealed class DarkwoodWall3 : DarkwoodWallBase
{
    /// <summary>Registers the piece's text.</summary>
    /// <param name="instance">The piece manager.</param>
    public DarkwoodWall3(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override float Height => 3f;
}

/// <summary>A darkwood wall 2 m wide and 4 m high, beside the 4 m door and the gate.</summary>
public sealed class DarkwoodWall4 : DarkwoodWallBase
{
    /// <summary>Registers the piece's text.</summary>
    /// <param name="instance">The piece manager.</param>
    public DarkwoodWall4(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override float Height => 4f;
}

/// <summary>A darkwood gable wall under a beam that slopes like a roof.</summary>
public abstract class DarkwoodGableBase : DarkwoodJoineryBase
{
    /// <summary>Registers the piece's text.</summary>
    /// <param name="instance">The piece manager.</param>
    protected DarkwoodGableBase(PieceManager instance) : base(instance) { }

    /// <summary>Roof pitch in degrees, for the name: 26, 45 or 67.</summary>
    protected abstract int Pitch { get; }

    /// <inheritdoc/>
    protected override JoineryKind Kind => JoineryKind.Gable;

    /// <inheritdoc/>
    protected override string FullName => $"Darkwood Gable Wall {Pitch}°";

    /// <inheritdoc/>
    protected override string Description => $"A darkwood wall cut under a beam that slopes like the {Pitch}° roofs, rising {Height:0} m over its 2 m, to close a gable under a darkwood roof.";

    /// <inheritdoc/>
    protected override string PrefabName => $"piece_whitehilt_darkwood_wall_{Pitch}";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = Mathf.RoundToInt(2f * Height), Recover = true },
        new() { Item = "Tar", Amount = 1, Recover = true }
    };
}

/// <summary>A darkwood gable wall for the 26° roofs.</summary>
public sealed class DarkwoodGable26 : DarkwoodGableBase
{
    /// <summary>Registers the piece's text.</summary>
    /// <param name="instance">The piece manager.</param>
    public DarkwoodGable26(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override int Pitch => 26;
    /// <inheritdoc/>
    protected override float Height => 1f;
}

/// <summary>A darkwood gable wall for the 45° roofs.</summary>
public sealed class DarkwoodGable45 : DarkwoodGableBase
{
    /// <summary>Registers the piece's text.</summary>
    /// <param name="instance">The piece manager.</param>
    public DarkwoodGable45(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override int Pitch => 45;
    /// <inheritdoc/>
    protected override float Height => 2f;
}

/// <summary>A darkwood gable wall for the 67° roofs.</summary>
public sealed class DarkwoodGable67 : DarkwoodGableBase
{
    /// <summary>Registers the piece's text.</summary>
    /// <param name="instance">The piece manager.</param>
    public DarkwoodGable67(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override int Pitch => 67;
    /// <inheritdoc/>
    protected override float Height => 4f;
}
