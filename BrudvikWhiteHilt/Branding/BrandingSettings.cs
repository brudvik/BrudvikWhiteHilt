using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Branding;

/// <summary>
/// Config for the White Hilt loading screen and main menu logo, section "Branding". The player's own.
/// </summary>
public static class BrandingSettings
{
    private const string Section = "Branding";

    /// <summary>Whether the loading screen is black with the logo in the middle instead of the vanilla painting.</summary>
    public static ConfigEntry<bool> LoadingScreenLogo { get; private set; }

    /// <summary>Whether the White Hilt logo and version are shown in the main menu.</summary>
    public static ConfigEntry<bool> MainMenuLogo { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        LoadingScreenLogo = WhiteHiltConfig.BindLocal(Section, "LoadingScreenLogo", true,
            "Show a black loading screen with the White Hilt logo in the middle when entering a world, teleporting and respawning.");
        MainMenuLogo = WhiteHiltConfig.BindLocal(Section, "MainMenuLogo", true,
            "Show the White Hilt logo and version in the bottom right corner of the main menu.");
    }
}
