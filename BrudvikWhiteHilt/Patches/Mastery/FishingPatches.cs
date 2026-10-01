using BrudvikWhiteHilt.Mastery;
using HarmonyLib;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Mastery;

/// <summary>
/// The fight on the line and the Fishing milestones.
/// </summary>
[HarmonyPatch]
public static class FishingPatches
{
    /// <summary>
    /// Strains the line while the player reels in a running fish.
    /// </summary>
    /// <param name="__instance">The float.</param>
    /// <returns>False if the line snapped.</returns>
    [HarmonyPatch(typeof(FishingFloat), nameof(FishingFloat.FixedUpdate))]
    [HarmonyPrefix]
    private static bool Fight(FishingFloat __instance)
    {
        return Angling.UpdateFight(__instance);
    }

    /// <summary>
    /// Rolls a legendary fish.
    /// </summary>
    /// <param name="fish">The fish.</param>
    /// <param name="owner">The fisher.</param>
    [HarmonyPatch(typeof(FishingFloat), nameof(FishingFloat.Catch))]
    [HarmonyPrefix]
    private static void BeforeCatch(Fish fish, Character owner)
    {
        Angling.BeforeCatch(fish, owner);
    }

    /// <summary>
    /// Snags, double catches and the catch log.
    /// </summary>
    /// <param name="fish">The fish.</param>
    /// <param name="owner">The fisher.</param>
    [HarmonyPatch(typeof(FishingFloat), nameof(FishingFloat.Catch))]
    [HarmonyPostfix]
    private static void AfterCatch(Fish fish, Character owner)
    {
        Angling.AfterCatch(fish, owner);
    }

    /// <summary>
    /// Makes a legendary fish as it is picked up.
    /// </summary>
    /// <param name="go">The object picked up.</param>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.Pickup))]
    [HarmonyPrefix]
    private static void LegendaryPickup(GameObject go)
    {
        Angling.OnPickup(go);
    }

    /// <summary>
    /// Shows that a fish is legendary.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="__result">The tooltip.</param>
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool))]
    [HarmonyPostfix]
    private static void LegendaryTooltip(ItemDrop.ItemData item, ref string __result)
    {
        if (item != null && item.m_customData.ContainsKey(Angling.LegendaryKey))
        {
            __result += "\n<color=orange>$whitehilt_legendary</color>";
        }
    }
}
