using BrudvikWhiteHilt.SeaBattles.Raids;
using Jotunn.Entities;
using Jotunn.Managers;
using System.Collections.Generic;

namespace BrudvikWhiteHilt.SeaBattles;

/// <summary>
/// Console command whitehilt_seabattle: shows the conditions for a Nail Ship and the ones that are loaded, or (admins)
/// asks the server to send one after the ship the local player is aboard, or to make a draugr harbour on the coast
/// nearest the player. The services on the server do the work, so the same checks and the same ship come as in a natural
/// encounter, only without the roll and the cooldown.
/// </summary>
public static class SeaBattleCommands
{
    /// <summary>
    /// Registers the command. Call from the plugin's Awake.
    /// </summary>
    public static void Register()
    {
        CommandManager.Instance.AddConsoleCommand(new SeaBattleCommand());
    }

    private sealed class SeaBattleCommand : ConsoleCommand
    {
        public override string Name => "whitehilt_seabattle";

        public override string Help => "[status|summon|raid] Shows the conditions for a Nail Ship; admins can send one after their ship or make a draugr harbour on the nearest coast.";

        public override void Run(string[] args)
        {
            if (args.Length > 0 && args[0] == "summon")
            {
                SeaBattleService.Summon();
                return;
            }

            if (args.Length > 0 && args[0] == "raid")
            {
                RaidService.RequestAdmin();
                return;
            }

            global::Console.instance?.Print(SeaBattleService.Describe() + "\n" + RaiderShip.Describe());
        }

        public override List<string> CommandOptionList()
        {
            return new List<string> { "status", "summon", "raid" };
        }
    }
}
