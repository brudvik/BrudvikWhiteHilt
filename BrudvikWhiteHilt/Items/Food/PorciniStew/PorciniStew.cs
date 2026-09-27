using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.PorciniStew;

/// <summary>
/// A health-leaning Black Forest stew.
/// </summary>
public class PorciniStew : WhiteHiltFoodBase
{
    /// <summary>
    /// Constructor for the PorciniStew class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public PorciniStew(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltPorciniStew";

    /// <inheritdoc/>
    protected override string FullName => "Porcini Stew";

    /// <inheritdoc/>
    protected override string Description => "A thick stew of porcini, chanterelle and deer meat. Keeps you standing deep in the Black Forest.";

    /// <inheritdoc/>
    protected override string CopyFrom => "DeerStew";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = Foraging.Porcini.Porcini.PrefabName, Amount = 3, Recover = false },
        new() { Item = Foraging.Chanterelle.Chanterelle.PrefabName, Amount = 1, Recover = false },
        new() { Item = "DeerMeat", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 40f;

    /// <inheritdoc/>
    protected override float Stamina => 26f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.85f, 0.72f, 0.55f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 1800f;

    /// <inheritdoc/>
    protected override float Regen => 3f;
}
