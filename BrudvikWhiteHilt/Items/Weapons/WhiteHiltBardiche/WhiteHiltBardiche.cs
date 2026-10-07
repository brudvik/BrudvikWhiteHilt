using BrudvikWhiteHilt.Items.Weapons.Styles;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltBardiche;

/// <summary>
/// The White Hilt Bardiche, a pole-axe swung like the vanilla <c>AtgeirIron</c>, with slash in place of pierce at
/// Mountain strength, with the White Hilt model <c>whbardiche</c>. In linear progression it unlocks with the Mountain
/// tier.
/// </summary>
public class WhiteHiltBardiche : WhiteHiltWeaponBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltBardiche(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltBardiche";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Bardiche";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Bardiche of Dyrnwyn. A long axe-blade on a pole.";

    /// <inheritdoc/>
    protected override string CopyFrom => "AtgeirIron";

    /// <summary>
    /// A long axe blade on a pole: cleaves first.
    /// </summary>
    protected override AttackStyle[] Swings => new[] { AttackStyle.Cleave, AttackStyle.Polearm };

    /// <summary>
    /// Bardiche by Kanpai, coloured steel grey (the model has no texture).
    /// </summary>
    protected override string ModelName => "whbardiche";

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Mountain;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Silver", Amount = 20, Recover = false },
        new() { Item = "FineWood", Amount = 10, Recover = false },
        new() { Item = "WolfFang", Amount = 5, Recover = false }
    };

    /// <summary>
    /// Slash in place of the atgeir's pierce.
    /// </summary>
    /// <param name="shared">The weapon's shared data.</param>
    protected override void AdjustStats(ItemDrop.ItemData.SharedData shared)
    {
        shared.m_damages = new HitData.DamageTypes { m_slash = 90f };
        shared.m_damagesPerLevel = new HitData.DamageTypes { m_slash = 5f };
    }
}
