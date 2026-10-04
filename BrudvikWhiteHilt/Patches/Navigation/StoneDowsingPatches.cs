using BrudvikWhiteHilt.Navigation.Dowsing;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Navigation;

/// <summary>
/// Hooks the Stone Dowser's service onto the game object, and remembers the rocks it pings toward.
/// </summary>
[HarmonyPatch]
public static class StoneDowsingPatches
{
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    [HarmonyPostfix]
    private static void GameStartPostfix(Game __instance)
    {
        __instance.gameObject.AddComponent<StoneDowsingService>();
    }

    [HarmonyPatch(typeof(Pickable), nameof(Pickable.Awake))]
    [HarmonyPostfix]
    private static void PickableAwakePostfix(Pickable __instance)
    {
        if (__instance.m_nview != null && __instance.m_nview.GetZDO() != null)
        {
            StoneDowsingTargets.Register(__instance);
            RootDowsingEffect.Register(__instance);
        }
    }
}
