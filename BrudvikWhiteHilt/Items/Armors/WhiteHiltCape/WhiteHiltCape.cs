using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Armors.WhiteHiltCape;

/// <summary>
/// The White Hilt Cape, cloned from the vanilla <c>CapeFeather</c>. In linear progression it unlocks with the Plains
/// tier.
/// </summary>
public class WhiteHiltCape : WhiteHiltArmorBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltCape(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltCape";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Cape";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Cape of Dyrnwyn";

    /// <inheritdoc/>
    protected override string CopyFrom => "CapeFeather";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Feathers", Amount = 30, Recover = false },
        new() { Item = "TrophyDeathsquito", Amount = 1, Recover = false },
        new() { Item = "CapeTrollHide", Amount = 1, Recover = false }
    };

    // Feather fall comes from the Mistlands feather cape, so the cape waits for the Plains.
    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Plains;

    /// <inheritdoc/>
    public override bool Enabled => true;
}
