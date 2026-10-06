using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Backpack;

/// <summary>
/// Five accessory slots for belts, Wisplight, Wishbone and the like. The first is the game's own utility slot; the other
/// four give their item's effects, stats and weight as if it were worn, without showing it on the character, and also
/// take the White Hilt items registered with <see cref="AllowInExtraSlots"/>. Each kind of accessory can be worn once.
/// </summary>
public static class UtilitySlots
{
    /// <summary>Number of accessory slots.</summary>
    public const int Count = 5;

    private const float CheckSeconds = 1f;

    // The Swamp Key opens crypt doors from anywhere in the inventory, so it may be kept here out of the way.
    private static readonly HashSet<string> extraItems = new() { "$item_cryptkey" };
    private static readonly List<ItemDrop.ItemData> worn = new();
    private static readonly HashSet<StatusEffect> applied = new();
    private static Player wornBy;
    private static float[] modifierSums;
    private static float nextCheck;

    /// <summary>Eitr regeneration added by the extra accessories.</summary>
    public static float EitrRegen { get; private set; }

    /// <summary>Weight of the extra accessories, counted as worn equipment.</summary>
    public static float Weight { get; private set; }

    /// <summary>
    /// Lets an item that is not a vanilla accessory lie in the extra slots, e.g. the Home Stone.
    /// </summary>
    /// <param name="sharedName">The item's name token, e.g. <c>$item_whitehilthomestone</c>.</param>
    public static void AllowInExtraSlots(string sharedName)
    {
        extraItems.Add(sharedName);
    }

    /// <summary>
    /// Whether an item fits an accessory slot: the game's own slot takes only vanilla accessories, the extra slots also
    /// the registered White Hilt items.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="pos">The accessory slot.</param>
    /// <returns>True if it fits.</returns>
    public static bool Fits(ItemDrop.ItemData item, Vector2i pos)
    {
        return item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility
            || (pos.x > 0 && extraItems.Contains(item.m_shared.m_name));
    }

    /// <summary>
    /// True if the player wears an item with this name in one of the extra slots.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="sharedName">The item's name token.</param>
    /// <returns>True when worn there.</returns>
    public static bool IsWornExtra(Player player, string sharedName)
    {
        return player == wornBy && worn.Exists(item => item != null && item.m_shared.m_name == sharedName);
    }

    /// <summary>
    /// True for one of the four accessory slots added by the mod.
    /// </summary>
    /// <param name="pos">The grid position.</param>
    /// <returns>True for an extra slot.</returns>
    public static bool IsExtraSlot(Vector2i pos)
    {
        return pos.y == BackpackLayout.UtilityRow && pos.x >= 1 && pos.x < Count;
    }

    /// <summary>
    /// True if the item is worn in one of the extra slots by the given player.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="item">The item.</param>
    /// <returns>True when worn in an extra slot.</returns>
    public static bool IsWornExtra(Player player, ItemDrop.ItemData item)
    {
        return player == wornBy && worn.Contains(item);
    }

    /// <summary>
    /// The accessories in the extra slots of the local player.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The worn extra accessories, empty for other players.</returns>
    public static IReadOnlyList<ItemDrop.ItemData> Extras(Player player)
    {
        return player == wornBy ? worn : System.Array.Empty<ItemDrop.ItemData>();
    }

    /// <summary>
    /// True if another accessory slot already holds the same kind of item. The target position itself is not counted,
    /// since what lies there is swapped out.
    /// </summary>
    /// <param name="inventory">The player's inventory.</param>
    /// <param name="item">The item to put in.</param>
    /// <param name="pos">The target position.</param>
    /// <returns>True when the same kind is already worn.</returns>
    public static bool WornElsewhere(Inventory inventory, ItemDrop.ItemData item, Vector2i pos)
    {
        for (int x = 0; x < Count; x++)
        {
            if (x == pos.x && pos.y == BackpackLayout.UtilityRow)
            {
                continue;
            }

            ItemDrop.ItemData other = inventory.GetItemAt(x, BackpackLayout.UtilityRow);
            if (other != null && other != item && other.m_shared.m_name == item.m_shared.m_name)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// True if the item lies in one of the accessory slots, where crafting, building and fuelling may not take it.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>True when kept in an accessory slot.</returns>
    public static bool IsKept(ItemDrop.ItemData item)
    {
        return item.m_gridPos.y == BackpackLayout.UtilityRow && item.m_gridPos.x >= 0 && item.m_gridPos.x < Count;
    }

    /// <summary>
    /// How many of the matching items lie in the accessory slots, by the same rules as <see cref="Inventory.CountItems"/>.
    /// </summary>
    /// <param name="inventory">The player's inventory.</param>
    /// <param name="name">Shared item name.</param>
    /// <param name="quality">Quality, or -1 for any.</param>
    /// <param name="matchWorldLevel">Vanilla's world level check.</param>
    /// <returns>The number kept there.</returns>
    public static int CountKept(Inventory inventory, string name, int quality, bool matchWorldLevel)
    {
        int count = 0;
        for (int x = 0; x < Count; x++)
        {
            ItemDrop.ItemData item = inventory.GetItemAt(x, BackpackLayout.UtilityRow);
            if (item != null && Matches(item, name, quality, matchWorldLevel))
            {
                count += item.m_stack;
            }
        }

        return count;
    }

    /// <summary>
    /// Takes items by name like <see cref="Inventory.RemoveItem(string, int, int, bool)"/>, but never from the
    /// accessory slots.
    /// </summary>
    /// <param name="inventory">The player's inventory.</param>
    /// <param name="name">Shared item name.</param>
    /// <param name="amount">How many.</param>
    /// <param name="quality">Quality, or -1 for any.</param>
    /// <param name="worldLevelBased">Vanilla's world level check.</param>
    public static void RemoveUnkept(Inventory inventory, string name, int amount, int quality, bool worldLevelBased)
    {
        foreach (ItemDrop.ItemData item in inventory.m_inventory)
        {
            if (amount <= 0)
            {
                break;
            }

            if (!IsKept(item) && Matches(item, name, quality, worldLevelBased))
            {
                int taken = Mathf.Min(item.m_stack, amount);
                item.m_stack -= taken;
                amount -= taken;
            }
        }

        inventory.m_inventory.RemoveAll(item => item.m_stack <= 0);
        inventory.Changed();
    }

    /// <summary>
    /// Handles equipping an accessory from the grid: the game's slot when it is free, else a free extra slot, else the
    /// game swaps out the one in its slot. Refused when the same kind is already worn.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="item">The accessory.</param>
    /// <returns>True when the game should equip it itself.</returns>
    public static bool BeforeEquip(Player player, ItemDrop.ItemData item)
    {
        Inventory inventory = player.m_inventory;
        if (item.m_gridPos.y == BackpackLayout.UtilityRow || inventory.GetItemAt(0, BackpackLayout.UtilityRow) == null)
        {
            return true;
        }

        if (WornElsewhere(inventory, item, new Vector2i(-1, -1)))
        {
            player.Message(MessageHud.MessageType.Center, "$whitehilt_backpack_duplicate");
            return false;
        }

        for (int x = 1; x < Count; x++)
        {
            if (inventory.GetItemAt(x, BackpackLayout.UtilityRow) == null)
            {
                item.m_gridPos = new Vector2i(x, BackpackLayout.UtilityRow);
                inventory.Changed();
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Keeps the effects of the extra accessories on the player. Called every frame for the local player.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Tick(Player player)
    {
        bool changed = player != wornBy;
        if (changed)
        {
            wornBy = player;
            worn.Clear();
            applied.Clear();
        }

        if (BackpackLayout.IsLocalInventory(player.m_inventory) && !player.IsDead())
        {
            for (int x = 1; x < Count; x++)
            {
                ItemDrop.ItemData item = player.m_inventory.GetItemAt(x, BackpackLayout.UtilityRow);
                int index = x - 1;
                ItemDrop.ItemData current = index < worn.Count ? worn[index] : null;
                if (item != current)
                {
                    changed = true;
                }
            }
        }
        else
        {
            changed |= worn.Exists(item => item != null);
        }

        if (changed)
        {
            Collect(player);
            player.UpdateModifiers();
        }

        if (changed || Time.time >= nextCheck)
        {
            nextCheck = Time.time + CheckSeconds;
            ApplyStatusEffects(player);
        }
    }

    /// <summary>
    /// Adds the extra accessories' equipment modifiers to the player's.
    /// </summary>
    /// <param name="values">The player's summed equipment modifiers.</param>
    public static void AddModifiers(float[] values)
    {
        if (modifierSums == null || values == null)
        {
            return;
        }

        for (int i = 0; i < values.Length && i < modifierSums.Length; i++)
        {
            values[i] += modifierSums[i];
        }
    }

    // Collects the accessories worn in the utility row and sums their eitr regeneration, weight and the equipment
    // modifiers the game reads by reflection.
    private static void Collect(Player player)
    {
        worn.Clear();
        bool active = BackpackLayout.IsLocalInventory(player.m_inventory) && !player.IsDead();
        for (int x = 1; x < Count; x++)
        {
            worn.Add(active ? player.m_inventory.GetItemAt(x, BackpackLayout.UtilityRow) : null);
        }

        EitrRegen = 0f;
        Weight = 0f;
        System.Reflection.FieldInfo[] fields = Player.s_equipmentModifierSourceFields;
        modifierSums = fields != null ? new float[fields.Length] : null;
        foreach (ItemDrop.ItemData item in worn)
        {
            if (item == null)
            {
                continue;
            }

            EitrRegen += item.m_shared.m_eitrRegenModifier;
            Weight += item.m_shared.m_weight;
            for (int i = 0; modifierSums != null && i < fields.Length; i++)
            {
                if (fields[i].GetValue(item.m_shared) is float value)
                {
                    modifierSums[i] += value;
                }
            }
        }
    }

    // Keeps the status effects of the worn accessories on the player, removing those of accessories taken off (unless
    // worn gear gives the same).
    private static void ApplyStatusEffects(Player player)
    {
        HashSet<StatusEffect> desired = new();
        foreach (ItemDrop.ItemData item in worn)
        {
            if (item != null && item.m_shared.m_equipStatusEffect != null)
            {
                desired.Add(item.m_shared.m_equipStatusEffect);
            }
        }

        foreach (StatusEffect effect in applied)
        {
            if (!desired.Contains(effect) && !player.m_equipmentStatusEffects.Contains(effect))
            {
                player.m_seman.RemoveStatusEffect(effect.NameHash());
            }
        }

        foreach (StatusEffect effect in desired)
        {
            if (!player.m_seman.HaveStatusEffect(effect.NameHash()))
            {
                player.m_seman.AddStatusEffect(effect);
            }
        }

        applied.Clear();
        applied.UnionWith(desired);
    }

    private static bool Matches(ItemDrop.ItemData item, string name, int quality, bool matchWorldLevel)
    {
        return item.m_shared.m_name == name && (quality < 0 || item.m_quality == quality)
            && (!matchWorldLevel || item.m_worldLevel >= Game.m_worldLevel);
    }
}
