using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.CranberrySoup;

/// <summary>
/// A stamina-leaning Swamp dish.
/// </summary>
public class CranberrySoup : WhiteHiltFoodBase
{
    /// <summary>
    /// Constructor for the CranberrySoup class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public CranberrySoup(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltCranberrySoup";

    /// <inheritdoc/>
    protected override string FullName => "Cranberry Soup";

    /// <inheritdoc/>
    protected override string Description => "A sour red soup of cranberries, turnip and lingonberries. Keeps your legs moving through the mud.";

    /// <inheritdoc/>
    protected override string CopyFrom => "BlackSoup";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = Foraging.Cranberries.Cranberries.PrefabName, Amount = 3, Recover = false },
        new() { Item = Foraging.Lingonberries.Lingonberries.PrefabName, Amount = 1, Recover = false },
        new() { Item = "Turnip", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 26f;

    /// <inheritdoc/>
    protected override float Stamina => 48f;

    /// <inheritdoc/>
    protected override Color Tint => new(1f, 0.35f, 0.35f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 1950f;

    /// <inheritdoc/>
    protected override float Regen => 3f;
}
