using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.JuniperLoxPot;

/// <summary>
/// A health-leaning Plains pot roast of lox meat with juniper and mountain sorrel.
/// </summary>
public class JuniperLoxPot : WhiteHiltFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public JuniperLoxPot(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltJuniperLoxPot";

    /// <inheritdoc/>
    protected override string FullName => "Juniper Lox Pot";

    /// <inheritdoc/>
    protected override string Description => "Lox meat braised all day in a soapstone cauldron with juniper berries, onion and sour mountain sorrel. Dark, rich and filling.";

    /// <inheritdoc/>
    protected override string CopyFrom => "SerpentStew";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "LoxMeat", Amount = 2, Recover = false },
        new() { Item = Foraging.Juniper.Juniper.PrefabName, Amount = 2, Recover = false },
        new() { Item = "Onion", Amount = 1, Recover = false },
        new() { Item = Foraging.MountainSorrel.MountainSorrel.PrefabName, Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 74f;

    /// <inheritdoc/>
    protected override float Stamina => 38f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.6f, 0.38f, 0.3f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 2700f;

    /// <inheritdoc/>
    protected override float Regen => 4f;

    /// <inheritdoc/>
    protected override int MinStationLevel => 4;
}
