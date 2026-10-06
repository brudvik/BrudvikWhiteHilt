using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltClaymore;

/// <summary>
/// The White Hilt Claymore, a two-handed sword cloned from the vanilla <c>THSwordKrom</c> with the White Hilt model
/// <c>whclaymore</c>. In linear progression it unlocks with the Mistlands tier.
/// </summary>
public class WhiteHiltClaymore : WhiteHiltWeaponBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltClaymore(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltClaymore";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Claymore";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Claymore of Dyrnwyn. A great blade for both hands.";

    /// <inheritdoc/>
    protected override string CopyFrom => "THSwordKrom";

    /// <summary>
    /// Scottish Claymore by Yudha Mfr, with a white grip.
    /// </summary>
    protected override string ModelName => "whclaymore";

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Mistlands;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "BlackMetal", Amount = 30, Recover = false },
        new() { Item = "YggdrasilWood", Amount = 10, Recover = false },
        new() { Item = "Carapace", Amount = 10, Recover = false }
    };
}
