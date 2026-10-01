using UnityEngine;

namespace BrudvikWhiteHilt.Backpack;

/// <summary>
/// A slot for a shield and one for ammo under the accessories, and three for coins. The shield in its slot is taken up
/// with a one-handed weapon and put away with it; the ammo in its slot is used before any other of its kind; new coins
/// go into an empty coin slot before the grid.
/// </summary>
public static class HandSlots
{
    /// <summary>Number of shield and ammo slots.</summary>
    public const int Count = 2;

    /// <summary>Column of the first coin slot.</summary>
    public const int CoinColumn = Count;

    /// <summary>Number of coin slots.</summary>
    public const int CoinCount = 3;

    private const string CoinsName = "$item_coins";

    /// <summary>Where the shield slot lies in the inventory.</summary>
    public static readonly Vector2i ShieldSlot = new(0, BackpackLayout.HandRow);

    /// <summary>Where the ammo slot lies in the inventory.</summary>
    public static readonly Vector2i AmmoSlot = new(1, BackpackLayout.HandRow);

    private static bool checkShield;
    private static ItemDrop.ItemData lastAmmo;

    /// <summary>
    /// True for shields.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>True for a shield.</returns>
    public static bool IsShield(ItemDrop.ItemData item)
    {
        return item?.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield;
    }

    /// <summary>
    /// True for arrows, bolts and other ammo.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>True for ammo.</returns>
    public static bool IsAmmo(ItemDrop.ItemData item)
    {
        ItemDrop.ItemData.ItemType? type = item?.m_shared.m_itemType;
        return type == ItemDrop.ItemData.ItemType.Ammo || type == ItemDrop.ItemData.ItemType.AmmoNonEquipable;
    }

    /// <summary>
    /// True for coins.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>True for coins.</returns>
    public static bool IsCoins(ItemDrop.ItemData item)
    {
        return item?.m_shared.m_name == CoinsName;
    }

    /// <summary>
    /// The first empty coin slot.
    /// </summary>
    /// <param name="inventory">The player's inventory.</param>
    /// <returns>The position, or null when all hold coins.</returns>
    public static Vector2i? EmptyCoinSlot(Inventory inventory)
    {
        for (int x = CoinColumn; x < CoinColumn + CoinCount; x++)
        {
            if (inventory.GetItemAt(x, BackpackLayout.HandRow) == null)
            {
                return new Vector2i(x, BackpackLayout.HandRow);
            }
        }

        return null;
    }

    /// <summary>
    /// Number of empty coin slots.
    /// </summary>
    /// <param name="inventory">The player's inventory.</param>
    /// <returns>The count.</returns>
    public static int EmptyCoinSlots(Inventory inventory)
    {
        int empty = 0;
        for (int x = CoinColumn; x < CoinColumn + CoinCount; x++)
        {
            if (inventory.GetItemAt(x, BackpackLayout.HandRow) == null)
            {
                empty++;
            }
        }

        return empty;
    }

    /// <summary>
    /// True for a weapon a shield goes with: one-handed weapons, but not the fishing rod.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>True if a shield belongs with it.</returns>
    public static bool GoesWithShield(ItemDrop.ItemData item)
    {
        return item?.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon
            && item.m_shared.m_skillType != Skills.SkillType.Fishing;
    }

    /// <summary>
    /// The ammo in the slot, if it is what the given ammo type asks for.
    /// </summary>
    /// <param name="inventory">The player's inventory.</param>
    /// <param name="ammoType">The ammo type a weapon uses.</param>
    /// <returns>The ammo, or null.</returns>
    public static ItemDrop.ItemData SlotAmmo(Inventory inventory, string ammoType)
    {
        ItemDrop.ItemData ammo = inventory.GetItemAt(AmmoSlot.x, AmmoSlot.y);
        return !string.IsNullOrEmpty(ammoType) && IsAmmo(ammo) && ammo.m_shared.m_ammoType == ammoType ? ammo : null;
    }

    /// <summary>
    /// Takes up the shield with a one-handed weapon when the other hand is free, and the ammo with a bow or crossbow.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="item">The item just equipped.</param>
    public static void OnEquipped(Player player, ItemDrop.ItemData item)
    {
        if (player != Player.m_localPlayer || !BackpackLayout.IsLocalInventory(player.m_inventory) || item == null)
        {
            return;
        }

        if (GoesWithShield(item) && player.m_leftItem == null && BackpackSettings.ShieldFollowsWeapon.Value)
        {
            ItemDrop.ItemData shield = player.m_inventory.GetItemAt(ShieldSlot.x, ShieldSlot.y);
            if (IsShield(shield))
            {
                player.EquipItem(shield);
            }
        }

        if (!IsAmmo(item) && item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Consumable)
        {
            EquipSlotAmmo(player, item);
        }
    }

    /// <summary>
    /// Notes that a one-handed weapon was put away; the shield follows once no other took its place.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="item">The item just unequipped.</param>
    public static void OnUnequipped(Player player, ItemDrop.ItemData item)
    {
        if (player == Player.m_localPlayer && GoesWithShield(item) && BackpackSettings.ShieldFollowsWeapon.Value)
        {
            checkShield = true;
        }
    }

    /// <summary>
    /// Puts the shield away after its weapon, and takes up ammo put in the slot while holding a bow. Called every frame
    /// for the local player.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Tick(Player player)
    {
        if (player.IsDead() || player.IsTeleporting() || player.InAttack() || !BackpackLayout.IsLocalInventory(player.m_inventory))
        {
            return;
        }

        if (checkShield)
        {
            checkShield = false;
            ItemDrop.ItemData shield = player.m_leftItem;
            if (shield != null && shield.m_gridPos == ShieldSlot && !GoesWithShield(player.m_rightItem))
            {
                player.UnequipItem(shield);
            }
        }

        ItemDrop.ItemData ammo = player.m_inventory.GetItemAt(AmmoSlot.x, AmmoSlot.y);
        if (ammo != lastAmmo)
        {
            lastAmmo = ammo;
            ItemDrop.ItemData weapon = player.GetCurrentWeapon();
            if (ammo != null && weapon != null)
            {
                EquipSlotAmmo(player, weapon);
            }
        }
    }

    private static void EquipSlotAmmo(Player player, ItemDrop.ItemData weapon)
    {
        ItemDrop.ItemData ammo = SlotAmmo(player.m_inventory, weapon.m_shared.m_ammoType);
        if (ammo != null && ammo.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo && !player.IsItemEquiped(ammo))
        {
            player.EquipItem(ammo);
        }
    }
}
