using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltBecDeCorbin;

/// <summary>
/// The White Hilt Bec de Corbin, a war-hammer on a pole, the raven's beak, swung like the vanilla
/// <c>AtgeirBlackmetal</c> with blunt and pierce. Its beak breaks armour (<see cref="ArmorBreakEffect"/>), with the
/// White Hilt model <c>whbecdecorbin</c>. In linear progression it unlocks with the Mistlands tier.
/// </summary>
public class WhiteHiltBecDeCorbin : WhiteHiltWeaponBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltBecDeCorbin(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltBecDeCorbin";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Bec de Corbin";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Bec de Corbin of Dyrnwyn. Its beak breaks armour.";

    /// <inheritdoc/>
    protected override string CopyFrom => "AtgeirBlackmetal";

    /// <summary>
    /// Bec De Corbin by AndreySurnachev, with a white grip.
    /// </summary>
    protected override string ModelName => "whbecdecorbin";

    /// <inheritdoc/>
    protected override WeaponTrait Trait => new() { BreaksArmor = true };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Mistlands;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "BlackMetal", Amount = 20, Recover = false },
        new() { Item = "YggdrasilWood", Amount = 10, Recover = false },
        new() { Item = "Carapace", Amount = 8, Recover = false }
    };

    /// <summary>
    /// Blunt and pierce at Mistlands strength.
    /// </summary>
    /// <param name="shared">The weapon's shared data.</param>
    protected override void AdjustStats(ItemDrop.ItemData.SharedData shared)
    {
        shared.m_damages = new HitData.DamageTypes { m_blunt = 60f, m_pierce = 70f };
        shared.m_damagesPerLevel = new HitData.DamageTypes { m_blunt = 3f, m_pierce = 4f };
    }
}
