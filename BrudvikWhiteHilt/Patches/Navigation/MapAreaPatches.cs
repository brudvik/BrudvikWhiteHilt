using BrudvikWhiteHilt.Navigation.Areas;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Navigation;

/// <summary>
/// Hooks built areas, fields, pastures and wards into the game: the service on the game object and the drawing on the map.
/// </summary>
[HarmonyPatch]
public static class MapAreaPatches
{
    /// <summary>
    /// Adds the service that scans the world on the server and receives the areas on a client.
    /// </summary>
    /// <param name="__instance">The game.</param>
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    [HarmonyPostfix]
    public static void GameStartPostfix(Game __instance)
    {
        __instance.gameObject.AddComponent<MapAreaService>();
    }

    /// <summary>
    /// Keeps the drawings in place over the map.
    /// </summary>
    /// <param name="__instance">The map.</param>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Update))]
    [HarmonyPostfix]
    public static void UpdatePostfix(Minimap __instance)
    {
        if (Player.m_localPlayer != null)
        {
            MapAreaOverlay.Update(__instance);
        }
    }
}
