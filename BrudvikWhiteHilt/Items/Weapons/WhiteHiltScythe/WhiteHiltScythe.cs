using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltScythe;

/// <summary>
/// The White Hilt Scythe, a war scythe swung like the vanilla <c>AtgeirIron</c> with Mistlands strength, slash and
/// spirit damage and the White Hilt model <c>whscythe</c>. It reaps life: part of the damage it deals heals the wielder.
/// In linear progression it unlocks with the Mistlands tier.
/// </summary>
public class WhiteHiltScythe : WhiteHiltWeaponBase
{
    private static ConfigEntry<float> lifeSteal;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltScythe(ItemManager instance) : base(instance)
    {
        lifeSteal ??= WhiteHiltConfig.BindAdminOnly("Gear.Weapons", "ScytheLifeSteal", 0.05f,
            "Share of the damage the White Hilt Scythe deals that heals its wielder (0.05 = 5%).", new AcceptableValueRange<float>(0f, 1f));
    }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltScythe";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Scythe";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Scythe of Dyrnwyn. What it reaps, it gives back to you.";

    /// <inheritdoc/>
    protected override string CopyFrom => "AtgeirIron";

    /// <summary>
    /// War Scythe by Yudha Mfr, held slanted like the vanilla atgeirs, with a white grip.
    /// </summary>
    protected override string ModelName => "whscythe";

    /// <inheritdoc/>
    protected override WeaponTrait Trait => new() { LifeSteal = () => lifeSteal.Value };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Mistlands;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "BlackMetal", Amount = 15, Recover = false },
        new() { Item = "YggdrasilWood", Amount = 10, Recover = false },
        new() { Item = "Eitr", Amount = 10, Recover = false }
    };

    /// <summary>
    /// A blade's slash and the spirit that harms the undead, at Mistlands strength.
    /// </summary>
    /// <param name="shared">The weapon's shared data.</param>
    protected override void AdjustStats(ItemDrop.ItemData.SharedData shared)
    {
        shared.m_damages = new HitData.DamageTypes { m_slash = 85f, m_spirit = 40f };
        shared.m_damagesPerLevel = new HitData.DamageTypes { m_slash = 5f, m_spirit = 2f };
    }
}
