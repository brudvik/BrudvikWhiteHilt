using Jotunn.Entities;
using Jotunn.Managers;
using System.Collections.Generic;

namespace BrudvikWhiteHilt.Darkness;

/// <summary>
/// The console command whitehilt_darkness: status for everyone, start and stop for admins, in the place below ground
/// the player is in.
/// </summary>
public static class DarknessCommands
{
    /// <summary>
    /// Registers the command. Call from the plugin's Awake.
    /// </summary>
    public static void Register()
    {
        CommandManager.Instance.AddConsoleCommand(new DarknessCommand());
    }

    private sealed class DarknessCommand : ConsoleCommand
    {
        public override string Name => "whitehilt_darkness";

        public override string Help => "[status|start|stop] Shows the darkness here; admins bring it now or lift it.";

        public override void Run(string[] args)
        {
            string command = args.Length > 0 ? args[0].ToLowerInvariant() : "status";
            if (command == "status")
            {
                string place = DarknessClient.PlaceKey ?? "none (not below ground)";
                Print($"Darkness: place {place}, {DarknessClient.Phase}, darkness {DarknessClient.Level:0.##}, "
                    + $"{(DarknessClient.Lit ? "lit" : "unlit")}, fear {DarknessClient.Fear:0}%.");
                return;
            }

            DarknessService.SendAdmin(command, DarknessClient.PlaceKey);
        }

        public override List<string> CommandOptionList()
        {
            return new List<string> { "status", "start", "stop" };
        }

        private static void Print(string message)
        {
            if (global::Console.instance != null)
            {
                global::Console.instance.Print(message);
            }
        }
    }
}
