using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.SeaBattles;

/// <summary>
/// Config and English texts of the Nail Ships and the raids. Admin only, synced from the server. The texts of items
/// that come in later phases are registered here too, so one place holds all of the feature's words.
/// </summary>
public static class SeaBattleSettings
{
    /// <summary>The crew a new config file starts with: one tier per progression key, the last set key wins.</summary>
    public const string DefaultCrewTiers =
        "defeated_bonemass=Draugr:3,Draugr_Ranged:2; defeated_dragon=Draugr:3,Draugr_Ranged:2,Draugr_Elite:1; " +
        "defeated_goblinking=Goblin:3,GoblinArcher:2,GoblinBrute:1; " +
        "defeated_queen=Draugr_Elite:4,Draugr_Ranged:3,GoblinBrute:1; " +
        "defeated_fader=Charred_Melee:4,Charred_Archer:2,Charred_Mage:1";

    /// <summary>The crew list of earlier versions, which ended at Yagluth; a config file that still holds it gets the new one.</summary>
    public const string OldDefaultCrewTiers =
        "defeated_bonemass=Draugr:3,Draugr_Ranged:2; defeated_dragon=Draugr:3,Draugr_Ranged:2,Draugr_Elite:1; " +
        "defeated_goblinking=Goblin:3,GoblinArcher:2,GoblinBrute:1";

    /// <summary>The factor on the hull's health per progression key a new config file starts with.</summary>
    public const string DefaultHullTiers = "defeated_dragon=1.25; defeated_goblinking=1.5; defeated_queen=2; defeated_fader=2.5";

    /// <summary>The least level (stars plus one) of the crew per progression key a new config file starts with.</summary>
    public const string DefaultCrewLevels = "defeated_queen=2; defeated_fader=2";

    /// <summary>How many warriors leap at once per progression key, as a new config file has it.</summary>
    public const string DefaultLeapTiers = "defeated_goblinking=2";

    /// <summary>The hold's loot list a new config file starts with.</summary>
    public const string DefaultLoot =
        "Coins:40-120:10, Iron:3-8:6, Chain:1-2:4, WitheredBone:4-8:6, Amber:1-2:4, AmberPearl:1-2:3, Ruby:1-2:2, " +
        "WhiteHiltShipBolt:5-10:6, TrophyDraugr:1-1:2";

    /// <summary>
    /// The hold's loot list per progression key a new config file starts with, in place of <see cref="DefaultLoot"/> once
    /// the key is set: the materials of the biome the world has reached.
    /// </summary>
    public const string DefaultLootTiers =
        "defeated_dragon=Coins:60-160:10, Silver:3-8:6, Iron:3-6:4, Obsidian:4-10:4, WolfPelt:2-4:4, FreezeGland:2-4:3, " +
        "AmberPearl:1-2:3, Ruby:1-3:3, WhiteHiltShipBolt:5-10:6, TrophyDraugrElite:1-1:2; " +
        "defeated_goblinking=Coins:80-200:10, BlackMetal:3-8:6, Silver:2-5:3, LinenThread:4-10:4, Tar:4-8:4, Needle:2-5:3, " +
        "AmberPearl:1-3:4, Ruby:1-3:3, WhiteHiltShipBolt:6-12:6, TrophyGoblin:1-1:2; " +
        "defeated_queen=Coins:100-250:10, BlackMarble:4-10:5, Eitr:2-5:4, YggdrasilWood:4-10:4, Carapace:2-5:4, Sap:2-5:3, " +
        "BlackMetal:3-6:3, Ruby:2-4:3, WhiteHiltShipBolt:8-14:6, TrophyDraugrElite:1-1:2; " +
        "defeated_fader=Coins:120-300:10, FlametalNew:2-6:5, CharredBone:4-8:4, Charredskull:1-2:3, MoltenCore:1-2:3, " +
        "Eitr:3-6:3, Ruby:2-4:3, WhiteHiltShipBolt:8-14:6, TrophyCharredMelee:1-1:2";

    private const string Section = "SeaBattles";
    private const string RaidSection = "SeaBattles.Raids";

    /// <summary>Nail Ships can come, and raid charts work.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>Global key that must be set before a Nail Ship comes; empty for none.</summary>
    public static ConfigEntry<string> RequiredKey { get; private set; }

    /// <summary>Chance per minute, in percent, for one player while the conditions hold.</summary>
    public static ConfigEntry<float> ChancePerMinute { get; private set; }

    /// <summary>Extra percentage points per additional player aboard.</summary>
    public static ConfigEntry<float> ChancePerExtraPlayer { get; private set; }

    /// <summary>Real minutes before the next Nail Ship anywhere in the world.</summary>
    public static ConfigEntry<float> CooldownMinutes { get; private set; }

    /// <summary>Least depth of water under the player's ship, in metres.</summary>
    public static ConfigEntry<float> MinDepth { get; private set; }

    /// <summary>No land within this many metres of the player's ship.</summary>
    public static ConfigEntry<float> MinShoreDistance { get; private set; }

    /// <summary>Metres from the target where the Nail Ship appears.</summary>
    public static ConfigEntry<float> SpawnDistance { get; private set; }

    /// <summary>The crew per progression key, see <see cref="CrewTable"/>.</summary>
    public static ConfigEntry<string> CrewTiers { get; private set; }

    /// <summary>Extra melee crew per extra player aboard the target.</summary>
    public static ConfigEntry<int> CrewPerExtraPlayer { get; private set; }

    /// <summary>Health of a Nail Ship's hull.</summary>
    public static ConfigEntry<float> HullHealth { get; private set; }

    /// <summary>Multiplier on the vanilla longship's sail force.</summary>
    public static ConfigEntry<float> SailForceFactor { get; private set; }

    /// <summary>Whether Nail Ships always have the wind behind them.</summary>
    public static ConfigEntry<bool> Tailwind { get; private set; }

    /// <summary>Metres it keeps alongside while its archers shoot.</summary>
    public static ConfigEntry<float> BroadsideDistance { get; private set; }

    /// <summary>Seconds alongside before it closes in to board; 0 means never.</summary>
    public static ConfigEntry<float> BroadsideSeconds { get; private set; }

    /// <summary>Metres within which its warriors leap across.</summary>
    public static ConfigEntry<float> BoardDistance { get; private set; }

    /// <summary>Seconds between two warriors leaping.</summary>
    public static ConfigEntry<float> BoardIntervalSeconds { get; private set; }

    /// <summary>It flees when this share of its crew is left.</summary>
    public static ConfigEntry<float> FleeCrewShare { get; private set; }

    /// <summary>It flees when this share of its hull health is left.</summary>
    public static ConfigEntry<float> FleeHullShare { get; private set; }

    /// <summary>Metres; with no player this close it sinks out of sight.</summary>
    public static ConfigEntry<float> DespawnRange { get; private set; }

    /// <summary>Seconds without a player in range before it goes.</summary>
    public static ConfigEntry<float> DespawnSeconds { get; private set; }

    /// <summary>Minutes a prize floats before it sinks.</summary>
    public static ConfigEntry<float> PrizeMinutes { get; private set; }

    /// <summary>Whether the hunted ship is held below the Nail Ships' speed, see <see cref="Pursuit"/>.</summary>
    public static ConfigEntry<bool> PursuitEnabled { get; private set; }

    /// <summary>Seconds the hunted ship is held after the Nail Ships begin to hunt it.</summary>
    public static ConfigEntry<float> PursuitShockSeconds { get; private set; }

    /// <summary>Share of the Nail Ships' speed the hunted ship may keep.</summary>
    public static ConfigEntry<float> PursuitSpeedFactor { get; private set; }

    /// <summary>Least speed cap, in metres per second.</summary>
    public static ConfigEntry<float> PursuitFloorSpeed { get; private set; }

    /// <summary>Loot list of the hold, in the format of <c>[Treasure] Loot</c>.</summary>
    public static ConfigEntry<string> Loot { get; private set; }

    /// <summary>Draws from the loot list.</summary>
    public static ConfigEntry<int> LootRolls { get; private set; }

    /// <summary>Chance that the hold carries a Raid Chart.</summary>
    public static ConfigEntry<float> RaidChartChance { get; private set; }

    /// <summary>Metres a player can leap from deck to deck.</summary>
    public static ConfigEntry<float> BoardingLeapRange { get; private set; }

    /// <summary>Factor on the hull's health per global key, see <see cref="TierValues"/>.</summary>
    public static ConfigEntry<string> HullTiers { get; private set; }

    /// <summary>The hold's loot list per global key, in place of <see cref="Loot"/>.</summary>
    public static ConfigEntry<string> LootTiers { get; private set; }

    /// <summary>Least level of the crew per global key.</summary>
    public static ConfigEntry<string> CrewLevels { get; private set; }

    /// <summary>How many warriors leap at once per global key.</summary>
    public static ConfigEntry<string> LeapTiers { get; private set; }

    /// <summary>Seconds a Nail Ship may be kept from its target before it gives up; 0 never.</summary>
    public static ConfigEntry<float> StuckSeconds { get; private set; }

    /// <summary>Seconds between two repairs of a Nail Ship's crew; 0 never.</summary>
    public static ConfigEntry<float> CrewRepairSeconds { get; private set; }

    /// <summary>Factor on the chance at night.</summary>
    public static ConfigEntry<float> ChanceAtNight { get; private set; }

    /// <summary>Factor on the chance in a storm.</summary>
    public static ConfigEntry<float> ChanceInStorm { get; private set; }

    /// <summary>Factor on the chance in fog.</summary>
    public static ConfigEntry<float> ChanceInFog { get; private set; }

    /// <summary>Seconds the place a Nail Ship was sighted stays on the map; 0 for no pin.</summary>
    public static ConfigEntry<float> SightingPinSeconds { get; private set; }

    /// <summary>
    /// Binds the config entries and adds the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true,
            "A Nail Ship from Naglfar's fleet can hunt ships at sea, and Raid Charts work.");
        RequiredKey = WhiteHiltConfig.BindAdminOnly(Section, "RequiredKey", "defeated_bonemass",
            "Global key needed before a Nail Ship comes (defeated_bonemass = Bonemass is slain). Empty: from the start.");
        ChancePerMinute = WhiteHiltConfig.BindAdminOnly(Section, "ChancePerMinute", 4f,
            "Chance per minute for a ship with one player aboard, in percent, while every condition holds. 0 disables natural encounters.",
            new AcceptableValueRange<float>(0f, 100f));
        ChancePerExtraPlayer = WhiteHiltConfig.BindAdminOnly(Section, "ChancePerExtraPlayer", 2f,
            "Extra percentage points per additional player aboard the same ship.", new AcceptableValueRange<float>(0f, 100f));
        CooldownMinutes = WhiteHiltConfig.BindAdminOnly(Section, "CooldownMinutes", 45f,
            "Real minutes before the next Nail Ship can come, anywhere in the world.", new AcceptableValueRange<float>(0f, 1440f));
        MinDepth = WhiteHiltConfig.BindAdminOnly(Section, "MinDepth", 20f,
            "Least depth of water under the player's ship, in metres.", new AcceptableValueRange<float>(5f, 200f));
        MinShoreDistance = WhiteHiltConfig.BindAdminOnly(Section, "MinShoreDistance", 100f,
            "No land within this many metres of the player's ship.", new AcceptableValueRange<float>(0f, 500f));
        SpawnDistance = WhiteHiltConfig.BindAdminOnly(Section, "SpawnDistance", 180f,
            "Metres from the target ship where the Nail Ship appears.", new AcceptableValueRange<float>(80f, 400f));
        CrewTiers = WhiteHiltConfig.BindAdminOnly(Section, "CrewTiers", DefaultCrewTiers,
            "The crew per global key: key=Prefab:count,Prefab:count; next key=... The last tier whose key is set wins. " +
            "A prefab with Ranged, Archer or Mage in its name shoots from its post, the others fight in melee.");
        CrewPerExtraPlayer = WhiteHiltConfig.BindAdminOnly(Section, "CrewPerExtraPlayer", 1,
            "Extra melee crew per additional player aboard the target ship.", new AcceptableValueRange<int>(0, 5));
        HullHealth = WhiteHiltConfig.BindAdminOnly(Section, "HullHealth", 1200f,
            "Health of a Nail Ship's hull, before HullTiers. Applies to new Nail Ships.", new AcceptableValueRange<float>(100f, 20000f));
        SailForceFactor = WhiteHiltConfig.BindAdminOnly(Section, "SailForceFactor", 1f,
            "Multiplier on the vanilla longship's sail force. Applies after a restart.", new AcceptableValueRange<float>(0.2f, 5f));
        Tailwind = WhiteHiltConfig.BindAdminOnly(Section, "Tailwind", true, "Nail Ships always have the wind behind them.");
        BroadsideDistance = WhiteHiltConfig.BindAdminOnly(Section, "BroadsideDistance", 25f,
            "Metres it keeps alongside while its archers shoot.", new AcceptableValueRange<float>(10f, 60f));
        BroadsideSeconds = WhiteHiltConfig.BindAdminOnly(Section, "BroadsideSeconds", 40f,
            "Seconds alongside before it closes in to board. 0: it never boards.", new AcceptableValueRange<float>(0f, 300f));
        BoardDistance = WhiteHiltConfig.BindAdminOnly(Section, "BoardDistance", 6f,
            "Metres within which its warriors leap across.", new AcceptableValueRange<float>(3f, 15f));
        BoardIntervalSeconds = WhiteHiltConfig.BindAdminOnly(Section, "BoardIntervalSeconds", 4f,
            "Seconds between two warriors leaping.", new AcceptableValueRange<float>(1f, 30f));
        FleeCrewShare = WhiteHiltConfig.BindAdminOnly(Section, "FleeCrewShare", 0.25f,
            "It flees when this share of its crew is left.", new AcceptableValueRange<float>(0f, 1f));
        FleeHullShare = WhiteHiltConfig.BindAdminOnly(Section, "FleeHullShare", 0.2f,
            "It flees when this share of its hull health is left.", new AcceptableValueRange<float>(0f, 1f));
        PursuitEnabled = WhiteHiltConfig.BindAdminOnly(Section, "PursuitEnabled", true,
            "While Nail Ships hunt a ship, its top speed is held a little below theirs so they can catch it, and while their crew lives aboard.");
        PursuitShockSeconds = WhiteHiltConfig.BindAdminOnly(Section, "PursuitShockSeconds", 60f,
            "Seconds the hunted ship is held after the Nail Ships begin to hunt it. The hold stays longer while their crew lives aboard.",
            new AcceptableValueRange<float>(0f, 600f));
        PursuitSpeedFactor = WhiteHiltConfig.BindAdminOnly(Section, "PursuitSpeedFactor", 0.85f,
            "Share of the Nail Ships' speed the held ship may keep. Below 1, so they catch up.", new AcceptableValueRange<float>(0.3f, 1f));
        PursuitFloorSpeed = WhiteHiltConfig.BindAdminOnly(Section, "PursuitFloorSpeed", 4f,
            "Least speed, in metres per second, the held ship is cut to, so it is not stopped while the Nail Ships turn.",
            new AcceptableValueRange<float>(0f, 15f));
        DespawnRange = WhiteHiltConfig.BindAdminOnly(Section, "DespawnRange", 300f,
            "Metres. With no player this close, the Nail Ship sinks out of sight.", new AcceptableValueRange<float>(100f, 1000f));
        DespawnSeconds = WhiteHiltConfig.BindAdminOnly(Section, "DespawnSeconds", 60f,
            "Seconds without a player in range before the Nail Ship goes.", new AcceptableValueRange<float>(10f, 600f));
        PrizeMinutes = WhiteHiltConfig.BindAdminOnly(Section, "PrizeMinutes", 10f,
            "Minutes a Nail Ship that has lost its crew floats before it sinks.", new AcceptableValueRange<float>(1f, 60f));
        Loot = WhiteHiltConfig.BindAdminOnly(Section, "Loot", DefaultLoot,
            "Loot list of the hold, \"Item:min-max:weight\" separated by commas, as in [Treasure] Loot.");
        LootRolls = WhiteHiltConfig.BindAdminOnly(Section, "LootRolls", 4,
            "Draws from the loot list.", new AcceptableValueRange<int>(0, 20));
        RaidChartChance = WhiteHiltConfig.BindAdminOnly(Section, "RaidChartChance", 0.35f,
            "Chance that the hold carries a Raid Chart.", new AcceptableValueRange<float>(0f, 1f));
        BoardingLeapRange = WhiteHiltConfig.BindAdminOnly(Section, "BoardingLeapRange", 10f,
            "Metres a player can leap from one ship's deck to another.", new AcceptableValueRange<float>(3f, 25f));
        HullTiers = WhiteHiltConfig.BindAdminOnly(Section, "HullTiers", DefaultHullTiers,
            "Factor on HullHealth (and the flagship's) per global key: key=factor; next key=... The last key that is set wins; 1 when none is.");
        LootTiers = WhiteHiltConfig.BindAdminOnly(Section, "LootTiers", DefaultLootTiers,
            "The hold's loot list per global key, in place of Loot once the key is set: key=Item:min-max:weight, Item:...; next key=... The last key that is set wins.");
        CrewLevels = WhiteHiltConfig.BindAdminOnly(Section, "CrewLevels", DefaultCrewLevels,
            "Least level of the crew per global key (2 = one star): key=level; next key=... The last key that is set wins; no least level when none is.");
        LeapTiers = WhiteHiltConfig.BindAdminOnly(Section, "LeapTiers", DefaultLeapTiers,
            "How many warriors leap aboard at once per global key: key=count; next key=... The last key that is set wins; 1 when none is.");
        StuckSeconds = WhiteHiltConfig.BindAdminOnly(Section, "StuckSeconds", 45f,
            "Seconds a Nail Ship may be kept from its target by land or shallows before it gives up and turns away. 0: never.",
            new AcceptableValueRange<float>(0f, 600f));
        CrewRepairSeconds = WhiteHiltConfig.BindAdminOnly(Section, "CrewRepairSeconds", 10f,
            "Seconds between two repairs by a Nail Ship's crew: each puts out a fire on its deck or patches a hole. 0: the crew never repairs.",
            new AcceptableValueRange<float>(0f, 300f));
        ChanceAtNight = WhiteHiltConfig.BindAdminOnly(Section, "ChanceAtNight", 1.5f,
            "Factor on the chance per minute at night.", new AcceptableValueRange<float>(0f, 5f));
        ChanceInStorm = WhiteHiltConfig.BindAdminOnly(Section, "ChanceInStorm", 1.5f,
            "Factor on the chance per minute in a storm.", new AcceptableValueRange<float>(0f, 5f));
        ChanceInFog = WhiteHiltConfig.BindAdminOnly(Section, "ChanceInFog", 1.5f,
            "Factor on the chance per minute in fog or mist.", new AcceptableValueRange<float>(0f, 5f));
        SightingPinSeconds = WhiteHiltConfig.BindAdminOnly(Section, "SightingPinSeconds", 90f,
            "Seconds the place where Nail Ships were sighted stays on the map. 0: no pin.", new AcceptableValueRange<float>(0f, 600f));
        WhiteHiltConfig.UpgradeDefault(CrewTiers, OldDefaultCrewTiers);

        Raids.Initialize();
        WhiteHiltConfig.AddFeatureGate(NailShipRegistry.RaidChartName, Enabled);
        AddTranslations();
    }

    /// <summary>
    /// The Nail Ship's numbers as the plain values <see cref="RaiderTactics"/> works with.
    /// </summary>
    /// <returns>The current tuning.</returns>
    public static RaiderTuning Tuning()
    {
        return new RaiderTuning(BroadsideDistance.Value, BroadsideSeconds.Value, BoardDistance.Value,
            FleeCrewShare.Value, FleeHullShare.Value, Weapons.WeaponSettings.RamRange.Value, Weapons.WeaponSettings.RamTargetSpeed.Value);
    }

    // English texts of the whole feature. The items of later phases are named here so one place holds every word.
    private static void AddTranslations()
    {
        Translations.AddEnglish("whitehilt_nailship", "Nail Ship");
        Translations.AddEnglish("msg_whitehilt_raider_sighted", "Sails on the horizon, {0}! A Nail Ship comes for you.");
        Translations.AddEnglish("whitehilt_bearing_ahead", "dead ahead");
        Translations.AddEnglish("whitehilt_bearing_starboardbow", "off the starboard bow");
        Translations.AddEnglish("whitehilt_bearing_starboard", "to starboard");
        Translations.AddEnglish("whitehilt_bearing_starboardquarter", "off the starboard quarter");
        Translations.AddEnglish("whitehilt_bearing_astern", "astern");
        Translations.AddEnglish("whitehilt_bearing_portquarter", "off the port quarter");
        Translations.AddEnglish("whitehilt_bearing_port", "to port");
        Translations.AddEnglish("whitehilt_bearing_portbow", "off the port bow");
        Translations.AddEnglish("whitehilt_raider_sighted_pin", "Nail Ship sighted");
        Translations.AddEnglish("msg_whitehilt_raider_gives_up", "The Nail Ship cannot reach you and turns away.");
        Translations.AddEnglish("msg_whitehilt_raider_rams", "The Nail Ship turns to ram you!");
        Translations.AddEnglish("msg_whitehilt_raider_flees", "The Nail Ship turns and runs.");
        Translations.AddEnglish("msg_whitehilt_raider_prize",
            "The crew is dead. The Nail Ship is yours, but its nails will not hold for long.");
        Translations.AddEnglish("whitehilt_raider_prize_hover", "Prize: sinks in {0} min");
        Translations.AddEnglish("msg_whitehilt_raider_helm_held", "The dead still hold the helm.");
        Translations.AddEnglish("msg_whitehilt_raider_hold_guarded", "The crew guards the hold.");
        Translations.AddEnglish("msg_whitehilt_pursuit_shock", "The Nail Ships close in. The wind fails your sail.");
        Translations.AddEnglish("msg_whitehilt_pursuit_held", "The raiders aboard drag at your ship. You cannot outrun them.");
        Translations.AddEnglish("msg_whitehilt_pursuit_free", "You shake off the ambush. Now you can run.");
        Translations.AddEnglish("whitehilt_pursuit_hud", "Held by the Nail Ships");
        Translations.AddEnglish("whitehilt_boarding_leap", "Leap aboard");
        Translations.AddEnglish("whitehilt_ballista_man", "Man the ballista");
        Translations.AddEnglish("msg_whitehilt_ballista_noammo", "No ship ammunition");
        Translations.AddEnglish("msg_whitehilt_raid_marked", "A draugr harbour is marked on your map.");
        Translations.AddEnglish("msg_whitehilt_raid_nosite", "No coast on this chart is known to you.");
        Translations.AddEnglish("msg_whitehilt_raid_guarded", "The guards still live.");
        Translations.AddEnglish("msg_whitehilt_raid_toomany", "Every draugr harbour is manned already. Sack one first.");
        Translations.AddEnglish("whitehilt_raid_pin", "Draugr harbour");
        Translations.AddEnglish("whitehilt_raid_hoard", "Draugr hoard");

        // The Ship Ballista and the Ship Bolt register their own names, in ShipBallistaUpgrade and ShipBolts.
        Translations.AddEnglishNameAndDescription("item_whitehiltraidchart", "Raid Chart",
            "A sea chart in a dead man's hand. Use it to mark a draugr harbour on a coast you know.");
    }

    /// <summary>
    /// Config of the draugr harbours, section <c>SeaBattles.Raids</c>.
    /// </summary>
    public static class Raids
    {
        /// <summary>The shore guards a new config file starts with.</summary>
        public const string DefaultGuards = "Draugr:4,Draugr_Ranged:2,Draugr_Elite:1";

        /// <summary>The hoard's loot list a new config file starts with.</summary>
        public const string DefaultLoot = SeaBattleSettings.DefaultLoot + ", GiftOfOdin:1-1:2";

        /// <summary>Least distance from the chart's user to the harbour, in metres.</summary>
        public static ConfigEntry<float> MinDistance { get; private set; }

        /// <summary>Farthest distance from the chart's user to the harbour, in metres.</summary>
        public static ConfigEntry<float> MaxDistance { get; private set; }

        /// <summary>The shore guards: <c>Prefab:count</c> separated by commas.</summary>
        public static ConfigEntry<string> Guards { get; private set; }

        /// <summary>Metres round the hoard the guards stand.</summary>
        public static ConfigEntry<float> GuardRadius { get; private set; }

        /// <summary>Metres a guard walks from its post before it turns back.</summary>
        public static ConfigEntry<float> GuardHoldRadius { get; private set; }

        /// <summary>Metres from the hoard to look for deep water for the moored ship.</summary>
        public static ConfigEntry<float> ShipSearchRadius { get; private set; }

        /// <summary>Loot list of the hoard.</summary>
        public static ConfigEntry<string> Loot { get; private set; }

        /// <summary>Draws from the hoard's loot list.</summary>
        public static ConfigEntry<int> LootRolls { get; private set; }

        /// <summary>Most raid sites in the world at once.</summary>
        public static ConfigEntry<int> MaxActive { get; private set; }

        internal static void Initialize()
        {
            MinDistance = WhiteHiltConfig.BindAdminOnly(RaidSection, "MinDistance", 400f,
                "Least distance from the one who uses a Raid Chart to the harbour, in metres.", new AcceptableValueRange<float>(100f, 3000f));
            MaxDistance = WhiteHiltConfig.BindAdminOnly(RaidSection, "MaxDistance", 1500f,
                "Farthest distance from the one who uses a Raid Chart to the harbour, in metres.", new AcceptableValueRange<float>(200f, 5000f));
            Guards = WhiteHiltConfig.BindAdminOnly(RaidSection, "Guards", DefaultGuards,
                "The shore guards, Prefab:count separated by commas.");
            GuardRadius = WhiteHiltConfig.BindAdminOnly(RaidSection, "GuardRadius", 12f,
                "Metres round the hoard the guards stand.", new AcceptableValueRange<float>(4f, 40f));
            GuardHoldRadius = WhiteHiltConfig.BindAdminOnly(RaidSection, "GuardHoldRadius", 8f,
                "Metres a guard walks from its post before it turns back.", new AcceptableValueRange<float>(2f, 40f));
            ShipSearchRadius = WhiteHiltConfig.BindAdminOnly(RaidSection, "ShipSearchRadius", 80f,
                "Metres from the hoard to look for deep water for the moored Nail Ship.", new AcceptableValueRange<float>(20f, 200f));
            Loot = WhiteHiltConfig.BindAdminOnly(RaidSection, "Loot", DefaultLoot,
                "Loot list of the hoard, \"Item:min-max:weight\" separated by commas, as in [Treasure] Loot.");
            LootRolls = WhiteHiltConfig.BindAdminOnly(RaidSection, "LootRolls", 8,
                "Draws from the hoard's loot list.", new AcceptableValueRange<int>(0, 30));
            MaxActive = WhiteHiltConfig.BindAdminOnly(RaidSection, "MaxActive", 6,
                "Most raid sites in the world at once.", new AcceptableValueRange<int>(1, 50));
        }
    }
}
