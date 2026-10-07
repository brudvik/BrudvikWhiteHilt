using BepInEx.Configuration;
using BrudvikWhiteHilt.Items.Weapons.Styles;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltHalberd;

/// <summary>
/// The White Hilt Halberd, cloned from the vanilla <c>AtgeirBlackmetal</c> with the White Hilt model
/// <c>whhalberd</c>. Made for big game: it deals more damage to large creatures (<see cref="WeaponTraits.IsLarge"/>).
/// In linear progression it unlocks with the Plains tier.
/// </summary>
public class WhiteHiltHalberd : WhiteHiltWeaponBase
{
    private static ConfigEntry<float> largeFoeBonus;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltHalberd(ItemManager instance) : base(instance)
    {
        largeFoeBonus ??= WhiteHiltConfig.BindAdminOnly("Gear.Weapons", "HalberdLargeFoeDamage", 1.25f,
            "Damage multiplier of the White Hilt Halberd against large creatures (trolls, lox, abominations, serpents; 1.25 = 25% more).",
            new AcceptableValueRange<float>(1f, 5f));
    }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltHalberd";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Halberd";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Halberd of Dyrnwyn. Made to bring down big game.";

    /// <inheritdoc/>
    protected override string CopyFrom => "AtgeirBlackmetal";

    /// <summary>
    /// Spike, axe and hook: thrusts, cleaves and hews.
    /// </summary>
    protected override AttackStyle[] Swings => new[] { AttackStyle.Polearm, AttackStyle.Cleave, AttackStyle.Hew };

    /// <summary>
    /// Viking Halberd by beyondmatter, held slanted like the vanilla atgeirs, with a white grip.
    /// </summary>
    protected override string ModelName => "whhalberd";

    /// <inheritdoc/>
    protected override WeaponTrait Trait => new() { LargeFoeBonus = () => largeFoeBonus.Value };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Plains;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "BlackMetal", Amount = 25, Recover = false },
        new() { Item = "FineWood", Amount = 10, Recover = false },
        new() { Item = "LinenThread", Amount = 10, Recover = false }
    };
}
