using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.HotSpringBread;

/// <summary>
/// A stamina-leaning Ashlands bread of lyme grain, salted with hot spring salt and sweetened with sea buckthorn, as the
/// Icelanders baked rye in the hot ground.
/// </summary>
public class HotSpringBread : WhiteHiltFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public HotSpringBread(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltHotSpringBread";

    /// <inheritdoc/>
    protected override string FullName => "Hot Spring Bread";

    /// <inheritdoc/>
    protected override string Description => "Dark, sweet bread of lyme grain with sea buckthorn, baked slowly in the hot ground. It fills a pack and the legs for a whole day.";

    /// <inheritdoc/>
    protected override string CopyFrom => "Bread";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = Foraging.LymeGrass.LymeGrass.PrefabName, Amount = 4, Recover = false },
        new() { Item = Foraging.SeaBuckthorn.SeaBuckthorn.PrefabName, Amount = 2, Recover = false },
        new() { Item = Foraging.HotSpringSalt.HotSpringSalt.PrefabName, Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 45f;

    /// <inheritdoc/>
    protected override float Stamina => 92f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.6f, 0.45f, 0.35f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 2100f;

    /// <inheritdoc/>
    protected override float Regen => 5f;

    /// <inheritdoc/>
    protected override int MinStationLevel => 4;
}
