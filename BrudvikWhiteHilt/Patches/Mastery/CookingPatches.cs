using BrudvikWhiteHilt.Mastery;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Mastery;

/// <summary>
/// Cooking milestones on cooking stations: stars on what is taken off, faster cooking and no burning near a watchful cook.
/// These run on the machine that owns the station, using the cooks' shared levels.
/// </summary>
[HarmonyPatch]
public static class CookingPatches
{
    /// <summary>
    /// Gives what is taken off the station stars from the Cooking skill of the player taking it.
    /// </summary>
    /// <param name="__instance">The station.</param>
    /// <param name="sender">The player's peer.</param>
    [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.RPC_RemoveDoneItem))]
    [HarmonyPrefix]
    private static void StarCookedFood(CookingStation __instance, long sender)
    {
        if (__instance.m_nview == null || !__instance.m_nview.IsOwner())
        {
            return;
        }

        float level = SkillLevels.Get(SkillLevels.FindPlayer(sender), Skills.SkillType.Cooking);
        if (Stars.MaxAt(level, Perks.FineCooking, Perks.MasterChef) > 0)
        {
            Stars.DropStars = _ => CookingStars.Roll(level);
        }
    }

    /// <summary>
    /// Stops giving stars.
    /// </summary>
    [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.RPC_RemoveDoneItem))]
    [HarmonyFinalizer]
    private static void StopStarring()
    {
        Stars.DropStars = null;
    }

    /// <summary>
    /// Cooks faster with a skilled cook near.
    /// </summary>
    /// <param name="__instance">The station.</param>
    /// <param name="__result">Seconds of cooking since the last update.</param>
    [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.GetDeltaTime))]
    [HarmonyPostfix]
    private static void CookFaster(CookingStation __instance, ref float __result)
    {
        __result *= Perks.KitchenSpeed(SkillLevels.BestNear(__instance.transform.position, CookingStars.CookRange, Skills.SkillType.Cooking));
    }

    /// <summary>
    /// Keeps food from burning while a watchful cook is near.
    /// </summary>
    /// <param name="__instance">The station.</param>
    /// <param name="__state">Whether vanilla lets food burn.</param>
    [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.UpdateCooking))]
    [HarmonyPrefix]
    private static void KeepFromBurning(CookingStation __instance, out bool __state)
    {
        __state = __instance.m_canOvercookItems;
        if (__state && __instance.m_nview != null && __instance.m_nview.IsValid() && __instance.m_nview.IsOwner()
            && CookingStars.WatchfulCookNear(__instance.transform.position))
        {
            __instance.m_canOvercookItems = false;
        }
    }

    /// <summary>
    /// Restores whether food can burn.
    /// </summary>
    /// <param name="__instance">The station.</param>
    /// <param name="__state">Whether vanilla lets food burn.</param>
    [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.UpdateCooking))]
    [HarmonyFinalizer]
    private static void RestoreBurning(CookingStation __instance, bool __state)
    {
        __instance.m_canOvercookItems = __state;
    }
}
