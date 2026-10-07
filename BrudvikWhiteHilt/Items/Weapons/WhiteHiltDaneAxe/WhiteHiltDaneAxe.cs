using BrudvikWhiteHilt.Items.Weapons.Styles;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltDaneAxe;

/// <summary>
/// The White Hilt Dane Axe, a long two-handed axe cloned from the vanilla <c>Battleaxe</c> with the strength of the
/// Crystal Battleaxe (without its spirit damage) and the White Hilt model <c>whdaneaxe</c>. Its long haft gives it a
/// quarter more reach. In linear progression it unlocks with the Mountain tier.
/// </summary>
public class WhiteHiltDaneAxe : WhiteHiltWeaponBase
{
    private const float Reach = 1.25f;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltDaneAxe(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltDaneAxe";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Dane Axe";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Dane Axe of Dyrnwyn. Its long haft reaches far.";

    /// <inheritdoc/>
    protected override string CopyFrom => "Battleaxe";

    /// <summary>
    /// Wide cleaves, overhead hews and long two-handed cuts.
    /// </summary>
    protected override AttackStyle[] Swings => new[] { AttackStyle.Cleave, AttackStyle.Hew, AttackStyle.Greatsword };

    /// <inheritdoc/>
    protected override string StatsFrom => "BattleaxeCrystal";

    /// <summary>
    /// Dane Axe by macadaneo, with a white grip.
    /// </summary>
    protected override string ModelName => "whdaneaxe";

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Mountain;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Silver", Amount = 20, Recover = false },
        new() { Item = "WolfFang", Amount = 10, Recover = false },
        new() { Item = "Battleaxe", Amount = 1, Recover = false }
    };

    /// <summary>
    /// The Crystal Battleaxe's slash and chop without its spirit, and a quarter more reach.
    /// </summary>
    /// <param name="shared">The weapon's shared data.</param>
    protected override void AdjustStats(ItemDrop.ItemData.SharedData shared)
    {
        shared.m_damages.m_spirit = 0f;
        shared.m_damagesPerLevel.m_spirit = 0f;
        shared.m_attack.m_attackRange *= Reach;
        if (shared.m_secondaryAttack != null)
        {
            shared.m_secondaryAttack.m_attackRange *= Reach;
        }
    }
}
