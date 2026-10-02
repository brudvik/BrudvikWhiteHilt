using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Portals.Effects;

/// <summary>
/// Config for the portal travel effects, section "Portals.Effects". Each player's own: it only changes what they see.
/// </summary>
public static class PortalFxSettings
{
    private const string Section = "Portals.Effects";

    /// <summary>Whether travel by portal shows the effects, for yourself and for others you see travelling.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>Whether the camera swings out around you as you leave.</summary>
    public static ConfigEntry<bool> Camera { get; private set; }

    /// <summary>Whether the screen flashes white with a rune ring as you leave.</summary>
    public static ConfigEntry<bool> Flash { get; private set; }

    /// <summary>Whether the effects play their sounds.</summary>
    public static ConfigEntry<bool> Sounds { get; private set; }

    /// <summary>Whether a dog travelling along gets a small effect too.</summary>
    public static ConfigEntry<bool> Dog { get; private set; }

    /// <summary>Whether the effects take the colour of the runes at the portal travelled from.</summary>
    public static ConfigEntry<bool> RuneColours { get; private set; }

    /// <summary>Colour of the effects without runes, as #RRGGBB.</summary>
    public static ConfigEntry<string> Colour { get; private set; }

    /// <summary>Emote played as you step into the portal; empty for none.</summary>
    public static ConfigEntry<string> Emote { get; private set; }

    /// <summary>Seconds after stepping in when the body vanishes in a flash. The game moves you at 2 seconds.</summary>
    public static ConfigEntry<float> FlashAt { get; private set; }

    /// <summary>Seconds the arrival takes.</summary>
    public static ConfigEntry<float> ArriveSeconds { get; private set; }

    /// <summary>Metres the body rises before it vanishes.</summary>
    public static ConfigEntry<float> LiftHeight { get; private set; }

    /// <summary>Turns the body makes before it vanishes.</summary>
    public static ConfigEntry<float> Spins { get; private set; }

    /// <summary>Strength of the glow; above 1 the light blooms.</summary>
    public static ConfigEntry<float> Glow { get; private set; }

    /// <summary>Width of the rune ring on the ground, in metres.</summary>
    public static ConfigEntry<float> RingSize { get; private set; }

    /// <summary>Metres the camera pulls back as you leave.</summary>
    public static ConfigEntry<float> CameraDistance { get; private set; }

    /// <summary>Degrees the camera swings around you as you leave.</summary>
    public static ConfigEntry<float> CameraTurn { get; private set; }

    /// <summary>Degrees the field of view widens as you leave.</summary>
    public static ConfigEntry<float> FieldOfView { get; private set; }

    /// <summary>
    /// The colour without runes, or the default blue if the setting is no colour.
    /// </summary>
    public static Color DefaultColour => ColorUtility.TryParseHtmlString(Colour?.Value ?? string.Empty, out Color colour)
        ? colour
        : new Color(0.55f, 0.8f, 1f);

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindLocal(Section, "Enabled", true,
            "Show effects when someone travels by portal: they rise, spin and vanish in a flash of runes, and appear the same way.");
        Camera = WhiteHiltConfig.BindLocal(Section, "Camera", true,
            "Swing the camera out around you as you leave. Turn it off if camera moves make you feel unwell.");
        Flash = WhiteHiltConfig.BindLocal(Section, "Flash", true, "Flash the screen white with a ring of runes as you leave.");
        Sounds = WhiteHiltConfig.BindLocal(Section, "Sounds", true, "Play the sounds of leaving and arriving.");
        Dog = WhiteHiltConfig.BindLocal(Section, "Dog", true, "Show a small effect for a dog that travels along.");
        RuneColours = WhiteHiltConfig.BindLocal(Section, "RuneColours", true,
            "Colour the effects after the strongest rune on the rune posts at the portal travelled from.");
        Colour = WhiteHiltConfig.BindLocal(Section, "Colour", "#8CCBFF", "Colour of the effects without runes, as #RRGGBB.");
        Emote = WhiteHiltConfig.BindLocal(Section, "Emote", "cheer",
            "Emote you play as you step into the portal, seen by others too, e.g. cheer, bow or kneel. Empty for none.");
        FlashAt = WhiteHiltConfig.BindLocal(Section, "FlashAt", 1.7f,
            "Seconds after stepping in when you vanish in a flash. The game moves you at 2 seconds.",
            new AcceptableValueRange<float>(1f, 1.95f));
        ArriveSeconds = WhiteHiltConfig.BindLocal(Section, "ArriveSeconds", 1.6f, "Seconds the arrival takes.",
            new AcceptableValueRange<float>(0.5f, 5f));
        LiftHeight = WhiteHiltConfig.BindLocal(Section, "LiftHeight", 0.6f, "Metres you rise before you vanish.",
            new AcceptableValueRange<float>(0f, 2f));
        Spins = WhiteHiltConfig.BindLocal(Section, "Spins", 1.5f, "Turns you make before you vanish.",
            new AcceptableValueRange<float>(0f, 5f));
        Glow = WhiteHiltConfig.BindLocal(Section, "Glow", 2f, "Strength of the glow of runes and sparks. Above 1 they shine.",
            new AcceptableValueRange<float>(0.5f, 6f));
        RingSize = WhiteHiltConfig.BindLocal(Section, "RingSize", 3f, "Width of the rune ring on the ground, in metres.",
            new AcceptableValueRange<float>(1f, 6f));
        CameraDistance = WhiteHiltConfig.BindLocal(Section, "CameraDistance", 3f, "Metres the camera pulls back as you leave.",
            new AcceptableValueRange<float>(0f, 10f));
        CameraTurn = WhiteHiltConfig.BindLocal(Section, "CameraTurn", 120f, "Degrees the camera swings around you as you leave.",
            new AcceptableValueRange<float>(0f, 360f));
        FieldOfView = WhiteHiltConfig.BindLocal(Section, "FieldOfView", 25f, "Degrees the view widens as you are pulled in.",
            new AcceptableValueRange<float>(0f, 60f));
    }
}
