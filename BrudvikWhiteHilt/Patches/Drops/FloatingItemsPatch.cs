using BrudvikWhiteHilt.Drops;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Drops;

/// <summary>
/// Gives dropped items a float as they appear in the world.
/// </summary>
[HarmonyPatch]
public static class FloatingItemsPatch
{
    /// <summary>
    /// Makes the item float after it wakes.
    /// </summary>
    /// <param name="__instance">The item.</param>
    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Awake))]
    [HarmonyPostfix]
    public static void ItemDropAwake(ItemDrop __instance)
    {
        FloatingItems.MakeFloat(__instance);
    }
}
