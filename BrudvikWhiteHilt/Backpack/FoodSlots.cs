using BepInEx.Configuration;
using UnityEngine;

namespace BrudvikWhiteHilt.Backpack;

/// <summary>
/// Three slots for food and two for potions in the equipment panel. Each has a key (Alt+1 to Alt+5) that eats or
/// drinks from it.
/// </summary>
public static class FoodSlots
{
    /// <summary>Column of the first food slot in the equipment row.</summary>
    public const int FirstColumn = 5;

    /// <summary>Number of food slots.</summary>
    public const int Count = 3;

    /// <summary>Column of the first potion slot in the accessory row.</summary>
    public const int PotionColumn = UtilitySlots.Count;

    /// <summary>Number of potion slots.</summary>
    public const int PotionCount = 2;

    /// <summary>Number of food and potion slots together.</summary>
    public const int SlotCount = Count + PotionCount;

    /// <summary>
    /// Where a food or potion slot lies in the inventory.
    /// </summary>
    /// <param name="index">The slot: 0 to 2 food, 3 and 4 potions.</param>
    /// <returns>The grid position.</returns>
    public static Vector2i Position(int index)
    {
        return index < Count
            ? new Vector2i(FirstColumn + index, BackpackLayout.GearRow)
            : new Vector2i(PotionColumn + index - Count, BackpackLayout.UtilityRow);
    }

    /// <summary>
    /// The food or potion slot at a position.
    /// </summary>
    /// <param name="pos">The grid position.</param>
    /// <returns>The slot, or -1.</returns>
    public static int IndexOf(Vector2i pos)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (Position(i) == pos)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// The positions of all food and potion slots, food first.
    /// </summary>
    /// <returns>The grid positions.</returns>
    public static Vector2i[] Positions()
    {
        Vector2i[] positions = new Vector2i[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            positions[i] = Position(i);
        }

        return positions;
    }

    /// <summary>
    /// True for food: consumables that give health, stamina or eitr.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>True for food.</returns>
    public static bool IsFood(ItemDrop.ItemData item)
    {
        ItemDrop.ItemData.SharedData shared = item?.m_shared;
        return shared != null && shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable
            && (shared.m_food > 0f || shared.m_foodStamina > 0f || shared.m_foodEitr > 0f);
    }

    /// <summary>
    /// True for potions: consumables that are not food, such as meads.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>True for potions.</returns>
    public static bool IsPotion(ItemDrop.ItemData item)
    {
        return item?.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable && !IsFood(item);
    }

    /// <summary>
    /// The key of a food or potion slot.
    /// </summary>
    /// <param name="index">The slot: 0 to 2 food, 3 and 4 potions.</param>
    /// <returns>The key config.</returns>
    public static ConfigEntry<KeyboardShortcut> Key(int index)
    {
        return index switch
        {
            0 => BackpackSettings.KeyFood1,
            1 => BackpackSettings.KeyFood2,
            2 => BackpackSettings.KeyFood3,
            3 => BackpackSettings.KeyPotion1,
            _ => BackpackSettings.KeyPotion2
        };
    }

    /// <summary>
    /// Eats or drinks from a slot when its key is pressed. Called every frame for the local player.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Tick(Player player)
    {
        if (player.IsDead() || player.IsTeleporting() || BackpackInput.Typing() || !BackpackLayout.IsLocalInventory(player.m_inventory))
        {
            return;
        }

        for (int i = 0; i < SlotCount; i++)
        {
            if (!BackpackInput.Pressed(Key(i)))
            {
                continue;
            }

            Vector2i pos = Position(i);
            ItemDrop.ItemData item = player.m_inventory.GetItemAt(pos.x, pos.y);
            if (item == null)
            {
                player.Message(MessageHud.MessageType.TopLeft, i < Count ? "$whitehilt_backpack_food_empty" : "$whitehilt_backpack_potion_empty");
            }
            else
            {
                player.UseItem(null, item, true);
            }
        }
    }

    /// <summary>
    /// True when a food or potion key uses the same number as a hotbar slot and its modifier keys are held, so the
    /// hotbar stays put.
    /// </summary>
    /// <param name="hotbarIndex">The hotbar slot, 1 to 8.</param>
    /// <returns>True to skip the hotbar.</returns>
    public static bool BlocksHotbar(int hotbarIndex)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            KeyboardShortcut key = Key(i).Value;
            if (key.MainKey != KeyCode.Alpha0 + hotbarIndex)
            {
                continue;
            }

            bool held = false;
            foreach (KeyCode modifier in key.Modifiers)
            {
                held = Input.GetKey(modifier);
                if (!held)
                {
                    break;
                }
            }

            if (held)
            {
                return true;
            }
        }

        return false;
    }
}
