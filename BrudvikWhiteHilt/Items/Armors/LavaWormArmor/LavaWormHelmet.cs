using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Armors.LavaWormArmor;

/// <summary>
/// The Lava Worm helmet, a copy of the vanilla <c>HelmetAshlandsMediumHood</c> (see <see cref="LavaWormArmorBase"/>).
/// </summary>
public class LavaWormHelmet : LavaWormArmorBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public LavaWormHelmet(ItemManager instance)
    {
    }

    /// <inheritdoc/>
    public override string Id => "WhiteHilt_LavaWormHelmet";

    /// <inheritdoc/>
    protected override string CopyFrom => "HelmetAshlandsMediumHood";

    /// <inheritdoc/>
    protected override string FullName => "Lava Worm Helm";

    /// <inheritdoc/>
    protected override string Description => "A hood of black scales that never cool. The lava's heat slides off it.";

    /// <inheritdoc/>
    protected override int Scales => 4;
}
