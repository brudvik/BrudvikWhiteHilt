using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.AshMorelBroth;

/// <summary>
/// A dark Ashlands broth of ash morels, smoke puffs and fiddleheads that gives eitr.
/// </summary>
public class AshMorelBroth : WhiteHiltFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public AshMorelBroth(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltAshMorelBroth";

    /// <inheritdoc/>
    protected override string FullName => "Ash Morel Broth";

    /// <inheritdoc/>
    protected override string Description => "A dark broth of ash morels, smoke puffs and fiddleheads. Its smoke wakes the eitr.";

    /// <inheritdoc/>
    protected override string CopyFrom => "SizzlingBerryBroth";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = Foraging.AshMorel.AshMorel.PrefabName, Amount = 3, Recover = false },
        new() { Item = "MushroomSmokePuff", Amount = 2, Recover = false },
        new() { Item = "Fiddleheadfern", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 30f;

    /// <inheritdoc/>
    protected override float Stamina => 15f;

    /// <inheritdoc/>
    protected override float Eitr => 92f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.7f, 0.6f, 0.5f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 1800f;

    /// <inheritdoc/>
    protected override float Regen => 4f;

    /// <inheritdoc/>
    protected override int MinStationLevel => 4;
}
