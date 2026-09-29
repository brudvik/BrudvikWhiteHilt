using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Meads.CrowberryWine;

/// <summary>
/// A berry wine that makes health regenerate faster.
/// </summary>
public class CrowberryWine : WhiteHiltMeadBase
{
    /// <summary>
    /// Constructor for the CrowberryWine class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public CrowberryWine(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltCrowberryWine";

    /// <inheritdoc/>
    protected override string FullName => "Crowberry Wine";

    /// <inheritdoc/>
    protected override string Description => "A near-black wine of mountain crowberries and a little roseroot. Wounds close faster with it in your blood.";

    /// <inheritdoc/>
    protected override string EffectTooltip => "Health regenerates 50% faster";

    /// <inheritdoc/>
    protected override string CopyMeadFrom => "MeadHealthMinor";

    /// <inheritdoc/>
    protected override string CopyMeadBaseFrom => "MeadBaseHealthMinor";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Honey", Amount = 10, Recover = false },
        new() { Item = Foraging.Crowberries.Crowberries.PrefabName, Amount = 10, Recover = false },
        new() { Item = Foraging.Roseroot.Roseroot.PrefabName, Amount = 2, Recover = false }
    };

    /// <inheritdoc/>
    protected override Color Tint => new(0.5f, 0.35f, 0.6f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override void ConfigureEffect(SE_Stats effect)
    {
        effect.m_healthRegenMultiplier = 1.5f;
    }
}
