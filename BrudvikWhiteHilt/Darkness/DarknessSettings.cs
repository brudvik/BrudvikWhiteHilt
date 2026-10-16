using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Darkness;

/// <summary>
/// Config for the creeping darkness, section "Darkness". Everything but the volume and the eyes and sounds is admin-only
/// and synced from the server.
/// </summary>
public static class DarknessSettings
{
    private const string Section = "Darkness";

    /// <summary>Whether the darkness creeps in below ground at all.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>Minutes after the first player goes in before the first darkness.</summary>
    public static ConfigEntry<float> CalmMinutes { get; private set; }

    /// <summary>Minutes between darknesses at first.</summary>
    public static ConfigEntry<float> LongestGapMinutes { get; private set; }

    /// <summary>Minutes between darknesses at the most.</summary>
    public static ConfigEntry<float> ShortestGapMinutes { get; private set; }

    /// <summary>Minutes below ground until the darkness comes as often and lasts as long as it can.</summary>
    public static ConfigEntry<float> RampMinutes { get; private set; }

    /// <summary>Seconds of black at first.</summary>
    public static ConfigEntry<float> ShortestBlackSeconds { get; private set; }

    /// <summary>Seconds of black at the most.</summary>
    public static ConfigEntry<float> LongestBlackSeconds { get; private set; }

    /// <summary>Seconds in the black without a light from no fear to full.</summary>
    public static ConfigEntry<float> FearSeconds { get; private set; }

    /// <summary>Share of maximum health lost per second at full fear.</summary>
    public static ConfigEntry<float> FearDamage { get; private set; }

    /// <summary>Whether shadows come for those without a light.</summary>
    public static ConfigEntry<bool> Shadows { get; private set; }

    /// <summary>The most shadows in one place below ground.</summary>
    public static ConfigEntry<int> MaxShadows { get; private set; }

    /// <summary>How much faster a torch burns down in the black.</summary>
    public static ConfigEntry<float> TorchWear { get; private set; }

    /// <summary>Whether the darkness has eyes and sounds, for each player.</summary>
    public static ConfigEntry<bool> EyesAndSounds { get; private set; }

    /// <summary>Volume of the darkness's sounds, for each player.</summary>
    public static ConfigEntry<float> Volume { get; private set; }

    /// <summary>
    /// Binds the config entries and registers the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true,
            "Below ground (crypts, caves, mines, troll caves and every other dungeon) the light now and then flickers, fades and goes almost black for a while. Only a torch in hand, a circlet or the fires you build hold it off; out of the light fear grows and shadows come.");
        CalmMinutes = WhiteHiltConfig.BindAdminOnly(Section, "CalmMinutes", 3f,
            "Minutes after the first player goes in before the first darkness.", new AcceptableValueRange<float>(0f, 30f));
        LongestGapMinutes = WhiteHiltConfig.BindAdminOnly(Section, "LongestGapMinutes", 4f,
            "Minutes between darknesses at first.", new AcceptableValueRange<float>(0.5f, 30f));
        ShortestGapMinutes = WhiteHiltConfig.BindAdminOnly(Section, "ShortestGapMinutes", 1.5f,
            "Minutes between darknesses once it has crept in fully.", new AcceptableValueRange<float>(0.5f, 30f));
        RampMinutes = WhiteHiltConfig.BindAdminOnly(Section, "RampMinutes", 10f,
            "Minutes after the first darkness until it comes as often and lasts as long as it can.", new AcceptableValueRange<float>(0f, 60f));
        ShortestBlackSeconds = WhiteHiltConfig.BindAdminOnly(Section, "ShortestBlackSeconds", 15f,
            "Seconds of black at first.", new AcceptableValueRange<float>(3f, 120f));
        LongestBlackSeconds = WhiteHiltConfig.BindAdminOnly(Section, "LongestBlackSeconds", 45f,
            "Seconds of black once it has crept in fully.", new AcceptableValueRange<float>(3f, 300f));
        FearSeconds = WhiteHiltConfig.BindAdminOnly(Section, "FearSeconds", 75f,
            "Seconds in the black without a light from no fear to full. Fear falls twice as fast in the light.", new AcceptableValueRange<float>(10f, 600f));
        FearDamage = WhiteHiltConfig.BindAdminOnly(Section, "FearDamage", 0.02f,
            "Share of maximum health lost per second at full fear. 0: fear never hurts.", new AcceptableValueRange<float>(0f, 0.2f));
        Shadows = WhiteHiltConfig.BindAdminOnly(Section, "Shadows", true,
            "From half fear, a shadow comes for a player standing without a light in the black. It only attacks those without a light, light makes it easy to hurt, and it is gone when the light comes back.");
        MaxShadows = WhiteHiltConfig.BindAdminOnly(Section, "MaxShadows", 3,
            "The most shadows in one place below ground.", new AcceptableValueRange<int>(1, 10));
        TorchWear = WhiteHiltConfig.BindAdminOnly(Section, "TorchWear", 3f,
            "How many times as fast a torch burns down in the black; it may also gutter out for a moment.", new AcceptableValueRange<float>(1f, 10f));
        EyesAndSounds = WhiteHiltConfig.BindLocal(Section, "EyesAndSounds", true,
            "Glowing eyes in the black that blink and are gone, scraping in the walls, breathing and whispers. Your own setting.");
        Volume = WhiteHiltConfig.BindLocal(Section, "Volume", 1f,
            "Volume of the darkness's sounds, on top of the effects volume.", new AcceptableValueRange<float>(0f, 1f));

        Translations.AddEnglish("msg_whitehilt_dark_flicker", "The lights flicker...");
        Translations.AddEnglish("msg_whitehilt_dark_falls", "The darkness creeps in!");
        Translations.AddEnglish("msg_whitehilt_dark_lifts", "The light returns");
        Translations.AddEnglish("msg_whitehilt_dark_afraid", "You are afraid of the dark. Find a light!");
        Translations.AddEnglish("msg_whitehilt_dark_panic", "Panic grips you!");
        Translations.AddEnglish("msg_whitehilt_dark_dying", "The dark is killing you!");
        Translations.AddEnglish("msg_whitehilt_dark_gutter", "Your torch gutters!");
        Translations.AddEnglish("se_whitehiltfear", "Fear of the dark");
        Translations.AddEnglish("se_whitehiltfear_tooltip", "Alone in the dark. A torch in hand, a circlet or a fire you built keeps it off.");
        Translations.AddEnglish("whitehilt_dark_lit", "In the light: the fear falls");
        Translations.AddEnglish("whitehilt_dark_unlit", "In the dark: the fear grows");
        Translations.AddEnglish("enemy_whitehilt_shadow", "Shadow");
    }
}
