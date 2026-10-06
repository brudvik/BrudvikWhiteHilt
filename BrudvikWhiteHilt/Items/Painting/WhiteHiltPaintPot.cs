using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Painting;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Painting;

/// <summary>
/// A pot of paint mixed at the Paint Bench. Its colour is kept on the item; its durability is how many pieces it still
/// covers. Using it loads the White Hilt Paint Brush.
/// </summary>
public class WhiteHiltPaintPot : IWhiteHiltCustomItem
{
    /// <summary>Prefab name of the pot.</summary>
    public const string PrefabName = "WhiteHiltPaintPot";

    private const string FullName = "Paint Pot";
    private const string Description = "A pot of paint from the Paint Bench. Use it to load the White Hilt Paint Brush with its colour.";
    private const string ColorKey = "whitehilt_paint_color";
    private const float Size = 0.35f;

    private readonly ItemManager instance;

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.BlackForest;

    /// <inheritdoc/>
    public string Id => PrefabName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(Translations.ItemKey(PrefabName));

    /// <inheritdoc/>
    public string GatedPrefabName => PrefabName;

    /// <summary>
    /// Registers the English text.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public WhiteHiltPaintPot(ItemManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(PrefabName), FullName, Description);
    }

    /// <summary>
    /// True for a paint pot.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>True for a pot.</returns>
    public static bool IsPot(ItemDrop.ItemData item)
    {
        return item?.m_shared.m_name == Translations.Token(Translations.ItemKey(PrefabName));
    }

    /// <summary>
    /// The colour of a pot.
    /// </summary>
    /// <param name="item">The pot.</param>
    /// <param name="color">Its colour.</param>
    /// <returns>False if it has none.</returns>
    public static bool TryGetColor(ItemDrop.ItemData item, out Color32 color)
    {
        color = default;
        return item != null && item.m_customData.TryGetValue(ColorKey, out string hex) && PaintColor.TryParseHex(hex, out color);
    }

    /// <summary>
    /// Puts a full pot of a colour in the player's inventory.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="color">The colour.</param>
    /// <returns>The pot, or null if the inventory is full.</returns>
    public static ItemDrop.ItemData Create(Player player, Color32 color)
    {
        ItemDrop.ItemData pot = player.GetInventory().AddItem(PrefabName, 1, 1, 0, player.GetPlayerID(), player.GetPlayerName(), false);
        if (pot != null)
        {
            pot.m_customData[ColorKey] = PaintColor.ToHex(color);
            pot.m_durability = pot.GetMaxDurability();
        }

        return pot;
    }

    /// <summary>
    /// Adds the pot. It has no recipe: the Paint Bench mixes it.
    /// </summary>
    public void Add()
    {
        try
        {
            CustomItem pot = new(PrefabName, "Tar", new ItemConfig
            {
                Name = NameToken,
                Description = Translations.Token($"{Translations.ItemKey(PrefabName)}_description")
            });

            ItemDrop.ItemData.SharedData shared = pot.ItemDrop.m_itemData.m_shared;
            shared.m_itemType = ItemDrop.ItemData.ItemType.Material;
            shared.m_maxStackSize = 1;
            shared.m_weight = 1f;
            shared.m_value = 0;
            shared.m_teleportable = true;
            shared.m_useDurability = true;
            shared.m_maxDurability = PaintSettings.PotUses;
            shared.m_durabilityPerLevel = 0f;
            shared.m_durabilityDrain = 0f;
            shared.m_canBeReparied = false;
            shared.m_destroyBroken = false;
            pot.ItemDrop.m_itemData.m_durability = PaintSettings.PotUses;

            TryApplyVisual(pot);
            instance.AddItem(pot);
            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    // Replaces the cloned item's look with the paint bucket model and renders an icon from it.
    private static void TryApplyVisual(CustomItem pot)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            VisualHelper.ReplaceMesh(pot.ItemPrefab, ForagingAssets.LoadMesh("paintbucket"), ForagingAssets.LoadTexture("paintbucket_albedo"), size: Size);
            Sprite icon = VisualHelper.RenderIcon(pot.ItemPrefab);
            if (icon != null)
            {
                ItemDrop.ItemData.SharedData shared = pot.ItemDrop.m_itemData.m_shared;
                shared.m_icons = Enumerable.Repeat(icon, Math.Max(1, shared.m_icons.Length)).ToArray();
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: keeping the vanilla look until its model is in the bundle: {ex.Message}");
        }
    }
}
