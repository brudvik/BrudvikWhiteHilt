using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using System;

namespace BrudvikWhiteHilt.Items.Indestructible;

/// <summary>
/// This class defines an indestructible item.
/// </summary>
public class IndestructibleItem : CustomItem, IWhiteHiltConfigurable
{
    private const string Section = "Gear.Indestructible";
    private const float DefaultWeight = 0f;

    private static ConfigEntry<float> weight;

    private ItemDrop.ItemData.SharedData _ItemData = null;

    /// <summary>
    /// Gets or sets the shared data for the item.
    /// </summary>
    public ItemDrop.ItemData.SharedData ItemData
    {
        get { return _ItemData; }
        set { _ItemData = value; }
    }

    /// <summary>
    /// Constructor for the IndestructibleItem class.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="basePrefabName"></param>
    /// <param name="itemConfig"></param>
    public IndestructibleItem(string name, string basePrefabName, ItemConfig itemConfig) : base(name, basePrefabName, itemConfig)
    {
        try
        {
            ItemData = ItemDrop.m_itemData.m_shared;

            ApplyConfig();
            ItemData.m_durabilityDrain = 0;
            ItemData.m_maxDurability = 999;
            ItemData.m_useDurability = false;
            ItemData.m_useDurabilityDrain = 0;

            var wearNTear = ItemDrop.GetComponent<WearNTear>();
            WearNTearHelper.MakeIndestructible(wearNTear);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{name} failed to configure as indestructible!");
            Jotunn.Logger.LogError(ex);
        }
    }

    /// <summary>
    /// Binds the config shared by all indestructible items, once. Call from the plugin's Awake.
    /// </summary>
    public static void BindConfig()
    {
        if (weight != null)
        {
            return;
        }

        weight = WhiteHiltConfig.BindAdminOnly(Section, "Weight", DefaultWeight,
            "Weight of every indestructible White Hilt item.", new AcceptableValueRange<float>(0f, 50f));
    }

    /// <summary>
    /// Applies the configured weight.
    /// </summary>
    public void ApplyConfig()
    {
        if (ItemData == null)
        {
            return;
        }

        ItemData.m_weight = weight?.Value ?? DefaultWeight;
    }
}
