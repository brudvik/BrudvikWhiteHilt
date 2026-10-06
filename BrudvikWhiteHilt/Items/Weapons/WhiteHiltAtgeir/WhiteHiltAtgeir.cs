using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltAtgeir;

/// <summary>
/// The White Hilt Atgeir, cloned from the vanilla <c>AtgeirIron</c> and the White Hilt model <c>whatgeir</c> from the
/// asset bundle.
/// </summary>
public class WhiteHiltAtgeir : WhiteHiltWeaponBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltAtgeir(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltAtgeir";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Atgeir";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Atgeir of Dyrnwyn";

    /// <inheritdoc/>
    protected override string CopyFrom => "AtgeirIron";

    /// <summary>
    /// Polearm from Weapon Set by Asylum Nox, with a white shaft.
    /// </summary>
    protected override string ModelName => "whatgeir";

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 25, Recover = false },
        new() { Item = "ElderBark", Amount = 10, Recover = false },
        new() { Item = "AtgeirBronze", Amount = 1, Recover = false }
    };
}
