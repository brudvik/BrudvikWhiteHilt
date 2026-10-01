using BrudvikWhiteHilt.Items.Indestructible;
using Jotunn.Configs;
using Jotunn.Managers;
using System;

namespace BrudvikWhiteHilt.Items.Armors.WhiteHiltUniform;

/// <summary>
/// The black White Hilt officer's breeches with gold trim.
/// </summary>
public class WhiteHiltOfficerBreeches : WhiteHiltArmorBase
{
    /// <summary>
    /// Creates the officer's breeches.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public WhiteHiltOfficerBreeches(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltOfficerBreeches";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Officer's Breeches";

    /// <inheritdoc/>
    protected override string Description => "The indestructible black breeches of Dyrnwyn's officers.";

    // Not obtainable in vanilla yet (no recipe), so the look may change with a game update.
    /// <inheritdoc/>
    protected override string CopyFrom => "ArmorDeepNorthMediumlegs";

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
            UniformLook.Recolor(item.ItemPrefab, item.ItemData, UniformLook.BlackGold(lightToGold: false, goldMinValue: 0.45f));
            UniformLook.RenderIcon(item.ItemPrefab, item.ItemData);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: keeping the vanilla look: {ex.Message}");
        }
    }
}
