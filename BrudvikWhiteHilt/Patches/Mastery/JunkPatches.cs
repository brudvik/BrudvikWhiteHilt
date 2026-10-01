using BrudvikWhiteHilt.Mastery;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Mastery;

/// <summary>
/// The junk filter: Shift+E marks an item on the ground as junk, which auto pickup then leaves lying.
/// </summary>
[HarmonyPatch]
public static class JunkPatches
{
    /// <summary>
    /// Shift+E on an item on the ground marks or unmarks it as junk instead of picking it up.
    /// </summary>
    /// <param name="__instance">The item drop.</param>
    /// <param name="character">Who interacts.</param>
    /// <param name="repeat">True while held.</param>
    /// <param name="alt">True with the alternative key.</param>
    /// <param name="__result">Whether something happened.</param>
    /// <returns>False when handled here.</returns>
    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Interact))]
    [HarmonyPrefix]
    private static bool ToggleJunk(ItemDrop __instance, Humanoid character, bool repeat, bool alt, ref bool __result)
    {
        if (!alt || repeat || __instance.IsPiece() || character is not Player player || player != Player.m_localPlayer)
        {
            return true;
        }

        JunkFilter.Toggle(player, __instance.m_itemData.m_shared.m_name);
        __result = true;
        return false;
    }

    /// <summary>
    /// Shows the junk key on items on the ground.
    /// </summary>
    /// <param name="__instance">The item drop.</param>
    /// <param name="__result">Hover text.</param>
    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.GetHoverText))]
    [HarmonyPostfix]
    private static void JunkHint(ItemDrop __instance, ref string __result)
    {
        if (!string.IsNullOrEmpty(__result) && Player.m_localPlayer != null && !__instance.IsPiece())
        {
            __result += JunkFilter.Hint(Player.m_localPlayer, __instance.m_itemData.m_shared.m_name);
        }
    }

    /// <summary>
    /// Shows in the tooltip that an item is junk.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="__result">The tooltip.</param>
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool))]
    [HarmonyPostfix]
    private static void JunkTooltip(ItemDrop.ItemData item, ref string __result)
    {
        if (item != null && JunkFilter.IsJunk(Player.m_localPlayer, item.m_shared.m_name))
        {
            __result += "\n<color=#a0a0a0>$whitehilt_junk</color>";
        }
    }

    /// <summary>
    /// Keeps auto pickup away from junk.
    /// </summary>
    /// <param name="__instance">The player.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.AutoPickup))]
    [HarmonyPrefix]
    private static void SilenceJunk(Player __instance)
    {
        if (__instance == Player.m_localPlayer)
        {
            JunkFilter.SilenceAround(__instance);
        }
    }

    /// <summary>
    /// Lets junk be picked up by others again.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.AutoPickup))]
    [HarmonyFinalizer]
    private static void RestoreJunk()
    {
        JunkFilter.Restore();
    }
}
