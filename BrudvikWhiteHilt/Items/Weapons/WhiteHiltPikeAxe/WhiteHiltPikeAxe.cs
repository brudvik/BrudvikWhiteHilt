using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltPikeAxe;

/// <summary>
/// The White Hilt Pike Axe, a gilded axe-spear swung like the vanilla <c>AtgeirBlackmetal</c> with pierce and slash at
/// Mistlands strength; its head glows gold, with the White Hilt model <c>whpikeaxe</c>. In linear progression it
/// unlocks with the Mistlands tier.
/// </summary>
public class WhiteHiltPikeAxe : WhiteHiltWeaponBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltPikeAxe(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltPikeAxe";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Pike Axe";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Pike Axe of Dyrnwyn. Its gilded head glows.";

    /// <inheritdoc/>
    protected override string CopyFrom => "AtgeirBlackmetal";

    /// <summary>
    /// Pike Axe by soidev, with a white grip; its head glows gold.
    /// </summary>
    protected override string ModelName => "whpikeaxe";

    /// <inheritdoc/>
    protected override Color? ModelGlow => new Color(1f, 0.78f, 0.35f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Mistlands;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "BlackMetal", Amount = 20, Recover = false },
        new() { Item = "YggdrasilWood", Amount = 10, Recover = false },
        new() { Item = "Eitr", Amount = 8, Recover = false }
    };

    /// <summary>
    /// Pierce and slash at Mistlands strength.
    /// </summary>
    /// <param name="shared">The weapon's shared data.</param>
    protected override void AdjustStats(ItemDrop.ItemData.SharedData shared)
    {
        shared.m_damages = new HitData.DamageTypes { m_pierce = 95f, m_slash = 45f };
        shared.m_damagesPerLevel = new HitData.DamageTypes { m_pierce = 5f, m_slash = 3f };
    }
}
