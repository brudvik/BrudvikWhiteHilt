using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.MossPorridge;

/// <summary>
/// A stamina-leaning Mountain porridge of boiled Iceland moss.
/// </summary>
public class MossPorridge : WhiteHiltFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public MossPorridge(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltMossPorridge";

    /// <inheritdoc/>
    protected override string FullName => "Moss Porridge";

    /// <inheritdoc/>
    protected override string Description => "Iceland moss boiled until it sets, sweetened with honey and crowberries. Bitter at first, but it carries you over the passes.";

    /// <inheritdoc/>
    protected override string CopyFrom => "OnionSoup";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = Foraging.IcelandMoss.IcelandMoss.PrefabName, Amount = 3, Recover = false },
        new() { Item = Foraging.Crowberries.Crowberries.PrefabName, Amount = 2, Recover = false },
        new() { Item = "Honey", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 30f;

    /// <inheritdoc/>
    protected override float Stamina => 56f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.8f, 0.7f, 0.5f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 2100f;

    /// <inheritdoc/>
    protected override float Regen => 3f;

    /// <inheritdoc/>
    protected override int MinStationLevel => 2;
}
