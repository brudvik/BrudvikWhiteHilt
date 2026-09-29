using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using System.Text;
using UnityEngine;

namespace BrudvikWhiteHilt.Building;

/// <summary>
/// Config for the build camera and the build toolbar, section "BuildTools". The rules are server-synced; keys,
/// speeds and what is shown are the player's own.
/// </summary>
public static class BuildToolSettings
{
    private const string Section = "BuildTools";
    private const string KeySection = "BuildTools.Keys";

    /// <summary>Whether the build camera may be used at all.</summary>
    public static ConfigEntry<bool> CameraEnabled { get; private set; }

    /// <summary>How far from the player the camera may fly, in metres.</summary>
    public static ConfigEntry<float> CameraPlayerRange { get; private set; }

    /// <summary>Whether the camera may also fly anywhere within a crafting station's build range.</summary>
    public static ConfigEntry<bool> CameraStationRange { get; private set; }

    /// <summary>How far from the camera pieces may be placed, in metres.</summary>
    public static ConfigEntry<float> MaxPlaceDistance { get; private set; }

    /// <summary>Whether the camera picks up items lying close to it.</summary>
    public static ConfigEntry<bool> AutoPickup { get; private set; }

    /// <summary>How close to the camera items are picked up, in metres.</summary>
    public static ConfigEntry<float> AutoPickupRange { get; private set; }

    /// <summary>How long after placing a piece it may be undone with a full refund, in seconds.</summary>
    public static ConfigEntry<float> UndoSeconds { get; private set; }

    /// <summary>Whether the toolbar is shown while a build tool is held.</summary>
    public static ConfigEntry<bool> ShowToolbar { get; private set; }

    /// <summary>Whether the key hint is shown bottom-left while a build tool is held.</summary>
    public static ConfigEntry<bool> ShowHint { get; private set; }

    /// <summary>Camera flying speed in metres per second.</summary>
    public static ConfigEntry<float> MoveSpeed { get; private set; }

    /// <summary>Speed multiplier while the run key is held.</summary>
    public static ConfigEntry<float> FastMultiplier { get; private set; }

    /// <summary>Mouse look multiplier on top of the game's own sensitivity.</summary>
    public static ConfigEntry<float> MouseSensitivity { get; private set; }

    /// <summary>Inverts vertical mouse look in the camera.</summary>
    public static ConfigEntry<bool> InvertMouseY { get; private set; }

    /// <summary>Brightness of the camera light.</summary>
    public static ConfigEntry<float> LightIntensity { get; private set; }

    /// <summary>Reach of the camera light in metres.</summary>
    public static ConfigEntry<float> LightRange { get; private set; }

    /// <summary>How far one nudge moves the piece, in metres.</summary>
    public static ConfigEntry<float> NudgeStep { get; private set; }

    /// <summary>Grid size in metres.</summary>
    public static ConfigEntry<float> GridSize { get; private set; }

    /// <summary>Whether coloured axes are drawn on the piece being placed.</summary>
    public static ConfigEntry<bool> ShowAxes { get; private set; }

    /// <summary>Whether the toolbar is folded to its title and status lines.</summary>
    public static ConfigEntry<bool> ToolbarCollapsed { get; private set; }

    /// <summary>Held with the mouse wheel to tilt the piece.</summary>
    public static ConfigEntry<KeyCode> TiltWheelModifier { get; private set; }

    /// <summary>Held with the mouse wheel to roll the piece.</summary>
    public static ConfigEntry<KeyCode> RollWheelModifier { get; private set; }

    /// <summary>Held to circle the build camera around the aimed point.</summary>
    public static ConfigEntry<KeyCode> OrbitKey { get; private set; }

    /// <summary>Held to show the cursor so the toolbar can be clicked.</summary>
    public static ConfigEntry<KeyCode> CursorKey { get; private set; }

    /// <summary>Toggles the build camera.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyCamera { get; private set; }

    /// <summary>Cycles the rotation step.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyStep { get; private set; }

    /// <summary>Tilts the piece forward.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyTiltForward { get; private set; }

    /// <summary>Tilts the piece back.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyTiltBack { get; private set; }

    /// <summary>Rolls the piece left.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyRollLeft { get; private set; }

    /// <summary>Rolls the piece right.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyRollRight { get; private set; }

    /// <summary>Turns the piece upside down.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyFlip { get; private set; }

    /// <summary>Resets rotation, tilt, roll and nudge.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyReset { get; private set; }

    /// <summary>Toggles snapping.</summary>
    public static ConfigEntry<KeyboardShortcut> KeySnap { get; private set; }

    /// <summary>Copies the piece under the crosshair with its rotation.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyCopy { get; private set; }

    /// <summary>Toggles the grid.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyGrid { get; private set; }

    /// <summary>Undoes the last placed piece.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyUndo { get; private set; }

    /// <summary>Toggles the camera light.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyLight { get; private set; }

    /// <summary>Nudges the piece away from the camera.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyNudgeForward { get; private set; }

    /// <summary>Nudges the piece towards the camera.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyNudgeBack { get; private set; }

    /// <summary>Nudges the piece left.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyNudgeLeft { get; private set; }

    /// <summary>Nudges the piece right.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyNudgeRight { get; private set; }

    /// <summary>Nudges the piece up.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyNudgeUp { get; private set; }

    /// <summary>Nudges the piece down.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyNudgeDown { get; private set; }

    /// <summary>Flies the build camera to the aimed point.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyFlyTo { get; private set; }

    /// <summary>Places a copy of the last piece next to it.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyStamp { get; private set; }

    /// <summary>Puts back the last undone piece.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyRedo { get; private set; }

    /// <summary>Steps the tilt through 0, 45 and 90 degrees.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyQuickTilt { get; private set; }

    /// <summary>Steps the roll through 0, 45 and 90 degrees.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyQuickRoll { get; private set; }

    /// <summary>Makes the build camera faster.</summary>
    public static ConfigEntry<KeyboardShortcut> KeySpeedUp { get; private set; }

    /// <summary>Makes the build camera slower.</summary>
    public static ConfigEntry<KeyboardShortcut> KeySpeedDown { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        AddTranslations();

        CameraEnabled = WhiteHiltConfig.BindAdminOnly(Section, "CameraEnabled", true, "Allow the free build camera while holding a build tool.");
        CameraPlayerRange = WhiteHiltConfig.BindAdminOnly(Section, "CameraPlayerRange", 12f,
            "How far from the player the build camera may fly, in metres.", new AcceptableValueRange<float>(2f, 100f));
        CameraStationRange = WhiteHiltConfig.BindAdminOnly(Section, "CameraStationRange", true,
            "The build camera may also fly anywhere within a crafting station's build range.");
        MaxPlaceDistance = WhiteHiltConfig.BindAdminOnly(Section, "MaxPlaceDistance", 50f,
            "How far from the build camera pieces may be placed and removed, in metres.", new AcceptableValueRange<float>(5f, 100f));
        AutoPickup = WhiteHiltConfig.BindAdminOnly(Section, "AutoPickup", false, "The build camera picks up items lying close to it.");
        AutoPickupRange = WhiteHiltConfig.BindAdminOnly(Section, "AutoPickupRange", 4f,
            "How close to the build camera items are picked up, in metres.", new AcceptableValueRange<float>(1f, 15f));
        UndoSeconds = WhiteHiltConfig.BindAdminOnly(Section, "UndoSeconds", 30f,
            "How long after placing a piece it may be undone with a full refund, in seconds. 0 turns undo off.",
            new AcceptableValueRange<float>(0f, 600f));

        ShowToolbar = WhiteHiltConfig.BindLocal(Section, "ShowToolbar", true, "Show the build toolbar on the left while holding a build tool.");
        ShowHint = WhiteHiltConfig.BindLocal(Section, "ShowHint", true, "Show the build camera key hint bottom-left while holding a build tool.");
        MoveSpeed = WhiteHiltConfig.BindLocal(Section, "MoveSpeed", 6f, "Build camera speed in metres per second.");
        FastMultiplier = WhiteHiltConfig.BindLocal(Section, "FastMultiplier", 2.5f, "Build camera speed multiplier while the run key is held.");
        MouseSensitivity = WhiteHiltConfig.BindLocal(Section, "MouseSensitivity", 1f, "Build camera mouse look multiplier on top of the game's own setting.");
        InvertMouseY = WhiteHiltConfig.BindLocal(Section, "InvertMouseY", false, "Invert vertical mouse look in the build camera.");
        LightIntensity = WhiteHiltConfig.BindLocal(Section, "LightIntensity", 1.5f, "Brightness of the build camera light.");
        LightRange = WhiteHiltConfig.BindLocal(Section, "LightRange", 40f, "Reach of the build camera light in metres.");
        NudgeStep = WhiteHiltConfig.BindLocal(Section, "NudgeStep", 0.05f, "How far one nudge moves the piece, in metres.");
        GridSize = WhiteHiltConfig.BindLocal(Section, "GridSize", 0.5f, "Grid size in metres.");
        ShowAxes = WhiteHiltConfig.BindLocal(Section, "ShowAxes", true, "Draw red, green and blue axes on the piece being placed.");
        ToolbarCollapsed = WhiteHiltConfig.BindLocal(Section, "ToolbarCollapsed", false, "Fold the toolbar to its title and status lines. Click the title to switch.");

        CursorKey = WhiteHiltConfig.BindLocal(KeySection, "Cursor", KeyCode.LeftAlt, "Hold to show the cursor and click the toolbar.");
        TiltWheelModifier = WhiteHiltConfig.BindLocal(KeySection, "TiltWheelModifier", KeyCode.LeftControl, "Hold and turn the mouse wheel to tilt the piece.");
        RollWheelModifier = WhiteHiltConfig.BindLocal(KeySection, "RollWheelModifier", KeyCode.LeftAlt, "Hold and turn the mouse wheel to roll the piece.");
        OrbitKey = WhiteHiltConfig.BindLocal(KeySection, "Orbit", KeyCode.Z, "Hold to circle the build camera around the point you aim at.");
        KeyCamera = BindKey("Camera", KeyCode.B, "Build camera on and off.");
        KeyStep = BindKey("RotationStep", KeyCode.Y, "Cycle the rotation step: 22.5, 15, 5 and 1 degrees.");
        KeyTiltForward = BindKey("TiltForward", KeyCode.Keypad8, "Tilt the piece forward.");
        KeyTiltBack = BindKey("TiltBack", KeyCode.Keypad2, "Tilt the piece back.");
        KeyRollLeft = BindKey("RollLeft", KeyCode.Keypad4, "Roll the piece to the left.");
        KeyRollRight = BindKey("RollRight", KeyCode.Keypad6, "Roll the piece to the right.");
        KeyFlip = BindKey("Flip", KeyCode.Keypad5, "Turn the piece upside down.");
        KeyReset = BindKey("Reset", KeyCode.Keypad0, "Reset rotation, tilt, roll and nudge.");
        KeySnap = BindKey("Snap", KeyCode.N, "Snapping on and off.");
        KeyCopy = BindKey("Copy", KeyCode.I, "Copy the piece under the crosshair, with its rotation and tilt.");
        KeyGrid = BindKey("Grid", KeyCode.H, "Grid on and off.");
        KeyUndo = BindKey("Undo", new KeyboardShortcut(KeyCode.Z, KeyCode.LeftControl), "Undo the last placed piece.");
        KeyLight = BindKey("Light", KeyCode.L, "Build camera light on and off.");
        KeyNudgeForward = BindKey("NudgeForward", KeyCode.UpArrow, "Nudge the piece away from you.");
        KeyNudgeBack = BindKey("NudgeBack", KeyCode.DownArrow, "Nudge the piece towards you.");
        KeyNudgeLeft = BindKey("NudgeLeft", KeyCode.LeftArrow, "Nudge the piece to the left.");
        KeyNudgeRight = BindKey("NudgeRight", KeyCode.RightArrow, "Nudge the piece to the right.");
        KeyNudgeUp = BindKey("NudgeUp", KeyCode.PageUp, "Nudge the piece up.");
        KeyNudgeDown = BindKey("NudgeDown", KeyCode.PageDown, "Nudge the piece down.");
        KeyFlyTo = BindKey("FlyTo", KeyCode.J, "Fly the build camera to just in front of the point you aim at.");
        KeyStamp = BindKey("Stamp", KeyCode.K, "Place a copy of the last piece right next to it, in the direction you look.");
        KeyRedo = BindKey("Redo", new KeyboardShortcut(KeyCode.Y, KeyCode.LeftControl), "Put back the last undone piece.");
        KeyQuickTilt = BindKey("QuickTilt", KeyCode.Keypad7, "Set the tilt to 45, 90 or 0 degrees.");
        KeyQuickRoll = BindKey("QuickRoll", KeyCode.Keypad9, "Set the roll to 45, 90 or 0 degrees.");
        KeySpeedUp = BindKey("SpeedUp", KeyCode.KeypadPlus, "Make the build camera faster.");
        KeySpeedDown = BindKey("SpeedDown", KeyCode.KeypadMinus, "Make the build camera slower.");
    }

    /// <summary>
    /// Short readable name of a key combination, e.g. "Ctrl+Z" or "Num8".
    /// </summary>
    /// <param name="entry">The key config.</param>
    /// <returns>The name, or an empty string if no key is bound.</returns>
    public static string KeyName(ConfigEntry<KeyboardShortcut> entry)
    {
        if (entry == null || entry.Value.MainKey == KeyCode.None)
        {
            return string.Empty;
        }

        StringBuilder text = new();
        foreach (KeyCode modifier in entry.Value.Modifiers)
        {
            text.Append(KeyName(modifier)).Append('+');
        }

        return text.Append(KeyName(entry.Value.MainKey)).ToString();
    }

    /// <summary>
    /// Short readable name of a single key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The name.</returns>
    public static string KeyName(KeyCode key)
    {
        if (key >= KeyCode.Keypad0 && key <= KeyCode.Keypad9)
        {
            return "Num" + (key - KeyCode.Keypad0);
        }

        if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9)
        {
            return ((int)(key - KeyCode.Alpha0)).ToString();
        }

        return key switch
        {
            KeyCode.LeftControl or KeyCode.RightControl => "Ctrl",
            KeyCode.LeftShift or KeyCode.RightShift => "Shift",
            KeyCode.LeftAlt or KeyCode.RightAlt => "Alt",
            KeyCode.PageUp => "PgUp",
            KeyCode.PageDown => "PgDn",
            KeyCode.UpArrow => "Up",
            KeyCode.DownArrow => "Down",
            KeyCode.LeftArrow => "Left",
            KeyCode.RightArrow => "Right",
            KeyCode.KeypadPlus => "Num+",
            KeyCode.KeypadMinus => "Num-",
            _ => key.ToString()
        };
    }

    private static void AddTranslations()
    {
        Translations.AddEnglish("whitehilt_build_toolbar", "Build tools");
        Translations.AddEnglish("whitehilt_build_readout", "Heading {0}   Tilt {1}   Roll {2}\nHeight above ground {3}\nFrom last piece {4}");
        Translations.AddEnglish("whitehilt_build_on", "on");
        Translations.AddEnglish("whitehilt_build_off", "off");
        Translations.AddEnglish("whitehilt_build_camera", "Build camera");
        Translations.AddEnglish("whitehilt_build_flyto", "Fly to aim");
        Translations.AddEnglish("whitehilt_build_speed_down", "Slower");
        Translations.AddEnglish("whitehilt_build_speed_up", "Faster");
        Translations.AddEnglish("whitehilt_build_step", "Rotation step");
        Translations.AddEnglish("whitehilt_build_tilt_forward", "Tilt forward");
        Translations.AddEnglish("whitehilt_build_tilt_back", "Tilt back");
        Translations.AddEnglish("whitehilt_build_roll_left", "Roll left");
        Translations.AddEnglish("whitehilt_build_roll_right", "Roll right");
        Translations.AddEnglish("whitehilt_build_quick_tilt", "Tilt 45/90/0");
        Translations.AddEnglish("whitehilt_build_quick_roll", "Roll 45/90/0");
        Translations.AddEnglish("whitehilt_build_flip", "Flip");
        Translations.AddEnglish("whitehilt_build_reset", "Reset");
        Translations.AddEnglish("whitehilt_build_snap", "Snap");
        Translations.AddEnglish("whitehilt_build_snappoint", "Snap point");
        Translations.AddEnglish("whitehilt_build_copy", "Copy");
        Translations.AddEnglish("whitehilt_build_grid", "Grid");
        Translations.AddEnglish("whitehilt_build_stamp", "Stamp");
        Translations.AddEnglish("whitehilt_build_nudge_away", "Away");
        Translations.AddEnglish("whitehilt_build_nudge_closer", "Closer");
        Translations.AddEnglish("whitehilt_build_nudge_up", "Up");
        Translations.AddEnglish("whitehilt_build_nudge_left", "Left");
        Translations.AddEnglish("whitehilt_build_nudge_right", "Right");
        Translations.AddEnglish("whitehilt_build_nudge_down", "Down");
        Translations.AddEnglish("whitehilt_build_undo", "Undo");
        Translations.AddEnglish("whitehilt_build_redo", "Redo");
        Translations.AddEnglish("whitehilt_build_light", "Light");
        Translations.AddEnglish("whitehilt_build_footer",
            "Hold [{0}] to click the buttons\n[{1}]+wheel tilt   [{2}]+wheel roll\nHold [{3}] to circle the piece\nNudge {5}: {4}");
        Translations.AddEnglish("whitehilt_build_hint", "[{0}] Build camera    Hold [{1}] for the cursor");
        Translations.AddEnglish("whitehilt_build_hint_active",
            "[{0}] Leave build camera   [{1}] fly to aim   Hold [{2}] circle the piece\n[W A S D] fly   [Space] up   [Ctrl] down   [Shift] faster   [{3}] speed x{4}\n[{5}] photo   [{6}] photo view   [{7}] media and film");
        Translations.AddEnglish("whitehilt_build_tip_camera", "A free camera within building reach. It may also go below ground. You stand still while it is on.");
        Translations.AddEnglish("whitehilt_build_tip_flyto", "Flies the camera to 3 m in front of what you aim at, as far as the building reach allows.");
        Translations.AddEnglish("whitehilt_build_tip_speed", "Camera flying speed.");
        Translations.AddEnglish("whitehilt_build_tip_step", "Degrees per turn of the mouse wheel, also used for tilt and roll.");
        Translations.AddEnglish("whitehilt_build_tip_tilt", "Tips the piece forward or back, around its own sideways axis (red).");
        Translations.AddEnglish("whitehilt_build_tip_roll", "Leans the piece left or right, around its own forward axis (blue).");
        Translations.AddEnglish("whitehilt_build_tip_quick", "Jumps straight to 45, 90 and back to 0 degrees.");
        Translations.AddEnglish("whitehilt_build_tip_flip", "Turns the piece upside down.");
        Translations.AddEnglish("whitehilt_build_tip_reset", "Straightens the piece and clears the nudge.");
        Translations.AddEnglish("whitehilt_build_tip_snap", "Whether the piece snaps to the corners and edges of other pieces.");
        Translations.AddEnglish("whitehilt_build_tip_snappoint", "Chooses which corner of the held piece does the snapping.");
        Translations.AddEnglish("whitehilt_build_tip_copy", "Picks the piece you aim at, with its heading, tilt and roll.");
        Translations.AddEnglish("whitehilt_build_tip_grid", "Keeps the piece on a grid when it is not snapped.");
        Translations.AddEnglish("whitehilt_build_tip_stamp", "Places a copy of the last piece right next to it, on the side you look towards. Press again for a row.");
        Translations.AddEnglish("whitehilt_build_tip_nudge", "Moves the piece a little along its own axes; the one closest to where you look is used.");
        Translations.AddEnglish("whitehilt_build_tip_undo", "Takes back the last piece you placed, with everything it cost.");
        Translations.AddEnglish("whitehilt_build_tip_redo", "Puts back the last piece you took back.");
        Translations.AddEnglish("whitehilt_build_tip_light", "A lamp on the build camera.");
        Translations.AddEnglish("msg_whitehilt_build_camera_disabled", "The build camera is switched off on this server");
        Translations.AddEnglish("msg_whitehilt_build_camera_edge", "The camera cannot go further from you or your crafting stations");
        Translations.AddEnglish("msg_whitehilt_build_camera_speed", "Camera speed");
        Translations.AddEnglish("msg_whitehilt_build_snap_on", "Snapping on");
        Translations.AddEnglish("msg_whitehilt_build_snap_off", "Snapping off");
        Translations.AddEnglish("msg_whitehilt_build_grid_on", "Grid on");
        Translations.AddEnglish("msg_whitehilt_build_grid_off", "Grid off");
        Translations.AddEnglish("msg_whitehilt_build_light_on", "Camera light on");
        Translations.AddEnglish("msg_whitehilt_build_light_off", "Camera light off");
        Translations.AddEnglish("msg_whitehilt_build_undone", "Piece taken back");
        Translations.AddEnglish("msg_whitehilt_build_undo_none", "Nothing to undo");
        Translations.AddEnglish("msg_whitehilt_build_redo_none", "Nothing to redo");
        Translations.AddEnglish("msg_whitehilt_build_stamp_none", "Place a piece first");
        Translations.AddEnglish("msg_whitehilt_build_stamp_unsupported", "This piece cannot be stamped");
        Translations.AddEnglish("msg_whitehilt_build_occupied", "There is already a piece there");
    }

    private static ConfigEntry<KeyboardShortcut> BindKey(string key, KeyCode defaultKey, string description)
    {
        return BindKey(key, new KeyboardShortcut(defaultKey), description);
    }

    private static ConfigEntry<KeyboardShortcut> BindKey(string key, KeyboardShortcut defaultKey, string description)
    {
        return WhiteHiltConfig.BindLocal(KeySection, key, defaultKey, description);
    }
}
