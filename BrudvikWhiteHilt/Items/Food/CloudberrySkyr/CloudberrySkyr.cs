using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.CloudberrySkyr;

/// <summary>
/// A balanced Plains dish of soured lox milk with cloudberries whose buff speeds up health regeneration.
/// </summary>
public class CloudberrySkyr : WhiteHiltFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public CloudberrySkyr(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltCloudberrySkyr";

    /// <inheritdoc/>
    protected override string FullName => "Skyr with Cloudberries";

    /// <inheritdoc/>
    protected override string Description => "Thick soured lox milk, strained and stirred with golden cloudberries and honey. The farmers' strength.";

    /// <inheritdoc/>
    protected override string CopyFrom => "Eyescream";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = global::BrudvikWhiteHilt.Ranching.LoxMilking.MilkName, Amount = 2, Recover = false },
        new() { Item = "Cloudberry", Amount = 3, Recover = false },
        new() { Item = "Honey", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 48f;

    /// <inheritdoc/>
    protected override float Stamina => 48f;

    /// <inheritdoc/>
    protected override Color Tint => new(1f, 0.92f, 0.75f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 2700f;

    /// <inheritdoc/>
    protected override float Regen => 4f;

    /// <inheritdoc/>
    protected override int MinStationLevel => 2;

    /// <inheritdoc/>
    protected override string BuffTooltip => "Health regenerates 25% faster";

    /// <inheritdoc/>
    protected override void ConfigureBuff(SE_Stats effect)
    {
        effect.m_healthRegenMultiplier = 1.25f;
    }
}
