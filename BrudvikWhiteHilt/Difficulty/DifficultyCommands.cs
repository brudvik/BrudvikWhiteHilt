using BrudvikWhiteHilt.Difficulty.Beasts;
using Jotunn.Entities;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;

namespace BrudvikWhiteHilt.Difficulty;

/// <summary>
/// Console commands: whitehilt_difficulty shows the state; whitehilt_bloodmoon and whitehilt_beast are for admins.
/// </summary>
public static class DifficultyCommands
{
    /// <summary>
    /// Registers the commands. Call from the plugin's Awake.
    /// </summary>
    public static void Register()
    {
        CommandManager.Instance.AddConsoleCommand(new DifficultyCommand());
        CommandManager.Instance.AddConsoleCommand(new BloodMoonCommand());
        CommandManager.Instance.AddConsoleCommand(new BeastCommand());
    }

    private static void Print(string message)
    {
        if (global::Console.instance != null)
        {
            global::Console.instance.Print(message);
        }
    }

    private sealed class DifficultyCommand : ConsoleCommand
    {
        public override string Name => "whitehilt_difficulty";

        public override string Help => "Shows the White Hilt difficulty: pressure, what it is made of and the blood moon.";

        public override void Run(string[] args)
        {
            Print(DifficultyState.Describe());
        }
    }

    private sealed class BloodMoonCommand : ConsoleCommand
    {
        public override string Name => "whitehilt_bloodmoon";

        public override string Help => "[start|stop] Admin: starts a blood moon tonight, or stops it.";

        public override void Run(string[] args)
        {
            DifficultyService.SendAdmin("bloodmoon", args.Length > 0 ? args[0] : "start");
        }

        public override List<string> CommandOptionList()
        {
            return new List<string> { "start", "stop" };
        }
    }

    private sealed class BeastCommand : ConsoleCommand
    {
        public override string Name => "whitehilt_beast";

        public override string Help => "[beast or biome] Admin: a beast comes for you now. Without a name, the beast of where you are.";

        public override void Run(string[] args)
        {
            string key = args.Length > 0 ? args[0] : CurrentBeast();
            if (string.IsNullOrEmpty(key))
            {
                Print("No beast comes here. Name one: " + string.Join(", ", BeastDefinition.All.Select(beast => beast.Key)));
                return;
            }

            DifficultyService.SendAdmin("beast", key);
        }

        public override List<string> CommandOptionList()
        {
            return BeastDefinition.All.Select(beast => beast.Key).ToList();
        }

        private static string CurrentBeast()
        {
            Player player = Player.m_localPlayer;
            return player == null ? null : BeastDefinition.Resolve(player.GetCurrentBiome(), Ship.GetLocalShip() != null, true)?.Key;
        }
    }
}
