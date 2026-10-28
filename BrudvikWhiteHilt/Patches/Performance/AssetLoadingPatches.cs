using System;
using BrudvikWhiteHilt.Performance;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Performance;

/// <summary>
/// When assets are loaded and unloaded: the hourly unload waits for a quiet moment (<see cref="AssetUnloadScheduler"/>),
/// and loading runs at full speed behind a black screen (<see cref="LoadingPriority"/>). Also works out which fixes run
/// once every plugin has loaded and again for each world, see <see cref="VanillaFixSettings.Refresh"/>.
/// </summary>
[HarmonyPatch]
public static class AssetLoadingPatches
{
    private const float RefreshSeconds = 60f;

    private static float nextRefresh;

    /// <summary>
    /// Holds back the hourly unload until a quiet moment.
    /// </summary>
    /// <param name="__instance">The game.</param>
    /// <returns>False to hold it back.</returns>
    [HarmonyPatch(typeof(Game), nameof(Game.CollectResourcesCheckPeriodic))]
    [HarmonyPrefix]
    public static bool HourlyUnload(Game __instance)
    {
        try
        {
            return AssetUnloadScheduler.AllowHourly(__instance);
        }
        catch (Exception ex)
        {
            VanillaFixSettings.AssetUnload.Fail("deciding on the hourly asset unload", ex);
            return true;
        }
    }

    /// <summary>
    /// Runs a held-back unload when the moment comes, and once a minute works out again which fixes run, since Valheim
    /// Community Patch may stand one of its own down at its first use in the world.
    /// </summary>
    /// <param name="__instance">The game.</param>
    [HarmonyPatch(typeof(Game), nameof(Game.Update))]
    [HarmonyPostfix]
    public static void GameUpdate(Game __instance)
    {
        try
        {
            if (UnityEngine.Time.realtimeSinceStartup >= nextRefresh)
            {
                nextRefresh = UnityEngine.Time.realtimeSinceStartup + RefreshSeconds;
                if (CommunityPatchCoexistence.IsInstalled)
                {
                    VanillaFixSettings.Refresh();
                }
            }

            AssetUnloadScheduler.Tick(__instance);
        }
        catch (Exception ex)
        {
            VanillaFixSettings.AssetUnload.Fail("the held-back asset unload", ex);
        }
    }

    /// <summary>
    /// Raises the loading priority while the screen is black for a portal or a respawn, and lowers it after.
    /// </summary>
    /// <param name="__instance">The HUD.</param>
    /// <param name="player">The local player.</param>
    [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateBlackScreen))]
    [HarmonyPostfix]
    public static void BlackScreen(Hud __instance, Player player)
    {
        try
        {
            LoadingPriority.Update(__instance, player);
        }
        catch (Exception ex)
        {
            VanillaFixSettings.LoadingPriority.Fail("setting the loading priority", ex);
        }
    }

    /// <summary>
    /// Gives back a raised loading priority when the HUD goes with the world.
    /// </summary>
    [HarmonyPatch(typeof(Hud), nameof(Hud.OnDestroy))]
    [HarmonyPostfix]
    public static void HudDestroyed()
    {
        LoadingPriority.Release();
    }

    /// <summary>
    /// Works out which fixes run once the main menu is up, when every plugin has loaded.
    /// </summary>
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Start))]
    [HarmonyPostfix]
    public static void MainMenu()
    {
        VanillaFixSettings.Refresh();
    }

    /// <summary>
    /// Works out which fixes run again for each world; Valheim Community Patch decides some of its own only then.
    /// </summary>
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    [HarmonyPostfix]
    public static void WorldStart()
    {
        VanillaFixSettings.Refresh();
    }
}
