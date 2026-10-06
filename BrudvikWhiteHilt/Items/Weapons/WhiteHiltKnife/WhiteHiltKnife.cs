using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltKnife;

/// <summary>
/// The White Hilt Knife, cloned from the vanilla <c>KnifeChitin</c> and the White Hilt model <c>whknife</c> from the
/// asset bundle.
/// </summary>
public class WhiteHiltKnife : WhiteHiltWeaponBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltKnife(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltKnife";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Knife";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Knife of Dyrnwyn";

    /// <inheritdoc/>
    protected override string CopyFrom => "KnifeChitin";

    /// <summary>
    /// Seax Sword by iedalton, with a white grip.
    /// </summary>
    protected override string ModelName => "whknife";

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 10, Recover = false },
        new() { Item = "LeatherScraps", Amount = 5, Recover = false },
        new() { Item = "KnifeFlint", Amount = 1, Recover = false }
    };
}
