using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Navigation.Portraits;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Navigation;

/// <summary>
/// Hooks player portraits into the main menu (taking your own) and the map (sharing and showing them).
/// </summary>
[HarmonyPatch]
public static class PortraitPatches
{
    /// <summary>
    /// Takes a portrait of the character shown in the menu if its look has changed.
    /// </summary>
    /// <param name="__instance">The main menu.</param>
    /// <param name="profile">The character shown.</param>
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.SetupCharacterPreview))]
    [HarmonyPostfix]
    public static void SetupCharacterPreviewPostfix(FejdStartup __instance, PlayerProfile profile)
    {
        PortraitCapture.OnPreview(__instance, profile);
    }

    /// <summary>
    /// Starts sharing portraits when the game starts; a dedicated server only relays them.
    /// </summary>
    /// <param name="__instance">The game.</param>
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    [HarmonyPostfix]
    public static void GameStartPostfix(Game __instance)
    {
        if (!VisualHelper.IsHeadless)
        {
            __instance.gameObject.AddComponent<PortraitNetwork>();
        }
    }

    /// <summary>
    /// Remembers which player each player pin stands for.
    /// </summary>
    /// <param name="__instance">The map.</param>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdatePlayerPins))]
    [HarmonyPostfix]
    public static void UpdatePlayerPinsPostfix(Minimap __instance)
    {
        PortraitPins.MapPlayers(__instance);
    }

    /// <summary>
    /// Keeps the portraits on the player pins and your own marker, on top of everything else on the map.
    /// </summary>
    /// <param name="__instance">The map.</param>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Update))]
    [HarmonyPostfix]
    public static void UpdatePostfix(Minimap __instance)
    {
        PortraitPins.Update(__instance);
    }
}
