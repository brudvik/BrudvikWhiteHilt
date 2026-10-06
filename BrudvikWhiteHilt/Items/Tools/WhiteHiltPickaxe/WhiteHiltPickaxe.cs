using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Tools.WhiteHiltPickaxe;

/// <summary>
/// The White Hilt Pickaxe, cloned from the vanilla <c>PickaxeIron</c>.
/// </summary>
public class WhiteHiltPickaxe : WhiteHiltToolBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltPickaxe(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltPickaxe";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Pickaxe";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Pickaxe of Dyrnwyn";

    /// <inheritdoc/>
    protected override string CopyFrom => "PickaxeIron";

    /// <inheritdoc/>
    public override bool Enabled => true;
}
