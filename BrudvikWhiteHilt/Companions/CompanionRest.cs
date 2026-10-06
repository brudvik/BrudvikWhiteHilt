using Jotunn.Entities;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Companions;

/// <summary>
/// Adds the whitehilt_rest test command, which lays the nearest dog down.
/// </summary>
public static class CompanionRest
{
    private const float CommandRange = 10f;

    /// <summary>
    /// Registers the command. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        CommandManager.Instance.AddConsoleCommand(new RestCommand());
    }

    private static RestPose FindNearestTamed(Vector3 position)
    {
        RestPose nearest = null;
        float nearestDistance = CommandRange;
        foreach (Character creature in Character.GetAllCharacters())
        {
            float distance = Vector3.Distance(position, creature.transform.position);
            if (creature.IsTamed() && distance < nearestDistance && creature.TryGetComponent(out RestPose rest))
            {
                nearest = rest;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private sealed class RestCommand : ConsoleCommand
    {
        public override string Name => "whitehilt_rest";

        public override string Help => "[lie|sleep|sit|off] Lays the nearest dog down, to try the resting pose.";

        public override bool IsCheat => true;

        /// <summary>
        /// Makes the nearest tame dog lie, sit, sleep, or stand up again; a console command.
        /// </summary>
        public override void Run(string[] args)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            RestPose rest = FindNearestTamed(player.transform.position);
            if (rest == null)
            {
                global::Console.instance?.Print($"No dog within {CommandRange} m.");
                return;
            }

            RestPose.Pose pose = (args.Length > 0 ? args[0].ToLowerInvariant() : "lie") switch
            {
                "sleep" => RestPose.Pose.Sleep,
                "sit" => RestPose.Pose.Sit,
                "off" => RestPose.Pose.None,
                _ => RestPose.Pose.Lie
            };

            rest.GetComponent<ZNetView>().ClaimOwnership();
            if (pose != RestPose.Pose.None)
            {
                rest.GetComponent<MonsterAI>()?.SetFollowTarget(null);
            }

            rest.SetPose(pose);
        }

        public override List<string> CommandOptionList()
        {
            return new List<string> { "lie", "sleep", "sit", "off" };
        }
    }
}
