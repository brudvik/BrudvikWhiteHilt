using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Portals.Effects;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Portals;

/// <summary>
/// Hooks the portal travel effects into the game: a player starting a trip, every player's body, the camera and the
/// loading screen.
/// </summary>
[HarmonyPatch]
public static class PortalFxPatches
{
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    [HarmonyPostfix]
    private static void RegisterRpc()
    {
        PortalFx.RegisterRpc();
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
    [HarmonyPostfix]
    private static void AddEffects(Player __instance)
    {
        if (!VisualHelper.IsHeadless && __instance.GetComponent<PortalFxPlayer>() == null)
        {
            __instance.gameObject.AddComponent<PortalFxPlayer>();
        }
    }

    // Only trips over a distance: short hops by other mods (ladders and the like) pass the same way.
    [HarmonyPatch(typeof(Player), nameof(Player.TeleportTo))]
    [HarmonyPostfix]
    private static void BeginDeparture(Player __instance, bool distantTeleport, bool __result)
    {
        if (__result && distantTeleport && __instance == Player.m_localPlayer && __instance.TryGetComponent(out PortalFxPlayer effects))
        {
            effects.BeginDeparture(PortalFx.ColourAt(__instance.transform.position));
        }
    }

    [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateCamera))]
    [HarmonyPostfix]
    private static void SwingCamera(GameCamera __instance)
    {
        PortalFxScreen.ApplyCamera(__instance);
    }

    [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateBlackScreen))]
    [HarmonyPostfix]
    private static void HoldLoadingScreen(Hud __instance)
    {
        PortalFxScreen.ApplyLoadingScreen(__instance);
    }
}
