using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using UnityEngine;

namespace BrudvikWhiteHilt.Building.Groups;

/// <summary>
/// Config and texts for the group tools: selection, copy, move, delete, blueprints, line and area.
/// </summary>
public static class GroupSettings
{
    private const string Section = "BuildTools.Groups";

    /// <summary>Whether the group tools may be used.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>Most pieces in one paste, line or area; 0 means no limit.</summary>
    public static ConfigEntry<int> MaxPieces { get; private set; }

    /// <summary>How far away pieces may be pasted without the build camera, in metres.</summary>
    public static ConfigEntry<float> PasteReach { get; private set; }

    /// <summary>Selection mode on and off.</summary>
    public static ConfigEntry<KeyboardShortcut> KeySelect { get; private set; }

    /// <summary>Clears the selection.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyClear { get; private set; }

    /// <summary>Copies the selection.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyCopy { get; private set; }

    /// <summary>Moves the selection.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyCut { get; private set; }

    /// <summary>Pastes what was copied.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyPaste { get; private set; }

    /// <summary>Tears down the selection.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyDelete { get; private set; }

    /// <summary>Saves the selection as a blueprint.</summary>
    public static ConfigEntry<KeyboardShortcut> KeySave { get; private set; }

    /// <summary>Opens the blueprint panel.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyBlueprints { get; private set; }

    /// <summary>Line and area mode.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyLine { get; private set; }

    /// <summary>
    /// Binds the config entries and adds the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        AddTranslations();
        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true, "Allow selecting, copying, moving and tearing down groups of pieces, blueprints, and the line and area tool.");
        MaxPieces = WhiteHiltConfig.BindAdminOnly(Section, "MaxPieces", 0, "Most pieces in one paste, blueprint, line or area. 0 means no limit.",
            new AcceptableValueRange<int>(0, 100000));
        PasteReach = WhiteHiltConfig.BindAdminOnly(Section, "PasteReach", 30f,
            "How far away, in metres, copied pieces, lines and areas can be placed without the build camera.", new AcceptableValueRange<float>(5f, 100f));

        KeySelect = BindKey("Select", KeyCode.A, "Selection mode on and off.");
        KeyClear = BindKey("ClearSelection", KeyCode.D, "Clear the selection.");
        KeyCopy = BindKey("Copy", KeyCode.C, "Copy the selected pieces.");
        KeyCut = BindKey("Move", KeyCode.X, "Move the selected pieces: they are torn down when you place them somewhere else.");
        KeyPaste = BindKey("Paste", KeyCode.V, "Place what was copied.");
        KeyDelete = WhiteHiltConfig.BindLocal(Section + ".Keys", "Delete", new KeyboardShortcut(KeyCode.Delete), "Tear down the selected pieces.");
        KeySave = BindKey("SaveBlueprint", KeyCode.S, "Save the selected pieces as a blueprint.");
        KeyBlueprints = BindKey("Blueprints", KeyCode.O, "Open the blueprint panel.");
        KeyLine = BindKey("LineAndArea", KeyCode.L, "Line and area mode: press again to go from line to area to off.");
    }

    private static ConfigEntry<KeyboardShortcut> BindKey(string key, KeyCode main, string description)
    {
        return WhiteHiltConfig.BindLocal(Section + ".Keys", key, new KeyboardShortcut(main, KeyCode.LeftControl), description);
    }

    // The English texts of the group tools; Norwegian.json has the translations.
    private static void AddTranslations()
    {
        Translations.AddEnglish("whitehilt_group_select", "Select");
        Translations.AddEnglish("whitehilt_group_blueprints", "Blueprints");
        Translations.AddEnglish("whitehilt_group_line", "Line/area");
        Translations.AddEnglish("whitehilt_group_line_line", "line");
        Translations.AddEnglish("whitehilt_group_line_area", "area");
        Translations.AddEnglish("whitehilt_group_paste", "Paste");
        Translations.AddEnglish("whitehilt_build_tip_select", "Select pieces to copy, move, tear down or save as a blueprint.");
        Translations.AddEnglish("whitehilt_build_tip_blueprints", "Your saved blueprints, with what they cost.");
        Translations.AddEnglish("whitehilt_build_tip_line", "Places a row of the held piece between two points, or fills an area with floors.");
        Translations.AddEnglish("whitehilt_build_tip_paste", "Places what you copied.");
        Translations.AddEnglish("whitehilt_group_hint_select",
            "Selected: {0}   Click a piece: add or remove   Shift+click two corners: box (height {1} m, PgUp/PgDn)\n[{2}] copy   [{3}] move   [{4}] tear down   [{5}] save blueprint   [{6}] clear   Right mouse: done");
        Translations.AddEnglish("whitehilt_group_hint_paste",
            "{0}: {1} pieces   Click: place   Wheel: turn   Arrows, PgUp/PgDn: shift   Right mouse: stop\n{2}");
        Translations.AddEnglish("whitehilt_group_hint_line",
            "{0}: {1}   Click: {2}   Wheel: turn   [{3}] line / area / off   Right mouse: back\n{4}");
        Translations.AddEnglish("whitehilt_group_line_start", "set the start");
        Translations.AddEnglish("whitehilt_group_line_place", "place {0} pieces");
        Translations.AddEnglish("whitehilt_group_ready", "Ready");
        Translations.AddEnglish("whitehilt_group_missing", "Missing: {0}");
        Translations.AddEnglish("whitehilt_group_moving", "Moving");
        Translations.AddEnglish("whitehilt_group_copy", "Copy");
        Translations.AddEnglish("whitehilt_group_unknown", "{0} unknown pieces are skipped");
        Translations.AddEnglish("whitehilt_group_pieces", "{0} pieces");
        Translations.AddEnglish("whitehilt_group_search", "Search");
        Translations.AddEnglish("whitehilt_group_use", "Place");
        Translations.AddEnglish("whitehilt_group_rename", "Rename");
        Translations.AddEnglish("whitehilt_group_close", "Close");
        Translations.AddEnglish("whitehilt_group_none", "No blueprints yet. Select pieces and save them, or put .blueprint and .vbuild files in the blueprint folder.");
        Translations.AddEnglish("whitehilt_group_name", "Blueprint name");
        Translations.AddEnglish("msg_whitehilt_group_disabled", "Group building is switched off on this server");
        Translations.AddEnglish("msg_whitehilt_group_nothing", "Nothing selected");
        Translations.AddEnglish("msg_whitehilt_group_copied", "{0} pieces copied");
        Translations.AddEnglish("msg_whitehilt_group_saved", "Blueprint saved: {0}");
        Translations.AddEnglish("msg_whitehilt_group_removed", "{0} pieces torn down");
        Translations.AddEnglish("msg_whitehilt_group_skipped", "{0} pieces could not be torn down (ward, station or contents)");
        Translations.AddEnglish("msg_whitehilt_group_blocked", "Empty chests and stands first, and check wards and stations: {0} pieces cannot be moved");
        Translations.AddEnglish("msg_whitehilt_group_too_many", "Too many pieces: {0} (the server allows {1})");
        Translations.AddEnglish("msg_whitehilt_group_unknown_piece", "You do not know how to build {0}");
        Translations.AddEnglish("msg_whitehilt_group_no_piece", "Pick a piece to build first");
    }
}
