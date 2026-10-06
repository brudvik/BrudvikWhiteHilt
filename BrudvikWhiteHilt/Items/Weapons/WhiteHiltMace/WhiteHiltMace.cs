using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltMace;

/// <summary>
/// The White Hilt Mace, cloned from the vanilla <c>MaceIron</c> and the White Hilt model <c>whmace</c> from the asset
/// bundle.
/// </summary>
public class WhiteHiltMace : WhiteHiltWeaponBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltMace(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltMace";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Mace";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Mace of Dyrnwyn";

    /// <inheritdoc/>
    protected override string CopyFrom => "MaceIron";

    /// <summary>
    /// Brass Viking Mace by Asylum Nox, with a white grip.
    /// </summary>
    protected override string ModelName => "whmace";

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 20, Recover = false },
        new() { Item = "WitheredBone", Amount = 5, Recover = false },
        new() { Item = "MaceBronze", Amount = 1, Recover = false }
    };
}
