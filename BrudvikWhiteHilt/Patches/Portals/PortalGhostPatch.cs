using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Portals;

/// <summary>
/// Removes the frozen copy other players see at a portal after someone travels through it.
/// Vanilla tells peers an object left their area while the object still has its OLD position, so a jump
/// from inside their area to far away is never reported. The server repeats the check with the new position.
/// </summary>
[HarmonyPatch]
public static class PortalGhostPatch
{
    private static ZDO movedSector;

    [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.ZDOSectorInvalidated))]
    [HarmonyPostfix]
    private static void RememberSectorChange(ZDO zdo)
    {
        movedSector = zdo;
    }

    [HarmonyPatch(typeof(ZDO), nameof(ZDO.InternalSetPosition))]
    [HarmonyPostfix]
    private static void RepeatWithNewPosition(ZDO __instance)
    {
        if (movedSector != __instance)
        {
            return;
        }

        ZDOMan.instance.ZDOSectorInvalidated(__instance);
        movedSector = null;
    }
}
