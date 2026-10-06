using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Armors.WhiteHiltShield;

/// <summary>
/// The White Hilt Shield, cloned from the vanilla <c>ShieldBanded</c>.
/// </summary>
public class WhiteHiltShield : WhiteHiltArmorBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltShield(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltShield";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Shield";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Shield of Dyrnwyn";

    /// <inheritdoc/>
    protected override string CopyFrom => "ShieldBanded";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 30, Recover = false },
        new() { Item = "IronNails", Amount = 100, Recover = false },
        new() { Item = "ShieldIronBuckler", Amount = 1, Recover = false },
    };

    /// <inheritdoc/>
    public override bool Enabled => true;
}
