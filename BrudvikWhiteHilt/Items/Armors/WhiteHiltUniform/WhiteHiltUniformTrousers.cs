using BrudvikWhiteHilt.Items.Indestructible;
using Jotunn.Configs;
using Jotunn.Managers;
using System;

namespace BrudvikWhiteHilt.Items.Armors.WhiteHiltUniform;

/// <summary>
/// The black White Hilt uniform trousers, made from the troll leather trousers.
/// </summary>
public class WhiteHiltUniformTrousers : WhiteHiltArmorBase
{
    /// <summary>
    /// Creates the uniform trousers.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public WhiteHiltUniformTrousers(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltUniformTrousers";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Uniform Trousers";

    /// <inheritdoc/>
    protected override string Description => "The indestructible black trousers of Dyrnwyn.";

    /// <inheritdoc/>
    protected override string CopyFrom => "ArmorTrollLeatherLegs";

    /// <inheritdoc/>
    protected override string StatsFrom => "ArmorIronLegs";

    /// <inheritdoc/>
    protected override string CraftingStation => CraftingStations.Workbench;

    /// <inheritdoc/>
    protected override int MinStationLevel => 2;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "DeerHide", Amount = 4, Recover = false },
        new() { Item = "LeatherScraps", Amount = 4, Recover = false },
        new() { Item = "Coal", Amount = 2, Recover = false }
    };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override void ApplyVisual(IndestructibleItem item)
    {
        try
        {
            UniformLook.Recolor(item.ItemPrefab, item.ItemData, UniformLook.BlackGold(lightToGold: false, blackLevel: 1.15f));
            UniformLook.RenderIcon(item.ItemPrefab, item.ItemData);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: keeping the vanilla look: {ex.Message}");
        }
    }
}
