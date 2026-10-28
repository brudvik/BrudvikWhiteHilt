using BrudvikWhiteHilt.Pieces.Ships;
using BrudvikWhiteHilt.Pieces.Ships.Skidbladnir;
using BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;
using System.Runtime.CompilerServices;

namespace BrudvikWhiteHilt.Patches.Ships;

/// <summary>
/// The mod's parts on a ship that the hooks on its physics step drive. They are looked up once per ship and
/// remembered, as the hooks run for every ship on every physics step, and the parts are on the ship from its prefab
/// or its Awake, so they never change.
/// </summary>
public sealed class ShipParts
{
    private static readonly ConditionalWeakTable<Ship, ShipParts> byShip = new();
    private static readonly ConditionalWeakTable<Ship, ShipParts>.CreateValueCallback find = ship => new ShipParts
    {
        Assist = ship.GetComponent<ShipAssist>(),
        Upgrades = ship.GetComponent<WhiteHiltShipUpgrades>(),
        Skidbladnir = ship.GetComponent<SkidbladnirShip>(),
    };

    /// <summary>The sailing help, or null.</summary>
    public ShipAssist Assist { get; private set; }

    /// <summary>A White Hilt Ship's upgrades, or null.</summary>
    public WhiteHiltShipUpgrades Upgrades { get; private set; }

    /// <summary>The sailing home, or null.</summary>
    public SkidbladnirShip Skidbladnir { get; private set; }

    /// <summary>
    /// The parts of a ship.
    /// </summary>
    /// <param name="ship">The ship.</param>
    /// <returns>Its parts.</returns>
    public static ShipParts Of(Ship ship)
    {
        return byShip.GetValue(ship, find);
    }
}
