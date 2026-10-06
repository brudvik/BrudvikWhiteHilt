using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltSledge;

/// <summary>
/// The White Hilt Sledge, cloned from the vanilla <c>SledgeIron</c> and the White Hilt model <c>whsledge</c> from the
/// asset bundle.
/// </summary>
public class WhiteHiltSledge : WhiteHiltWeaponBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltSledge(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltSledge";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Sledge";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Sledge of Dyrnwyn";

    /// <inheritdoc/>
    protected override string CopyFrom => "SledgeIron";

    /// <summary>
    /// Viking Warhammer by Peter Nox, with a white grip.
    /// </summary>
    protected override string ModelName => "whsledge";

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 30, Recover = false },
        new() { Item = "YmirRemains", Amount = 10, Recover = false },
        new() { Item = "SledgeStagbreaker", Amount = 1, Recover = false }
    };
}
