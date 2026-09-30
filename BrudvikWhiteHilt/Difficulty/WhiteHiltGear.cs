using BrudvikWhiteHilt.Items;
using BrudvikWhiteHilt.Items.Armors;
using BrudvikWhiteHilt.Items.Weapons;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Difficulty;

/// <summary>
/// Tells from a player's equipment, as the server sees it in the player's ZDO, whether they wear White Hilt gear.
/// </summary>
public static class WhiteHiltGear
{
    private static readonly HashSet<int> gear = new();

    /// <summary>
    /// Remembers a White Hilt weapon or armour piece; tools do not count.
    /// </summary>
    /// <param name="item">The item.</param>
    public static void Register(IWhiteHiltCustomItem item)
    {
        if (item is WhiteHiltWeaponBase || item is WhiteHiltArmorBase)
        {
            gear.Add(item.GatedPrefabName.GetStableHashCode());
        }
    }

    /// <summary>
    /// Whether a player wears a White Hilt weapon or at least two White Hilt armour pieces. A shield counts as armour.
    /// </summary>
    /// <param name="zdo">The player's ZDO.</param>
    /// <returns>True if the player counts as wearing White Hilt gear.</returns>
    public static bool IsWearing(ZDO zdo)
    {
        int pieces = 0;
        foreach (int hash in new[] { zdo.GetInt(ZDOVars.s_rightItem), zdo.GetInt(ZDOVars.s_leftItem) })
        {
            if (!gear.Contains(hash))
            {
                continue;
            }

            if (!IsShield(hash))
            {
                return true;
            }

            pieces++;
        }

        foreach (int hash in new[] { zdo.GetInt(ZDOVars.s_chestItem), zdo.GetInt(ZDOVars.s_legItem), zdo.GetInt(ZDOVars.s_helmetItem), zdo.GetInt(ZDOVars.s_shoulderItem) })
        {
            if (gear.Contains(hash))
            {
                pieces++;
            }
        }

        return pieces >= 2;
    }

    private static bool IsShield(int hash)
    {
        GameObject prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(hash) : null;
        ItemDrop item = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
        return item != null && item.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield;
    }
}
