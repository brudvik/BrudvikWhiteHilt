using BrudvikWhiteHilt.Pieces.Defenses;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.LogHouse;

/// <summary>
/// A door of the log house, 2 m high, that snaps in place of a wall like the vanilla door. As on the gatehouse, the
/// vanilla door the piece is cloned from stays as an invisible driver: its Door component and animation open, close and
/// sync it (and the White Hilt self-closing doors close it), and the layout's leaves follow its swing.
/// </summary>
public abstract class LogHouseDoorBase : DefensePieceBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    protected LogHouseDoorBase(PieceManager instance) : base(instance)
    {
    }

    /// <summary>The leaf that turns with the vanilla door, hung on its +x edge.</summary>
    protected virtual string LeafGroup => "leaf";

    /// <summary>The leaf that turns the other way, hung on its -x edge, or null for a single door.</summary>
    protected virtual string MirroredLeafGroup => null;

    /// <inheritdoc/>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        Transform door = prefab.transform.Find("door");
        if (door == null)
        {
            Jotunn.Logger.LogWarning($"{FullName}: the vanilla door was not found, the door will not open.");
            return;
        }

        foreach (Renderer renderer in door.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = false;
        }

        foreach (Collider collider in door.GetComponentsInChildren<Collider>(true))
        {
            Object.DestroyImmediate(collider);
        }

        GateLeafDriver driver = prefab.AddComponent<GateLeafDriver>();
        driver.m_door = door;
        driver.m_leaf = groups.TryGetValue(LeafGroup, out Transform leaf) ? leaf : null;
        driver.m_mirroredLeaf = MirroredLeafGroup != null && groups.TryGetValue(MirroredLeafGroup, out Transform mirrored) ? mirrored : null;
    }
}

/// <summary>A board door on iron strap hinges between two hewn posts.</summary>
public sealed class PlankDoor : LogHouseDoorBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public PlankDoor(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "plankedor";
    /// <inheritdoc/>
    protected override string FullName => "Plank Door";
    /// <inheritdoc/>
    protected override string Description => "A door of tarred boards on strap hinges between two hewn posts, 2 m wide and 2 m high. It snaps in place of a 2 m wall, log or vanilla.";
    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 6, Recover = true },
        new() { Item = "Resin", Amount = 2, Recover = false }
    };
    /// <inheritdoc/>
    protected override float Health => 400f;
    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Start;
}

/// <summary>A door in a board portal with dragon heads.</summary>
public sealed class DragonPortal : LogHouseDoorBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public DragonPortal(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "dorportal";
    /// <inheritdoc/>
    protected override string FullName => "Dragon Portal";
    /// <inheritdoc/>
    protected override string Description => "A door in a portal of broad boards, with two dragon heads looking out from the posts and a bronze ring on the door, as at the entrance of a hall or a stave church. 2 m wide and 2 m high.";
    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "FineWood", Amount = 8, Recover = true },
        new() { Item = "Wood", Amount = 4, Recover = true },
        new() { Item = "Bronze", Amount = 1, Recover = true }
    };
    /// <inheritdoc/>
    protected override float Health => 600f;
    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;
}

/// <summary>Double barn doors, wide enough for a cart.</summary>
public sealed class BarnDoors : LogHouseDoorBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public BarnDoors(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "lavedor";
    /// <inheritdoc/>
    protected override string FullName => "Barn Doors";
    /// <inheritdoc/>
    protected override string Description => "Two tarred board doors hung on hewn posts, 4 m wide and 2 m high: wide enough to pull a cart into the barn. They snap in place of a 4 m wall.";
    /// <inheritdoc/>
    protected override string LeafGroup => "leaf_right";
    /// <inheritdoc/>
    protected override string MirroredLeafGroup => "leaf_left";
    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 16, Recover = true },
        new() { Item = "Resin", Amount = 4, Recover = false }
    };
    /// <inheritdoc/>
    protected override float Health => 800f;
    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Start;
}
