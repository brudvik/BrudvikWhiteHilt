using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Armors.LavaWormArmor;

/// <summary>
/// The Lava Worm greaves, a copy of the vanilla <c>ArmorAshlandsMediumlegs</c> (see <see cref="LavaWormArmorBase"/>).
/// </summary>
public class LavaWormGreaves : LavaWormArmorBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public LavaWormGreaves(ItemManager instance)
    {
    }

    /// <inheritdoc/>
    public override string Id => "WhiteHilt_LavaWormGreaves";

    /// <inheritdoc/>
    protected override string CopyFrom => "ArmorAshlandsMediumlegs";

    /// <inheritdoc/>
    protected override string FullName => "Lava Worm Greaves";

    /// <inheritdoc/>
    protected override string Description => "Scaled greaves for wading where no one should. The heat climbs slower.";

    /// <inheritdoc/>
    protected override int Scales => 6;
}
