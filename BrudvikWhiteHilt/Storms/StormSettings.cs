using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using System;
using System.Collections.Generic;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// Config for the great storms, section "Storms". Everything but the sound is admin-only and synced from the server.
/// </summary>
public static class StormSettings
{
    private const string Section = "Storms";

    private const string DefaultBiomes = "BlackForest, Swamp, Mountain, Plains, Mistlands, AshLands, DeepNorth, Ocean";

    private static readonly HashSet<Heightmap.Biome> biomes = new();
    private static string parsedBiomes;

    /// <summary>Whether storms come at all.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>Storms per game day on average.</summary>
    public static ConfigEntry<float> StormsPerDay { get; private set; }

    /// <summary>World days without storms at the start.</summary>
    public static ConfigEntry<int> GraceDays { get; private set; }

    /// <summary>Minutes of calm after a storm before the next can come.</summary>
    public static ConfigEntry<float> MinutesBetween { get; private set; }

    /// <summary>Minutes of warning before a storm.</summary>
    public static ConfigEntry<float> WarningMinutes { get; private set; }

    /// <summary>Shortest storm in minutes.</summary>
    public static ConfigEntry<float> MinMinutes { get; private set; }

    /// <summary>Longest storm in minutes.</summary>
    public static ConfigEntry<float> MaxMinutes { get; private set; }

    /// <summary>Biomes that have storms.</summary>
    public static ConfigEntry<string> Biomes { get; private set; }

    /// <summary>Minutes in the open from no exposure to full, in a storm of pace 1.</summary>
    public static ConfigEntry<float> MinutesToFull { get; private set; }

    /// <summary>Minutes in shelter from full exposure to none.</summary>
    public static ConfigEntry<float> MinutesToRecover { get; private set; }

    /// <summary>Multiplier of the health lost.</summary>
    public static ConfigEntry<float> DamageMultiplier { get; private set; }

    /// <summary>Whether a storm can kill.</summary>
    public static ConfigEntry<bool> CanKill { get; private set; }

    /// <summary>How hard the view closes in, 0 to 1.</summary>
    public static ConfigEntry<float> VisionStrength { get; private set; }

    /// <summary>Whether the lee of rocks, cliffs and walls gives some shelter.</summary>
    public static ConfigEntry<bool> LeeShelters { get; private set; }

    /// <summary>Whether the storms keep away during a blood moon.</summary>
    public static ConfigEntry<bool> NotWithBloodMoon { get; private set; }

    /// <summary>Whether lightning strikes in the storms that thunder.</summary>
    public static ConfigEntry<bool> Lightning { get; private set; }

    /// <summary>Seconds between strikes on average.</summary>
    public static ConfigEntry<float> LightningSeconds { get; private set; }

    /// <summary>Chance in percent that a strike seeks out a player in the open.</summary>
    public static ConfigEntry<float> PlayerStrikeChance { get; private set; }

    /// <summary>Lightning damage as a share of maximum health.</summary>
    public static ConfigEntry<float> LightningDamage { get; private set; }

    /// <summary>Whether gusts fell trees.</summary>
    public static ConfigEntry<bool> FallingTrees { get; private set; }

    /// <summary>Seconds between felled trees on average.</summary>
    public static ConfigEntry<float> TreeFallSeconds { get; private set; }

    /// <summary>Whether fires without a roof and torches in the open go out in a raging storm.</summary>
    public static ConfigEntry<bool> FiresGoOut { get; private set; }

    /// <summary>Whether gusts blow doors open, letting the storm in.</summary>
    public static ConfigEntry<bool> DoorsBlowOpen { get; private set; }

    /// <summary>Whether the Storm Bar can be built, to bar a house's doors and windows in a storm.</summary>
    public static ConfigEntry<bool> BarEnabled { get; private set; }

    /// <summary>The comfort a Storm Bar needs where it stands before it can be put across.</summary>
    public static ConfigEntry<int> BarComfort { get; private set; }

    /// <summary>Metres from a Storm Bar within which it bars doors and windows.</summary>
    public static ConfigEntry<float> BarRadius { get; private set; }

    /// <summary>Whether gusts push players in the open and now and then knock them down.</summary>
    public static ConfigEntry<bool> Gusts { get; private set; }

    /// <summary>Whether hail showers hurt players in the open and break young crops.</summary>
    public static ConfigEntry<bool> Hail { get; private set; }

    /// <summary>How many times more likely the Kraken rises at sea in a raging storm.</summary>
    public static ConfigEntry<float> KrakenFactor { get; private set; }

    /// <summary>Whether the compass and the player's place on the map are lost at sea in a raging storm.</summary>
    public static ConfigEntry<bool> LostAtSea { get; private set; }

    /// <summary>Minutes before the warning that the weather forecast shows a storm.</summary>
    public static ConfigEntry<float> ForecastMinutes { get; private set; }

    /// <summary>Whether a storm leaves finds behind.</summary>
    public static ConfigEntry<bool> Finds { get; private set; }

    /// <summary>How many finds a storm leaves around each player who was out in it.</summary>
    public static ConfigEntry<int> FindsCount { get; private set; }

    /// <summary>The share the Weathering skill takes off the exposure's pace at level 100.</summary>
    public static ConfigEntry<float> SkillReduction { get; private set; }

    /// <summary>Volume of the storm's sound, for each player.</summary>
    public static ConfigEntry<float> Volume { get; private set; }

    /// <summary>Seconds of the still eye in the middle of a storm; 0 for none.</summary>
    public static ConfigEntry<float> EyeSeconds { get; private set; }

    /// <summary>How much harder the storm's last wave, after the eye, drives the exposure on.</summary>
    public static ConfigEntry<float> LastWaveFury { get; private set; }

    /// <summary>Whether Ragnarok, the worst storm, comes.</summary>
    public static ConfigEntry<bool> Ragnarok { get; private set; }

    /// <summary>Chance in percent that a storm is Ragnarok, once a boss has fallen.</summary>
    public static ConfigEntry<float> RagnarokChance { get; private set; }

    /// <summary>How much harder Ragnarok drives the exposure on.</summary>
    public static ConfigEntry<float> RagnarokFury { get; private set; }

    /// <summary>Whether Gjallarhorn sounds before Ragnarok and as its last wave comes.</summary>
    public static ConfigEntry<bool> RagnarokHorn { get; private set; }

    /// <summary>Whether the wolves swallow the sun and the moon in Ragnarok, and the sky turns red.</summary>
    public static ConfigEntry<bool> RagnarokSky { get; private set; }

    /// <summary>Whether ash rains in every Ragnarok storm.</summary>
    public static ConfigEntry<bool> RagnarokAsh { get; private set; }

    /// <summary>Whether the earth shakes in Ragnarok.</summary>
    public static ConfigEntry<bool> RagnarokQuakes { get; private set; }

    /// <summary>Seconds between quakes in Ragnarok on average.</summary>
    public static ConfigEntry<float> RagnarokQuakeSeconds { get; private set; }

    /// <summary>The share of its health a weak piece loses to a quake.</summary>
    public static ConfigEntry<float> RagnarokQuakeDamage { get; private set; }

    /// <summary>Whether the storm remembers who braves it.</summary>
    public static ConfigEntry<bool> Grudge { get; private set; }

    /// <summary>How much faster the meter climbs for each storm the storm remembers.</summary>
    public static ConfigEntry<float> GrudgeStep { get; private set; }

    /// <summary>Whether a player deep in the storm hears and sees things, for each player.</summary>
    public static ConfigEntry<bool> Visions { get; private set; }

    /// <summary>
    /// Binds the config entries and registers the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        AcceptableValueRange<float> minutes = new(0.5f, 30f);

        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true,
            "Now and then a great storm comes, with a warning first. Out in it you tire, freeze and in the end die, whatever you wear or have eaten. Shelter is a roof and a fire, a cave or a dungeon. Never in the Meadows.");
        StormsPerDay = WhiteHiltConfig.BindAdminOnly(Section, "StormsPerDay", 0.4f,
            "Storms per game day on average (a day is about 30 minutes).", new AcceptableValueRange<float>(0f, 5f));
        GraceDays = WhiteHiltConfig.BindAdminOnly(Section, "GraceDays", 5,
            "World days at the start without storms.", new AcceptableValueRange<int>(0, 100));
        MinutesBetween = WhiteHiltConfig.BindAdminOnly(Section, "MinutesBetween", 25f,
            "Minutes of calm after a storm before the next can come.", new AcceptableValueRange<float>(0f, 300f));
        WarningMinutes = WhiteHiltConfig.BindAdminOnly(Section, "WarningMinutes", 3f,
            "Minutes of warning before a storm breaks, to get home, into shelter or build one.", minutes);
        MinMinutes = WhiteHiltConfig.BindAdminOnly(Section, "MinMinutes", 3f, "Shortest storm in minutes.", minutes);
        MaxMinutes = WhiteHiltConfig.BindAdminOnly(Section, "MaxMinutes", 6f, "Longest storm in minutes.", minutes);
        Biomes = WhiteHiltConfig.BindAdminOnly(Section, "Biomes", DefaultBiomes,
            "Biomes that have storms, comma separated. The Meadows never do.");
        MinutesToFull = WhiteHiltConfig.BindAdminOnly(Section, "MinutesToFull", 4f,
            "Minutes out in the open from no exposure to full. The Mountain and the sea are a quarter faster, the Mistlands and the Ashlands half as fast again, the Deep North twice as fast.", minutes);
        MinutesToRecover = WhiteHiltConfig.BindAdminOnly(Section, "MinutesToRecover", 1.5f,
            "Minutes in shelter from full exposure to none.", minutes);
        DamageMultiplier = WhiteHiltConfig.BindAdminOnly(Section, "DamageMultiplier", 1f,
            "Multiplier of the health lost to the storm.", new AcceptableValueRange<float>(0f, 5f));
        CanKill = WhiteHiltConfig.BindAdminOnly(Section, "CanKill", true,
            "Whether a storm can kill. Off, it leaves you at 1 health.");
        VisionStrength = WhiteHiltConfig.BindAdminOnly(Section, "VisionStrength", 1f,
            "How hard the view closes in, from 0 (not at all) to 1.", new AcceptableValueRange<float>(0f, 1f));
        LeeShelters = WhiteHiltConfig.BindAdminOnly(Section, "LeeShelters", true,
            "In the lee of a rock, a cliff or a wall, exposure climbs at half the pace; among close trees in the forest storm, at three quarters.");
        NotWithBloodMoon = WhiteHiltConfig.BindAdminOnly(Section, "NotWithBloodMoon", true,
            "No storm comes during a blood moon.");
        Lightning = WhiteHiltConfig.BindAdminOnly(Section, "Lightning", true,
            "Lightning strikes in the forest storm, at sea and in the Mistlands: a tree close by, which falls, the ground, or a player out in the open (more often with metal in hand or on a ship). A crackle warns you first; move away.");
        LightningSeconds = WhiteHiltConfig.BindAdminOnly(Section, "LightningSeconds", 30f,
            "Seconds between strikes near each player on average.", new AcceptableValueRange<float>(5f, 600f));
        PlayerStrikeChance = WhiteHiltConfig.BindAdminOnly(Section, "PlayerStrikeChance", 15f,
            "Chance in percent that a strike seeks out a player in the open; twice with metal in hand, half as much again on a ship.", new AcceptableValueRange<float>(0f, 100f));
        LightningDamage = WhiteHiltConfig.BindAdminOnly(Section, "LightningDamage", 0.4f,
            "Lightning damage of a direct hit as a share of maximum health, before lightning resistance.", new AcceptableValueRange<float>(0f, 2f));
        FallingTrees = WhiteHiltConfig.BindAdminOnly(Section, "FallingTrees", true,
            "Gusts fell trees 10 to 35 m from players, with the wind; never inside a base. Mind the falling trunk, and take the timber.");
        TreeFallSeconds = WhiteHiltConfig.BindAdminOnly(Section, "TreeFallSeconds", 40f,
            "Seconds between felled trees near each player on average.", new AcceptableValueRange<float>(5f, 600f));
        FiresGoOut = WhiteHiltConfig.BindAdminOnly(Section, "FiresGoOut", true,
            "In a raging storm, fires with no roof over them go out and must be lit again, and a torch in the hand blows out in the open. Not in the firestorm.");
        DoorsBlowOpen = WhiteHiltConfig.BindAdminOnly(Section, "DoorsBlowOpen", true,
            "In a raging storm, gusts blow doors open near players. While a door close by stands open the storm comes in: the room shelters only as the lee of a wall, and its fire does not help, until the door is shut.");
        BarEnabled = WhiteHiltConfig.BindAdminOnly(Section, "StormBar", true,
            "The Storm Bar can be built. Put across while a storm is coming or raging, it slams shut every door and window within reach and bars them: no one opens them, inside or out, and no gust tears them open. Those outside can only pound on the door. Someone at the bar lifts it; it lifts by itself when the storm has passed.");
        BarComfort = WhiteHiltConfig.BindAdminOnly(Section, "StormBarComfort", 10,
            "The comfort a Storm Bar needs where it stands before it can be put across.", new AcceptableValueRange<int>(0, 30));
        BarRadius = WhiteHiltConfig.BindAdminOnly(Section, "StormBarRadius", 20f,
            "Metres from a Storm Bar within which it bars doors and windows. Gates are left alone.", new AcceptableValueRange<float>(5f, 50f));
        Gusts = WhiteHiltConfig.BindAdminOnly(Section, "Gusts", true,
            "In a raging storm, hard gusts push players in the open with the wind, and now and then knock them off their feet.");
        Hail = WhiteHiltConfig.BindAdminOnly(Section, "Hail", true,
            "In the forest storm, at sea and in the blizzard, hail showers come now and then: they hurt those in the open (a helmet and armour help) and break young crops that have no roof over them.");
        KrakenFactor = WhiteHiltConfig.BindAdminOnly(Section, "KrakenFactor", 4f,
            "How many times more likely the Kraken rises by a ship at sea in a raging storm, by day or night, in fog or not and in any wind. In Ragnarok it is two and a half times that again, and at least every other minute. 1: the storm changes nothing.",
            new AcceptableValueRange<float>(1f, 10f));
        LostAtSea = WhiteHiltConfig.BindAdminOnly(Section, "LostAtSea", true,
            "At sea in a raging storm the compass, the minimap and your own marker on the map are gone: the storm hides sun and stars. A Navigator's Table aboard keeps the course.");
        ForecastMinutes = WhiteHiltConfig.BindAdminOnly(Section, "ForecastMinutes", 5f,
            "Minutes before the warning that the weather forecast shows a coming storm, near a map table or a Cartographer's Desk and aboard a ship with a Navigator's Table, where the bell rings. 0: not foretold.", new AcceptableValueRange<float>(0f, 30f));
        Finds = WhiteHiltConfig.BindAdminOnly(Section, "Finds", true,
            "When a storm passes, things it brought lie about near each player who was out in it: amber and resin, pearls, coins and rubies, crystals, frost glands and more, after the biome. Lightning leaves a thunderstone now and then.");
        FindsCount = WhiteHiltConfig.BindAdminOnly(Section, "FindsCount", 4,
            "Finds a storm leaves around each player who was out in it.", new AcceptableValueRange<int>(0, 20));
        SkillReduction = WhiteHiltConfig.BindAdminOnly(Section, "SkillReduction", 0.3f,
            "How much more slowly the exposure climbs at Weathering 100 (0.3 = 30%).", new AcceptableValueRange<float>(0f, 0.9f));
        Volume = WhiteHiltConfig.BindLocal(Section, "Volume", 1f,
            "Volume of the storm's wind and thunder, on top of the ambience and effects volumes.", new AcceptableValueRange<float>(0f, 1f));
        EyeSeconds = WhiteHiltConfig.BindAdminOnly(Section, "EyeSeconds", 60f,
            "Seconds of the still eye in the middle of a storm: the wind dies and the exposure holds, then the storm comes back harder. 0: no eye.", new AcceptableValueRange<float>(0f, 300f));
        LastWaveFury = WhiteHiltConfig.BindAdminOnly(Section, "LastWaveFury", 1.5f,
            "How much faster the exposure climbs in the storm's last wave, after the eye.", new AcceptableValueRange<float>(1f, 3f));
        Ragnarok = WhiteHiltConfig.BindAdminOnly(Section, "Ragnarok", true,
            "Once a boss has fallen, now and then the storm is Ragnarok: always the first after a new boss falls, foretold and warned of twice as early, half as long again, half as hard again, with lightning in every storm, over the Meadows too, and rare finds after.");
        RagnarokChance = WhiteHiltConfig.BindAdminOnly(Section, "RagnarokChance", 5f,
            "Chance in percent that any other storm is Ragnarok, once a boss has fallen.", new AcceptableValueRange<float>(0f, 100f));
        RagnarokFury = WhiteHiltConfig.BindAdminOnly(Section, "RagnarokFury", 1.5f,
            "How much faster the exposure climbs in Ragnarok, on top of the wave.", new AcceptableValueRange<float>(1f, 3f));
        RagnarokHorn = WhiteHiltConfig.BindAdminOnly(Section, "RagnarokHorn", true,
            "Gjallarhorn, Heimdall's horn, sounds over the world when Ragnarok is coming and again as its last wave comes, as loud under a roof as in the open.");
        RagnarokSky = WhiteHiltConfig.BindAdminOnly(Section, "RagnarokSky", true,
            "In Ragnarok the sky reddens as it gathers, and the wolves Skoll and Hati swallow the sun and the moon as it breaks: the light goes out in a red dark and comes back when it has passed.");
        RagnarokAsh = WhiteHiltConfig.BindAdminOnly(Section, "RagnarokAsh", true,
            "Ash from Muspelheim rains through every Ragnarok storm, in every biome.");
        RagnarokQuakes = WhiteHiltConfig.BindAdminOnly(Section, "RagnarokQuakes", true,
            "The earth shakes now and then while Ragnarok rages, as Yggdrasil trembles: the ground rumbles, the view shakes, and building pieces already at half their health or less take a blow.");
        RagnarokQuakeSeconds = WhiteHiltConfig.BindAdminOnly(Section, "RagnarokQuakeSeconds", 60f,
            "Seconds between quakes in Ragnarok on average.", new AcceptableValueRange<float>(15f, 600f));
        RagnarokQuakeDamage = WhiteHiltConfig.BindAdminOnly(Section, "RagnarokQuakeDamage", 0.1f,
            "The share of its full health a building piece at half its health or less loses to a quake (0.1 = 10%). 0: quakes harm nothing.",
            new AcceptableValueRange<float>(0f, 0.5f));
        Grudge = WhiteHiltConfig.BindAdminOnly(Section, "Grudge", true,
            "The storm remembers who braves it: each storm a player lives through out in it makes the meter climb faster for them the next time, and leaves them more finds; each storm they sit out in shelter it forgets them a little.");
        GrudgeStep = WhiteHiltConfig.BindAdminOnly(Section, "GrudgeStep", 0.05f,
            "How much faster the meter climbs for each storm the storm remembers, up to 10 (0.05 = 5%, so 50% at most).", new AcceptableValueRange<float>(0f, 0.2f));
        Visions = WhiteHiltConfig.BindLocal(Section, "Visions", true,
            "Deep in the storm you hear and see things: footsteps behind you, whispers and a voice in the wind, and a dark figure at the edge of the fog that is gone when you look. Your own setting.");

        Translations.AddEnglish("msg_whitehilt_storm_warning", "A great storm is coming! Find shelter within {0}: a roof and a fire, or a cave.");
        Translations.AddEnglish("msg_whitehilt_storm_warning_safe", "A great storm is gathering beyond the Meadows.");
        Translations.AddEnglish("msg_whitehilt_storm_tempest", "The storm breaks!");
        Translations.AddEnglish("msg_whitehilt_storm_sea", "The sea rises in the storm!");
        Translations.AddEnglish("msg_whitehilt_storm_sand", "A sandstorm sweeps over the plains!");
        Translations.AddEnglish("msg_whitehilt_storm_blizzard", "A blizzard swallows the mountain!");
        Translations.AddEnglish("msg_whitehilt_storm_mist", "The mist thickens and turns bitter cold!");
        Translations.AddEnglish("msg_whitehilt_storm_fire", "A firestorm rains down!");
        Translations.AddEnglish("msg_whitehilt_storm_arctic", "The north wind howls!");
        Translations.AddEnglish("msg_whitehilt_storm_end", "The storm passes");
        Translations.AddEnglish("msg_whitehilt_storm_tired", "The storm saps your strength");
        Translations.AddEnglish("msg_whitehilt_storm_freezing", "You are freezing. Find shelter!");
        Translations.AddEnglish("msg_whitehilt_storm_burning", "You are on fire. Find a stone roof!");
        Translations.AddEnglish("msg_whitehilt_storm_dying", "The storm is killing you!");
        Translations.AddEnglish("msg_whitehilt_storm_foretold", "The Navigator's Table foretells a great storm in {0}");
        Translations.AddEnglish("msg_whitehilt_storm_finds", "The storm has left things lying about");
        Translations.AddEnglish("msg_whitehilt_storm_dog", "{0} whines and looks toward home");
        Translations.AddEnglish("whitehilt_forecast_storm", "Great storm in {0}");
        Translations.AddEnglish("whitehilt_forecast_storm_raging", "Great storm, {0} left");
        Translations.AddEnglish("msg_whitehilt_storm_static", "Your hair stands on end. Move!");
        Translations.AddEnglish("msg_whitehilt_storm_struck", "Struck by lightning!");
        Translations.AddEnglish("msg_whitehilt_storm_fire_out", "The storm puts out the fire!");
        Translations.AddEnglish("msg_whitehilt_storm_torch_out", "The wind blows out your torch!");
        Translations.AddEnglish("msg_whitehilt_storm_door", "A gust tears a door open! Shut it, or the storm comes in.");
        Translations.AddEnglish("msg_whitehilt_storm_door_in", "The storm comes in through the open door.");
        Translations.AddEnglish("whitehilt_stormbar_bar", "Bar the house");
        Translations.AddEnglish("whitehilt_stormbar_lift", "Lift the bar");
        Translations.AddEnglish("whitehilt_stormbar_barred", "Barred: the doors and windows hold");
        Translations.AddEnglish("whitehilt_stormbar_no_storm", "Bars the house when a storm is coming");
        Translations.AddEnglish("whitehilt_stormbar_comfort", "Comfort here $1, needs $2");
        Translations.AddEnglish("msg_whitehilt_stormbar_barred", "You bar the house: every door and window holds.");
        Translations.AddEnglish("msg_whitehilt_stormbar_lifted", "You lift the bar.");
        Translations.AddEnglish("msg_whitehilt_stormbar_no_storm", "No storm is coming.");
        Translations.AddEnglish("msg_whitehilt_stormbar_comfort", "Not homely enough to bar: comfort $1, needs $2.");
        Translations.AddEnglish("whitehilt_door_barred", "Barred from the inside");
        Translations.AddEnglish("whitehilt_door_pound", "Pound on the door");
        Translations.AddEnglish("msg_whitehilt_door_barred", "Barred from the inside! Someone in there must lift the bar.");
        Translations.AddEnglish("msg_whitehilt_door_pounding", "$1 is pounding on the door!");
        Translations.AddEnglish("msg_whitehilt_storm_knocked_down", "The gust knocks you off your feet!");
        Translations.AddEnglish("msg_whitehilt_storm_hail", "Hail! Get under a roof.");
        Translations.AddEnglish("msg_whitehilt_storm_lost", "The storm hides sun and stars: you have lost your bearings.");
        Translations.AddEnglish("msg_whitehilt_storm_found", "The storm lets go: you have your bearings again.");
        Translations.AddEnglish("whitehilt_storm_clock_coming", "Storm in {0}");
        Translations.AddEnglish("whitehilt_storm_clock_raging", "Storm, {0} left");
        Translations.AddEnglish("se_whitehiltstorm", "Storm exposure");
        Translations.AddEnglish("se_whitehiltstorm_tooltip", "Out in the storm. Shelter under a roof by a fire, or in a cave, to recover.");
        Translations.AddEnglish("whitehilt_storm_cover_open", "Out in the open");
        Translations.AddEnglish("whitehilt_storm_cover_lee", "In the lee: half the exposure");
        Translations.AddEnglish("whitehilt_storm_cover_forest", "Among the trees: less exposure");
        Translations.AddEnglish("whitehilt_storm_cover_roof", "Under a roof: light a fire to recover");
        Translations.AddEnglish("whitehilt_storm_cover_roofandfire", "Under a roof by a fire: recovering");
        Translations.AddEnglish("whitehilt_storm_cover_interior", "Inside: recovering");
        Translations.AddEnglish("msg_whitehilt_storm_eye", "The wind dies: the eye of the storm. It returns in {0}, worse than before!");
        Translations.AddEnglish("msg_whitehilt_storm_returns", "The storm returns, fiercer!");
        Translations.AddEnglish("whitehilt_storm_clock_eye", "Eye of the storm, {0}");
        Translations.AddEnglish("msg_whitehilt_storm_ragnarok_warning", "Ragnarok! The worst storm of all is coming, over the Meadows too. Find shelter within {0}!");
        Translations.AddEnglish("msg_whitehilt_storm_ragnarok", "Ragnarok breaks over the world!");
        Translations.AddEnglish("msg_whitehilt_storm_ragnarok_foretold", "The Navigator's Table foretells Ragnarok in {0}");
        Translations.AddEnglish("msg_whitehilt_storm_ragnarok_finds", "Ragnarok has left rare things lying about");
        Translations.AddEnglish("whitehilt_forecast_ragnarok", "Ragnarok in {0}");
        Translations.AddEnglish("whitehilt_forecast_ragnarok_raging", "Ragnarok, {0} left");
        Translations.AddEnglish("whitehilt_storm_clock_ragnarok_coming", "Ragnarok in {0}");
        Translations.AddEnglish("whitehilt_storm_clock_ragnarok", "Ragnarok, {0} left");
        Translations.AddEnglish("msg_whitehilt_ragnarok_sun", "Skoll swallows the sun!");
        Translations.AddEnglish("msg_whitehilt_ragnarok_moon", "Hati swallows the moon!");
        Translations.AddEnglish("msg_whitehilt_ragnarok_quake", "The earth shakes: Yggdrasil trembles!");
        Translations.AddEnglish("msg_whitehilt_storm_grudge_up", "The storm remembers you. It will come harder for you next time");
        Translations.AddEnglish("msg_whitehilt_storm_grudge_down", "The storm forgets you a little");
        Translations.AddEnglish("whitehilt_storm_grudge", "The storm remembers you: {0}% faster");
        StormVisions.AddTexts();
    }

    /// <summary>
    /// Whether a biome has storms.
    /// </summary>
    /// <param name="biome">The biome.</param>
    /// <returns>True if storms are on there; for the Meadows, only in Ragnarok.</returns>
    public static bool HasStorms(Heightmap.Biome biome)
    {
        // Ragnarok spares nowhere.
        if (biome == Heightmap.Biome.Meadows)
        {
            return StormState.Ragnarok && StormState.Phase != StormPhase.Calm;
        }

        if (StormProfile.For(biome).Kind == StormKind.None)
        {
            return false;
        }

        if (parsedBiomes != Biomes.Value)
        {
            parsedBiomes = Biomes.Value;
            biomes.Clear();
            foreach (string part in parsedBiomes.Split(','))
            {
                if (Enum.TryParse(part.Trim(), true, out Heightmap.Biome parsed))
                {
                    biomes.Add(parsed);
                }
            }
        }

        return biomes.Contains(biome);
    }
}
