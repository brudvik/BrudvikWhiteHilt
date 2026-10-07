using BepInEx.Configuration;
using BrudvikWhiteHilt.Items.Weapons.Styles;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltFlail;

/// <summary>
/// The White Hilt Flail, a one-handed mace cloned from the vanilla <c>MaceIron</c> with Mountain strength and the
/// White Hilt model <c>whflail</c> (its chain swung out along the haft). Its head swings past the guard: part of its
/// blows cannot be blocked. In linear progression it unlocks with the Mountain tier.
/// </summary>
public class WhiteHiltFlail : WhiteHiltWeaponBase
{
    private static ConfigEntry<float> guardBreak;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltFlail(ItemManager instance) : base(instance)
    {
        guardBreak ??= WhiteHiltConfig.BindAdminOnly("Gear.Weapons", "FlailGuardBreakChance", 0.3f,
            "Chance that a blow of the White Hilt Flail swings past the guard and cannot be blocked (0.3 = 30%).", new AcceptableValueRange<float>(0f, 1f));
    }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltFlail";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Flail";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Flail of Dyrnwyn. Its head swings past a raised shield.";

    /// <inheritdoc/>
    protected override string CopyFrom => "MaceIron";

    /// <summary>
    /// Swung, never thrust: a chain cannot stab.
    /// </summary>
    protected override AttackStyle[] Swings => new[] { AttackStyle.Slash, AttackStyle.Chop };

    /// <summary>
    /// Medieval Flail by Herzog, the chain swung out along the haft, with a white grip.
    /// </summary>
    protected override string ModelName => "whflail";

    /// <inheritdoc/>
    protected override WeaponTrait Trait => new() { GuardBreakChance = () => guardBreak.Value };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Mountain;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Silver", Amount = 15, Recover = false },
        new() { Item = "Chain", Amount = 3, Recover = false },
        new() { Item = "MaceIron", Amount = 1, Recover = false }
    };

    /// <summary>
    /// Pure blunt damage at Mountain strength.
    /// </summary>
    /// <param name="shared">The weapon's shared data.</param>
    protected override void AdjustStats(ItemDrop.ItemData.SharedData shared)
    {
        shared.m_damages = new HitData.DamageTypes { m_blunt = 75f };
        shared.m_damagesPerLevel = new HitData.DamageTypes { m_blunt = 5f };
    }
}
