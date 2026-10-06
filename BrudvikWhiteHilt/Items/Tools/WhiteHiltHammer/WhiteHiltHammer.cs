using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Tools.WhiteHiltHammer;

/// <summary>
/// The White Hilt Hammer, cloned from the vanilla <c>Hammer</c>.
/// </summary>
public class WhiteHiltHammer : WhiteHiltToolBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltHammer(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltHammer";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Hammer";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Hammer of Dyrnwyn";

    /// <inheritdoc/>
    protected override string CopyFrom => "Hammer";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 5, Recover = false },
        new() { Item = "Stone", Amount = 1, Recover = false },
        new() { Item = "Resin", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    public override bool Enabled => true;
}
