using BrudvikWhiteHilt.Progression;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Progression;

/// <summary>
/// Applies White Hilt progression before the game refreshes the local player's known recipes.
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.UpdateKnownRecipesList))]
public static class UpdateKnownRecipesListPatch
{
    private static void Prefix(Player __instance, out bool __state)
    {
        __state = Game.instance != null
            && __instance.m_nview != null
            && __instance.m_nview.IsOwner()
            && ProgressionManager.Apply(__instance);
    }

    private static void Postfix(Player __instance, bool __state)
    {
        if (__state)
        {
            __instance.UpdateAvailablePiecesList();
        }
    }
}

/// <summary>
/// Marks item discovery so newly unlocked tiers are announced.
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.AddKnownItem))]
public static class AddKnownItemPatch
{
    private static void Prefix()
    {
        ProgressionManager.AnnounceUnlocks = true;
    }

    private static void Finalizer()
    {
        ProgressionManager.AnnounceUnlocks = false;
    }
}
