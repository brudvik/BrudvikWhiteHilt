using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.AsksvinFireweedStew;

/// <summary>
/// A health-leaning Ashlands stew of asksvin meat, fireweed shoots, ash morels and onion.
/// </summary>
public class AsksvinFireweedStew : WhiteHiltFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public AsksvinFireweedStew(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltAsksvinFireweedStew";

    /// <inheritdoc/>
    protected override string FullName => "Asksvin Stew with Fireweed";

    /// <inheritdoc/>
    protected override string Description => "Asksvin meat stewed with fireweed shoots, ash morels and onion. A meal that keeps you on your feet in the burning lands.";

    /// <inheritdoc/>
    protected override string CopyFrom => "FierySvinstew";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "AsksvinMeat", Amount = 1, Recover = false },
        new() { Item = Foraging.Fireweed.Fireweed.PrefabName, Amount = 2, Recover = false },
        new() { Item = Foraging.AshMorel.AshMorel.PrefabName, Amount = 1, Recover = false },
        new() { Item = "Onion", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 98f;

    /// <inheritdoc/>
    protected override float Stamina => 34f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.9f, 0.7f, 0.75f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 1800f;

    /// <inheritdoc/>
    protected override float Regen => 6f;

    /// <inheritdoc/>
    protected override int MinStationLevel => 4;
}
