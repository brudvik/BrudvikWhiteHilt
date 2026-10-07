using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltHandAxe;

/// <summary>
/// The White Hilt Hand Axe, a light one-handed axe cloned from the vanilla <c>AxeIron</c>: less slash, more chop, and a
/// fifth less stamina, for felling trees, with the White Hilt model <c>whhandaxe</c>.
/// </summary>
public class WhiteHiltHandAxe : WhiteHiltWeaponBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltHandAxe(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltHandAxe";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Hand Axe";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Hand Axe of Dyrnwyn. Light, and quick through timber.";

    /// <inheritdoc/>
    protected override string CopyFrom => "AxeIron";

    /// <summary>
    /// Small One Handed Axe by iedalton, with a white haft.
    /// </summary>
    protected override string ModelName => "whhandaxe";

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 15, Recover = false },
        new() { Item = "FineWood", Amount = 5, Recover = false },
        new() { Item = "LeatherScraps", Amount = 3, Recover = false }
    };

    /// <summary>
    /// More chop than slash, and a fifth less stamina per blow.
    /// </summary>
    /// <param name="shared">The weapon's shared data.</param>
    protected override void AdjustStats(ItemDrop.ItemData.SharedData shared)
    {
        shared.m_damages = new HitData.DamageTypes { m_slash = 45f, m_chop = 70f };
        shared.m_damagesPerLevel = new HitData.DamageTypes { m_slash = 3f, m_chop = 4f };
        shared.m_attack.m_attackStamina *= 0.8f;
    }
}
