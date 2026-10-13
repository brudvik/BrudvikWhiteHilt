using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.SeaBattles.Weapons;

/// <summary>
/// Config of the Nail Ships' weapons and the Pitch Pot, section <c>SeaBattles.Weapons</c>. Admin only, synced from
/// the server. Also holds the English texts of the new ship weapons, including the names and descriptions of all
/// four new items (Pitch Pot, Chain Shot, Grapnel Bolt and Ram Prow); the item classes must not register them again.
/// </summary>
public static class WeaponSettings
{
    /// <summary>Prefab name, and feature-gate id, of the Pitch Pot.</summary>
    public const string PitchPotName = "WhiteHiltPitchPot";

    private const string Section = "SeaBattles.Weapons";

    /// <summary>Nail Ship crews throw pitch pots.</summary>
    public static ConfigEntry<bool> PitchPots { get; private set; }

    /// <summary>Seconds between two pots from one Nail Ship.</summary>
    public static ConfigEntry<float> PitchPotSeconds { get; private set; }

    /// <summary>Metres within which a Nail Ship's crew throws pots.</summary>
    public static ConfigEntry<float> PitchPotRange { get; private set; }

    /// <summary>Fire damage of a pot's hit.</summary>
    public static ConfigEntry<float> PitchPotDamage { get; private set; }

    /// <summary>Pots made at once.</summary>
    public static ConfigEntry<int> PitchPotsPerCraft { get; private set; }

    /// <summary>Whether Nail Ships can carry a ballista at all.</summary>
    public static ConfigEntry<bool> NailBallista { get; private set; }

    /// <summary>Global key from which Nail Ships carry a ballista; empty for always.</summary>
    public static ConfigEntry<string> NailBallistaKey { get; private set; }

    /// <summary>Seconds between two shots of a Nail Ship's ballista.</summary>
    public static ConfigEntry<float> NailBallistaSeconds { get; private set; }

    /// <summary>Metres within which a Nail Ship's ballista shoots.</summary>
    public static ConfigEntry<float> NailBallistaRange { get; private set; }

    /// <summary>Spread of a Nail Ship's ballista, in degrees.</summary>
    public static ConfigEntry<float> NailBallistaAccuracy { get; private set; }

    /// <summary>
    /// Binds the config entries and adds the English texts. Call from the plugin's Awake, after
    /// <see cref="SeaBattleSettings.Initialize"/>, whose switch the Pitch Pot follows.
    /// </summary>
    public static void Initialize()
    {
        PitchPots = WhiteHiltConfig.BindAdminOnly(Section, "PitchPots", true,
            "Nail Ship crews throw pitch pots at the deck of the ship they fight.");
        PitchPotSeconds = WhiteHiltConfig.BindAdminOnly(Section, "PitchPotSeconds", 12f,
            "Seconds between two pots from one Nail Ship.", new AcceptableValueRange<float>(2f, 120f));
        PitchPotRange = WhiteHiltConfig.BindAdminOnly(Section, "PitchPotRange", 35f,
            "Metres within which a Nail Ship's crew throws pots.", new AcceptableValueRange<float>(5f, 80f));
        PitchPotDamage = WhiteHiltConfig.BindAdminOnly(Section, "PitchPotDamage", 20f,
            "Fire damage of a pitch pot's hit.", new AcceptableValueRange<float>(0f, 500f));
        PitchPotsPerCraft = WhiteHiltConfig.BindAdminOnly(Section, "PitchPotsPerCraft", 2,
            "Pitch pots made at once.", new AcceptableValueRange<int>(1, 20));
        NailBallista = WhiteHiltConfig.BindAdminOnly(Section, "NailBallista", true,
            "Nail Ships carry a ballista once the world has reached NailBallistaKey.");
        NailBallistaKey = WhiteHiltConfig.BindAdminOnly(Section, "NailBallistaKey", "defeated_dragon",
            "Global key from which Nail Ships carry a ballista (defeated_dragon = Moder is slain). Empty: always.");
        NailBallistaSeconds = WhiteHiltConfig.BindAdminOnly(Section, "NailBallistaSeconds", 10f,
            "Seconds between two shots of a Nail Ship's ballista.", new AcceptableValueRange<float>(2f, 120f));
        NailBallistaRange = WhiteHiltConfig.BindAdminOnly(Section, "NailBallistaRange", 60f,
            "Metres within which a Nail Ship's ballista shoots.", new AcceptableValueRange<float>(10f, 150f));
        NailBallistaAccuracy = WhiteHiltConfig.BindAdminOnly(Section, "NailBallistaAccuracy", 3f,
            "Spread of a Nail Ship's ballista, in degrees.", new AcceptableValueRange<float>(0f, 20f));

        // There is no switch of its own for the Pitch Pot: it belongs to the sea battles.
        WhiteHiltConfig.AddFeatureGate(PitchPotName, SeaBattleSettings.Enabled);
        AddTranslations();
    }

    // The words of the new ship weapons, and the name and description of every new item of the damage-and-weapons
    // feature, so each is registered once.
    private static void AddTranslations()
    {
        Translations.AddEnglish("msg_whitehilt_ship_torn", "The sail is torn!");
        Translations.AddEnglish("whitehilt_ship_grapple", "Grapnel rope");
        Translations.AddEnglish("whitehilt_ship_cut", "Cut the rope");
        Translations.AddEnglish("msg_whitehilt_ship_grappled", "The ships are bound together!");
        Translations.AddEnglish("msg_whitehilt_ship_rope_snaps", "The rope snaps.");
        Translations.AddEnglish("whitehilt_ballista_ammo", "Ammunition: {0} ({1})");
        Translations.AddEnglish("whitehilt_ballista_switch", "Switch ammunition");

        Translations.AddEnglishNameAndDescription("item_whitehiltpitchpot", "Pitch Pot",
            "A clay pot of hot pine tar with a burning rag. Throw it at a deck to set it alight.");
        Translations.AddEnglishNameAndDescription("item_whitehiltchainshot", "Chain Shot",
            "Two iron balls on a chain, for the Ship Ballista. Tears a ship's sail.");
        Translations.AddEnglishNameAndDescription("item_whitehiltgrapnelbolt", "Grapnel Bolt",
            "A hooked bolt on a rope, for the Ship Ballista. Binds a ship to yours and pulls it close.");
        Translations.AddEnglishNameAndDescription("item_whitehiltshipram", "Ram Prow",
            "An iron-shod prow for the White Hilt Ship or Skidbladnir. Use it on the mast. Ramming bow-first hurts in proportion to your speed.");
    }
}
