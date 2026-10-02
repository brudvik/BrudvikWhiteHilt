using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Navigation;

/// <summary>
/// Config for the Exploration skill and what it does, section "Navigation". All of it is server-synced.
/// </summary>
public static class NavigationSettings
{
    private const string Section = "Navigation";

    /// <summary>Whether the Navigator's Table and the Pathfinder's Amulet widen the circle the map uncovers.</summary>
    public static ConfigEntry<bool> ExploreRadiusBonus { get; private set; }

    /// <summary>Extra radius from the Navigator's Table at Exploration 100, as a share of the vanilla radius.</summary>
    public static ConfigEntry<float> TableBonus { get; private set; }

    /// <summary>Extra radius from the Pathfinder's Amulet at Exploration 100, as a share of the vanilla radius.</summary>
    public static ConfigEntry<float> AmuletBonus { get; private set; }

    /// <summary>Share of the extra radius already given at Exploration 0.</summary>
    public static ConfigEntry<float> BonusAtLevelZero { get; private set; }

    /// <summary>Exploration experience per square metre of newly uncovered map.</summary>
    public static ConfigEntry<float> SkillPerSquareMetre { get; private set; }

    /// <summary>Whether map shared by others is drawn as the player's own from <see cref="SharedMapRevealLevel"/>.</summary>
    public static ConfigEntry<bool> RevealSharedMap { get; private set; }

    /// <summary>Exploration level from which shared map is drawn as the player's own.</summary>
    public static ConfigEntry<int> SharedMapRevealLevel { get; private set; }

    /// <summary>Radius in metres that the Pathfinder's Amulet's Raven Sight uncovers.</summary>
    public static ConfigEntry<float> RavenSightRadius { get; private set; }

    /// <summary>How close the Cartographer's Desk, Portal Astrolabe, Harbour Anchor and Munin's Perch must stand to a map table, in metres.</summary>
    public static ConfigEntry<float> MapTableRange { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake, before <see cref="ExplorationSkill.Register"/>.
    /// </summary>
    public static void Initialize()
    {
        ExploreRadiusBonus = WhiteHiltConfig.BindAdminOnly(Section, "ExploreRadiusBonus", true,
            "The Navigator's Table and the Pathfinder's Amulet widen the circle the map uncovers around you, more with the Exploration skill.");
        TableBonus = WhiteHiltConfig.BindAdminOnly(Section, "TableBonus", 2f,
            "Extra explore radius aboard a ship with a Navigator's Table at Exploration 100, as a share of the vanilla 100 m (2 = 300 m).",
            new AcceptableValueRange<float>(0f, 10f));
        AmuletBonus = WhiteHiltConfig.BindAdminOnly(Section, "AmuletBonus", 1f,
            "Extra explore radius while wearing the Pathfinder's Amulet at Exploration 100, as a share of the vanilla 100 m (1 = 200 m).",
            new AcceptableValueRange<float>(0f, 10f));
        BonusAtLevelZero = WhiteHiltConfig.BindAdminOnly(Section, "BonusAtLevelZero", 0.2f,
            "Share of the extra explore radius already given at Exploration 0; the rest comes with the skill.",
            new AcceptableValueRange<float>(0f, 1f));
        SkillPerSquareMetre = WhiteHiltConfig.BindAdminOnly(Section, "SkillPerSquareMetre", 0.0005f,
            "Exploration experience per square metre of newly uncovered map. The default takes about 40 km² of new map to reach level 100.",
            new AcceptableValueRange<float>(0f, 0.01f));
        RevealSharedMap = WhiteHiltConfig.BindAdminOnly(Section, "SharedMapReveal", true,
            "From SharedMapRevealLevel in Exploration, map shared by others through map tables is drawn like your own, without the see-through layer. Saved map data is not changed.");
        SharedMapRevealLevel = WhiteHiltConfig.BindAdminOnly(Section, "SharedMapRevealLevel", 50,
            "Exploration level from which shared map is drawn like your own.", new AcceptableValueRange<int>(0, 100));
        RavenSightRadius = WhiteHiltConfig.BindAdminOnly(Section, "RavenSightRadius", 500f,
            "Radius in metres that the Pathfinder's Amulet's Raven Sight uncovers.", new AcceptableValueRange<float>(0f, 3000f));
        MapTableRange = WhiteHiltConfig.BindAdminOnly(Section, "MapTableRange", 5f,
            "How close, in metres, the Cartographer's Desk, a Portal Astrolabe, a Harbour Anchor or Munin's Perch must stand to a map table.",
            new AcceptableValueRange<float>(1f, 30f));
    }
}
