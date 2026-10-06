using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.CandiedAngelica;

/// <summary>
/// A light Mountain snack of angelica stalks in honey that lasts long.
/// </summary>
public class CandiedAngelica : WhiteHiltFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public CandiedAngelica(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltCandiedAngelica";

    /// <inheritdoc/>
    protected override string FullName => "Candied Angelica";

    /// <inheritdoc/>
    protected override string Description => "Green angelica stalks simmered in honey until they shine. A sweet chew that keeps you going for a long march.";

    /// <inheritdoc/>
    protected override string CopyFrom => "QueensJam";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = Foraging.Angelica.Angelica.PrefabName, Amount = 2, Recover = false },
        new() { Item = "Honey", Amount = 3, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 20f;

    /// <inheritdoc/>
    protected override float Stamina => 38f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.6f, 0.9f, 0.5f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 3000f;

    /// <inheritdoc/>
    protected override float Regen => 2f;

    /// <inheritdoc/>
    protected override int MinStationLevel => 2;
}
