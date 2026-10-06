using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.CloudberryPorridge;

/// <summary>
/// A stamina-leaning Plains barley porridge with cloudberries and candied angelica.
/// </summary>
public class CloudberryPorridge : WhiteHiltFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public CloudberryPorridge(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltCloudberryPorridge";

    /// <inheritdoc/>
    protected override string FullName => "Cloudberry Porridge";

    /// <inheritdoc/>
    protected override string Description => "A slow barley porridge from the soapstone cauldron, topped with cloudberries, honey and a sprig of angelica.";

    /// <inheritdoc/>
    protected override string CopyFrom => "SerpentStew";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "BarleyFlour", Amount = 2, Recover = false },
        new() { Item = "Cloudberry", Amount = 2, Recover = false },
        new() { Item = Foraging.Angelica.Angelica.PrefabName, Amount = 1, Recover = false },
        new() { Item = "Honey", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 38f;

    /// <inheritdoc/>
    protected override float Stamina => 74f;

    /// <inheritdoc/>
    protected override Color Tint => new(1f, 0.82f, 0.55f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 2700f;

    /// <inheritdoc/>
    protected override float Regen => 4f;

    /// <inheritdoc/>
    protected override int MinStationLevel => 4;
}
