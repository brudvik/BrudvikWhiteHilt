using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltTowerShield;

/// <summary>
/// The White Hilt Tower Shield, cloned from the vanilla <c>ShieldIronTower</c> and the White Hilt model
/// <c>whtowershield</c> from the asset bundle.
/// </summary>
public class WhiteHiltTowerShield : WhiteHiltWeaponBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltTowerShield(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltTowerShield";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Tower Shield";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Tower Shield of Dyrnwyn";

    /// <inheritdoc/>
    protected override string CopyFrom => "ShieldIronTower";

    /// <summary>
    /// Medieval Kite Shield by iedalton, with white-painted boards.
    /// </summary>
    protected override string ModelName => "whtowershield";

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 30, Recover = false },
        new() { Item = "Chain", Amount = 10, Recover = false },
        new() { Item = "ShieldBanded", Amount = 1, Recover = false }
    };
}
