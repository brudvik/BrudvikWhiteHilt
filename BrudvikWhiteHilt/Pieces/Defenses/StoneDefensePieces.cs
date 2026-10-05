using Jotunn.Configs;
using Jotunn.Managers;
using BrudvikWhiteHilt.Progression;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Defenses;

/// <summary>
/// What makes the stone defences stone: built at the Stonecutter, the stone material, and hard to break. Blunt blows,
/// which trolls deal, do half damage; fire, frost, poison and spirit none; rain does not wear them.
/// </summary>
internal static class StoneDefense
{
    /// <summary>The station every stone defence is built near.</summary>
    internal static string Station => CraftingStations.Stonecutter;

    /// <summary>
    /// Turns a defence prefab into stone, whatever vanilla piece it was cloned from.
    /// </summary>
    /// <param name="prefab">The piece prefab.</param>
    internal static void Harden(GameObject prefab)
    {
        WearNTear wear = prefab.GetComponent<WearNTear>();
        if (wear == null)
        {
            return;
        }

        wear.m_materialType = WearNTear.MaterialType.Stone;
        wear.m_noRoofWear = true;
        wear.m_damages = new HitData.DamageModifiers
        {
            m_blunt = HitData.DamageModifier.Resistant,
            m_slash = HitData.DamageModifier.Resistant,
            m_pierce = HitData.DamageModifier.VeryResistant,
            m_chop = HitData.DamageModifier.Resistant,
            m_pickaxe = HitData.DamageModifier.Normal,
            m_fire = HitData.DamageModifier.Immune,
            m_frost = HitData.DamageModifier.Immune,
            m_lightning = HitData.DamageModifier.Normal,
            m_poison = HitData.DamageModifier.Immune,
            m_spirit = HitData.DamageModifier.Immune
        };
    }
}

/// <summary>
/// Base class for the stone walls, corners and stairs.
/// </summary>
public abstract class StoneDefensePieceBase : DefensePieceBase
{
    /// <summary>
    /// Constructor for the StoneDefensePieceBase class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    protected StoneDefensePieceBase(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BuildStation => StoneDefense.Station;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;

    /// <inheritdoc/>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        StoneDefense.Harden(prefab);
    }
}

/// <summary>
/// 4 m of stone curtain wall with the walk on top, 3 m up, a buttress in front and a crenellated parapet.
/// </summary>
public class StoneRampart : StoneDefensePieceBase
{
    /// <summary>
    /// Constructor for the StoneRampart class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StoneRampart(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "steinmur";

    /// <inheritdoc/>
    protected override string FullName => "Stone Rampart";

    /// <inheritdoc/>
    protected override string Description => "Four metres of thick stone wall with a buttress in front. Its walk runs along the top, three metres up, behind merlons with arrow slits.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = 40, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(4000f);
}

/// <summary>
/// The stone curtain wall without a buttress, so long walls can alternate.
/// </summary>
public class StoneRampartPlain : StoneDefensePieceBase
{
    /// <summary>
    /// Constructor for the StoneRampartPlain class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StoneRampartPlain(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "steinmur_slett";

    /// <inheritdoc/>
    protected override string FullName => "Plain Stone Rampart";

    /// <inheritdoc/>
    protected override string Description => "The stone rampart without a buttress, to alternate with it along a long wall.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = 36, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(4000f);
}

/// <summary>
/// A square corner of the stone wall with a round bastion over the outside corner, level with the walk.
/// </summary>
public class StoneCorner : StoneDefensePieceBase
{
    /// <summary>
    /// Constructor for the StoneCorner class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StoneCorner(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "steinhjorne";

    /// <inheritdoc/>
    protected override string FullName => "Stone Corner Bastion";

    /// <inheritdoc/>
    protected override string Description => "Two stone walls meeting at a right angle, with a round bastion standing out over the corner. Its top joins the wall walks.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = 50, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(3500f);
}

/// <summary>
/// A 45 degree corner of the stone wall with an angle buttress over the joint.
/// </summary>
public class StoneCorner45 : StoneDefensePieceBase
{
    /// <summary>
    /// Constructor for the StoneCorner45 class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StoneCorner45(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "steinhjorne45";

    /// <inheritdoc/>
    protected override string FullName => "Stone Corner 45";

    /// <inheritdoc/>
    protected override string Description => "Two stone walls meeting at 45 degrees, with a buttress over the joint, to turn a wall gently.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = 40, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(3500f);
}

/// <summary>
/// Solid stone steps up the back of the wall to the walk.
/// </summary>
public class StoneStairs : StoneDefensePieceBase
{
    /// <summary>
    /// Constructor for the StoneStairs class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StoneStairs(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "steintrapp";

    /// <inheritdoc/>
    protected override string FullName => "Stone Rampart Stairs";

    /// <inheritdoc/>
    protected override string Description => "Twelve solid stone steps with a cheek wall, from the ground up to the stone wall walk.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = 30, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(2000f);
}

/// <summary>
/// The stone gatehouse: the palisade's gate leaves in an arched gateway between two D-shaped towers, with a raised
/// portcullis and machicolations. Ladders in the towers lead from the wall walks to the walk over the gate.
/// </summary>
public class StoneGatehouse : PalisadeGatehouse
{
    /// <summary>
    /// Constructor for the StoneGatehouse class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StoneGatehouse(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "steinport";

    /// <inheritdoc/>
    protected override string FullName => "Stone Gatehouse";

    /// <inheritdoc/>
    protected override string Description => "A double gate under a stone arch between two round-fronted towers, with a portcullis and machicolations over it. Ladders in the towers lead from the wall walks to the walk over the gate and to the tower tops.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = 120, Recover = true },
        new() { Item = "RoundLog", Amount = 20, Recover = true },
        new() { Item = "Wood", Amount = 20, Recover = true },
        new() { Item = "Iron", Amount = 10, Recover = true }
    };

    /// <inheritdoc/>
    protected override string BuildStation => StoneDefense.Station;

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(10000f);

    /// <inheritdoc/>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        base.CustomizePrefab(prefab, data, groups);
        StoneDefense.Harden(prefab);
    }
}

/// <summary>
/// The palisade's drawbridge deck between two stone piers joined by a crenellated lintel.
/// </summary>
public class StoneDrawbridge : Drawbridge
{
    /// <summary>
    /// Constructor for the StoneDrawbridge class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StoneDrawbridge(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "steinvindebro";

    /// <inheritdoc/>
    protected override string FullName => "Stone Drawbridge";

    /// <inheritdoc/>
    protected override string Description => "A deck nine metres long hinged between two stone piers, with chains to the crenellated lintel. Open it to lower it and close it to raise it; it follows the nearest gate.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = 30, Recover = true },
        new() { Item = "RoundLog", Amount = 10, Recover = true },
        new() { Item = "Wood", Amount = 20, Recover = true },
        new() { Item = "Iron", Amount = 4, Recover = true },
        new() { Item = "Chain", Amount = 2, Recover = true }
    };

    /// <inheritdoc/>
    protected override string BuildStation => StoneDefense.Station;

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(6000f);

    /// <inheritdoc/>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        base.CustomizePrefab(prefab, data, groups);
        StoneDefense.Harden(prefab);
    }
}

/// <summary>
/// Dragon's teeth: stone posts with pointed caps that hurt creatures running into them, like the cheval de frise.
/// </summary>
public class DragonsTeeth : ChevalDeFrise
{
    /// <summary>
    /// Constructor for the DragonsTeeth class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public DragonsTeeth(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "draketenner";

    /// <inheritdoc/>
    protected override string FullName => "Dragon's Teeth";

    /// <inheritdoc/>
    protected override string Description => "Two staggered rows of stone posts with sharp points. Creatures that run into them get hurt, and they stand up to a troll far longer than wood.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = 20, Recover = true }
    };

    /// <inheritdoc/>
    protected override string BuildStation => StoneDefense.Station;

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(1500f);

    /// <inheritdoc/>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        base.CustomizePrefab(prefab, data, groups);
        StoneDefense.Harden(prefab);
    }
}

/// <summary>
/// Base class for the stone towers, which differ only in size.
/// </summary>
public abstract class StoneTowerBase : WatchtowerBase
{
    /// <summary>
    /// Constructor for the StoneTowerBase class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    protected StoneTowerBase(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BuildStation => StoneDefense.Station;

    /// <inheritdoc/>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        base.CustomizePrefab(prefab, data, groups);
        StoneDefense.Harden(prefab);
    }
}

/// <summary>
/// A 2 x 2 m stone tower with two floors and an open, crenellated top.
/// </summary>
public class StoneTowerSmall : StoneTowerBase
{
    /// <summary>
    /// Constructor for the StoneTowerSmall class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StoneTowerSmall(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "steintarn_liten";

    /// <inheritdoc/>
    protected override string FullName => "Small Stone Tower";

    /// <inheritdoc/>
    protected override string Description => "A narrow stone tower. A door at the foot, doorways to the stone wall walks on both sides, and a ladder up to the crenellated top.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = 60, Recover = true },
        new() { Item = "Wood", Amount = 10, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(5000f);
}

/// <summary>
/// A 3 x 3 m stone tower with two floors and an open, crenellated top.
/// </summary>
public class StoneTowerMedium : StoneTowerBase
{
    /// <summary>
    /// Constructor for the StoneTowerMedium class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StoneTowerMedium(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "steintarn";

    /// <inheritdoc/>
    protected override string FullName => "Stone Tower";

    /// <inheritdoc/>
    protected override string Description => "A stone tower with dressed corner stones and a parapet on corbels. A door at the foot, doorways to the stone wall walks, and a ladder to the top.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = 90, Recover = true },
        new() { Item = "Wood", Amount = 16, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(7500f);
}

/// <summary>
/// A 4 x 4 m stone tower with three floors and a slate roof.
/// </summary>
public class StoneTowerLarge : StoneTowerBase
{
    /// <summary>
    /// Constructor for the StoneTowerLarge class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StoneTowerLarge(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "steintarn_stor";

    /// <inheritdoc/>
    protected override string FullName => "Large Stone Tower";

    /// <inheritdoc/>
    protected override string Description => "A broad three-storey stone tower under a slate roof. A door at the foot, doorways to the stone wall walks, and a ladder through every floor.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = 130, Recover = true },
        new() { Item = "Wood", Amount = 30, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(11000f);
}
