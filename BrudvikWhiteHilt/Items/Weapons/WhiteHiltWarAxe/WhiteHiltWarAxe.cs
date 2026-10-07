using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltWarAxe;

/// <summary>
/// The White Hilt War Axe, a two-handed axe cloned from the vanilla <c>Battleaxe</c> with the strength of the Black
/// Metal Battleaxe, with the White Hilt model <c>whwaraxe</c>. In linear progression it unlocks with the Plains tier.
/// </summary>
public class WhiteHiltWarAxe : WhiteHiltWeaponBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltWarAxe(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltWarAxe";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt War Axe";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible War Axe of Dyrnwyn. A great axe with a spike, for both hands.";

    /// <inheritdoc/>
    protected override string CopyFrom => "Battleaxe";

    /// <inheritdoc/>
    protected override string StatsFrom => "BattleaxeBlackmetal";

    /// <summary>
    /// Medieval-Fantasy Multipurpose War Axe by sethsaemann, coloured steel grey (the model has no texture).
    /// </summary>
    protected override string ModelName => "whwaraxe";

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Plains;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "BlackMetal", Amount = 25, Recover = false },
        new() { Item = "FineWood", Amount = 10, Recover = false },
        new() { Item = "Needle", Amount = 5, Recover = false }
    };
}
