using BrudvikWhiteHilt.SeaBattles.Raids;
using Jotunn.Entities;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles;

/// <summary>
/// Console command whitehilt_seabattle: shows the conditions for a Nail Ship and the ones that are loaded, or (admins)
/// asks the server to send one after the ship the local player is aboard, or to make a draugr harbour on the coast
/// nearest the player. The services on the server do the work, so the same checks and the same ship come as in a natural
/// encounter, only without the roll and the cooldown. With cheats on, <c>hole</c> rams the ship the player stands on as a
/// ram would, to try flooding and patching without a fight.
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

        public override string Help => "[status|summon|raid|hole [count]] Shows the conditions for a Nail Ship; admins can send one after their ship or make a draugr harbour on the nearest coast; with cheats on, hole knocks holes in the ship you stand on.";

        public override void Run(string[] args)
        {
            if (args.Length > 0 && args[0] == "hole")
            {
                MakeHoles(args.Length > 1 && int.TryParse(args[1], out int count) ? count : 1);
                return;
            }

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
            return new List<string> { "status", "summon", "raid", "hole" };
        }

        // A blow of the kind a ram gives, sent to the ship's hull as the game sends any hit, so the owner makes the holes
        // through the same hook as in a fight. The blow does next to no damage.
        private static void MakeHoles(int count)
        {
            global::Console console = global::Console.instance;
            if (console == null || !console.IsCheatsEnabled())
            {
                console?.Print("Holes need cheats: run devcommands first.");
                return;
            }

            Player player = Player.m_localPlayer;
            Ship ship = player != null ? player.GetStandingOnShip() ?? Ship.GetLocalShip() : null;
            WearNTear hull = ship != null ? ship.GetComponent<WearNTear>() : null;
            if (hull == null)
            {
                console.Print("Stand on a ship first.");
                return;
            }

            count = Mathf.Clamp(count, 1, 20);
            for (int i = 0; i < count; i++)
            {
                float side = i % 2 == 0 ? 1f : -1f;
                HitData hit = new()
                {
                    m_hitType = HitData.HitType.Boat,
                    m_point = ship.transform.TransformPoint(new Vector3(side * 3f, 0.3f, (i - (count - 1) / 2f) * 2.5f)),
                    m_dir = -ship.transform.right * side
                };
                hit.m_damage.m_blunt = 1f;
                hull.Damage(hit);
            }
        }
    }
}
