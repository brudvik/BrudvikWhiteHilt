using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltRoundShield;

/// <summary>
/// The White Hilt Round Shield, a round viking shield cloned from the vanilla <c>ShieldBanded</c>, with the White Hilt
/// model <c>whroundshield</c>.
/// </summary>
public class WhiteHiltRoundShield : WhiteHiltWeaponBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltRoundShield(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltRoundShield";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Round Shield";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Round Shield of Dyrnwyn. The shield of every viking.";

    /// <inheritdoc/>
    protected override string CopyFrom => "ShieldBanded";

    /// <summary>
    /// Viking shield by kozachoks, with a white board.
    /// </summary>
    protected override string ModelName => "whroundshield";

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 10, Recover = false },
        new() { Item = "FineWood", Amount = 10, Recover = false },
        new() { Item = "LeatherScraps", Amount = 5, Recover = false }
    };
}
