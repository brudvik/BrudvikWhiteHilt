using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltBuckler;

/// <summary>
/// The White Hilt Buckler, cloned from the vanilla <c>ShieldBanded</c> and the White Hilt model <c>whbuckler</c> from
/// the asset bundle.
/// </summary>
public class WhiteHiltBuckler : WhiteHiltWeaponBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltBuckler(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltBuckler";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Buckler";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Buckler of Dyrnwyn";

    /// <inheritdoc/>
    protected override string CopyFrom => "ShieldBanded";

    /// <summary>
    /// Worn Round Shield by iedalton, with white-painted boards.
    /// </summary>
    protected override string ModelName => "whbuckler";

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 15, Recover = false },
        new() { Item = "Chain", Amount = 5, Recover = false },
        new() { Item = "ShieldBronzeBuckler", Amount = 1, Recover = false }
    };
}
