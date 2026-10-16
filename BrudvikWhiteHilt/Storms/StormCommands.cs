using Jotunn.Entities;
using Jotunn.Managers;
using System.Collections.Generic;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// The console command whitehilt_storm: status for everyone, start, ragnarok, warn and stop for admins.
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

        public override string Help => "[status|start|ragnarok|warn|stop] Shows the storm; admins start one now (or Ragnarok), warn of one or stop it.";

        public override void Run(string[] args)
        {
            string command = args.Length > 0 ? args[0].ToLowerInvariant() : "status";
            if (command == "status")
            {
                string ragnarok = StormState.Ragnarok ? ", Ragnarok" : string.Empty;
                Print($"Storm: {StormState.Phase}{ragnarok}, {StormState.Wave} wave, {StormState.Countdown(StormState.SecondsLeft())} left (storm {StormState.Number}). "
                    + $"Here: {StormClient.Profile.Kind}, {StormClient.Cover}, exposure {StormClient.Exposure:0}%, fury {StormClient.Fury:0.##}, "
                    + $"grudge {StormGrudge.Of(Player.m_localPlayer)}.");
                return;
            }

            StormService.SendAdmin(command);
        }

        public override List<string> CommandOptionList()
        {
            return new List<string> { "status", "start", "ragnarok", "warn", "stop" };
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
