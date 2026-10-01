using BrudvikWhiteHilt.Items.Indestructible;
using Jotunn.Configs;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Armors.WhiteHiltUniform;

/// <summary>
/// The black White Hilt officer's jerkin with gold knotwork and the logo on the chest.
/// </summary>
public class WhiteHiltOfficerJerkin : WhiteHiltArmorBase
{
    /// <summary>
    /// Creates the officer's jerkin.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public WhiteHiltOfficerJerkin(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltOfficerJerkin";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Officer's Jerkin";

    /// <inheritdoc/>
    protected override string Description => "The indestructible black jerkin of Dyrnwyn's officers, with gold knotwork and the White Hilt on the chest.";

    // Not obtainable in vanilla yet (no recipe), so the look may change with a game update.
    /// <inheritdoc/>
    protected override string CopyFrom => "ArmorDeepNorthMediumChest";

    /// <inheritdoc/>
    protected override string StatsFrom => "ArmorIronChest";

    /// <inheritdoc/>
    protected override string CraftingStation => CraftingStations.Workbench;

    /// <inheritdoc/>
    protected override int MinStationLevel => 2;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "DeerHide", Amount = 6, Recover = false },
        new() { Item = "LeatherScraps", Amount = 4, Recover = false },
        new() { Item = "Coal", Amount = 4, Recover = false },
        new() { Item = "Coins", Amount = 20, Recover = false }
    };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override void ApplyVisual(IndestructibleItem item)
    {
        try
        {
            UniformLook.Recolor(item.ItemPrefab, item.ItemData, UniformLook.BlackGold(lightToGold: false, goldMinValue: 0.45f));
            // The jerkin covers both bodies, so the badge sits on it the same way on both.
            UniformBadge.Placement onJerkin = new(new Vector3(-0.1055f, -0.0758f, 0.2376f), new Vector3(-0.158f, -0.022f, 0.987f));
            UniformLook.AddChestBadge(item.ItemPrefab, onJerkin, onJerkin);
            UniformLook.RenderIcon(item.ItemPrefab, item.ItemData);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: keeping the vanilla look: {ex.Message}");
        }
    }
}
