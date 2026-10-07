using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltPike;

/// <summary>
/// The White Hilt Pike, a long spear cloned from the vanilla <c>SpearWolfFang</c> with Plains strength: it reaches half
/// again as far but costs a fifth more stamina, and flies as itself when thrown, with the White Hilt model
/// <c>whpike</c>. In linear progression it unlocks with the Plains tier.
/// </summary>
public class WhiteHiltPike : WhiteHiltWeaponBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltPike(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltPike";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Pike";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Pike of Dyrnwyn. It reaches farther than any other spear.";

    /// <inheritdoc/>
    protected override string CopyFrom => "SpearWolfFang";

    /// <summary>
    /// Steel Pike by DrKryzel, with a white grip.
    /// </summary>
    protected override string ModelName => "whpike";

    /// <inheritdoc/>
    protected override bool ThrownAsModel => true;

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Plains;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "BlackMetal", Amount = 15, Recover = false },
        new() { Item = "FineWood", Amount = 15, Recover = false },
        new() { Item = "LinenThread", Amount = 5, Recover = false }
    };

    /// <summary>
    /// Plains pierce, half again the reach and a fifth more stamina per stab.
    /// </summary>
    /// <param name="shared">The weapon's shared data.</param>
    protected override void AdjustStats(ItemDrop.ItemData.SharedData shared)
    {
        shared.m_damages = new HitData.DamageTypes { m_pierce = 90f };
        shared.m_damagesPerLevel = new HitData.DamageTypes { m_pierce = 5f };
        shared.m_attack.m_attackRange *= 1.5f;
        shared.m_attack.m_attackStamina *= 1.2f;
    }
}
