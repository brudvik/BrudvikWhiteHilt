using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.LadysMantleBroth;

/// <summary>
/// A Mistlands broth of lady's mantle and mist mushrooms that gives eitr.
/// </summary>
public class LadysMantleBroth : WhiteHiltFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public LadysMantleBroth(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltLadysMantleBroth";

    /// <inheritdoc/>
    protected override string FullName => "Lady's Mantle Broth";

    /// <inheritdoc/>
    protected override string Description => "A pale green broth of lady's mantle and mist mushrooms, thickened with sap. It wakes the eitr.";

    /// <inheritdoc/>
    protected override string CopyFrom => "YggdrasilPorridge";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = Foraging.LadysMantle.LadysMantle.PrefabName, Amount = 3, Recover = false },
        new() { Item = "MushroomJotunPuffs", Amount = 2, Recover = false },
        new() { Item = "Sap", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 26f;

    /// <inheritdoc/>
    protected override float Stamina => 13f;

    /// <inheritdoc/>
    protected override float Eitr => 80f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.75f, 0.9f, 0.7f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 1800f;

    /// <inheritdoc/>
    protected override float Regen => 3f;

    /// <inheritdoc/>
    protected override int MinStationLevel => 4;
}
