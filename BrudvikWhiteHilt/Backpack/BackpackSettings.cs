using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Backpack;

/// <summary>
/// Config for the backpack, section "Backpack". The rules are server-synced; keys are the player's own.
/// </summary>
public static class BackpackSettings
{
    private const string Section = "Backpack";

    /// <summary>Rows added to every player's inventory, on top of the vanilla rows.</summary>
    public static ConfigEntry<int> ExtraRows { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        ExtraRows = WhiteHiltConfig.BindAdminOnly(Section, "ExtraRows", 1,
            "Rows added to every player's inventory, on top of the rows the game gives (4, or more when bought from a trader).",
            new AcceptableValueRange<int>(0, 2));
    }
}
