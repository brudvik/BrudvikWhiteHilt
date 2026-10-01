using BrudvikWhiteHilt.Items.Indestructible;
using Jotunn.Configs;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Armors.WhiteHiltUniform;

/// <summary>
/// The black White Hilt uniform tunic with gold trim and the logo on the chest, made from Hildir's tunic.
/// </summary>
public class WhiteHiltUniformTunic : WhiteHiltArmorBase
{
    /// <summary>
    /// Creates the uniform tunic.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public WhiteHiltUniformTunic(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltUniformTunic";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Uniform Tunic";

    /// <inheritdoc/>
    protected override string Description => "The indestructible black tunic of Dyrnwyn, trimmed with gold, with the White Hilt on the chest.";

    /// <inheritdoc/>
    protected override string CopyFrom => "ArmorTunic1";

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
            UniformLook.Recolor(item.ItemPrefab, item.ItemData, UniformLook.BlackGold(lightToGold: true));
            // Measured on the vanilla bodies in their rest pose, at the left breast.
            UniformLook.AddChestBadge(item.ItemPrefab,
                new UniformBadge.Placement(new Vector3(-0.1065f, -0.0485f, 0.1596f), new Vector3(-0.209f, -0.151f, 0.966f)),
                new UniformBadge.Placement(new Vector3(-0.1053f, -0.0321f, 0.1964f), new Vector3(-0.091f, -0.224f, 0.97f)));
            UniformLook.RenderIcon(item.ItemPrefab, item.ItemData);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: keeping the vanilla look: {ex.Message}");
        }
    }
}
