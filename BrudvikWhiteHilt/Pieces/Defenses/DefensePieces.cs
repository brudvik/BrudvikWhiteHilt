using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Defenses;

/// <summary>
/// 4 m of palisade with a walkway 2 m up behind it.
/// </summary>
public class PalisadeRampart : DefensePieceBase
{
    /// <summary>
    /// Constructor for the PalisadeRampart class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public PalisadeRampart(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "skansevegg";

    /// <inheritdoc/>
    protected override string FullName => "Palisade Rampart";

    /// <inheritdoc/>
    protected override string Description => "Four metres of sharpened stakes with a walkway behind them. Stand on it and shoot over the points.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "RoundLog", Amount = 8, Recover = true },
        new() { Item = "Wood", Amount = 10, Recover = true },
        new() { Item = "Stone", Amount = 4, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(1600f);

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;
}

/// <summary>
/// A square corner of the palisade, with the walkway going round it.
/// </summary>
public class PalisadeCorner : DefensePieceBase
{
    /// <summary>
    /// Constructor for the PalisadeCorner class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public PalisadeCorner(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "skansehjorne";

    /// <inheritdoc/>
    protected override string FullName => "Rampart Corner";

    /// <inheritdoc/>
    protected override string Description => "A square corner for the palisade rampart, with a thick corner stake. The walkway goes round it.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "RoundLog", Amount = 6, Recover = true },
        new() { Item = "Wood", Amount = 6, Recover = true },
        new() { Item = "Stone", Amount = 4, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(1200f);

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;
}

/// <summary>
/// A 45 degree bend in the palisade, with the walkway going round it.
/// </summary>
public class PalisadeCorner45 : DefensePieceBase
{
    /// <summary>
    /// Constructor for the PalisadeCorner45 class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public PalisadeCorner45(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "skansehjorne45";

    /// <inheritdoc/>
    protected override string FullName => "Rampart Bend";

    /// <inheritdoc/>
    protected override string Description => "Turns the palisade rampart by 45 degrees, either way. The walkway goes round it.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "RoundLog", Amount = 6, Recover = true },
        new() { Item = "Wood", Amount = 6, Recover = true },
        new() { Item = "Stone", Amount = 4, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(1200f);

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;
}

/// <summary>
/// Stairs from the ground up to the rampart walkway.
/// </summary>
public class RampartStairs : DefensePieceBase
{
    /// <summary>
    /// Constructor for the RampartStairs class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public RampartStairs(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "skansetrapp";

    /// <inheritdoc/>
    protected override string FullName => "Rampart Stairs";

    /// <inheritdoc/>
    protected override string Description => "Stairs up to the rampart walkway. Snap the top to the back edge of the walkway.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 12, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(600f);

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Start;
}

/// <summary>
/// A gatehouse: a double gate of stakes between two small towers, with a walk over the gate.
/// </summary>
public class PalisadeGatehouse : DefensePieceBase
{
    /// <summary>
    /// Constructor for the PalisadeGatehouse class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public PalisadeGatehouse(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "skanseport";

    /// <inheritdoc/>
    protected override string FullName => "Gatehouse";

    /// <inheritdoc/>
    protected override string Description => "A double gate of iron-banded stakes between two small towers. Stairs in the towers lead from the rampart walkway to the walk over the gate.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "RoundLog", Amount = 30, Recover = true },
        new() { Item = "Wood", Amount = 30, Recover = true },
        new() { Item = "Bronze", Amount = 4, Recover = true },
        new() { Item = "Stone", Amount = 10, Recover = true },
        new() { Item = "Resin", Amount = 6, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(4000f);

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;

    // The vanilla door stays as an invisible driver: its Door component and animation open and close the gate.
    /// <inheritdoc/>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        Transform door = prefab.transform.Find("door");
        if (door == null)
        {
            Jotunn.Logger.LogWarning($"{FullName}: the vanilla door was not found, the gate will not open.");
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
        driver.m_leaf = groups.TryGetValue("leaf_right", out Transform right) ? right : null;
        driver.m_mirroredLeaf = groups.TryGetValue("leaf_left", out Transform left) ? left : null;
    }
}

/// <summary>
/// Base class for the watchtowers, which differ only in size.
/// </summary>
public abstract class WatchtowerBase : DefensePieceBase
{
    /// <summary>
    /// Constructor for the WatchtowerBase class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    protected WatchtowerBase(PieceManager instance) : base(instance)
    {
        Translations.AddEnglish("whitehilt_ladder", "Ladder");
        Translations.AddEnglish("whitehilt_ladder_up", "Climb up");
        Translations.AddEnglish("whitehilt_ladder_down", "Climb down");
    }

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;
}

/// <summary>
/// A 2 x 2 m watchtower with two floors and a roof.
/// </summary>
public class WatchtowerSmall : WatchtowerBase
{
    /// <summary>
    /// Constructor for the WatchtowerSmall class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public WatchtowerSmall(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "vakttarn_liten";

    /// <inheritdoc/>
    protected override string FullName => "Small Watchtower";

    /// <inheritdoc/>
    protected override string Description => "A narrow roofed tower. Its first floor is level with the rampart walkway; a ladder leads to the parapet at the top.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "RoundLog", Amount = 12, Recover = true },
        new() { Item = "Wood", Amount = 20, Recover = true },
        new() { Item = "Stone", Amount = 4, Recover = true },
        new() { Item = "Resin", Amount = 4, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(2000f);
}

/// <summary>
/// A 3 x 3 m watchtower with two floors and a roof.
/// </summary>
public class WatchtowerMedium : WatchtowerBase
{
    /// <summary>
    /// Constructor for the WatchtowerMedium class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public WatchtowerMedium(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "vakttarn";

    /// <inheritdoc/>
    protected override string FullName => "Watchtower";

    /// <inheritdoc/>
    protected override string Description => "A roofed tower with an overhanging parapet. Its first floor is level with the rampart walkway; a ladder leads to the top.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "RoundLog", Amount = 20, Recover = true },
        new() { Item = "Wood", Amount = 30, Recover = true },
        new() { Item = "Stone", Amount = 6, Recover = true },
        new() { Item = "Resin", Amount = 6, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(3000f);
}

/// <summary>
/// A 4 x 4 m watchtower with three floors and a roof.
/// </summary>
public class WatchtowerLarge : WatchtowerBase
{
    /// <summary>
    /// Constructor for the WatchtowerLarge class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public WatchtowerLarge(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "vakttarn_stor";

    /// <inheritdoc/>
    protected override string FullName => "Large Watchtower";

    /// <inheritdoc/>
    protected override string Description => "A broad three-storey tower with parapets on the two upper floors. Its first floor is level with the rampart walkway.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "RoundLog", Amount = 32, Recover = true },
        new() { Item = "Wood", Amount = 50, Recover = true },
        new() { Item = "Stone", Amount = 10, Recover = true },
        new() { Item = "Resin", Amount = 10, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(4500f);
}

/// <summary>
/// A cheval de frise: a log with crossed sharpened stakes that hurts creatures running into it.
/// </summary>
public class ChevalDeFrise : DefensePieceBase
{
    /// <summary>
    /// Constructor for the ChevalDeFrise class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public ChevalDeFrise(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "spansk_rytter";

    /// <inheritdoc/>
    protected override string FullName => "Cheval de Frise";

    /// <inheritdoc/>
    protected override string Description => "A log bristling with crossed, sharpened stakes. Creatures that run into it get hurt, like on sharp stakes.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "RoundLog", Amount = 4, Recover = true },
        new() { Item = "Wood", Amount = 4, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(500f);

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;

    // The vanilla sharp stakes hurt through their HIT AREA trigger; it is stretched to the longer log.
    /// <inheritdoc/>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        Transform hitArea = prefab.transform.Find("HIT AREA");
        BoxCollider trigger = hitArea != null ? hitArea.GetComponent<BoxCollider>() : null;
        if (trigger == null || data.hitArea?.center == null)
        {
            Jotunn.Logger.LogWarning($"{FullName}: the vanilla hit area was not found, it will not hurt creatures.");
            return;
        }

        ReadBox(data.hitArea, out Vector3 position, out Quaternion rotation, out Vector3 size);
        hitArea.localPosition = position;
        hitArea.localRotation = rotation;
        hitArea.localScale = Vector3.one;
        trigger.center = Vector3.zero;
        trigger.size = size;
    }
}
