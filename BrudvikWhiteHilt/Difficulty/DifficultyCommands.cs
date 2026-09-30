using Jotunn.Entities;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Difficulty;

/// <summary>
/// Console command whitehilt_difficulty, which shows the state.
/// </summary>
public static class DifficultyCommands
{
    /// <summary>
    /// Registers the commands. Call from the plugin's Awake.
    /// </summary>
    public static void Register()
    {
        CommandManager.Instance.AddConsoleCommand(new DifficultyCommand());
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

        public override string Help => "Shows the White Hilt difficulty: the pressure and what it is made of.";

        public override void Run(string[] args)
        {
            Print(DifficultyState.Describe());
        }
    }
}
