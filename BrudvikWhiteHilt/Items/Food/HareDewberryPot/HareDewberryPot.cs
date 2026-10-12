using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.HareDewberryPot;

/// <summary>
/// A health-leaning Mistlands dish of hare simmered with onion and dewberries.
/// </summary>
public class HareDewberryPot : WhiteHiltFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public HareDewberryPot(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltHareDewberryPot";

    /// <inheritdoc/>
    protected override string FullName => "Hare and Dewberry Pot";

    /// <inheritdoc/>
    protected override string Description => "Hare simmered with onion until it falls apart, with dewberries for a sweet, dark sauce.";

    /// <inheritdoc/>
    protected override string CopyFrom => "MisthareSupreme";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "HareMeat", Amount = 1, Recover = false },
        new() { Item = Foraging.Dewberries.Dewberries.PrefabName, Amount = 3, Recover = false },
        new() { Item = "Onion", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 82f;

    /// <inheritdoc/>
    protected override float Stamina => 30f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.7f, 0.6f, 0.85f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 1800f;

    /// <inheritdoc/>
    protected override float Regen => 5f;

    /// <inheritdoc/>
    protected override int MinStationLevel => 4;
}
