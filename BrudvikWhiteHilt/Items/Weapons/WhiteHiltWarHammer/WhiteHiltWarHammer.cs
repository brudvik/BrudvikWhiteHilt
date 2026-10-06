using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltWarHammer;

/// <summary>
/// The White Hilt War Hammer, a one-handed hammer with a pick, cloned from the vanilla <c>MaceIron</c> with the White
/// Hilt model <c>whwarhammer</c> (plain grey: the model has no texture). It crushes and pierces, and its pick mines
/// ore like an iron pickaxe. In linear progression it unlocks with the Mountain tier.
/// </summary>
public class WhiteHiltWarHammer : WhiteHiltWeaponBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltWarHammer(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltWarHammer";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt War Hammer";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible War Hammer of Dyrnwyn. Its pick bites into foe and ore alike.";

    /// <inheritdoc/>
    protected override string CopyFrom => "MaceIron";

    /// <summary>
    /// War Hammer by Yudha Mfr, coloured steel grey.
    /// </summary>
    protected override string ModelName => "whwarhammer";

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Mountain;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Silver", Amount = 15, Recover = false },
        new() { Item = "Obsidian", Amount = 5, Recover = false },
        new() { Item = "PickaxeIron", Amount = 1, Recover = false }
    };

    /// <summary>
    /// Blunt and pierce, and pickaxe damage at the iron pickaxe's tier, so it mines silver.
    /// </summary>
    /// <param name="shared">The weapon's shared data.</param>
    protected override void AdjustStats(ItemDrop.ItemData.SharedData shared)
    {
        shared.m_damages = new HitData.DamageTypes { m_blunt = 50f, m_pierce = 30f, m_pickaxe = 30f };
        shared.m_damagesPerLevel = new HitData.DamageTypes { m_blunt = 3f, m_pierce = 2f, m_pickaxe = 2f };
        shared.m_toolTier = 2;
    }
}
