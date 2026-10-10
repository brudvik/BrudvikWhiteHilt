using BrudvikWhiteHilt.Pieces.Defenses;
using BrudvikWhiteHilt.Pieces.LogHouse;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Farming;

/// <summary>
/// A fence or wall of the farm built from wood alone. The shapes are in the layout (AssetSource/Preview/build_farm.py).
/// </summary>
public abstract class FarmPieceBase : DefensePieceBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    protected FarmPieceBase(PieceManager instance) : base(instance)
    {
    }

    /// <summary>Wood needed.</summary>
    protected abstract int Wood { get; }

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = Wood, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => 60f * Wood;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Start;
}

/// <summary>A 4 m skigard.</summary>
public sealed class Skigard : FarmPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public Skigard(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "skigard";
    /// <inheritdoc/>
    protected override string FullName => "Skigard";
    /// <inheritdoc/>
    protected override string Description => "The Norwegian round-pole fence, 4 m long and 1.3 m high: pairs of stakes bound with withies, and grey poles laid slanting in the bands. Sections snap end to end and run on without a seam; the last one slopes down to the ground.";
    /// <inheritdoc/>
    protected override int Wood => 8;
}

/// <summary>A 2 m skigard.</summary>
public sealed class SkigardShort : FarmPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public SkigardShort(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "skigard_2m";
    /// <inheritdoc/>
    protected override string FullName => "Skigard 2 m";
    /// <inheritdoc/>
    protected override string Description => "A skigard 2 m long, to fill a gap.";
    /// <inheritdoc/>
    protected override int Wood => 4;
}

/// <summary>A 2 m wattle fence.</summary>
public sealed class WattleFence : FarmPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public WattleFence(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "flettgjerde";
    /// <inheritdoc/>
    protected override string FullName => "Wattle Fence";
    /// <inheritdoc/>
    protected override string Description => "Withies woven in and out between stakes, 2 m long and 1 m high, round a garden or a pen.";
    /// <inheritdoc/>
    protected override int Wood => 3;
}

/// <summary>A wattle-and-daub wall, 2 m.</summary>
public sealed class WattleAndDaubWall : FarmPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public WattleAndDaubWall(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "bindingsverk";
    /// <inheritdoc/>
    protected override string FullName => "Wattle and Daub Wall";
    /// <inheritdoc/>
    protected override string Description => "A timber frame with a brace and clay daubed on wattle between the timbers, 2 m long and 2 m high: the cheapest wall of the Meadows, for a byre, a smithy or a house. The Paint Bench can whitewash it.";
    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 4, Recover = true },
        new() { Item = "Stone", Amount = 2, Recover = false }
    };
    /// <inheritdoc/>
    protected override int Wood => 4;
}

/// <summary>A wattle-and-daub wall, 1 m.</summary>
public sealed class WattleAndDaubWallShort : FarmPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public WattleAndDaubWallShort(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "bindingsverk_1m";
    /// <inheritdoc/>
    protected override string FullName => "Wattle and Daub Wall 1 m";
    /// <inheritdoc/>
    protected override string Description => "A wattle-and-daub wall 1 m long and 2 m high.";
    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 2, Recover = true },
        new() { Item = "Stone", Amount = 1, Recover = false }
    };
    /// <inheritdoc/>
    protected override int Wood => 2;
}

/// <summary>A timber frame with bare wattle.</summary>
public sealed class WattleWall : FarmPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public WattleWall(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "flettverk";
    /// <inheritdoc/>
    protected override string FullName => "Wattle Wall";
    /// <inheritdoc/>
    protected override string Description => "A timber frame with bare wattle woven between the timbers, 2 m long and 2 m high, for a shed or a pen where the wind may blow through.";
    /// <inheritdoc/>
    protected override int Wood => 4;
}

/// <summary>A gate in the skigard.</summary>
public sealed class FarmGate : LogHouseDoorBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public FarmGate(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "grind";
    /// <inheritdoc/>
    protected override string FullName => "Farm Gate";
    /// <inheritdoc/>
    protected override string Description => "A gate of grey poles 2 m wide between two stout posts, for the skigard. Like every door it shuts by itself after you, so the animals stay in.";
    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 6, Recover = true }
    };
    /// <inheritdoc/>
    protected override float Health => 300f;
    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Start;
}

/// <summary>A double gate 4 m wide.</summary>
public sealed class FarmGateDouble : LogHouseDoorBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public FarmGateDouble(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "dobbelgrind";
    /// <inheritdoc/>
    protected override string FullName => "Double Farm Gate";
    /// <inheritdoc/>
    protected override string Description => "Two gates of grey poles opening from the middle, 4 m wide, wide enough for a cart into the field.";
    /// <inheritdoc/>
    protected override string LeafGroup => "leaf_right";
    /// <inheritdoc/>
    protected override string MirroredLeafGroup => "leaf_left";
    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 12, Recover = true }
    };
    /// <inheritdoc/>
    protected override float Health => 500f;
    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Start;
}

/// <summary>A well sweep: use it and it dips the bucket into the well.</summary>
public sealed class WellSweep : LogHouseDoorBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public WellSweep(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "bronnvipp";
    /// <inheritdoc/>
    protected override string FullName => "Well Sweep";
    /// <inheritdoc/>
    protected override string Description => "A well with a timber curb and a sweep (brønnvippe): a long pole on a forked post with a stone at its short end. Use it and the sweep dips the bucket into the well; a while after, it rises again.";
    /// <inheritdoc/>
    protected override string Category => PieceCategories.Misc;
    /// <inheritdoc/>
    protected override (string Group, Vector3 Axis)[] OneWaySwings => new[] { ("sweep", new Vector3(0f, 0f, -0.4f)) };
    /// <inheritdoc/>
    protected override (string Group, string From)[] Hangers => new[] { ("bucket", "sweep") };
    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 12, Recover = true },
        new() { Item = "Stone", Amount = 6, Recover = true }
    };
    /// <inheritdoc/>
    protected override float Health => 400f;
    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Start;
}

/// <summary>
/// A stream mill (bekkekvern): the vanilla windmill without its sails, in a little mill house on stone feet. It grinds
/// barley into flour like the windmill, but at full speed whatever the wind, as water turns it.
/// </summary>
public sealed class StreamMill : DefensePieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StreamMill(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "bekkekvern";
    /// <inheritdoc/>
    protected override string FullName => "Stream Mill";
    /// <inheritdoc/>
    protected override string Description => "A little mill house on stone feet with a horizontal wheel under its floor, as the farms had by their streams. Put barley in the hopper at the back and take the flour from the bin at the front. It grinds like the windmill, but at full speed whatever the wind; set it by a stream.";
    /// <inheritdoc/>
    protected override string Category => PieceCategories.Crafting;
    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 20, Recover = true },
        new() { Item = "FineWood", Amount = 10, Recover = true },
        new() { Item = "Stone", Amount = 20, Recover = true },
        new() { Item = "BlackMetal", Amount = 2, Recover = true }
    };
    /// <inheritdoc/>
    protected override float Health => 1000f;
    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Plains;

    // The windmill's sails are gone, so is its Windmill: without it the smelter grinds at full power.
    /// <inheritdoc/>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        Smelter smelter = prefab.GetComponent<Smelter>();
        if (smelter != null)
        {
            smelter.m_windmill = null;
        }

        Windmill windmill = prefab.GetComponent<Windmill>();
        if (windmill != null)
        {
            Object.DestroyImmediate(windmill);
        }

        // The windmill's own hopper; the mill house has one of its own there.
        Transform add = prefab.transform.Find("add_switch");
        foreach (Renderer renderer in add != null ? add.GetComponentsInChildren<Renderer>(true) : new Renderer[0])
        {
            renderer.enabled = false;
        }

        if (groups.TryGetValue("wheel", out Transform wheel))
        {
            prefab.AddComponent<MillWheel>().m_wheel = wheel;
        }
    }
}

/// <summary>Turns the stream mill's horizontal wheel while it has barley to grind.</summary>
public class MillWheel : MonoBehaviour
{
    private const float DegreesPerSecond = 120f;

    /// <summary>The wheel, turning about its upright shaft.</summary>
    public Transform m_wheel;

    private Smelter smelter;

    private void Awake()
    {
        smelter = GetComponent<Smelter>();
        if (m_wheel == null || smelter == null)
        {
            enabled = false;
        }
    }

    private void Update()
    {
        if (smelter.m_nview != null && smelter.m_nview.IsValid() && smelter.GetQueueSize() > 0)
        {
            m_wheel.Rotate(0f, DegreesPerSecond * Time.deltaTime, 0f, Space.Self);
        }
    }
}
