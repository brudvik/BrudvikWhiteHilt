using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltBattleaxe;

/// <summary>
/// The White Hilt Battleaxe, cloned from the vanilla <c>Battleaxe</c> and the White Hilt model <c>whbattleaxe</c> from
/// the asset bundle.
/// </summary>
public class WhiteHiltBattleaxe : WhiteHiltWeaponBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltBattleaxe(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltBattleaxe";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Battleaxe";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Battleaxe of Dyrnwyn";

    /// <inheritdoc/>
    protected override string CopyFrom => "Battleaxe";

    /// <summary>
    /// Nordic Axe - Cloudcleaver by Peter Nox, with a white grip.
    /// </summary>
    protected override string ModelName => "whbattleaxe";

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 30, Recover = false },
        new() { Item = "ElderBark", Amount = 10, Recover = false },
        new() { Item = "AxeBronze", Amount = 1, Recover = false }
    };
}
