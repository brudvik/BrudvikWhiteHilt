using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltSpear;

/// <summary>
/// The White Hilt Spear, cloned from the vanilla <c>SpearElderbark</c> and the White Hilt model <c>whspear</c> from the
/// asset bundle.
/// </summary>
public class WhiteHiltSpear : WhiteHiltWeaponBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltSpear(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltSpear";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Spear";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Spear of Dyrnwyn";

    /// <inheritdoc/>
    protected override string CopyFrom => "SpearElderbark";

    /// <summary>
    /// Winterbite by Peter Nox, with a white grip.
    /// </summary>
    protected override string ModelName => "whspear";

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 20, Recover = false },
        new() { Item = "ElderBark", Amount = 10, Recover = false },
        new() { Item = "SpearBronze", Amount = 1, Recover = false }
    };
}
