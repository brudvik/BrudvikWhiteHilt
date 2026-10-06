using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Tools.WhiteHiltHoe;

/// <summary>
/// The White Hilt Hoe, cloned from the vanilla <c>Hoe</c>.
/// </summary>
public class WhiteHiltHoe : WhiteHiltToolBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltHoe(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltHoe";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Hoe";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Hoe of Dyrnwyn";

    /// <inheritdoc/>
    protected override string CopyFrom => "Hoe";

    /// <inheritdoc/>
    public override bool Enabled => true;
}
