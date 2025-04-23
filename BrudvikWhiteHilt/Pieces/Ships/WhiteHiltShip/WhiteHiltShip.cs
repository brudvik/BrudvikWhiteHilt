using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;

/// <summary>
/// The Indestructible Ship of Dyrnwyn
/// </summary>
public class WhiteHiltShip : WhiteHiltShipBase
{
    /// <summary>
    /// Constructor for the WhiteHiltShip class.
    /// </summary>
    /// <param name="instance"></param>
    public WhiteHiltShip(PieceManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the ship.
    /// </summary>
    protected override string BaseName => "WhiteHiltShip";

    /// <summary>
    /// The full name of the ship.
    /// </summary>
    protected override string FullName => "White Hilt Ship";

    /// <summary>
    /// The description of the ship.
    /// </summary>
    protected override string Description => "The Indestructible Ship of Dyrnwyn";

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected override string CopyFrom => "VikingShip";

    /// <summary>
    /// Indicates whether the White Hilt ship is enabled.
    /// </summary>
    public override bool Enabled => true;

    /// <summary>
    /// The name of the ship prefab.
    /// </summary>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "FineWood", Amount = 20, Recover = false },
        new() { Item = "IronNails", Amount = 100, Recover = false },
        new() { Item = "BronzeNails", Amount = 100, Recover = false },
        new() { Item = "DeerHide", Amount = 15, Recover = false },
        new() { Item = "LeatherScraps", Amount = 15, Recover = false }
    };
}
