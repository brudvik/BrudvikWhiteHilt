using BrudvikWhiteHilt.Monsters;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.DragonscaleBroth;

/// <summary>
/// A Plains broth of Desert Dragon scale whose buff halves fire damage.
/// </summary>
public class DragonscaleBroth : WhiteHiltFoodBase
{
    /// <summary>
    /// Constructor for the DragonscaleBroth class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public DragonscaleBroth(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltDragonscaleBroth";

    /// <inheritdoc/>
    protected override string FullName => "Dragonscale Broth";

    /// <inheritdoc/>
    protected override string Description => "A Desert Dragon scale simmered for a day with onion and barley until it gave up its heat. The broth keeps the fire out of you.";

    /// <inheritdoc/>
    protected override string CopyFrom => "OnionSoup";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = DesertDragonRegistry.ScaleName, Amount = 1, Recover = false },
        new() { Item = "Onion", Amount = 2, Recover = false },
        new() { Item = "Barley", Amount = 2, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 50f;

    /// <inheritdoc/>
    protected override float Stamina => 30f;

    /// <inheritdoc/>
    protected override Color Tint => new(1f, 0.7f, 0.45f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 2400f;

    /// <inheritdoc/>
    protected override float Regen => 3f;

    /// <inheritdoc/>
    protected override int MinStationLevel => 2;

    /// <inheritdoc/>
    protected override string BuffTooltip => "Fire damage taken is halved";

    /// <inheritdoc/>
    protected override void ConfigureBuff(SE_Stats effect)
    {
        effect.m_mods.Add(new HitData.DamageModPair { m_type = HitData.DamageType.Fire, m_modifier = HitData.DamageModifier.Resistant });
    }
}
