using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Tools.WhiteHiltAxe;

/// <summary>
/// The White Hilt Axe, cloned from the vanilla <c>AxeIron</c>.
/// </summary>
public class WhiteHiltAxe : WhiteHiltToolBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltAxe(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltAxe";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Axe";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Axe of Dyrnwyn";

    /// <inheritdoc/>
    protected override string CopyFrom => "AxeIron";

    /// <inheritdoc/>
    public override bool Enabled => true;
}
