using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Armors.LavaWormArmor;

/// <summary>
/// The Lava Worm mail, a copy of the vanilla <c>ArmorAshlandsMediumChest</c> (see <see cref="LavaWormArmorBase"/>).
/// </summary>
public class LavaWormMail : LavaWormArmorBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public LavaWormMail(ItemManager instance)
    {
    }

    /// <inheritdoc/>
    public override string Id => "WhiteHilt_LavaWormMail";

    /// <inheritdoc/>
    protected override string CopyFrom => "ArmorAshlandsMediumChest";

    /// <inheritdoc/>
    protected override string FullName => "Lava Worm Mail";

    /// <inheritdoc/>
    protected override string Description => "Scale on scale from a worm that swam in fire. What burned it once cannot burn you.";

    /// <inheritdoc/>
    protected override int Scales => 6;

    /// <inheritdoc/>
    protected override bool ShowsSet => true;
}
