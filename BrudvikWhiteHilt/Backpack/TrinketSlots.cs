using UnityEngine;

namespace BrudvikWhiteHilt.Backpack;

/// <summary>
/// Trinket slots, three by default (<see cref="BackpackSettings.TrinketSlots"/>). The first is the game's own trinket
/// slot; the others give their trinket's effects, stats and weight as if it were worn, without showing it on the
/// character. Each kind of trinket can be worn once. All of them share the one adrenaline bar: it holds what they hold
/// together, and when it is full every worn trinket's effect comes at once.
/// </summary>
public static class TrinketSlots
{
    /// <summary>Most trinket slots.</summary>
    public const int MaxCount = 4;

    /// <summary>Column of the first extra slot in the hand row; the extra slots lie beside the coins.</summary>
    public const int FirstExtraColumn = 5;

    private const float CheckSeconds = 1f;

    private static readonly ExtraWear wear = new();
    private static Player wornBy;
    private static float nextCheck;

    /// <summary>Number of trinket slots, the game's own one included.</summary>
    public static int Count => Mathf.Clamp(BackpackSettings.TrinketSlots?.Value ?? 1, 1, MaxCount);

    /// <summary>Eitr regeneration added by the extra trinkets.</summary>
    public static float EitrRegen => wear.EitrRegen;

    /// <summary>Weight of the extra trinkets, counted as worn equipment.</summary>
    public static float Weight => wear.Weight;

    /// <summary>
    /// Every position a trinket slot can have, the game's own first, whether in use or not.
    /// </summary>
    /// <returns>The positions.</returns>
    public static Vector2i[] AllPositions()
    {
        Vector2i[] positions = new Vector2i[MaxCount];
        positions[0] = new Vector2i(4, BackpackLayout.GearRow);
        for (int i = 1; i < MaxCount; i++)
        {
            positions[i] = new Vector2i(FirstExtraColumn + i - 1, BackpackLayout.HandRow);
        }

        return positions;
    }

    /// <summary>
    /// True for one of the trinket slots added by the mod that is in use.
    /// </summary>
    /// <param name="pos">The grid position.</param>
    /// <returns>True for an extra slot.</returns>
    public static bool IsExtraSlot(Vector2i pos)
    {
        return pos.y == BackpackLayout.HandRow && pos.x >= FirstExtraColumn && pos.x < FirstExtraColumn + Count - 1;
    }

    /// <summary>
    /// Whether a trinket may lie at a trinket slot: the game's slot or an extra one, and not a second of its kind.
    /// </summary>
    /// <param name="inventory">The player's inventory.</param>
    /// <param name="item">The item.</param>
    /// <param name="pos">The trinket slot.</param>
    /// <returns>True if it may lie there.</returns>
    public static bool CanHold(Inventory inventory, ItemDrop.ItemData item, Vector2i pos)
    {
        return item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trinket && !WornElsewhere(inventory, item, pos);
    }

    /// <summary>
    /// True if another trinket slot already holds the same kind of trinket. The target position itself is not counted,
    /// since what lies there is swapped out.
    /// </summary>
    /// <param name="inventory">The player's inventory.</param>
    /// <param name="item">The trinket to put in.</param>
    /// <param name="pos">The target position.</param>
    /// <returns>True when the same kind is already worn.</returns>
    public static bool WornElsewhere(Inventory inventory, ItemDrop.ItemData item, Vector2i pos)
    {
        Vector2i[] positions = AllPositions();
        for (int i = 0; i < Count; i++)
        {
            if (positions[i] == pos)
            {
                continue;
            }

            ItemDrop.ItemData other = inventory.GetItemAt(positions[i].x, positions[i].y);
            if (other != null && other != item && other.m_shared.m_name == item.m_shared.m_name)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// True if the player wears a trinket with this name in one of the extra slots.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="sharedName">The trinket's name token.</param>
    /// <returns>True when worn there.</returns>
    public static bool IsWornExtra(Player player, string sharedName)
    {
        return player == wornBy && wear.Worn.Exists(item => item != null && item.m_shared.m_name == sharedName);
    }

    /// <summary>
    /// True if the item is worn in one of the extra slots by the given player.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="item">The item.</param>
    /// <returns>True when worn in an extra slot.</returns>
    public static bool IsWornExtra(Player player, ItemDrop.ItemData item)
    {
        return player == wornBy && wear.Worn.Contains(item);
    }

    /// <summary>
    /// Handles equipping a trinket from the grid: the game's slot when it is free, else a free extra slot, else the game
    /// swaps out the one in its slot. Refused when the same kind is already worn.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="item">The trinket.</param>
    /// <returns>True when the game should equip it itself.</returns>
    public static bool BeforeEquip(Player player, ItemDrop.ItemData item)
    {
        Inventory inventory = player.m_inventory;
        if (IsExtraSlot(item.m_gridPos))
        {
            return false;
        }

        // In the game's slot, or that slot free: the game puts it on there.
        if (item.m_gridPos == new Vector2i(4, BackpackLayout.GearRow) || inventory.GetItemAt(4, BackpackLayout.GearRow) == null)
        {
            return true;
        }

        if (WornElsewhere(inventory, item, new Vector2i(-1, -1)))
        {
            player.Message(MessageHud.MessageType.Center, "$whitehilt_backpack_duplicate");
            return false;
        }

        Vector2i[] positions = AllPositions();
        for (int i = 1; i < Count; i++)
        {
            if (inventory.GetItemAt(positions[i].x, positions[i].y) == null)
            {
                item.m_gridPos = positions[i];
                inventory.Changed();
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Keeps the effects of the extra trinkets on the player. Called every frame for the local player.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Tick(Player player)
    {
        bool changed = player != wornBy;
        if (changed)
        {
            wornBy = player;
            wear.Reset();
        }

        bool active = BackpackLayout.IsLocalInventory(player.m_inventory) && !player.IsDead();
        Vector2i[] positions = AllPositions();
        for (int i = 1; i < MaxCount; i++)
        {
            ItemDrop.ItemData item = active && i < Count ? player.m_inventory.GetItemAt(positions[i].x, positions[i].y) : null;
            ItemDrop.ItemData current = i - 1 < wear.Worn.Count ? wear.Worn[i - 1] : null;
            changed |= item != current;
        }

        if (changed)
        {
            wear.Worn.Clear();
            for (int i = 1; i < MaxCount; i++)
            {
                wear.Worn.Add(active && i < Count ? player.m_inventory.GetItemAt(positions[i].x, positions[i].y) : null);
            }

            wear.Collect();
            player.UpdateModifiers();
        }

        if (changed || Time.time >= nextCheck)
        {
            nextCheck = Time.time + CheckSeconds;
            wear.ApplyStatusEffects(player);
        }
    }

    /// <summary>
    /// Adds the extra trinkets' equipment modifiers to the player's, among them how much adrenaline they hold.
    /// </summary>
    /// <param name="values">The player's summed equipment modifiers.</param>
    public static void AddModifiers(float[] values)
    {
        wear.AddModifiers(values);
    }

    /// <summary>
    /// After adrenaline was added: when the shared bar is full, the extra trinkets' effects come too. When the game's own
    /// trinket filled it, the game has already let its effect come and emptied the bar; with only extra trinkets worn
    /// the game leaves it full, so it is emptied here.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="before">Adrenaline before it was added.</param>
    /// <param name="added">Adrenaline added; nothing happens for less than none.</param>
    public static void AfterAddAdrenaline(Player player, float before, float added)
    {
        if (player != wornBy || added <= 0f)
        {
            return;
        }

        float max = player.GetMaxAdrenaline();
        bool popped = player.m_adrenaline < before;
        bool full = max > 0f && player.m_adrenaline >= max;
        if (!popped && !full)
        {
            return;
        }

        bool any = false;
        foreach (ItemDrop.ItemData item in wear.Worn)
        {
            StatusEffect effect = item?.m_shared.m_fullAdrenalineSE;
            if (effect == null)
            {
                continue;
            }

            any = true;
            StatusEffect running = player.m_seman.GetStatusEffect(effect.NameHash());
            if (running != null)
            {
                running.ResetTime();
            }
            else
            {
                player.m_seman.AddStatusEffect(effect, resetTime: false);
            }
        }

        if (any && !popped)
        {
            player.m_adrenaline = 0f;
            player.m_adrenalinePopEffects.Create(player.transform.position, Quaternion.identity);
        }
    }
}
