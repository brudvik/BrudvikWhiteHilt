using BrudvikWhiteHilt.Kraken;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.KrakenFeast;

/// <summary>
/// A black, hearty dish of Kraken tentacle cooked in its own ink that speeds up health and stamina regeneration.
/// </summary>
public class KrakenFeast : WhiteHiltFoodBase
{
    /// <summary>
    /// Constructor for the KrakenFeast class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public KrakenFeast(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltKrakenFeast";

    /// <inheritdoc/>
    protected override string FullName => "Kraken Feast";

    /// <inheritdoc/>
    protected override string Description => "Kraken tentacle stewed black in its own ink with turnip and roseroot. A meal to boast of.";

    /// <inheritdoc/>
    protected override string CopyFrom => "SerpentStew";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = KrakenRegistry.MeatName, Amount = 1, Recover = false },
        new() { Item = KrakenRegistry.InkName, Amount = 1, Recover = false },
        new() { Item = "Turnip", Amount = 2, Recover = false },
        new() { Item = Foraging.Roseroot.Roseroot.PrefabName, Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 70f;

    /// <inheritdoc/>
    protected override float Stamina => 40f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.35f, 0.3f, 0.4f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 2400f;

    /// <inheritdoc/>
    protected override float Regen => 5f;

    /// <inheritdoc/>
    protected override int MinStationLevel => 3;

    /// <inheritdoc/>
    protected override string BuffTooltip => "Health and stamina regenerate 25% faster";

    /// <inheritdoc/>
    protected override void ConfigureBuff(SE_Stats effect)
    {
        effect.m_healthRegenMultiplier = 1.25f;
        effect.m_staminaRegenMultiplier = 1.25f;
    }
}
