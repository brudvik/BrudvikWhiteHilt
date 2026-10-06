using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.CattailPorridge;

/// <summary>
/// A stamina-leaning Swamp dish of ground cattail root.
/// </summary>
public class CattailPorridge : WhiteHiltFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public CattailPorridge(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltCattailPorridge";

    /// <inheritdoc/>
    protected override string FullName => "Cattail Porridge";

    /// <inheritdoc/>
    protected override string Description => "A coarse porridge of ground cattail root, sweetened with honey and sour cranberries. Swamp folk start the day with it.";

    /// <inheritdoc/>
    protected override string CopyFrom => "CarrotSoup";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = Foraging.Cattail.Cattail.PrefabName, Amount = 3, Recover = false },
        new() { Item = Foraging.Cranberries.Cranberries.PrefabName, Amount = 2, Recover = false },
        new() { Item = "Honey", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 22f;

    /// <inheritdoc/>
    protected override float Stamina => 52f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.85f, 0.7f, 0.55f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 1950f;

    /// <inheritdoc/>
    protected override float Regen => 3f;
}
