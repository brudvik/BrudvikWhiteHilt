using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.ChanterelleStew;

/// <summary>
/// A health-leaning Meadows stew.
/// </summary>
public class ChanterelleStew : WhiteHiltFoodBase
{
    /// <summary>
    /// Constructor for the ChanterelleStew class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public ChanterelleStew(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltChanterelleStew";

    /// <inheritdoc/>
    protected override string FullName => "Chanterelle Stew";

    /// <inheritdoc/>
    protected override string Description => "A hearty stew of chanterelles, wild garlic and meat. Gives you strength for the Black Forest.";

    /// <inheritdoc/>
    protected override string CopyFrom => "CarrotSoup";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = Foraging.Chanterelle.Chanterelle.PrefabName, Amount = 3, Recover = false },
        new() { Item = Foraging.WildGarlic.WildGarlic.PrefabName, Amount = 1, Recover = false },
        new() { Item = "RawMeat", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 30f;

    /// <inheritdoc/>
    protected override float Stamina => 22f;

    /// <inheritdoc/>
    protected override Color Tint => new(1f, 0.85f, 0.45f);

    /// <inheritdoc/>
    public override bool Enabled => true;
}
