using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using Jotunn.Entities;
using Jotunn.Managers;
using System.Collections.Generic;

namespace BrudvikWhiteHilt.Navigation.Portraits;

/// <summary>
/// Config for player portraits on the map, section "Map", and the console command whitehilt_portrait.
/// </summary>
public static class PortraitSettings
{
    private const string Section = "Map";

    /// <summary>Whether other players are shown as portraits, and whether your own portrait is taken and shared.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>Whether names are shown under the portraits on the minimap too, not only on the large map.</summary>
    public static ConfigEntry<bool> ShowNamesOnMinimap { get; private set; }

    /// <summary>Whether a ring with a point around the portraits shows which way each player is heading.</summary>
    public static ConfigEntry<bool> HeadingMarker { get; private set; }

    /// <summary>
    /// Binds the config entries and registers the console command. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindLocal(Section, "PlayerPortraits", true,
            "Show other players on the map as a portrait of their Viking with the name under it. Your own portrait is taken in the main menu, once per look, and sent to the others when you join.");
        ShowNamesOnMinimap = WhiteHiltConfig.BindLocal(Section, "ShowNamesOnMinimap", false,
            "Show the names under the portraits on the minimap too. On the large map they are always shown.");
        HeadingMarker = WhiteHiltConfig.BindLocal(Section, "HeadingMarker", true,
            "A ring around the portraits with a point that circles the edge: yours points where you look, a nearby player's where they face.");
        Enabled.SettingChanged += (_, _) => PortraitPins.Refresh();
        ShowNamesOnMinimap.SettingChanged += (_, _) => PortraitPins.Refresh();

        CommandManager.Instance.AddConsoleCommand(new PortraitCommand());
    }

    private sealed class PortraitCommand : ConsoleCommand
    {
        public override string Name => "whitehilt_portrait";

        public override string Help => "[status|test] Shows which players' portraits are known, or toggles a test pin with your own portrait 20 m east of you.";

        public override void Run(string[] args)
        {
            string message = args.Length > 0 && args[0] == "test" ? PortraitPins.ToggleTestPin() : PortraitNetwork.Describe();
            global::Console.instance?.Print(message);
        }

        public override List<string> CommandOptionList()
        {
            return new List<string> { "status", "test" };
        }
    }
}
