using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Armors.WhiteHiltHelmet;

/// <summary>
/// The White Hilt Helmet, cloned from the vanilla <c>HelmetFlametal</c>, which gives its look and how it is used, with
/// the stats of <c>HelmetIron</c>.
/// </summary>
public class WhiteHiltHelmet : WhiteHiltArmorBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltHelmet(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltHelmet";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Helmet";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Helmet of Dyrnwyn";

    /// <inheritdoc/>
    protected override string CopyFrom => "HelmetFlametal";

    /// <inheritdoc/>
    protected override string StatsFrom => "HelmetIron";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 30, Recover = false },
        new() { Item = "IronNails", Amount = 100, Recover = false },
        new() { Item = "HelmetIron", Amount = 1, Recover = false },
    };

    /// <inheritdoc/>
    public override bool Enabled => true;
}
