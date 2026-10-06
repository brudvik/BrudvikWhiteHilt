using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltIceSword;

/// <summary>
/// The White Hilt Ice Sword, cloned from the vanilla <c>SwordSilver</c> with frost in place of its spirit damage and
/// the White Hilt model <c>whicesword</c>, whose blade glows icy blue. Frost slows what it hits. In linear progression
/// it unlocks with the Mountain tier.
/// </summary>
public class WhiteHiltIceSword : WhiteHiltWeaponBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltIceSword(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltIceSword";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Ice Sword";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Ice Sword of Dyrnwyn. Its frost slows what it cuts.";

    /// <inheritdoc/>
    protected override string CopyFrom => "SwordSilver";

    /// <summary>
    /// Ice Crystal Sword by Sir Erdees, one of its two swords, with a white grip and its own glow.
    /// </summary>
    protected override string ModelName => "whicesword";

    /// <inheritdoc/>
    protected override Color? ModelGlow => new Color(0.45f, 0.8f, 1f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Mountain;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Silver", Amount = 20, Recover = false },
        new() { Item = "FreezeGland", Amount = 10, Recover = false },
        new() { Item = "Crystal", Amount = 5, Recover = false }
    };

    /// <summary>
    /// The Silver Sword's slash with frost instead of spirit.
    /// </summary>
    /// <param name="shared">The weapon's shared data.</param>
    protected override void AdjustStats(ItemDrop.ItemData.SharedData shared)
    {
        shared.m_damages.m_frost = shared.m_damages.m_spirit;
        shared.m_damages.m_spirit = 0f;
        shared.m_damagesPerLevel.m_frost = shared.m_damagesPerLevel.m_spirit;
        shared.m_damagesPerLevel.m_spirit = 0f;
    }
}
