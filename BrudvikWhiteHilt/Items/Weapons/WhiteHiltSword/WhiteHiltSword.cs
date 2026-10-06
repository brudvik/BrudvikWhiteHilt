using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltSword;

/// <summary>
/// The White Hilt Sword, cloned from the vanilla <c>SwordDyrnwyn</c>, which gives its look and how it is used, with the
/// stats of <c>SwordIron</c> and the White Hilt model <c>whsword</c> from the asset bundle. In linear progression it
/// unlocks with the BlackForest tier.
/// </summary>
public class WhiteHiltSword : WhiteHiltWeaponBase
{
    private static ConfigEntry<float> fireDamage;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltSword(ItemManager instance) : base(instance)
    {
        fireDamage ??= WhiteHiltConfig.BindAdminOnly("Gear.Weapons", "SwordFireDamage", 5f,
            "Fire damage of the White Hilt Sword; sets the target briefly alight.", new AcceptableValueRange<float>(0f, 100f));
    }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltSword";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Sword";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Sword of Dyrnwyn";

    /// <inheritdoc/>
    protected override string CopyFrom => "SwordDyrnwyn";

    /// <summary>
    /// Dyrnwyn's flaming hits and trail stay; its Ashlands damage does not.
    /// </summary>
    protected override string StatsFrom => "SwordIron";

    /// <inheritdoc/>
    protected override float BonusFireDamage => fireDamage?.Value ?? 0f;

    /// <summary>
    /// Decorated Viking King Sword by Asylum Nox, with a white hilt.
    /// </summary>
    protected override string ModelName => "whsword";

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Bronze", Amount = 20, Recover = false },
        new() { Item = "SurtlingCore", Amount = 5, Recover = false },
        new() { Item = "SwordBronze", Amount = 1, Recover = false }
    };
}
