using Jotunn.Entities;
using Jotunn.Managers;
using System.Collections.Generic;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// The console command whitehilt_storm: status for everyone, start, warn and stop for admins.
/// </summary>
public static class StormCommands
{
    /// <summary>
    /// Registers the command. Call from the plugin's Awake.
    /// </summary>
    public static void Register()
    {
        CommandManager.Instance.AddConsoleCommand(new StormCommand());
    }

    private sealed class StormCommand : ConsoleCommand
    {
        public override string Name => "whitehilt_storm";

        public override string Help => "[status|start|warn|stop] Shows the storm; admins start one now, warn of one or stop it.";

        public override void Run(string[] args)
        {
            string command = args.Length > 0 ? args[0].ToLowerInvariant() : "status";
            if (command == "status")
            {
                Print($"Storm: {StormState.Phase}, {StormState.Countdown(StormState.SecondsLeft())} left (storm {StormState.Number}). "
                    + $"Here: {StormClient.Profile.Kind}, {StormClient.Cover}, exposure {StormClient.Exposure:0}%.");
                return;
            }

            StormService.SendAdmin(command);
        }

        public override List<string> CommandOptionList()
        {
            return new List<string> { "status", "start", "warn", "stop" };
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
