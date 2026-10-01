using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltSword;

/// <summary>
/// Class for the White Hilt Sword item.
/// </summary>
public class WhiteHiltSword : WhiteHiltWeaponBase
{
    private static ConfigEntry<float> fireDamage;

    /// <summary>
    /// Constructor for the WhiteHiltSword class.
    /// </summary>
    /// <param name="instance"></param>
    public WhiteHiltSword(ItemManager instance) : base(instance)
    {
        fireDamage ??= WhiteHiltConfig.BindAdminOnly("Gear.Weapons", "SwordFireDamage", 5f,
            "Fire damage of the White Hilt Sword; sets the target briefly alight.", new AcceptableValueRange<float>(0f, 100f));
    }

    /// <summary>
    /// The base name of the sword.
    /// </summary>
    protected override string BaseName => "WhiteHiltSword";

    /// <summary>
    /// The full name of the sword.
    /// </summary>
    protected override string FullName => "White Hilt Sword";

    /// <summary>
    /// The description of the sword.
    /// </summary>
    protected override string Description => "The Indestructible Sword of Dyrnwyn";

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
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

    /// <summary>
    /// Indicates whether the White Hilt Bow is enabled.
    /// </summary>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;

    /// <summary>
    /// The requirements for crafting the White Hilt Sword.
    /// </summary>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Bronze", Amount = 20, Recover = false },
        new() { Item = "SurtlingCore", Amount = 5, Recover = false },
        new() { Item = "SwordBronze", Amount = 1, Recover = false }
    };
}
