using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.HazelnutBread;

/// <summary>
/// A balanced Mistlands bread of barley and ground hazelnuts, sweetened with sweet cicely instead of honey.
/// </summary>
public class HazelnutBread : WhiteHiltFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public HazelnutBread(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltHazelnutBread";

    /// <inheritdoc/>
    protected override string FullName => "Hazelnut Bread";

    /// <inheritdoc/>
    protected override string Description => "A heavy, nutty loaf of barley and ground hazelnuts, sweet with sweet cicely. Rich enough to climb on all day.";

    /// <inheritdoc/>
    protected override string CopyFrom => "Bread";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = Foraging.Hazelnuts.Hazelnuts.PrefabName, Amount = 4, Recover = false },
        new() { Item = "BarleyFlour", Amount = 2, Recover = false },
        new() { Item = Foraging.SweetCicely.SweetCicely.PrefabName, Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 58f;

    /// <inheritdoc/>
    protected override float Stamina => 58f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.75f, 0.6f, 0.45f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 2100f;

    /// <inheritdoc/>
    protected override float Regen => 4f;

    /// <inheritdoc/>
    protected override int MinStationLevel => 4;
}
