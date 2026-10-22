using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Household;

/// <summary>
/// Config for the household (docs/plans/household.md, section 3): whether it is on, how far a High Seat reaches and
/// how many people it holds. The settings of later phases (wages, meals, wounds) are added with them.
/// </summary>
public static class HouseholdSettings
{
    private const string Section = "Household";

    /// <summary>Whether the High Seat can be built. Changing it needs a restart.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>How far a High Seat reaches, in metres: its farm.</summary>
    public static ConfigEntry<float> FarmRadius { get; private set; }

    /// <summary>The most people a High Seat holds.</summary>
    public static ConfigEntry<int> MaxPerFarm { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake, before the pieces are made.
    /// </summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true,
            "Whether the High Seat can be built, and with it a household of farmhands and huskarls. Needs a restart.");
        FarmRadius = WhiteHiltConfig.BindAdminOnly(Section, "FarmRadius", 80f,
            "How far a High Seat reaches, in metres: the household works, sleeps and keeps watch within it, and no other "
            + "High Seat may stand within it.", new AcceptableValueRange<float>(15f, 200f));
        MaxPerFarm = WhiteHiltConfig.BindAdminOnly(Section, "MaxPerFarm", 6, "The most people a High Seat holds.",
            new AcceptableValueRange<int>(1, 12));
    }
}
