using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.SmokedVolture;

/// <summary>
/// A health-leaning Ashlands dish of volture meat smoked over fire moss, whose buff speeds up stamina regeneration.
/// </summary>
public class SmokedVolture : WhiteHiltFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public SmokedVolture(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltSmokedVolture";

    /// <inheritdoc/>
    protected override string FullName => "Rowan-Smoked Volture";

    /// <inheritdoc/>
    protected override string Description => "Volture meat smoked over smouldering fire moss and glazed with rowanberries.";

    /// <inheritdoc/>
    protected override string CopyFrom => "CookedVoltureMeat";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "VoltureMeat", Amount = 1, Recover = false },
        new() { Item = Foraging.Rowanberries.Rowanberries.PrefabName, Amount = 2, Recover = false },
        new() { Item = Foraging.FireMoss.FireMoss.PrefabName, Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 96f;

    /// <inheritdoc/>
    protected override float Stamina => 32f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.85f, 0.6f, 0.45f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 1800f;

    /// <inheritdoc/>
    protected override float Regen => 6f;

    /// <inheritdoc/>
    protected override int MinStationLevel => 4;

    /// <inheritdoc/>
    protected override string BuffTooltip => "Stamina regenerates 25% faster";

    /// <inheritdoc/>
    protected override void ConfigureBuff(SE_Stats effect)
    {
        effect.m_staminaRegenMultiplier = 1.25f;
    }
}
