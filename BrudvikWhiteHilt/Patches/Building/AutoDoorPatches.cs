using BrudvikWhiteHilt.Building.Doors;
using HarmonyLib;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Building;

/// <summary>
/// Gives every door the <see cref="AutoDoor"/> that closes it, and adds Shift + Use to hold a door open.
/// </summary>
[HarmonyPatch]
public static class AutoDoorPatches
{
    /// <summary>
    /// Detects when travel changes the local active zone, before the old scene and its door ownership are released.
    /// </summary>
    /// <param name="__instance">The network manager.</param>
    /// <param name="pos">The new reference position.</param>
    /// <param name="__state">Whether the active zone changed.</param>
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.SetReferencePosition))]
    [HarmonyPrefix]
    public static void ReferencePositionChanging(ZNet __instance, Vector3 pos, out bool __state)
    {
        __state = !ZoneSystem.GetZone(__instance.GetReferencePosition()).Equals(ZoneSystem.GetZone(pos));
    }

    /// <summary>
    /// Saves unattended doors as closed when their owner leaves the active area, including through a portal.
    /// </summary>
    /// <param name="pos">The new reference position.</param>
    /// <param name="__state">Whether the active zone changed.</param>
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.SetReferencePosition))]
    [HarmonyPostfix]
    public static void ReferencePositionChanged(Vector3 pos, bool __state)
    {
        if (__state)
        {
            AutoDoor.CloseOutsideActiveArea(pos);
        }
    }

    /// <summary>
    /// Adds the closer to doors that exist in the world; the placement ghost has no ZDO.
    /// </summary>
    /// <param name="__instance">The door.</param>
    [HarmonyPatch(typeof(Door), nameof(Door.Awake))]
    [HarmonyPostfix]
    public static void DoorAwake(Door __instance)
    {
        ZNetView nview = __instance.GetComponent<ZNetView>();
        if (nview != null && nview.GetZDO() != null && __instance.GetComponent<AutoDoor>() == null)
        {
            __instance.gameObject.AddComponent<AutoDoor>();
        }
    }

    /// <summary>
    /// Shift + Use holds the door open, or lets a held door close on its own again.
    /// </summary>
    /// <param name="__instance">The door.</param>
    /// <param name="hold">True while the use key is held down.</param>
    /// <param name="alt">True with Shift.</param>
    /// <param name="__result">Whether the interaction was handled.</param>
    /// <returns>False when the door was already open and only the hold changed.</returns>
    [HarmonyPatch(typeof(Door), nameof(Door.Interact))]
    [HarmonyPrefix]
    public static bool DoorInteract(Door __instance, bool hold, bool alt, ref bool __result)
    {
        if (!alt || hold)
        {
            return true;
        }

        AutoDoor auto = __instance.GetComponent<AutoDoor>();
        if (auto == null || !auto.CanHold || !__instance.CanInteract())
        {
            return true;
        }

        // Without access, vanilla flashes the ward and refuses.
        if (__instance.m_checkGuardStone && !PrivateArea.CheckAccess(__instance.transform.position, 0f, false))
        {
            return true;
        }

        if (auto.IsOpen)
        {
            auto.RequestHold(!auto.IsHeld);
            __result = true;
            return false;
        }

        auto.RequestHold(true);
        return true;
    }

    /// <summary>
    /// Drops the hold when the door is closed.
    /// </summary>
    /// <param name="__instance">The door.</param>
    [HarmonyPatch(typeof(Door), nameof(Door.RPC_UseDoor))]
    [HarmonyPostfix]
    public static void DoorUsed(Door __instance)
    {
        AutoDoor auto = __instance.GetComponent<AutoDoor>();
        if (auto != null)
        {
            auto.ClearHoldIfClosed();
        }
    }

    /// <summary>
    /// Shows Shift + Use to hold the door open, and that it is held.
    /// </summary>
    /// <param name="__instance">The door.</param>
    /// <param name="__result">The hover text.</param>
    [HarmonyPatch(typeof(Door), nameof(Door.GetHoverText))]
    [HarmonyPostfix]
    public static void DoorHoverText(Door __instance, ref string __result)
    {
        if (string.IsNullOrEmpty(__result))
        {
            return;
        }

        AutoDoor auto = __instance.GetComponent<AutoDoor>();
        if (auto == null || !auto.CanHold || !__instance.CanInteract())
        {
            return;
        }

        if (__instance.m_checkGuardStone && !PrivateArea.CheckAccess(__instance.transform.position, 0f, false))
        {
            return;
        }

        __result += auto.GetHoverText();
    }
}
