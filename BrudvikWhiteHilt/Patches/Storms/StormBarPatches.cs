using BrudvikWhiteHilt.Storms;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Storms;

/// <summary>
/// Holds the doors and windows a Storm Bar bars: the use key pounds on them instead of opening them, and nothing,
/// not a gust nor anyone's door call, opens them while the bar is across. They may still be shut.
/// </summary>
[HarmonyPatch]
public static class StormBarPatches
{
    [HarmonyPatch(typeof(Door), nameof(Door.Awake))]
    [HarmonyPostfix]
    private static void DoorAwakePostfix(Door __instance)
    {
        ZNetView nview = __instance.GetComponent<ZNetView>();
        if (nview != null && nview.GetZDO() != null && __instance.GetComponent<StormBarredDoor>() == null)
        {
            __instance.gameObject.AddComponent<StormBarredDoor>();
        }
    }

    [HarmonyPatch(typeof(Door), nameof(Door.Interact))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static bool DoorInteractPrefix(Door __instance, Humanoid character, bool hold, ref bool __result)
    {
        if (hold || character is not Player player)
        {
            return true;
        }

        StormBarredDoor barred = __instance.GetComponent<StormBarredDoor>();
        if (barred == null || !barred.TryPound(player))
        {
            return true;
        }

        __result = true;
        return false;
    }

    // On the door's owner: a shut door the bar holds stays shut, whoever calls.
    [HarmonyPatch(typeof(Door), nameof(Door.RPC_UseDoor))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static bool DoorUsePrefix(Door __instance)
    {
        ZNetView nview = __instance.m_nview;
        return nview == null || !nview.IsValid() || nview.GetZDO().GetInt(ZDOVars.s_state) != 0 || StormBar.Holding(__instance) == null;
    }

    [HarmonyPatch(typeof(Door), nameof(Door.GetHoverText))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void DoorHoverTextPostfix(Door __instance, ref string __result)
    {
        if (string.IsNullOrEmpty(__result))
        {
            return;
        }

        string barred = __instance.GetComponent<StormBarredDoor>()?.HoverText();
        if (barred != null)
        {
            __result = barred;
        }
    }
}
