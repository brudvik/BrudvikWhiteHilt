using BepInEx.Configuration;
using UnityEngine;

namespace BrudvikWhiteHilt.Backpack;

/// <summary>
/// Three slots for food in the equipment panel. Each has a key (Alt+1, Alt+2, Alt+3) that eats from it.
/// </summary>
public static class FoodSlots
{
    /// <summary>Column of the first food slot in the equipment row.</summary>
    public const int FirstColumn = 5;

    /// <summary>Number of food slots.</summary>
    public const int Count = 3;

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
    /// The key of a food slot.
    /// </summary>
    /// <param name="index">The slot, 0 to 2.</param>
    /// <returns>The key config.</returns>
    public static ConfigEntry<KeyboardShortcut> Key(int index)
    {
        return index switch
        {
            0 => BackpackSettings.KeyFood1,
            1 => BackpackSettings.KeyFood2,
            _ => BackpackSettings.KeyFood3
        };
    }

    /// <summary>
    /// Eats from a food slot when its key is pressed. Called every frame for the local player.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Tick(Player player)
    {
        if (player.IsDead() || player.IsTeleporting() || BackpackInput.Typing() || !BackpackLayout.IsLocalInventory(player.m_inventory))
        {
            return;
        }

        for (int i = 0; i < Count; i++)
        {
            if (!BackpackInput.Pressed(Key(i)))
            {
                continue;
            }

            ItemDrop.ItemData food = player.m_inventory.GetItemAt(FirstColumn + i, BackpackLayout.GearRow);
            if (food == null)
            {
                player.Message(MessageHud.MessageType.TopLeft, "$whitehilt_backpack_food_empty");
            }
            else
            {
                player.UseItem(null, food, true);
            }
        }
    }

    /// <summary>
    /// True when a food key uses the same number as a hotbar slot and its modifier keys are held, so the hotbar stays put.
    /// </summary>
    /// <param name="hotbarIndex">The hotbar slot, 1 to 8.</param>
    /// <returns>True to skip the hotbar.</returns>
    public static bool BlocksHotbar(int hotbarIndex)
    {
        for (int i = 0; i < Count; i++)
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
