using BrudvikWhiteHilt.TradingPosts;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.TradingPosts;

/// <summary>
/// Hooks the trading posts onto the game and keeps their pieces whole: a piece of a post takes no damage, needs no
/// support, does not wear in rain or ash, does not burn and cannot be removed with the hammer.
/// </summary>
[HarmonyPatch]
public static class TradingPostPatches
{
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    [HarmonyPostfix]
    private static void GameStartPostfix(Game __instance)
    {
        __instance.gameObject.AddComponent<TradingPostService>();
    }

    // The flags are not saved, so they are set again every time a post's piece comes to life.
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Awake))]
    [HarmonyPostfix]
    private static void AwakePostfix(WearNTear __instance)
    {
        if (!IsPostPart(__instance))
        {
            return;
        }

        __instance.m_noSupportWear = true;
        __instance.m_noRoofWear = true;
        __instance.m_ashDamageImmune = true;
        __instance.m_burnable = false;
    }

    // Every blow, fire and fall reaches a piece through its owner's damage handler; a post's pieces skip it.
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.RPC_Damage))]
    [HarmonyPrefix]
    private static bool DamagePrefix(WearNTear __instance)
    {
        return !IsPostPart(__instance);
    }

    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.CanBeRemoved))]
    [HarmonyPostfix]
    private static void WearCanBeRemovedPostfix(WearNTear __instance, ref bool __result)
    {
        if (__result && IsPostPart(__instance))
        {
            __result = false;
        }
    }

    [HarmonyPatch(typeof(Piece), nameof(Piece.CanBeRemoved))]
    [HarmonyPostfix]
    private static void PieceCanBeRemovedPostfix(Piece __instance, ref bool __result)
    {
        if (__result && __instance.m_nview != null && TradingPostKeys.IsPostPart(__instance.m_nview.GetZDO()))
        {
            __result = false;
        }
    }

    private static bool IsPostPart(WearNTear piece)
    {
        return piece.m_nview != null && TradingPostKeys.IsPostPart(piece.m_nview.GetZDO());
    }
}
