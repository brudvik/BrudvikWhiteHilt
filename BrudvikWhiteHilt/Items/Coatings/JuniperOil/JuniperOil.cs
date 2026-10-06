using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Coatings.JuniperOil;

/// <summary>
/// Juniper oil rubbed on the blade: part of every hit becomes spirit damage, for a number of hits.
/// </summary>
public class JuniperOil : WeaponCoatingBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public JuniperOil(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltJuniperOil";

    /// <inheritdoc/>
    protected override string FullName => "Juniper Oil";

    /// <inheritdoc/>
    protected override string Description => "A flask of pungent oil pressed from juniper berries and resin. Rubbed on a blade, it stings the dead. Use it from the inventory.";

    /// <inheritdoc/>
    protected override string EffectTooltip => "Juniper oil: {0}% of the physical damage is added as spirit damage for {1} hits";

    /// <inheritdoc/>
    protected override string ModelName => "oilflask";

    /// <inheritdoc/>
    protected override Color Tint => new(0.7f, 0.85f, 0.95f);

    /// <inheritdoc/>
    protected override string CraftingStation => CraftingStations.Cauldron;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = Foraging.Juniper.Juniper.PrefabName, Amount = 5, Recover = false },
        new() { Item = "Resin", Amount = 3, Recover = false }
    };

    /// <inheritdoc/>
    protected override float DefaultStrength => 0.2f;

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Mountain;

    /// <inheritdoc/>
    protected override void Configure(WeaponCoatingEffect coating, float value)
    {
        coating.EdgeBonus = 0f;
        coating.AddedType = HitData.DamageType.Spirit;
        coating.AddedShare = value;
    }
}
