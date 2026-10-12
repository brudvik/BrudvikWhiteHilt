using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.LichenBread;

/// <summary>
/// A stamina-leaning Mistlands bread of barley and beard lichen whose buff speeds up eitr regeneration.
/// </summary>
public class LichenBread : WhiteHiltFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public LichenBread(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltLichenBread";

    /// <inheritdoc/>
    protected override string FullName => "Lichen Bread";

    /// <inheritdoc/>
    protected override string Description => "Hard bread of barley and ground beard lichen, as it was baked in the hungry years. Bitter, but it keeps a mage's mind clear.";

    /// <inheritdoc/>
    protected override string CopyFrom => "Bread";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = Foraging.BeardLichen.BeardLichen.PrefabName, Amount = 3, Recover = false },
        new() { Item = "BarleyFlour", Amount = 2, Recover = false },
        new() { Item = "Honey", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 32f;

    /// <inheritdoc/>
    protected override float Stamina => 76f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.75f, 0.8f, 0.7f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 1800f;

    /// <inheritdoc/>
    protected override float Regen => 3f;

    /// <inheritdoc/>
    protected override int MinStationLevel => 4;

    /// <inheritdoc/>
    protected override string BuffTooltip => "Eitr regenerates 25% faster";

    /// <inheritdoc/>
    protected override void ConfigureBuff(SE_Stats effect)
    {
        effect.m_eitrRegenMultiplier = 1.25f;
    }
}
