using BrudvikWhiteHilt.Branding;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Branding;

/// <summary>
/// Adds the White Hilt logo to the main menu.
/// </summary>
[HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
public static class MainMenuLogoPatch
{
    private static void Postfix(FejdStartup __instance)
    {
        MainMenuLogo.Create(__instance);
    }
}

/// <summary>
/// Adds the White Hilt logo to the loading screen of a new HUD.
/// </summary>
[HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
public static class LoadingScreenLogoCreatePatch
{
    private static void Postfix(Hud __instance)
    {
        LoadingScreenLogo.Create(__instance);
    }
}

/// <summary>
/// Shows or hides the White Hilt loading screen after the HUD has decided what the loading screen shows.
/// </summary>
[HarmonyPatch(typeof(Hud), nameof(Hud.UpdateBlackScreen))]
public static class LoadingScreenLogoRefreshPatch
{
    private static void Postfix(Hud __instance)
    {
        LoadingScreenLogo.Refresh(__instance);
    }
}
