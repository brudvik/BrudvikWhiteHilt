using Jotunn.Entities;
using Jotunn.Managers;
using System.Collections.Generic;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// Console command whitehilt_lindorm: shows the conditions, or (admins) lets the Lindorm break out near you.
/// </summary>
public static class MonsterCommands
{
    /// <summary>
    /// Registers the command. Call from the plugin's Awake.
    /// </summary>
    public static void Register()
    {
        CommandManager.Instance.AddConsoleCommand(new LindormCommand());
    }

    private sealed class LindormCommand : ConsoleCommand
    {
        public override string Name => "whitehilt_lindorm";

        public override string Help => "[status|summon] Shows whether the Lindorm can come here; admins can summon it nearby.";

        public override void Run(string[] args)
        {
            if (args.Length > 0 && args[0] == "summon")
            {
                LindormService.Summon();
                return;
            }

            global::Console.instance?.Print(LindormService.Describe());
        }

        public override List<string> CommandOptionList()
        {
            return new List<string> { "status", "summon" };
        }
    }
}
