using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.SweetGaleSausages;

/// <summary>
/// A health-leaning Swamp dish.
/// </summary>
public class SweetGaleSausages : WhiteHiltFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public SweetGaleSausages(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltSweetGaleSausages";

    /// <inheritdoc/>
    protected override string FullName => "Sweet Gale Sausages";

    /// <inheritdoc/>
    protected override string Description => "Coarse sausages spiced with sweet gale and wild garlic. Sits heavy in the stomach, just right for the Swamp.";

    /// <inheritdoc/>
    protected override string CopyFrom => "Sausages";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = Foraging.SweetGale.SweetGale.PrefabName, Amount = 2, Recover = false },
        new() { Item = Foraging.WildGarlic.WildGarlic.PrefabName, Amount = 1, Recover = false },
        new() { Item = "Entrails", Amount = 3, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 48f;

    /// <inheritdoc/>
    protected override float Stamina => 26f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.75f, 0.6f, 0.45f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 1950f;

    /// <inheritdoc/>
    protected override float Regen => 3f;
}
