using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Tools.WhiteHiltCultivator;

/// <summary>
/// The White Hilt Cultivator, cloned from the vanilla <c>Cultivator</c>.
/// </summary>
public class WhiteHiltCultivator : WhiteHiltToolBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltCultivator(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltCultivator";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Cultivator";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Cultivator of Dyrnwyn";

    /// <inheritdoc/>
    protected override string CopyFrom => "Cultivator";

    /// <inheritdoc/>
    public override bool Enabled => true;
}
