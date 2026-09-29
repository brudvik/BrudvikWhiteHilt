using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.ShipUpgrades;

/// <summary>
/// Hangs a lantern on the White Hilt Ship that lights up at night.
/// </summary>
public class ShipLantern : WhiteHiltShipUpgradeBase
{
    /// <summary>
    /// Bit of the lantern in the ship's upgrade mask.
    /// </summary>
    public const int Bit = 0;

    /// <summary>
    /// Constructor for the ShipLantern class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public ShipLantern(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    public override int Index => Bit;

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltShipLantern";

    /// <inheritdoc/>
    protected override string FullName => "Ship Lantern";

    /// <inheritdoc/>
    protected override string Description => "A lantern for the White Hilt Ship. Use it on the mast; it lights the deck when night falls.";

    /// <inheritdoc/>
    protected override string CopyFrom => "Lantern";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 2, Recover = false },
        new() { Item = "Resin", Amount = 10, Recover = false },
        new() { Item = "SurtlingCore", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    public override bool Enabled => true;
}

/// <summary>
/// Puts barrels on the White Hilt Ship and enlarges its cargo hold from 6 x 3 to 8 x 4.
/// </summary>
public class ShipBarrels : WhiteHiltShipUpgradeBase
{
    /// <summary>
    /// Bit of the barrels in the ship's upgrade mask.
    /// </summary>
    public const int Bit = 1;

    /// <summary>
    /// Constructor for the ShipBarrels class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public ShipBarrels(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    public override int Index => Bit;

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltShipBarrels";

    /// <inheritdoc/>
    protected override string FullName => "Cargo Barrels";

    /// <inheritdoc/>
    protected override string Description => "Barrels and crates lashed to the deck of the White Hilt Ship. Use them on the mast to enlarge the cargo hold to 8 x 4.";

    /// <inheritdoc/>
    protected override string CopyFrom => "BarrelRings";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "FineWood", Amount = 10, Recover = false },
        new() { Item = "Iron", Amount = 4, Recover = false }
    };

    /// <inheritdoc/>
    public override bool Enabled => true;
}

/// <summary>
/// Raises a tent on the White Hilt Ship that gives shelter and keeps the rain off.
/// </summary>
public class ShipTent : WhiteHiltShipUpgradeBase
{
    /// <summary>
    /// Bit of the tent in the ship's upgrade mask.
    /// </summary>
    public const int Bit = 2;

    /// <summary>
    /// Constructor for the ShipTent class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public ShipTent(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    public override int Index => Bit;

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltShipTent";

    /// <inheritdoc/>
    protected override string FullName => "Ship Tent";

    /// <inheritdoc/>
    protected override string Description => "A hide tent for the deck of the White Hilt Ship. Use it on the mast; under it you are sheltered and dry on long voyages.";

    /// <inheritdoc/>
    protected override string CopyFrom => "TrollHide";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "TrollHide", Amount = 6, Recover = false },
        new() { Item = "LeatherScraps", Amount = 10, Recover = false },
        new() { Item = "Wood", Amount = 6, Recover = false }
    };

    /// <inheritdoc/>
    public override bool Enabled => true;
}

/// <summary>
/// Sets a wisp at the top of the White Hilt Ship's mast that pushes back the mist around the ship.
/// </summary>
public class ShipMastWisp : WhiteHiltShipUpgradeBase
{
    /// <summary>
    /// Bit of the mast wisp in the ship's upgrade mask.
    /// </summary>
    public const int Bit = 3;

    /// <summary>
    /// Constructor for the ShipMastWisp class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public ShipMastWisp(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    public override int Index => Bit;

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltShipMastWisp";

    /// <inheritdoc/>
    protected override string FullName => "Mast Wisp";

    /// <inheritdoc/>
    protected override string Description => "A glowing light bound with guck and ancient bark. Use it on the mast of the White Hilt Ship; it clears the mist around the ship.";

    /// <inheritdoc/>
    protected override string CopyFrom => "Wisp";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Guck", Amount = 5, Recover = false },
        new() { Item = "ElderBark", Amount = 5, Recover = false },
        new() { Item = "SurtlingCore", Amount = 2, Recover = false }
    };

    /// <inheritdoc/>
    public override bool Enabled => true;
}

/// <summary>
/// Trails a net behind the White Hilt Ship that catches fish for the cargo hold while the ship sails.
/// </summary>
public class ShipFishingNet : WhiteHiltShipUpgradeBase
{
    /// <summary>
    /// Bit of the fishing net in the ship's upgrade mask.
    /// </summary>
    public const int Bit = 4;

    /// <summary>
    /// Constructor for the ShipFishingNet class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public ShipFishingNet(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    public override int Index => Bit;

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltShipFishingNet";

    /// <inheritdoc/>
    protected override string FullName => "Fishing Net";

    /// <inheritdoc/>
    protected override string Description => "A hide-strip net for the White Hilt Ship. Use it on the mast; while the ship sails, it catches the fish of the waters it sails through and puts them in the cargo hold.";

    /// <inheritdoc/>
    protected override string CopyFrom => "FishingRod";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "FineWood", Amount = 4, Recover = false },
        new() { Item = "LeatherScraps", Amount = 10, Recover = false },
        new() { Item = "DeerHide", Amount = 4, Recover = false }
    };

    /// <inheritdoc/>
    public override bool Enabled => true;
}

/// <summary>
/// Hangs an anchor on the White Hilt Ship. Lowered, it holds the ship where it is.
/// </summary>
public class ShipDriftAnchor : WhiteHiltShipUpgradeBase
{
    /// <summary>
    /// Bit of the anchor in the ship's upgrade mask.
    /// </summary>
    public const int Bit = 5;

    /// <summary>
    /// Constructor for the ShipDriftAnchor class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public ShipDriftAnchor(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    public override int Index => Bit;

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltShipDriftAnchor";

    /// <inheritdoc/>
    protected override string FullName => "Drift Anchor";

    /// <inheritdoc/>
    protected override string Description => "An iron anchor on a chain for the White Hilt Ship. Use it on the mast; lower it at the mast and the ship stays where it is, however the wind blows.";

    /// <inheritdoc/>
    protected override string CopyFrom => "Chain";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 4, Recover = false },
        new() { Item = "Chain", Amount = 2, Recover = false }
    };

    /// <inheritdoc/>
    public override bool Enabled => true;
}
