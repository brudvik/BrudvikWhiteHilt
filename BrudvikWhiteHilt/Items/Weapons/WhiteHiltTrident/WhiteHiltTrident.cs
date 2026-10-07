using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltTrident;

/// <summary>
/// The White Hilt Trident, a spear cloned from the vanilla <c>SpearSplitner</c> with lightning damage and the White Hilt
/// model <c>whtrident</c>. It is made for the sea: it deals more damage to foes in the water and to sea creatures, and
/// flies as itself when thrown. In linear progression it unlocks with the Ashlands tier.
/// </summary>
public class WhiteHiltTrident : WhiteHiltWeaponBase
{
    private static ConfigEntry<float> seaBonus;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltTrident(ItemManager instance) : base(instance)
    {
        seaBonus ??= WhiteHiltConfig.BindAdminOnly("Gear.Weapons", "TridentSeaDamage", 1.3f,
            "Damage multiplier of the White Hilt Trident against foes in the water and sea creatures (serpents, krakens; 1.3 = 30% more).",
            new AcceptableValueRange<float>(1f, 5f));
    }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltTrident";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Trident";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Trident of Dyrnwyn. The sea's own, crackling with lightning.";

    /// <inheritdoc/>
    protected override string CopyFrom => "SpearSplitner";

    /// <summary>
    /// Trident by Akshat (shooter24994), with a white grip.
    /// </summary>
    protected override string ModelName => "whtrident";

    /// <inheritdoc/>
    protected override bool ThrownAsModel => true;

    /// <inheritdoc/>
    protected override WeaponTrait Trait => new() { SeaBonus = () => seaBonus.Value };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Ashlands;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "FlametalNew", Amount = 15, Recover = false },
        new() { Item = "SerpentScale", Amount = 10, Recover = false },
        new() { Item = "Thunderstone", Amount = 5, Recover = false }
    };

    /// <summary>
    /// Splitnir's pierce with lightning added.
    /// </summary>
    /// <param name="shared">The weapon's shared data.</param>
    protected override void AdjustStats(ItemDrop.ItemData.SharedData shared)
    {
        shared.m_damages = new HitData.DamageTypes { m_pierce = 110f, m_lightning = 40f };
        shared.m_damagesPerLevel = new HitData.DamageTypes { m_pierce = 6f, m_lightning = 2f };
    }
}
