using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltGladius;

/// <summary>
/// The White Hilt Gladius, a short sword cloned from the vanilla <c>SwordBlackmetal</c>: it cuts and stabs, costs less
/// stamina and strikes hard from behind, with the White Hilt model <c>whgladius</c>. In linear progression it unlocks
/// with the Plains tier.
/// </summary>
public class WhiteHiltGladius : WhiteHiltWeaponBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltGladius(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltGladius";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Gladius";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Gladius of Dyrnwyn. Short, quick, and deadly from behind.";

    /// <inheritdoc/>
    protected override string CopyFrom => "SwordBlackmetal";

    /// <summary>
    /// The white-gripped sword from Roman Gladius Fantasy Set by Asylum Nox.
    /// </summary>
    protected override string ModelName => "whgladius";

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Plains;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "BlackMetal", Amount = 15, Recover = false },
        new() { Item = "LinenThread", Amount = 5, Recover = false },
        new() { Item = "Needle", Amount = 5, Recover = false }
    };

    /// <summary>
    /// Slash and pierce, a backstab bonus of 5 and a quarter less stamina per blow.
    /// </summary>
    /// <param name="shared">The weapon's shared data.</param>
    protected override void AdjustStats(ItemDrop.ItemData.SharedData shared)
    {
        shared.m_damages = new HitData.DamageTypes { m_slash = 55f, m_pierce = 40f };
        shared.m_damagesPerLevel = new HitData.DamageTypes { m_slash = 3f, m_pierce = 3f };
        shared.m_backstabBonus = 5f;
        shared.m_attack.m_attackStamina *= 0.75f;
    }
}
