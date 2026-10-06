using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltBeardedAxe;

/// <summary>
/// The White Hilt Bearded Axe, a one-handed axe cloned from the vanilla <c>AxeIron</c> with the White Hilt model
/// <c>whbeardedaxe</c>. Its beard hooks shields: it staggers a target that bears or raises a shield more.
/// </summary>
public class WhiteHiltBeardedAxe : WhiteHiltWeaponBase
{
    private static ConfigEntry<float> shieldHook;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltBeardedAxe(ItemManager instance) : base(instance)
    {
        shieldHook ??= WhiteHiltConfig.BindAdminOnly("Gear.Weapons", "BeardedAxeShieldStagger", 1.5f,
            "Stagger multiplier of the White Hilt Bearded Axe against a target that bears or raises a shield.", new AcceptableValueRange<float>(1f, 5f));
    }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltBeardedAxe";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Bearded Axe";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Bearded Axe of Dyrnwyn. Its beard hooks a foe's shield.";

    /// <inheritdoc/>
    protected override string CopyFrom => "AxeIron";

    /// <summary>
    /// Low-poly Viking Axe by ehcawen, without its hanging ring, with a white grip.
    /// </summary>
    protected override string ModelName => "whbeardedaxe";

    /// <inheritdoc/>
    protected override WeaponTrait Trait => new() { ShieldHook = () => shieldHook.Value };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 20, Recover = false },
        new() { Item = "Chain", Amount = 2, Recover = false },
        new() { Item = "AxeBronze", Amount = 1, Recover = false }
    };
}
