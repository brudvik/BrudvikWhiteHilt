using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Pieces.Defenses.Siege;

/// <summary>
/// Config and texts for the Oil Cauldron, the Alarm Bell and the Harbour Crane.
/// </summary>
public static class SiegeSettings
{
    private const string Section = "Defences.Siege";

    /// <summary>Fire damage of a cauldron of boiling pitch, before burning.</summary>
    public static ConfigEntry<float> PitchDamage { get; private set; }

    /// <summary>Blunt damage of a cauldron of stones.</summary>
    public static ConfigEntry<float> StoneDamage { get; private set; }

    /// <summary>Seconds pitch must heat before it can be poured.</summary>
    public static ConfigEntry<float> HeatSeconds { get; private set; }

    /// <summary>How far round the Alarm Bell gates, drawbridges, portcullises and doors are shut.</summary>
    public static ConfigEntry<float> BellShutRange { get; private set; }

    /// <summary>How far away players are told the bell rings.</summary>
    public static ConfigEntry<float> BellAlertRange { get; private set; }

    /// <summary>Whether a bell rings by itself when a guestbook nearby notes a raid.</summary>
    public static ConfigEntry<bool> BellRingsOnRaid { get; private set; }

    /// <summary>How far from the Harbour Crane a ship may lie to be loaded.</summary>
    public static ConfigEntry<float> CraneShipRange { get; private set; }

    /// <summary>
    /// Binds the config entries and adds the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        PitchDamage = WhiteHiltConfig.BindAdminOnly(Section, "PitchDamage", 60f, "Fire damage of boiling pitch poured from the Oil Cauldron; it also sets them burning.",
            new AcceptableValueRange<float>(0f, 500f));
        StoneDamage = WhiteHiltConfig.BindAdminOnly(Section, "StoneDamage", 80f, "Blunt damage of stones dropped from the Oil Cauldron.",
            new AcceptableValueRange<float>(0f, 500f));
        HeatSeconds = WhiteHiltConfig.BindAdminOnly(Section, "HeatSeconds", 20f, "Seconds pitch must heat in the Oil Cauldron before it can be poured.",
            new AcceptableValueRange<float>(0f, 120f));
        BellShutRange = WhiteHiltConfig.BindAdminOnly(Section, "BellShutRange", 40f, "How far round the Alarm Bell gates, drawbridges, portcullises and doors are shut when it rings, in metres.",
            new AcceptableValueRange<float>(5f, 100f));
        BellAlertRange = WhiteHiltConfig.BindAdminOnly(Section, "BellAlertRange", 150f, "How far away players are told the Alarm Bell rings and get a mark on the map, in metres.",
            new AcceptableValueRange<float>(20f, 1000f));
        BellRingsOnRaid = WhiteHiltConfig.BindAdminOnly(Section, "BellRingsOnRaid", true, "An Alarm Bell rings by itself when a guestbook near it notes a raid.");
        CraneShipRange = WhiteHiltConfig.BindAdminOnly(Section, "CraneShipRange", 15f, "How far from the Harbour Crane a ship may lie for the crane to load and unload it, in metres.",
            new AcceptableValueRange<float>(5f, 40f));

        Translations.AddEnglish("whitehilt_cauldron_empty", "Empty");
        Translations.AddEnglish("whitehilt_cauldron_pitch", "Boiling pitch");
        Translations.AddEnglish("whitehilt_cauldron_heating", "Pitch heating ({0} s)");
        Translations.AddEnglish("whitehilt_cauldron_stones", "Stones");
        Translations.AddEnglish("whitehilt_cauldron_pour", "Tip the cauldron");
        Translations.AddEnglish("whitehilt_cauldron_load", "Use Resin, Tar or Stone here to fill it");
        Translations.AddEnglish("whitehilt_cauldron_need", "Needs {0} x{1}");
        Translations.AddEnglish("whitehilt_cauldron_full", "The cauldron is already full");
        Translations.AddEnglish("whitehilt_cauldron_not_hot", "The pitch is not hot yet");
        Translations.AddEnglish("whitehilt_cauldron_nothing", "Fill the cauldron first");
        Translations.AddEnglish("whitehilt_bell_ring", "Ring the bell");
        Translations.AddEnglish("whitehilt_bell_rings", "The alarm bell rings! ({0})");
        Translations.AddEnglish("whitehilt_bell_raid", "raid");
        Translations.AddEnglish("whitehilt_bell_pin", "Alarm");
        Translations.AddEnglish("whitehilt_bell_auto", "Rings by itself when the guestbook notes a raid");
        Translations.AddEnglish("whitehilt_crane_open", "Load and unload");
        Translations.AddEnglish("whitehilt_crane_ship", "Ship alongside: {0}");
        Translations.AddEnglish("whitehilt_crane_no_ship", "No ship within {0} m");
    }
}
