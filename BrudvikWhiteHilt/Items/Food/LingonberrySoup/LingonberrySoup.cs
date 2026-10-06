using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.LingonberrySoup;

/// <summary>
/// A stamina-leaning Black Forest soup.
/// </summary>
public class LingonberrySoup : WhiteHiltFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public LingonberrySoup(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltLingonberrySoup";

    /// <inheritdoc/>
    protected override string FullName => "Lingonberry Soup";

    /// <inheritdoc/>
    protected override string Description => "A sweet and tart soup of lingonberries, wild garlic and honey. Good for long days on the forest paths.";

    /// <inheritdoc/>
    protected override string CopyFrom => "MinceMeatSauce";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = Foraging.Lingonberries.Lingonberries.PrefabName, Amount = 3, Recover = false },
        new() { Item = Foraging.WildGarlic.WildGarlic.PrefabName, Amount = 1, Recover = false },
        new() { Item = "Honey", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 24f;

    /// <inheritdoc/>
    protected override float Stamina => 42f;

    /// <inheritdoc/>
    protected override Color Tint => new(1f, 0.55f, 0.55f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 1800f;

    /// <inheritdoc/>
    protected override float Regen => 3f;
}
