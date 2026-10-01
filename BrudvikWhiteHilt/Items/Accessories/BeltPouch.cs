using BepInEx.Configuration;
using BrudvikWhiteHilt.Backpack;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Accessories;

/// <summary>
/// A leather pouch for the belt. Using it once gives the character one more inventory row for good; it is used up.
/// The row is a vanilla row (the "invrows" key), so it stays even without the mod.
/// </summary>
public class BeltPouch : IWhiteHiltCustomItem, IWhiteHiltConfigurable
{
    /// <summary>
    /// Prefab name of the item.
    /// </summary>
    public const string PrefabName = "WhiteHiltBeltPouch";

    private const string FullName = "Belt Pouch";
    private const string Description = "A sturdy pouch of troll hide riveted with iron, for the belt. Use it to get one more row in your inventory for good. Each character can only carry one.";
    private const string UsedKey = "whitehilt_beltpouch";
    private const int MaxVanillaRows = 9;
    private static readonly Color LeatherTint = new(0.55f, 0.42f, 0.3f);

    private readonly ItemManager instance;
    private readonly ConfigEntry<float> weight;
    private ItemDrop.ItemData.SharedData shared;

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Swamp;

    /// <inheritdoc/>
    public string Id => PrefabName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(Translations.ItemKey(PrefabName));

    /// <inheritdoc/>
    public string GatedPrefabName => PrefabName;

    /// <summary>
    /// Constructor for the BeltPouch class. Registers the English text.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public BeltPouch(ItemManager instance)
    {
        this.instance = instance;
        weight = WhiteHiltConfig.BindAdminOnly($"Gear.{PrefabName}", "Weight", 1f, "Weight of the Belt Pouch.", new AcceptableValueRange<float>(0f, 50f));
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(PrefabName), FullName, Description);
        Translations.AddEnglish("msg_whitehilt_beltpouch_used", "Your belt pouch gives you one more row");
        Translations.AddEnglish("msg_whitehilt_beltpouch_already", "You already carry a belt pouch");
        Translations.AddEnglish("msg_whitehilt_beltpouch_full", "There is no room for more rows");
    }

    /// <summary>
    /// Returns true if an inventory item is a Belt Pouch.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>True for the pouch.</returns>
    public static bool IsPouch(ItemDrop.ItemData item)
    {
        return item?.m_shared.m_name == Translations.Token(Translations.ItemKey(PrefabName));
    }

    /// <summary>
    /// Uses the pouch: one more inventory row for good, once per character.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="item">The pouch.</param>
    public static void Use(Player player, ItemDrop.ItemData item)
    {
        if (player.HaveUniqueKey(UsedKey))
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_beltpouch_already");
            return;
        }

        int rows = BackpackLayout.VanillaRows(player);
        if (rows >= MaxVanillaRows || BackpackLayout.VisibleRows(player) >= BackpackLayout.MaxVisibleRows)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_beltpouch_full");
            return;
        }

        if (!player.m_inventory.RemoveOneItem(item))
        {
            return;
        }

        player.AddUniqueKey(UsedKey);
        player.SetInventorySize(rows + 1);
        player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_beltpouch_used");
    }

    /// <summary>
    /// Adds the item and its recipe at the workbench.
    /// </summary>
    public void Add()
    {
        try
        {
            CustomItem pouch = new(PrefabName, "LeatherScraps", new ItemConfig
            {
                Name = NameToken,
                Description = Translations.Token($"{Translations.ItemKey(PrefabName)}_description"),
                CraftingStation = CraftingStations.Workbench,
                MinStationLevel = 2,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "TrollHide", Amount = 6 },
                    new() { Item = "LeatherScraps", Amount = 10 },
                    new() { Item = "Iron", Amount = 4 }
                }
            });

            ItemDrop.ItemData.SharedData shared = pouch.ItemDrop.m_itemData.m_shared;
            shared.m_itemType = ItemDrop.ItemData.ItemType.Material;
            shared.m_maxStackSize = 1;
            shared.m_value = 0;
            shared.m_teleportable = true;
            this.shared = shared;
            ApplyConfig();

            TryApplyVisual(pouch);
            instance.AddItem(pouch);
            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    /// <summary>
    /// Applies the configured weight.
    /// </summary>
    public void ApplyConfig()
    {
        if (shared != null)
        {
            shared.m_weight = weight.Value;
        }
    }

    private static void TryApplyVisual(CustomItem pouch)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            VisualHelper.Tint(pouch.ItemPrefab, LeatherTint);
            Sprite icon = VisualHelper.RenderIcon(pouch.ItemPrefab);
            if (icon != null)
            {
                pouch.ItemDrop.m_itemData.m_shared.m_icons = new[] { icon };
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: keeping the vanilla look: {ex.Message}");
        }
    }
}
