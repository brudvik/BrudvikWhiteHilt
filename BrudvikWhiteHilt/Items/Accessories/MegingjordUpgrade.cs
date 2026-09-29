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
/// Lets the vanilla Megingjord be upgraded at the forge to quality 4, each level adding carry weight on top of its own.
/// It is only upgraded, never crafted: the belt still comes from the trader.
/// </summary>
public static class MegingjordUpgrade
{
    private const string Section = "Megingjord";
    private const string PrefabName = "BeltStrength";
    private const int MaxQuality = 4;

    private static string beltName;

    /// <summary>Whether the Megingjord can be upgraded. Needs a restart.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>Carry weight added per quality level above 1.</summary>
    public static ConfigEntry<float> CarryWeightPerLevel { get; private set; }

    /// <summary>
    /// Binds the config and registers the recipe once the vanilla prefabs exist. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Upgrades", true,
            "The Megingjord can be upgraded at the forge up to quality 4. Needs a restart.");
        CarryWeightPerLevel = WhiteHiltConfig.BindAdminOnly(Section, "CarryWeightPerLevel", 50f,
            "Carry weight added per quality level above 1, on top of the belt's own +150.", new AcceptableValueRange<float>(0f, 500f));
        Translations.AddEnglish("whitehilt_megingjord_total", "Carry weight at this quality");

        if (Enabled.Value)
        {
            PrefabManager.OnVanillaPrefabsAvailable += AddRecipe;
            ItemManager.OnItemsRegistered += RaiseMaxQuality;
        }
    }

    /// <summary>
    /// True for a Megingjord.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>True for the belt.</returns>
    public static bool IsBelt(ItemDrop.ItemData item)
    {
        return beltName != null && item?.m_shared.m_name == beltName;
    }

    /// <summary>
    /// Carry weight the upgrades add to a belt, not counting its own.
    /// </summary>
    /// <param name="item">The belt.</param>
    /// <returns>The extra carry weight.</returns>
    public static float UpgradeBonus(ItemDrop.ItemData item)
    {
        return Mathf.Max(0, item.m_quality - 1) * CarryWeightPerLevel.Value;
    }

    /// <summary>
    /// Extra carry weight from the upgrades of the belts a player wears, in the game's slot or an extra accessory slot.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The extra carry weight.</returns>
    public static float WornBonus(Player player)
    {
        if (beltName == null)
        {
            return 0f;
        }

        float bonus = IsBelt(player.m_utilityItem) ? UpgradeBonus(player.m_utilityItem) : 0f;
        foreach (ItemDrop.ItemData item in UtilitySlots.Extras(player))
        {
            if (IsBelt(item))
            {
                bonus += UpgradeBonus(item);
            }
        }

        return bonus;
    }

    /// <summary>
    /// The belt's whole carry weight bonus at a quality: its own and the upgrades'.
    /// </summary>
    /// <param name="item">The belt.</param>
    /// <param name="quality">The quality shown.</param>
    /// <returns>The carry weight bonus.</returns>
    public static float TotalBonus(ItemDrop.ItemData item, int quality)
    {
        float own = item.m_shared.m_equipStatusEffect is SE_Stats stats ? stats.m_addMaxCarryWeight : 0f;
        return own + Mathf.Max(0, quality - 1) * CarryWeightPerLevel.Value;
    }

    private static void AddRecipe()
    {
        PrefabManager.OnVanillaPrefabsAvailable -= AddRecipe;
        try
        {
            CustomRecipe recipe = new(new RecipeConfig
            {
                Name = "Recipe_WhiteHiltBeltStrengthUpgrade",
                Item = PrefabName,
                CraftingStation = CraftingStations.Forge,
                MinStationLevel = 1,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "Iron", Amount = 0, AmountPerLevel = 8, Recover = false },
                    new() { Item = "Chain", Amount = 0, AmountPerLevel = 1, Recover = false },
                    new() { Item = "TrollHide", Amount = 0, AmountPerLevel = 3, Recover = false },
                    new() { Item = "SurtlingCore", Amount = 0, AmountPerLevel = 1, Recover = false }
                }
            });
            recipe.Recipe.m_noCraftOnlyUpgrade = true;
            ItemManager.Instance.AddRecipe(recipe);
            Jotunn.Logger.LogInfo("Megingjord upgrades added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError("Megingjord upgrades failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    private static void RaiseMaxQuality()
    {
        ItemDrop belt = ObjectDB.instance?.GetItemPrefab(PrefabName)?.GetComponent<ItemDrop>();
        if (belt == null)
        {
            Jotunn.Logger.LogWarning($"Megingjord upgrades: {PrefabName} not found.");
            return;
        }

        belt.m_itemData.m_shared.m_maxQuality = MaxQuality;
        beltName = belt.m_itemData.m_shared.m_name;
    }
}
