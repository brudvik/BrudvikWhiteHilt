using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using System.Collections.Generic;
using System.Linq;

namespace BrudvikWhiteHilt.Pieces.Trophies;

/// <summary>
/// Config for the Trophy Altar, section "TrophyAltar". Server-synced.
/// </summary>
public static class TrophyAltarSettings
{
    /// <summary>The vanilla boss trophies, the default list.</summary>
    public const string BossTrophies = "TrophyEikthyr,TrophyTheElder,TrophyBonemass,TrophyDragonQueen,TrophyGoblinKing,TrophySeekerQueen,TrophyFader";

    private const string Section = "TrophyAltar";

    /// <summary>Trophy prefab names the altar can copy, comma separated.</summary>
    public static ConfigEntry<string> Trophies { get; private set; }

    /// <summary>Trophies one copy takes.</summary>
    public static ConfigEntry<int> TrophiesPerCraft { get; private set; }

    /// <summary>Swamp Keys one copy takes.</summary>
    public static ConfigEntry<int> KeysPerCraft { get; private set; }

    /// <summary>Trophies one copy gives; 0 gives a full stack.</summary>
    public static ConfigEntry<int> TrophiesMade { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Trophies = WhiteHiltConfig.BindAdminOnly(Section, "Trophies", BossTrophies,
            "Trophy prefab names the altar can copy, comma separated. A name added here needs a restart.");
        TrophiesPerCraft = WhiteHiltConfig.BindAdminOnly(Section, "TrophiesPerCraft", 1, "Trophies one copy takes.",
            new AcceptableValueRange<int>(1, 20));
        KeysPerCraft = WhiteHiltConfig.BindAdminOnly(Section, "KeysPerCraft", 1, "Swamp Keys one copy takes.",
            new AcceptableValueRange<int>(1, 10));
        TrophiesMade = WhiteHiltConfig.BindAdminOnly(Section, "TrophiesMade", 0, "Trophies one copy gives. 0: a full stack.",
            new AcceptableValueRange<int>(0, 100));
    }

    /// <summary>
    /// The trophy prefab names in <see cref="Trophies"/>.
    /// </summary>
    /// <returns>Prefab names, without duplicates.</returns>
    public static IReadOnlyList<string> TrophyNames()
    {
        return (Trophies.Value ?? string.Empty).Split(',')
            .Select(name => name.Trim())
            .Where(name => name.Length > 0)
            .Distinct()
            .ToList();
    }
}
