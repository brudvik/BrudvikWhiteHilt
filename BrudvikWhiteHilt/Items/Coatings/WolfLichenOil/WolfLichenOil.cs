using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Coatings.WolfLichenOil;

/// <summary>
/// Wolf lichen boiled in fat and rubbed on the blade: part of every hit becomes poison damage, for a number of hits.
/// </summary>
public class WolfLichenOil : WeaponCoatingBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public WolfLichenOil(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltWolfLichenOil";

    /// <inheritdoc/>
    protected override string FullName => "Wolf Lichen Oil";

    /// <inheritdoc/>
    protected override string Description => "A yellow, foul-smelling grease of wolf lichen boiled in fat. Whatever the blade cuts, it poisons. Use it from the inventory.";

    /// <inheritdoc/>
    protected override string EffectTooltip => "Wolf lichen oil: {0}% of the physical damage is added as poison damage for {1} hits";

    /// <inheritdoc/>
    protected override string ModelName => "oilflask";

    /// <inheritdoc/>
    protected override Color Tint => new(1f, 0.95f, 0.55f);

    /// <inheritdoc/>
    protected override string CraftingStation => CraftingStations.Cauldron;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = Foraging.WolfLichen.WolfLichen.PrefabName, Amount = 5, Recover = false },
        new() { Item = "Entrails", Amount = 2, Recover = false }
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
        coating.AddedType = HitData.DamageType.Poison;
        coating.AddedShare = value;
    }
}
