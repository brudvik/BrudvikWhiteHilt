using BrudvikWhiteHilt.Navigation.Discoveries;
using BrudvikWhiteHilt.Pieces.Portals.PortalMap;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Navigation;

/// <summary>
/// Hooks Munin's Perch discoveries into the game: the service on the game object, the markers and the filter panel on
/// the map, and the hover text.
/// </summary>
[HarmonyPatch]
public static class DiscoveryPatches
{
    /// <summary>
    /// Adds the service that searches the world on the server and receives the discoveries on a client.
    /// </summary>
    /// <param name="__instance">The game.</param>
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    [HarmonyPostfix]
    public static void GameStartPostfix(Game __instance)
    {
        __instance.gameObject.AddComponent<DiscoveryService>();
    }

    /// <summary>
    /// Keeps the markers and the panel up to date.
    /// </summary>
    /// <param name="__instance">The map.</param>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Update))]
    [HarmonyPostfix]
    public static void UpdatePostfix(Minimap __instance)
    {
        if (Player.m_localPlayer != null)
        {
            DiscoveryOverlay.Update(__instance);
        }

        DiscoveryPanel.Update(__instance);
    }

    /// <summary>
    /// Over a marker, the top of the large map names the discovery instead of the biome. Portals and ships come first.
    /// </summary>
    /// <param name="__instance">The map.</param>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdateBiome))]
    [HarmonyPostfix]
    public static void UpdateBiomePostfix(Minimap __instance)
    {
        if (__instance.m_mode != Minimap.MapMode.Large || !ZInput.IsMouseActive())
        {
            return;
        }

        UnityEngine.Vector3 world = __instance.ScreenToWorldPoint(ZInput.pointerPosition);
        if (PortalMapPins.GetHoverText(world, __instance.PinInteractRadius) != null)
        {
            return;
        }

        string text = DiscoveryOverlay.GetHoverText(world, __instance.PinInteractRadius);
        if (text != null)
        {
            __instance.m_biomeNameLarge.text = text;
        }
    }
}
