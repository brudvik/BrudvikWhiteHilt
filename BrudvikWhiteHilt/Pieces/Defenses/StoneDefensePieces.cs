using Jotunn.Configs;
using System.Linq;
using Jotunn.Managers;
using BrudvikWhiteHilt.Progression;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Defenses;

/// <summary>
/// The stones the stone defences come in.
/// </summary>
public enum StoneVariant
{
    /// <summary>Plain stone, from the Black Forest on.</summary>
    Stone,

    /// <summary>Black marble, for the Mistlands.</summary>
    BlackMarble,

    /// <summary>Grausten, for the Ashlands.</summary>
    Grausten
}

/// <summary>
/// What makes the stone defences stone: built at the Stonecutter, the stone material, and hard to break. Blunt blows,
/// which trolls deal, do half damage (a quarter in grausten); fire, frost, poison and spirit none; rain does not wear
/// them. Black marble and grausten are the same pieces in a later stone: stronger, and costing that stone instead.
/// </summary>
internal static class StoneDefense
{
    /// <summary>The station every stone defence is built near.</summary>
    internal static string Station => CraftingStations.Stonecutter;

    /// <summary>The end of the layout name of a variant, e.g. "_marmor".</summary>
    /// <param name="variant">The stone.</param>
    /// <returns>The suffix; empty for plain stone.</returns>
    internal static string Suffix(StoneVariant variant) => variant switch
    {
        StoneVariant.BlackMarble => "_marmor",
        StoneVariant.Grausten => "_grausten",
        _ => string.Empty
    };

    /// <summary>The English name of a piece in a variant: "Stone Rampart" becomes "Black Marble Rampart".</summary>
    /// <param name="variant">The stone.</param>
    /// <param name="stoneName">The plain stone piece's name.</param>
    /// <returns>The name.</returns>
    internal static string Name(StoneVariant variant, string stoneName)
    {
        string word = variant switch
        {
            StoneVariant.BlackMarble => "Black Marble",
            StoneVariant.Grausten => "Grausten",
            _ => "Stone"
        };
        return stoneName.Contains("Stone") ? stoneName.Replace("Stone", word) : variant == StoneVariant.Stone ? stoneName : $"{word} {stoneName}";
    }

    /// <summary>The cost of a piece in a variant: half as much of the later stone in place of Stone, the rest the same.</summary>
    /// <param name="variant">The stone.</param>
    /// <param name="stoneCost">The plain stone piece's cost.</param>
    /// <returns>The cost.</returns>
    internal static RequirementConfig[] Cost(StoneVariant variant, RequirementConfig[] stoneCost)
    {
        if (variant == StoneVariant.Stone)
        {
            return stoneCost;
        }

        string item = variant == StoneVariant.BlackMarble ? "BlackMarble" : "Grausten";
        return stoneCost.Select(requirement => requirement.Item == "Stone"
            ? new RequirementConfig { Item = item, Amount = Mathf.Max(1, requirement.Amount / 2), Recover = requirement.Recover }
            : requirement).ToArray();
    }

    /// <summary>How much more health a variant has than plain stone.</summary>
    /// <param name="variant">The stone.</param>
    /// <returns>The factor.</returns>
    internal static float Strength(StoneVariant variant) => variant switch
    {
        StoneVariant.BlackMarble => 1.5f,
        StoneVariant.Grausten => 2f,
        _ => 1f
    };

    /// <summary>The tier a variant comes on.</summary>
    /// <param name="variant">The stone.</param>
    /// <param name="stoneTier">The plain stone piece's tier.</param>
    /// <returns>The tier.</returns>
    internal static ProgressionTier Tier(StoneVariant variant, ProgressionTier stoneTier) => variant switch
    {
        StoneVariant.BlackMarble => ProgressionTier.Mistlands,
        StoneVariant.Grausten => ProgressionTier.Ashlands,
        _ => stoneTier
    };

    /// <summary>
    /// Turns a defence prefab into stone, whatever vanilla piece it was cloned from.
    /// </summary>
    /// <param name="prefab">The piece prefab.</param>
    /// <param name="variant">The stone; grausten takes only a quarter of blunt damage.</param>
    internal static void Harden(GameObject prefab, StoneVariant variant = StoneVariant.Stone)
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
            m_blunt = variant == StoneVariant.Grausten ? HitData.DamageModifier.VeryResistant : HitData.DamageModifier.Resistant,
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

    /// <summary>The stone the piece is built of; the black marble and grausten subclasses set it.</summary>
    protected virtual StoneVariant Variant => StoneVariant.Stone;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => StoneDefense.Tier(Variant, ProgressionTier.BlackForest);

    /// <inheritdoc/>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        StoneDefense.Harden(prefab, Variant);
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
    protected override string LayoutName => "steinmur" + StoneDefense.Suffix(Variant);

    /// <inheritdoc/>
    protected override string FullName => StoneDefense.Name(Variant, "Stone Rampart");

    /// <inheritdoc/>
    protected override string Description => "Four metres of thick stone wall with a buttress in front. Its walk runs along the top, three metres up, behind merlons with arrow slits.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => StoneDefense.Cost(Variant, new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = 40, Recover = true }
    });

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(4000f * StoneDefense.Strength(Variant));
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
    protected override string LayoutName => "steinmur_slett" + StoneDefense.Suffix(Variant);

    /// <inheritdoc/>
    protected override string FullName => StoneDefense.Name(Variant, "Plain Stone Rampart");

    /// <inheritdoc/>
    protected override string Description => "The stone rampart without a buttress, to alternate with it along a long wall.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => StoneDefense.Cost(Variant, new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = 36, Recover = true }
    });

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(4000f * StoneDefense.Strength(Variant));
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
    protected override string LayoutName => "steinhjorne" + StoneDefense.Suffix(Variant);

    /// <inheritdoc/>
    protected override string FullName => StoneDefense.Name(Variant, "Stone Corner Bastion");

    /// <inheritdoc/>
    protected override string Description => "Two stone walls meeting at a right angle, with a round bastion standing out over the corner. Its top joins the wall walks.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => StoneDefense.Cost(Variant, new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = 50, Recover = true }
    });

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(3500f * StoneDefense.Strength(Variant));
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
    protected override string LayoutName => "steinhjorne45" + StoneDefense.Suffix(Variant);

    /// <inheritdoc/>
    protected override string FullName => StoneDefense.Name(Variant, "Stone Corner 45");

    /// <inheritdoc/>
    protected override string Description => "Two stone walls meeting at 45 degrees, with a buttress over the joint, to turn a wall gently.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => StoneDefense.Cost(Variant, new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = 40, Recover = true }
    });

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(3500f * StoneDefense.Strength(Variant));
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
    protected override string LayoutName => "steintrapp" + StoneDefense.Suffix(Variant);

    /// <inheritdoc/>
    protected override string FullName => StoneDefense.Name(Variant, "Stone Rampart Stairs");

    /// <inheritdoc/>
    protected override string Description => "Twelve solid stone steps with a cheek wall, from the ground up to the stone wall walk.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => StoneDefense.Cost(Variant, new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = 30, Recover = true }
    });

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(2000f * StoneDefense.Strength(Variant));
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

    /// <summary>The stone the piece is built of; the black marble and grausten subclasses set it.</summary>
    protected virtual StoneVariant Variant => StoneVariant.Stone;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => StoneDefense.Tier(Variant, ProgressionTier.BlackForest);

    /// <inheritdoc/>
    protected override string LayoutName => "steinport" + StoneDefense.Suffix(Variant);

    /// <inheritdoc/>
    protected override string FullName => StoneDefense.Name(Variant, "Stone Gatehouse");

    /// <inheritdoc/>
    protected override string Description => "A double gate under a stone arch between two round-fronted towers, with a portcullis and machicolations over it. Work the portcullis with a Gate Rope or a Windlass House. Ladders in the towers lead from the wall walks to the walk over the gate and to the tower tops.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => StoneDefense.Cost(Variant, new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = 120, Recover = true },
        new() { Item = "RoundLog", Amount = 20, Recover = true },
        new() { Item = "Wood", Amount = 20, Recover = true },
        new() { Item = "Iron", Amount = 10, Recover = true }
    });

    /// <inheritdoc/>
    protected override string BuildStation => StoneDefense.Station;

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(10000f * StoneDefense.Strength(Variant));

    /// <inheritdoc/>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        base.CustomizePrefab(prefab, data, groups);
        StoneDefense.Harden(prefab, Variant);
        if (groups.TryGetValue("portcullis", out Transform grate))
        {
            prefab.AddComponent<GateControl.PortcullisDriver>().m_grate = grate;
        }
        else
        {
            Jotunn.Logger.LogWarning($"{FullName}: the portcullis was not found, it will not move.");
        }
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

    /// <summary>The stone the piece is built of; the black marble and grausten subclasses set it.</summary>
    protected virtual StoneVariant Variant => StoneVariant.Stone;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => StoneDefense.Tier(Variant, ProgressionTier.Swamp);

    /// <inheritdoc/>
    protected override string LayoutName => "steinvindebro" + StoneDefense.Suffix(Variant);

    /// <inheritdoc/>
    protected override string FullName => StoneDefense.Name(Variant, "Stone Drawbridge");

    /// <inheritdoc/>
    protected override string Description => "A deck nine metres long hinged between two stone piers, with chains to the crenellated lintel. Open it to lower it and close it to raise it; it follows the nearest gate.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => StoneDefense.Cost(Variant, new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = 30, Recover = true },
        new() { Item = "RoundLog", Amount = 10, Recover = true },
        new() { Item = "Wood", Amount = 20, Recover = true },
        new() { Item = "Iron", Amount = 4, Recover = true },
        new() { Item = "Chain", Amount = 2, Recover = true }
    });

    /// <inheritdoc/>
    protected override string BuildStation => StoneDefense.Station;

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(6000f * StoneDefense.Strength(Variant));

    /// <inheritdoc/>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        base.CustomizePrefab(prefab, data, groups);
        StoneDefense.Harden(prefab, Variant);
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

    /// <summary>The stone the piece is built of; the black marble and grausten subclasses set it.</summary>
    protected virtual StoneVariant Variant => StoneVariant.Stone;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => StoneDefense.Tier(Variant, ProgressionTier.BlackForest);

    /// <inheritdoc/>
    protected override string LayoutName => "draketenner" + StoneDefense.Suffix(Variant);

    /// <inheritdoc/>
    protected override string FullName => StoneDefense.Name(Variant, "Dragon's Teeth");

    /// <inheritdoc/>
    protected override string Description => "Two staggered rows of stone posts with sharp points. Creatures that run into them get hurt, and they stand up to a troll far longer than wood.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => StoneDefense.Cost(Variant, new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = 20, Recover = true }
    });

    /// <inheritdoc/>
    protected override string BuildStation => StoneDefense.Station;

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(1500f * StoneDefense.Strength(Variant));

    /// <inheritdoc/>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        base.CustomizePrefab(prefab, data, groups);
        StoneDefense.Harden(prefab, Variant);
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

    /// <summary>The stone the piece is built of; the black marble and grausten subclasses set it.</summary>
    protected virtual StoneVariant Variant => StoneVariant.Stone;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => StoneDefense.Tier(Variant, ProgressionTier.BlackForest);

    /// <inheritdoc/>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        base.CustomizePrefab(prefab, data, groups);
        StoneDefense.Harden(prefab, Variant);
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
    protected override string LayoutName => "steintarn_liten" + StoneDefense.Suffix(Variant);

    /// <inheritdoc/>
    protected override string FullName => StoneDefense.Name(Variant, "Small Stone Tower");

    /// <inheritdoc/>
    protected override string Description => "A narrow stone tower. A door at the foot, doorways to the stone wall walks on both sides, and a ladder up to the crenellated top.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => StoneDefense.Cost(Variant, new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = 60, Recover = true },
        new() { Item = "Wood", Amount = 10, Recover = true }
    });

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(5000f * StoneDefense.Strength(Variant));
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
    protected override string LayoutName => "steintarn" + StoneDefense.Suffix(Variant);

    /// <inheritdoc/>
    protected override string FullName => StoneDefense.Name(Variant, "Stone Tower");

    /// <inheritdoc/>
    protected override string Description => "A stone tower with dressed corner stones and a parapet on corbels. A door at the foot, doorways to the stone wall walks, and a ladder to the top.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => StoneDefense.Cost(Variant, new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = 90, Recover = true },
        new() { Item = "Wood", Amount = 16, Recover = true }
    });

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(7500f * StoneDefense.Strength(Variant));
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
    protected override string LayoutName => "steintarn_stor" + StoneDefense.Suffix(Variant);

    /// <inheritdoc/>
    protected override string FullName => StoneDefense.Name(Variant, "Large Stone Tower");

    /// <inheritdoc/>
    protected override string Description => "A broad three-storey stone tower under a slate roof. A door at the foot, doorways to the stone wall walks, and a ladder through every floor.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => StoneDefense.Cost(Variant, new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = 130, Recover = true },
        new() { Item = "Wood", Amount = 30, Recover = true }
    });

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(11000f * StoneDefense.Strength(Variant));
}
