using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.SeaBattles.Escorts;

/// <summary>
/// Config and English texts of the escort contracts, section <c>SeaBattles.Escorts</c>. Admin only, synced from the
/// server. The saga line and its renown setting are with the other saga texts in <c>SagaSettings</c>.
/// </summary>
public static class EscortSettings
{
    private const string Section = "SeaBattles.Escorts";

    /// <summary>Escort contracts can be bought and used.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>Haldor's price for a contract, in coins.</summary>
    public static ConfigEntry<int> ContractPrice { get; private set; }

    /// <summary>Least straight-line distance to the destination, in metres.</summary>
    public static ConfigEntry<float> MinDistance { get; private set; }

    /// <summary>Most distance to the destination, in metres.</summary>
    public static ConfigEntry<float> MaxDistance { get; private set; }

    /// <summary>The whole straight line to the destination must be at least this deep, in metres.</summary>
    public static ConfigEntry<float> MinDepth { get; private set; }

    /// <summary>Multiplier on the cloned longship's sail force.</summary>
    public static ConfigEntry<float> SailForceFactor { get; private set; }

    /// <summary>Health of the trading ship's hull.</summary>
    public static ConfigEntry<float> HullHealth { get; private set; }

    /// <summary>Attacks on the way.</summary>
    public static ConfigEntry<int> Waves { get; private set; }

    /// <summary>Share of the route at which the first wave can come.</summary>
    public static ConfigEntry<float> WaveStart { get; private set; }

    /// <summary>Share of the route by which the last wave has come.</summary>
    public static ConfigEntry<float> WaveEnd { get; private set; }

    /// <summary>Percent of the cargo one living raider on the deck carries off per second.</summary>
    public static ConfigEntry<float> PlunderPerRaider { get; private set; }

    /// <summary>A player must be this close to the trading ship, in metres.</summary>
    public static ConfigEntry<float> AbandonRange { get; private set; }

    /// <summary>Seconds nobody may be in range before the merchant gives up.</summary>
    public static ConfigEntry<float> AbandonSeconds { get; private set; }

    /// <summary>The trading ship has arrived within this distance of the destination, in metres.</summary>
    public static ConfigEntry<float> ArriveRadius { get; private set; }

    /// <summary>Coins for a full cargo, to each player in reach.</summary>
    public static ConfigEntry<int> Pay { get; private set; }

    /// <summary>Players this close to the trading ship when it arrives are paid, in metres.</summary>
    public static ConfigEntry<float> RewardRange { get; private set; }

    /// <summary>Percent chance per paid player of a Raid Chart.</summary>
    public static ConfigEntry<float> RaidChartChance { get; private set; }

    /// <summary>Least real minutes between two contracts for one player.</summary>
    public static ConfigEntry<float> CooldownMinutes { get; private set; }

    /// <summary>No natural Nail Ship encounter is rolled for a ship this close to an active trading ship, in metres.</summary>
    public static ConfigEntry<float> NoEncounterRange { get; private set; }

    /// <summary>Coins for a full cargo per kilometre of a voyage between two trading posts.</summary>
    public static ConfigEntry<float> PayPerKilometre { get; private set; }

    /// <summary>Seconds a merchant waits at a trading post for the contractor to go aboard.</summary>
    public static ConfigEntry<float> WaitSeconds { get; private set; }

    /// <summary>How near the waiting merchant the contractor's own ship must be to set out, in metres.</summary>
    public static ConfigEntry<float> StartRange { get; private set; }

    /// <summary>
    /// Binds the config entries and adds the English texts. Call from the plugin's Awake, after
    /// <see cref="SeaBattleSettings.Initialize"/>.
    /// </summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true,
            "Haldor sells Escort Contracts, and a trading ship can be called to sail to a port with the players guarding it.");
        ContractPrice = WhiteHiltConfig.BindAdminOnly(Section, "ContractPrice", 300,
            "Haldor's price for an Escort Contract, in coins.", new AcceptableValueRange<int>(0, 5000));
        MinDistance = WhiteHiltConfig.BindAdminOnly(Section, "MinDistance", 1200f,
            "Least straight-line distance to the destination, in metres.", new AcceptableValueRange<float>(400f, 5000f));
        MaxDistance = WhiteHiltConfig.BindAdminOnly(Section, "MaxDistance", 2000f,
            "Most distance to the destination, in metres.", new AcceptableValueRange<float>(400f, 8000f));
        MinDepth = WhiteHiltConfig.BindAdminOnly(Section, "MinDepth", 6f,
            "The whole straight line to the destination must be at least this deep, in metres.", new AcceptableValueRange<float>(2f, 30f));
        SailForceFactor = WhiteHiltConfig.BindAdminOnly(Section, "SailForceFactor", 0.8f,
            "Multiplier on the longship's sail force, so the trading ship is a bit slower than a longship. Applies after a restart.",
            new AcceptableValueRange<float>(0.3f, 1.5f));
        HullHealth = WhiteHiltConfig.BindAdminOnly(Section, "HullHealth", 1500f,
            "Health of the trading ship's hull. Applies after a restart.", new AcceptableValueRange<float>(200f, 10000f));
        Waves = WhiteHiltConfig.BindAdminOnly(Section, "Waves", 2,
            "Nail Ship attacks on the way. A wave is a fleet once the world allows fleets.", new AcceptableValueRange<int>(0, 4));
        WaveStart = WhiteHiltConfig.BindAdminOnly(Section, "WaveStart", 0.25f,
            "Share of the route at which the first wave can come, 0 to 0.9.", new AcceptableValueRange<float>(0f, 0.9f));
        WaveEnd = WhiteHiltConfig.BindAdminOnly(Section, "WaveEnd", 0.8f,
            "Share of the route by which the last wave has come.", new AcceptableValueRange<float>(0.1f, 1f));
        PlunderPerRaider = WhiteHiltConfig.BindAdminOnly(Section, "PlunderPerRaider", 0.5f,
            "Percent of the cargo one living raider standing on the trading ship's deck carries off per second.",
            new AcceptableValueRange<float>(0f, 10f));
        AbandonRange = WhiteHiltConfig.BindAdminOnly(Section, "AbandonRange", 300f,
            "A player must be this close to the trading ship, in metres, or the merchant starts to lose heart.",
            new AcceptableValueRange<float>(50f, 1000f));
        AbandonSeconds = WhiteHiltConfig.BindAdminOnly(Section, "AbandonSeconds", 120f,
            "Seconds nobody may be within range before the merchant gives up and the contract fails.",
            new AcceptableValueRange<float>(10f, 900f));
        ArriveRadius = WhiteHiltConfig.BindAdminOnly(Section, "ArriveRadius", 60f,
            "The trading ship has arrived when it is this close to the destination, in metres.", new AcceptableValueRange<float>(20f, 200f));
        Pay = WhiteHiltConfig.BindAdminOnly(Section, "Pay", 600,
            "Coins for a full cargo, to each player in reach when the trading ship arrives. Less cargo pays less.",
            new AcceptableValueRange<int>(0, 10000));
        RewardRange = WhiteHiltConfig.BindAdminOnly(Section, "RewardRange", 150f,
            "Players this close to the trading ship when it arrives are paid, in metres.", new AcceptableValueRange<float>(20f, 500f));
        RaidChartChance = WhiteHiltConfig.BindAdminOnly(Section, "RaidChartChance", 25f,
            "Percent chance per paid player of a Raid Chart on top of the coins.", new AcceptableValueRange<float>(0f, 100f));
        CooldownMinutes = WhiteHiltConfig.BindAdminOnly(Section, "CooldownMinutes", 10f,
            "Least real minutes between two contracts for one player.", new AcceptableValueRange<float>(0f, 240f));
        NoEncounterRange = WhiteHiltConfig.BindAdminOnly(Section, "NoEncounterRange", 400f,
            "No natural Nail Ship encounter is rolled for a ship this close to a trading ship on its way, in metres. 0 turns it off.",
            new AcceptableValueRange<float>(0f, 2000f));
        PayPerKilometre = WhiteHiltConfig.BindAdminOnly(Section, "PayPerKilometre", 400f,
            "With trading posts: coins for a full cargo for every kilometre from the post where the escort begins to the nearest " +
            "other post it sails to, to each player in reach when it arrives. Pay is used for an admin's escort at sea.",
            new AcceptableValueRange<float>(0f, 5000f));
        WaitSeconds = WhiteHiltConfig.BindAdminOnly(Section, "WaitSeconds", 600f,
            "With trading posts: seconds the merchant waits at the post for the contractor to go aboard their own ship; after " +
            "that it sails off and the contract is given back.", new AcceptableValueRange<float>(60f, 3600f));
        StartRange = WhiteHiltConfig.BindAdminOnly(Section, "StartRange", 150f,
            "With trading posts: the escort begins when the contractor stands on their own ship this close to the waiting " +
            "merchant, in metres.", new AcceptableValueRange<float>(20f, 500f));

        AddTranslations();
    }

    // English texts of the whole feature, so one place holds every word. The saga line is in SagaSettings.
    private static void AddTranslations()
    {
        Translations.AddEnglish("whitehilt_tradeship", "Trading Ship");
        Translations.AddEnglishNameAndDescription("item_whitehilt_escortcontract", "Escort Contract",
            "A merchant's seal on a passage. Use it at a trading post: a trading ship comes in and sets out for the nearest other post " +
            "when you go aboard your own ship. See it safely to port. (With the trading posts off, Haldor sells it: use it aboard your ship at open sea.)");
        Translations.AddEnglish("msg_whitehilt_escort_started", "A trading ship joins you. Its port is marked on your map.");
        Translations.AddEnglish("msg_whitehilt_escort_wave", "Nail Ships! They are after the merchant.");
        Translations.AddEnglish("msg_whitehilt_escort_plunder", "Raiders on the merchant's deck! The cargo is being carried off.");
        Translations.AddEnglish("msg_whitehilt_escort_arrived", "The trading ship is safe in port. The merchant pays you {0} coins.");
        Translations.AddEnglish("msg_whitehilt_escort_waiting", "A trading ship comes in to the post. Go aboard your own ship to set out; its port is marked on your map.");
        Translations.AddEnglish("msg_whitehilt_escort_setout", "The merchant casts off. Guard it to port!");
        Translations.AddEnglish("msg_whitehilt_escort_stranded", "The merchant ran aground and found no way on. The escort is called off; you get the contract back and can try again.");
        Translations.AddEnglish("msg_whitehilt_escort_expired", "Nobody came, so the merchant sailed off. You get the contract back.");
        Translations.AddEnglish("msg_whitehilt_escort_refused_post", "Use the contract at a trading post, under its dome.");
        Translations.AddEnglish("msg_whitehilt_escort_refused_noport", "There is no other trading post to sail to.");
        Translations.AddEnglish("msg_whitehilt_escort_refused_noroute", "No sea route leads from here to the nearest other trading post.");
        Translations.AddEnglish("whitehilt_trade_waiting", "Waiting for you to go aboard your ship");
        Translations.AddEnglish("msg_whitehilt_escort_sunk", "The trading ship is lost. The contract has failed.");
        Translations.AddEnglish("msg_whitehilt_escort_plundered", "The cargo is gone. The contract has failed.");
        Translations.AddEnglish("msg_whitehilt_escort_abandoned", "Left alone, the merchant turns back. The contract has failed.");
        Translations.AddEnglish("msg_whitehilt_escort_refused_sea", "You must be aboard your own ship at open sea.");
        Translations.AddEnglish("msg_whitehilt_escort_refused_route", "No clear deep-water passage from here. Sail further out.");
        Translations.AddEnglish("msg_whitehilt_escort_refused_busy", "You already have a merchant to guard, or one sailed with you too recently.");
        Translations.AddEnglish("msg_whitehilt_escort_refused_off", "No trading ship will sail now.");
        Translations.AddEnglish("msg_whitehilt_trade_helm_held", "The merchant's skipper keeps the helm.");
        Translations.AddEnglish("msg_whitehilt_trade_hold_shut", "The merchant's cargo is not yours to open.");
        Translations.AddEnglish("whitehilt_escort_pin", "Merchant's port");
        Translations.AddEnglish("whitehilt_trade_hover", "Cargo {0} %, {1} m to port");
    }
}
