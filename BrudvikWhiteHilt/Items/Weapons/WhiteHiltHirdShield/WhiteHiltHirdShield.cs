using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltHirdShield;

/// <summary>
/// The White Hilt Hird Shield, a round shield of the king's guard cloned from the vanilla <c>ShieldBlackmetal</c>, with
/// half again the parry bonus, with the White Hilt model <c>whhirdshield</c>. In linear progression it unlocks with the
/// Plains tier.
/// </summary>
public class WhiteHiltHirdShield : WhiteHiltWeaponBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltHirdShield(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltHirdShield";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Hird Shield";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Hird Shield of Dyrnwyn. Made for the perfect parry.";

    /// <inheritdoc/>
    protected override string CopyFrom => "ShieldBlackmetal";

    /// <summary>
    /// Round shield by Mihail, with a white board.
    /// </summary>
    protected override string ModelName => "whhirdshield";

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Plains;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "BlackMetal", Amount = 10, Recover = false },
        new() { Item = "FineWood", Amount = 10, Recover = false },
        new() { Item = "LinenThread", Amount = 5, Recover = false }
    };

    /// <summary>
    /// Half again the bonus of a perfectly timed block.
    /// </summary>
    /// <param name="shared">The weapon's shared data.</param>
    protected override void AdjustStats(ItemDrop.ItemData.SharedData shared)
    {
        shared.m_timedBlockBonus *= 1.5f;
    }
}
