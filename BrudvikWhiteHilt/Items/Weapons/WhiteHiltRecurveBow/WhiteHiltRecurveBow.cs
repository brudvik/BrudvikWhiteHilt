using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltRecurveBow;

/// <summary>
/// The White Hilt Recurve Bow, a short bow cloned from the vanilla <c>BowHuntsman</c>: it draws in little more than
/// half the time, for a little less damage, with the White Hilt model <c>whrecurvebow</c>. In linear progression it
/// unlocks with the Plains tier.
/// </summary>
public class WhiteHiltRecurveBow : WhiteHiltWeaponBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltRecurveBow(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltRecurveBow";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Recurve Bow";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Recurve Bow of Dyrnwyn. Quick to draw.";

    /// <inheritdoc/>
    protected override string CopyFrom => "BowHuntsman";

    /// <summary>
    /// Recurve Bow by JeanStroh, with a white grip.
    /// </summary>
    protected override string ModelName => "whrecurvebow";

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Plains;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "BlackMetal", Amount = 10, Recover = false },
        new() { Item = "FineWood", Amount = 10, Recover = false },
        new() { Item = "LinenThread", Amount = 10, Recover = false }
    };

    /// <summary>
    /// Plains strength, drawn in 60% of the time for a tenth less damage.
    /// </summary>
    /// <param name="shared">The weapon's shared data.</param>
    protected override void AdjustStats(ItemDrop.ItemData.SharedData shared)
    {
        shared.m_damages = new HitData.DamageTypes { m_pierce = 50f };
        shared.m_damagesPerLevel = new HitData.DamageTypes { m_pierce = 4f };
        shared.m_attack.m_drawDurationMin *= 0.6f;
        shared.m_attack.m_damageMultiplier *= 0.9f;
    }
}
