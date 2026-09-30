using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Branding;

/// <summary>
/// Config for the White Hilt loading screen, section "Branding". The player's own.
/// </summary>
public static class BrandingSettings
{
    private const string Section = "Branding";

    /// <summary>Whether the loading screen is black with the logo in the middle instead of the vanilla painting.</summary>
    public static ConfigEntry<bool> LoadingScreenLogo { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        LoadingScreenLogo = WhiteHiltConfig.BindLocal(Section, "LoadingScreenLogo", true,
            "Show a black loading screen with the White Hilt logo in the middle when entering a world, teleporting and respawning.");
    }
}
