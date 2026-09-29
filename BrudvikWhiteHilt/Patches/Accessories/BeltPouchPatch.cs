using BrudvikWhiteHilt.Items.Accessories;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Accessories;

/// <summary>
/// Using the Belt Pouch from the hotbar or the inventory gives the character one more row.
/// </summary>
[HarmonyPatch]
public static class BeltPouchPatch
{
    /// <summary>
    /// Uses the pouch instead of asking what to use it on.
    /// </summary>
    /// <param name="__instance">The character.</param>
    /// <param name="inventory">The inventory the item is in, null for the character's own.</param>
    /// <param name="item">The item.</param>
    /// <returns>False when the pouch was used.</returns>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UseItem))]
    [HarmonyPrefix]
    public static bool UseItem(Humanoid __instance, Inventory inventory, ItemDrop.ItemData item)
    {
        if (__instance is not Player player || player != Player.m_localPlayer || !BeltPouch.IsPouch(item)
            || (inventory != null && inventory != player.m_inventory))
        {
            return true;
        }

        BeltPouch.Use(player, item);
        return false;
    }
}
