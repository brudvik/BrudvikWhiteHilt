using Jotunn.Entities;
using Jotunn.Managers;
using System.Collections.Generic;

namespace BrudvikWhiteHilt.Kraken;

/// <summary>
/// Console command whitehilt_kraken: shows the conditions, or (admins) raises the Kraken beside your ship.
/// </summary>
public static class KrakenCommands
{
    /// <summary>
    /// Registers the command. Call from the plugin's Awake.
    /// </summary>
    public static void Register()
    {
        CommandManager.Instance.AddConsoleCommand(new KrakenCommand());
    }

    private sealed class KrakenCommand : ConsoleCommand
    {
        public override string Name => "whitehilt_kraken";

        public override string Help => "[status|summon] Shows whether the Kraken can come here; admins can summon it beside their ship.";

        public override void Run(string[] args)
        {
            if (args.Length > 0 && args[0] == "summon")
            {
                KrakenService.Summon();
                return;
            }

            global::Console.instance?.Print(KrakenService.Describe());
        }

        public override List<string> CommandOptionList()
        {
            return new List<string> { "status", "summon" };
        }
    }
}
