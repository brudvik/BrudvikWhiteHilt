using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using System;
using System.Linq;

namespace BrudvikWhiteHilt.Building.Doors;

/// <summary>
/// What kind of opening a door piece is; each kind has its own closing rules.
/// </summary>
public enum DoorKind
{
    /// <summary>A door to walk through.</summary>
    Door,

    /// <summary>A gate or grate.</summary>
    Gate,

    /// <summary>A window shutter.</summary>
    Window
}

/// <summary>
/// Config for doors, gates and windows that close on their own, section "Doors". Server-synced, since the machine that
/// owns a door closes it.
/// </summary>
public static class AutoDoorSettings
{
    private const string Section = "Doors";
    private const string DrawbridgePrefab = "piece_whitehilt_vindebro";

    private static readonly NameSet excluded = new(exact: true);
    private static readonly NameSet gateNames = new(exact: false);
    private static readonly NameSet windowNames = new(exact: false);

    /// <summary>Whether doors, gates and windows close on their own at all.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>Whether doors close on their own.</summary>
    public static ConfigEntry<bool> CloseDoors { get; private set; }

    /// <summary>Seconds a door stays open after the last one went through.</summary>
    public static ConfigEntry<float> DoorDelaySeconds { get; private set; }

    /// <summary>Whether gates and grates close on their own.</summary>
    public static ConfigEntry<bool> CloseGates { get; private set; }

    /// <summary>Seconds a gate stays open after the last one went through.</summary>
    public static ConfigEntry<float> GateDelaySeconds { get; private set; }

    /// <summary>Whether window shutters close on their own after a while.</summary>
    public static ConfigEntry<bool> CloseWindows { get; private set; }

    /// <summary>Seconds a window stays open after the last one stood by it.</summary>
    public static ConfigEntry<float> WindowDelaySeconds { get; private set; }

    /// <summary>Nothing closes while a player is this many metres from it or closer.</summary>
    public static ConfigEntry<float> ClearRadius { get; private set; }

    /// <summary>Whether tamed animals in the opening also keep it open.</summary>
    public static ConfigEntry<bool> TamesBlockClosing { get; private set; }

    /// <summary>Whether only doors built by players close, not those in dungeons and other places.</summary>
    public static ConfigEntry<bool> OnlyPlayerBuilt { get; private set; }

    /// <summary>Whether Shift + Use holds a door open.</summary>
    public static ConfigEntry<bool> AllowHoldOpen { get; private set; }

    /// <summary>Prefab names that never close on their own, comma separated.</summary>
    public static ConfigEntry<string> ExcludedPieces { get; private set; }

    /// <summary>Parts of prefab names that make a door a gate, comma separated.</summary>
    public static ConfigEntry<string> GateNames { get; private set; }

    /// <summary>Parts of prefab names that make a door a window, comma separated.</summary>
    public static ConfigEntry<string> WindowNames { get; private set; }

    /// <summary>Whether open windows close when rain or a storm begins.</summary>
    public static ConfigEntry<bool> WindowsCloseInRain { get; private set; }

    /// <summary>Whether open windows close when night falls.</summary>
    public static ConfigEntry<bool> WindowsCloseAtNight { get; private set; }

    /// <summary>Whether everything closes when enemies come for the base.</summary>
    public static ConfigEntry<bool> RaidClose { get; private set; }

    /// <summary>How near, in metres, an enemy must come before doors close.</summary>
    public static ConfigEntry<float> RaidRadius { get; private set; }

    /// <summary>Whether enemies also close doors that are held open.</summary>
    public static ConfigEntry<bool> RaidIgnoresHoldOpen { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true,
            "Doors, gates and windows built by players close on their own. Doors with a key and doors that cannot be closed are left alone.");
        CloseDoors = WhiteHiltConfig.BindAdminOnly(Section, "CloseDoors", true, "Doors close on their own once nobody is in the doorway.");
        DoorDelaySeconds = WhiteHiltConfig.BindAdminOnly(Section, "DoorDelaySeconds", 5f,
            "Seconds a door stays open after the last one went through.", new AcceptableValueRange<float>(1f, 600f));
        CloseGates = WhiteHiltConfig.BindAdminOnly(Section, "CloseGates", true, "Gates and grates close on their own once nobody is in the opening.");
        GateDelaySeconds = WhiteHiltConfig.BindAdminOnly(Section, "GateDelaySeconds", 10f,
            "Seconds a gate stays open after the last one went through.", new AcceptableValueRange<float>(1f, 600f));
        CloseWindows = WhiteHiltConfig.BindAdminOnly(Section, "CloseWindows", false,
            "Window shutters close on their own once nobody stands by them. Rain, night and enemies close them even when this is off.");
        WindowDelaySeconds = WhiteHiltConfig.BindAdminOnly(Section, "WindowDelaySeconds", 30f,
            "Seconds a window stays open after the last one stood by it.", new AcceptableValueRange<float>(1f, 600f));
        ClearRadius = WhiteHiltConfig.BindAdminOnly(Section, "ClearRadius", 3f,
            "Nothing closes while a player is this many metres from it or closer, so a door never shuts in anyone's face.",
            new AcceptableValueRange<float>(0.5f, 10f));
        TamesBlockClosing = WhiteHiltConfig.BindAdminOnly(Section, "TamesBlockClosing", true,
            "Tamed animals in the opening keep it open too, so a pen gate does not close on an animal walking through.");
        OnlyPlayerBuilt = WhiteHiltConfig.BindAdminOnly(Section, "OnlyPlayerBuilt", true,
            "Only doors built by players close on their own, not those in dungeons, villages and other places in the world.");
        AllowHoldOpen = WhiteHiltConfig.BindAdminOnly(Section, "AllowHoldOpen", true,
            "Shift + Use opens a door and holds it open until someone closes it. Shift + Use again lets it close on its own again.");
        ExcludedPieces = WhiteHiltConfig.BindAdminOnly(Section, "ExcludedPieces", string.Empty,
            "Prefab names of doors that never close on their own, comma separated, e.g. wood_door,iron_grate.");
        GateNames = WhiteHiltConfig.BindAdminOnly(Section, "GateNames", "gate,grate,skanseport",
            "A door whose prefab name contains one of these words counts as a gate, comma separated.");
        WindowNames = WhiteHiltConfig.BindAdminOnly(Section, "WindowNames", "window",
            "A door whose prefab name contains one of these words counts as a window, comma separated.");
        WindowsCloseInRain = WhiteHiltConfig.BindAdminOnly(Section, "WindowsCloseInRain", true,
            "Open windows close when rain or a storm begins. They can be opened again while it rains.");
        WindowsCloseAtNight = WhiteHiltConfig.BindAdminOnly(Section, "WindowsCloseAtNight", true,
            "Open windows close when night falls. They can be opened again during the night.");
        RaidClose = WhiteHiltConfig.BindAdminOnly(Section, "RaidClose", true,
            "Doors, gates and windows close at once when enemies on the hunt come near, as long as no player is in the opening.");
        RaidRadius = WhiteHiltConfig.BindAdminOnly(Section, "RaidRadius", 20f,
            "How near, in metres, an enemy must come before everything closes.", new AcceptableValueRange<float>(5f, 60f));
        RaidIgnoresHoldOpen = WhiteHiltConfig.BindAdminOnly(Section, "RaidIgnoresHoldOpen", true,
            "Enemies also close doors that are held open with Shift + Use.");

        Translations.AddEnglish("whitehilt_door_hold", "Hold open");
        Translations.AddEnglish("whitehilt_door_release", "Let it close on its own");
        Translations.AddEnglish("whitehilt_door_held", "Held open");
    }

    /// <summary>
    /// Tells a door, a gate and a window apart by the prefab name.
    /// </summary>
    /// <param name="prefabName">Prefab name of the door piece.</param>
    /// <returns>Its kind.</returns>
    public static DoorKind Classify(string prefabName)
    {
        // The log walls with shuttered windows are named in Norwegian (piece_whitehilt_laftvegg_vindu).
        if (windowNames.Matches(WindowNames.Value, prefabName) || (prefabName.StartsWith("piece_whitehilt_") && prefabName.EndsWith("_vindu")))
        {
            return DoorKind.Window;
        }

        // A drawbridge is raised and lowered like a gate.
        if (prefabName == DrawbridgePrefab || prefabName == "piece_drawbridge")
        {
            return DoorKind.Gate;
        }

        return gateNames.Matches(GateNames.Value, prefabName) ? DoorKind.Gate : DoorKind.Door;
    }

    /// <summary>
    /// True if the door never closes on its own.
    /// </summary>
    /// <param name="prefabName">Prefab name of the door piece.</param>
    /// <returns>True to leave it alone.</returns>
    public static bool IsExcluded(string prefabName)
    {
        return excluded.Matches(ExcludedPieces.Value, prefabName);
    }

    /// <summary>
    /// Whether this kind closes a while after the last one went through.
    /// </summary>
    /// <param name="kind">The kind.</param>
    /// <returns>True if it closes after a delay.</returns>
    public static bool ClosesByTimer(DoorKind kind)
    {
        return kind switch
        {
            DoorKind.Gate => CloseGates.Value,
            DoorKind.Window => CloseWindows.Value,
            _ => CloseDoors.Value
        };
    }

    /// <summary>
    /// Seconds this kind stays open after the last one went through.
    /// </summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The delay.</returns>
    public static float Delay(DoorKind kind)
    {
        return kind switch
        {
            DoorKind.Gate => GateDelaySeconds.Value,
            DoorKind.Window => WindowDelaySeconds.Value,
            _ => DoorDelaySeconds.Value
        };
    }

    /// <summary>
    /// True while the weather or the night closes windows.
    /// </summary>
    /// <returns>True in rain or at night, as set.</returns>
    public static bool IsWindowWeather()
    {
        if (EnvMan.instance == null)
        {
            return false;
        }

        return (WindowsCloseInRain.Value && EnvMan.IsWet()) || (WindowsCloseAtNight.Value && EnvMan.IsNight());
    }

    /// <summary>
    /// A comma-separated list of names from the config, parsed again only when the text changes.
    /// </summary>
    private sealed class NameSet
    {
        private readonly bool exact;
        private string text;
        private string[] names = Array.Empty<string>();

        public NameSet(bool exact)
        {
            this.exact = exact;
        }

        public bool Matches(string configText, string prefabName)
        {
            configText ??= string.Empty;
            if (configText != text)
            {
                text = configText;
                names = configText.Split(',').Select(name => name.Trim().ToLowerInvariant()).Where(name => name.Length > 0).ToArray();
            }

            if (string.IsNullOrEmpty(prefabName))
            {
                return false;
            }

            string lower = prefabName.ToLowerInvariant();
            return exact ? names.Contains(lower) : names.Any(name => lower.Contains(name));
        }
    }
}
