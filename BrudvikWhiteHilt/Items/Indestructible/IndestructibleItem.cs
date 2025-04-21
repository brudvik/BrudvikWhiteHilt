using Jotunn.Configs;
using Jotunn.Entities;
using System;

namespace BrudvikWhiteHilt.Items.Indestructible;

/// <summary>
/// This class defines an indestructible item.
/// </summary>
public class IndestructibleItem : CustomItem
{

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

            ItemData.m_armor += 999;
            ItemData.m_durabilityDrain = 0;
            ItemData.m_maxDurability = 999;
            ItemData.m_useDurability = false;
            ItemData.m_useDurabilityDrain = 0;
            ItemData.m_weight = 0;

            var wearNTear = ItemDrop.GetComponent<WearNTear>();
            if (wearNTear != null)
            {
                wearNTear.m_health += 999;
                wearNTear.m_ashDamageImmune = true;
                wearNTear.m_ashDamageResist = true;
                wearNTear.m_damages.m_fire = HitData.DamageModifier.Immune;
                wearNTear.m_damages.m_blunt = HitData.DamageModifier.Immune;
                wearNTear.m_damages.m_slash = HitData.DamageModifier.Immune;
                wearNTear.m_damages.m_chop = HitData.DamageModifier.Immune;
                wearNTear.m_damages.m_pierce = HitData.DamageModifier.Immune;
                wearNTear.m_damages.m_spirit = HitData.DamageModifier.Immune;
                wearNTear.m_damages.m_frost = HitData.DamageModifier.Immune;
                wearNTear.m_damages.m_lightning = HitData.DamageModifier.Immune;
                wearNTear.m_damages.m_poison = HitData.DamageModifier.Immune;
                wearNTear.m_onDamaged += () => AlterDamage(wearNTear);
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{name} failed to configure as indistructible!");
            Jotunn.Logger.LogError(ex);
        }
    }

    /// <summary>
    /// This method alters the damage of the item.
    /// </summary>
    /// <param name="wearNTear"></param>
    private void AlterDamage(WearNTear wearNTear)
    {
        wearNTear.m_health += 999;
    }

}
