using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.SeaBattles.Damage;

/// <summary>
/// Config of holes, flooding and deck fires on ships during a sea battle, section <c>SeaBattles.Damage</c>. Admin
/// only, synced from the server. Also holds the English texts of that damage; the weapons' texts are in
/// <see cref="Weapons.WeaponSettings"/>.
/// </summary>
public static class DamageSettings
{
    private const string Section = "SeaBattles.Damage";

    /// <summary>Holes, flooding and deck fires happen in sea battles.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>A ship within this many metres of a loaded Nail Ship is in battle.</summary>
    public static ConfigEntry<float> BattleRange { get; private set; }

    /// <summary>Least blunt, pierce, slash and chop damage of one hit, before resistances, that makes a hole.</summary>
    public static ConfigEntry<float> HoleMinDamage { get; private set; }

    /// <summary>Holes a ship can have at once.</summary>
    public static ConfigEntry<int> MaxHoles { get; private set; }

    /// <summary>Share of a full ship that one hole lets in per second.</summary>
    public static ConfigEntry<float> FloodPerHole { get; private set; }

    /// <summary>Share of a full ship that drains per second once no hole is open.</summary>
    public static ConfigEntry<float> DrainPerSecond { get; private set; }

    /// <summary>Share of a full ship that one bail removes.</summary>
    public static ConfigEntry<float> BailAmount { get; private set; }

    /// <summary>Stamina one bail costs.</summary>
    public static ConfigEntry<float> BailStamina { get; private set; }

    /// <summary>Seconds between two bails.</summary>
    public static ConfigEntry<float> BailSeconds { get; private set; }

    /// <summary>Metres a full ship lies lower in the water.</summary>
    public static ConfigEntry<float> SinkDepth { get; private set; }

    /// <summary>Share of the sail force a full ship loses.</summary>
    public static ConfigEntry<float> FloodSpeedLoss { get; private set; }

    /// <summary>Hull damage per second while the ship is full; destructible ships only.</summary>
    public static ConfigEntry<float> FloodDamagePerSecond { get; private set; }

    /// <summary>Wood one patch takes.</summary>
    public static ConfigEntry<int> PatchWood { get; private set; }

    /// <summary>Resin one patch takes.</summary>
    public static ConfigEntry<int> PatchResin { get; private set; }

    /// <summary>Share of the hull's health that one patch mends.</summary>
    public static ConfigEntry<float> PatchRepair { get; private set; }

    /// <summary>Seconds a deck fire burns; half that in rain.</summary>
    public static ConfigEntry<float> DeckFireSeconds { get; private set; }

    /// <summary>Metres around a fire that burn.</summary>
    public static ConfigEntry<float> DeckFireRadius { get; private set; }

    /// <summary>Fire damage per second to a player standing in a fire.</summary>
    public static ConfigEntry<float> DeckFireDamage { get; private set; }

    /// <summary>Fire damage per second to the hull, per fire.</summary>
    public static ConfigEntry<float> DeckFireHullDamage { get; private set; }

    /// <summary>Fires a ship can have at once.</summary>
    public static ConfigEntry<int> MaxDeckFires { get; private set; }

    /// <summary>Stamina it costs to stamp out a fire.</summary>
    public static ConfigEntry<float> StampStamina { get; private set; }

    /// <summary>
    /// Binds the config entries and adds the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true,
            "Heavy hits and rams in a sea battle knock holes in ships, holes flood them, and pitch pots set decks alight. Off: none of that happens.");
        BattleRange = WhiteHiltConfig.BindAdminOnly(Section, "BattleRange", 200f,
            "A ship within this many metres of a loaded Nail Ship is in battle.", new AcceptableValueRange<float>(50f, 500f));
        HoleMinDamage = WhiteHiltConfig.BindAdminOnly(Section, "HoleMinDamage", 60f,
            "Least blunt, pierce, slash and chop damage of one hit, before resistances, that knocks a hole in the hull.",
            new AcceptableValueRange<float>(1f, 1000f));
        MaxHoles = WhiteHiltConfig.BindAdminOnly(Section, "MaxHoles", 6,
            "Holes a ship can have at once.", new AcceptableValueRange<int>(1, 20));
        FloodPerHole = WhiteHiltConfig.BindAdminOnly(Section, "FloodPerHole", 0.01f,
            "Share of a full ship that one hole lets in per second.", new AcceptableValueRange<float>(0f, 0.2f));
        DrainPerSecond = WhiteHiltConfig.BindAdminOnly(Section, "DrainPerSecond", 0.005f,
            "Share of a full ship that drains per second once every hole is patched.", new AcceptableValueRange<float>(0f, 0.1f));
        BailAmount = WhiteHiltConfig.BindAdminOnly(Section, "BailAmount", 0.04f,
            "Share of a full ship that one bail removes.", new AcceptableValueRange<float>(0f, 0.5f));
        BailStamina = WhiteHiltConfig.BindAdminOnly(Section, "BailStamina", 8f,
            "Stamina one bail costs.", new AcceptableValueRange<float>(0f, 100f));
        BailSeconds = WhiteHiltConfig.BindAdminOnly(Section, "BailSeconds", 0.6f,
            "Seconds between two bails.", new AcceptableValueRange<float>(0.1f, 5f));
        SinkDepth = WhiteHiltConfig.BindAdminOnly(Section, "SinkDepth", 0.8f,
            "Metres a full ship lies lower in the water.", new AcceptableValueRange<float>(0f, 3f));
        FloodSpeedLoss = WhiteHiltConfig.BindAdminOnly(Section, "FloodSpeedLoss", 0.6f,
            "Share of the sail force a full ship loses.", new AcceptableValueRange<float>(0f, 1f));
        FloodDamagePerSecond = WhiteHiltConfig.BindAdminOnly(Section, "FloodDamagePerSecond", 25f,
            "Hull damage per second while the ship is full of water. White Hilt ships are indestructible and never take it.",
            new AcceptableValueRange<float>(0f, 1000f));
        PatchWood = WhiteHiltConfig.BindAdminOnly(Section, "PatchWood", 2,
            "Wood one patch takes.", new AcceptableValueRange<int>(0, 20));
        PatchResin = WhiteHiltConfig.BindAdminOnly(Section, "PatchResin", 1,
            "Resin one patch takes.", new AcceptableValueRange<int>(0, 20));
        PatchRepair = WhiteHiltConfig.BindAdminOnly(Section, "PatchRepair", 0.1f,
            "Share of the hull's health that one patch mends.", new AcceptableValueRange<float>(0f, 1f));
        DeckFireSeconds = WhiteHiltConfig.BindAdminOnly(Section, "DeckFireSeconds", 30f,
            "Seconds a deck fire burns. Half that in rain.", new AcceptableValueRange<float>(1f, 300f));
        DeckFireRadius = WhiteHiltConfig.BindAdminOnly(Section, "DeckFireRadius", 1.5f,
            "Metres around a fire that burn.", new AcceptableValueRange<float>(0.5f, 5f));
        DeckFireDamage = WhiteHiltConfig.BindAdminOnly(Section, "DeckFireDamage", 8f,
            "Fire damage per second to a player standing in a fire.", new AcceptableValueRange<float>(0f, 200f));
        DeckFireHullDamage = WhiteHiltConfig.BindAdminOnly(Section, "DeckFireHullDamage", 4f,
            "Fire damage per second to the hull, per fire.", new AcceptableValueRange<float>(0f, 200f));
        MaxDeckFires = WhiteHiltConfig.BindAdminOnly(Section, "MaxDeckFires", 6,
            "Fires a ship can have at once.", new AcceptableValueRange<int>(1, 20));
        StampStamina = WhiteHiltConfig.BindAdminOnly(Section, "StampStamina", 10f,
            "Stamina it costs to stamp out a fire.", new AcceptableValueRange<float>(0f, 100f));

        AddTranslations();
    }

    private static void AddTranslations()
    {
        Translations.AddEnglish("whitehilt_ship_hole", "Hole in the hull");
        Translations.AddEnglish("whitehilt_ship_patch", "Patch ({0} wood, {1} resin)");
        Translations.AddEnglish("whitehilt_ship_bail", "Bail water");
        Translations.AddEnglish("whitehilt_ship_flood_hud", "Water in the hull: {0}%");
        Translations.AddEnglish("msg_whitehilt_ship_holed", "The hull is holed!");
        Translations.AddEnglish("msg_whitehilt_ship_sinking", "She is going down!");
        Translations.AddEnglish("msg_whitehilt_ship_nopatch", "You need wood and resin to patch it");
        Translations.AddEnglish("msg_whitehilt_ship_tired", "You are too tired");
        Translations.AddEnglish("whitehilt_ship_fire", "Fire on deck");
        Translations.AddEnglish("msg_whitehilt_ship_fire", "Fire on deck!");
        Translations.AddEnglish("whitehilt_ship_stamp", "Stamp out");
    }
}
