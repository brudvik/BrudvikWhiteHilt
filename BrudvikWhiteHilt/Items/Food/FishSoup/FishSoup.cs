using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.FishSoup;

/// <summary>
/// A creamy Plains soup of herring and tuna, simmered for hours in the soapstone cauldron.
/// </summary>
public class FishSoup : WhiteHiltFoodBase
{
    /// <summary>
    /// Constructor for the FishSoup class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public FishSoup(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltFishSoup";

    /// <inheritdoc/>
    protected override string FullName => "Fish Soup";

    /// <inheritdoc/>
    protected override string Description => "Herring and tuna simmered slowly in a soapstone cauldron with barley and caraway until the broth turns thick and white.";

    /// <inheritdoc/>
    protected override string CopyFrom => "SerpentStew";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Fish6", Amount = 1, Recover = false },
        new() { Item = "Fish3", Amount = 1, Recover = false },
        new() { Item = "BarleyFlour", Amount = 1, Recover = false },
        new() { Item = Foraging.Caraway.Caraway.PrefabName, Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 62f;

    /// <inheritdoc/>
    protected override float Stamina => 52f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.95f, 0.88f, 0.72f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 2700f;

    /// <inheritdoc/>
    protected override float Regen => 4f;

    /// <inheritdoc/>
    protected override int MinStationLevel => 4;
}
