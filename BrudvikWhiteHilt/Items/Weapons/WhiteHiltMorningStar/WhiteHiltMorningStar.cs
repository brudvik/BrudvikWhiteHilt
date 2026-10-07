using BrudvikWhiteHilt.Items.Weapons.Styles;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltMorningStar;

/// <summary>
/// The White Hilt Morning Star, a spiked mace cloned from the vanilla <c>MaceNeedle</c> (the Porcupine) with the White
/// Hilt model <c>whmorningstar</c>. Its spikes break armour: the target takes more damage for a while
/// (<see cref="ArmorBreakEffect"/>). In linear progression it unlocks with the Plains tier.
/// </summary>
public class WhiteHiltMorningStar : WhiteHiltWeaponBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltMorningStar(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltMorningStar";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Morning Star";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Morning Star of Dyrnwyn. Its spikes break armour.";

    /// <inheritdoc/>
    protected override string CopyFrom => "MaceNeedle";

    /// <summary>
    /// Blows from above first, as a spiked head wants.
    /// </summary>
    protected override AttackStyle[] Swings => new[] { AttackStyle.Chop, AttackStyle.Slash };

    /// <summary>
    /// Morning star by AndreySurnachev, with a white grip.
    /// </summary>
    protected override string ModelName => "whmorningstar";

    /// <inheritdoc/>
    protected override WeaponTrait Trait => new() { BreaksArmor = true };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Plains;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "BlackMetal", Amount = 20, Recover = false },
        new() { Item = "Needle", Amount = 10, Recover = false },
        new() { Item = "LinenThread", Amount = 5, Recover = false }
    };
}
