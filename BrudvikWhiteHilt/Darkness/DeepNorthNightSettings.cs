using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Darkness;

/// <summary>
/// Config for the night in the Deep North, section "DeepNorthNight". Admin-only and synced from the server.
/// </summary>
public static class DeepNorthNightSettings
{
    private const string Section = "DeepNorthNight";

    /// <summary>Whether the night in the Deep North is worse at all.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>Share of maximum health lost per second out in the cold.</summary>
    public static ConfigEntry<float> ColdHealthDrain { get; private set; }

    /// <summary>Stamina lost per second out in the cold.</summary>
    public static ConfigEntry<float> ColdStaminaDrain { get; private set; }

    /// <summary>Share of the night a white storm blows.</summary>
    public static ConfigEntry<float> WhiteoutShare { get; private set; }

    /// <summary>Minutes a white storm, or a lull between them, lasts.</summary>
    public static ConfigEntry<float> WhiteoutMinutes { get; private set; }

    /// <summary>Whether fear and shadows follow players out into the night.</summary>
    public static ConfigEntry<bool> FearOutside { get; private set; }

    /// <summary>Fewest black wolves in a pack.</summary>
    public static ConfigEntry<int> PackMin { get; private set; }

    /// <summary>Most black wolves in a pack.</summary>
    public static ConfigEntry<int> PackMax { get; private set; }

    /// <summary>Multiplier of the beast chance in the Deep North.</summary>
    public static ConfigEntry<float> BeastChanceMultiplier { get; private set; }

    /// <summary>Seconds out in the open before a pack that lost a player finds them again the same night.</summary>
    public static ConfigEntry<float> PackReturnSeconds { get; private set; }

    /// <summary>
    /// Binds the config entries and registers the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true,
            "At night out in the Deep North the cold bites through any frost resistance, white storms sweep in, fear and shadows follow you out of the light, and the black wolves hunt you until you find warmth.");
        ColdHealthDrain = WhiteHiltConfig.BindAdminOnly(Section, "ColdHealthDrain", 0.004f,
            "Share of maximum health lost per second out in the night of the Deep North without a fire, a roof or a warm place. It never takes the last tenth of your health. 0: off.", new AcceptableValueRange<float>(0f, 0.05f));
        ColdStaminaDrain = WhiteHiltConfig.BindAdminOnly(Section, "ColdStaminaDrain", 2f,
            "Stamina lost per second out in the night of the Deep North without a fire, a roof or a warm place. 0: off.", new AcceptableValueRange<float>(0f, 20f));
        WhiteoutShare = WhiteHiltConfig.BindAdminOnly(Section, "WhiteoutShare", 0.35f,
            "Share of the night a white storm blows in the Deep North: the view closes in, the wind howls, something whispers and the wolves are heard before they are seen. 0: never.", new AcceptableValueRange<float>(0f, 1f));
        WhiteoutMinutes = WhiteHiltConfig.BindAdminOnly(Section, "WhiteoutMinutes", 2f,
            "Minutes a white storm, or a lull between two, lasts.", new AcceptableValueRange<float>(0.5f, 15f));
        FearOutside = WhiteHiltConfig.BindAdminOnly(Section, "FearOutside", true,
            "The fear of the dark and its shadows follow you out into the night of the Deep North: without a light the fear grows, and shadows come.");
        PackMin = WhiteHiltConfig.BindAdminOnly(Section, "PackMin", 3,
            "Fewest Black Wolves in a pack.", new AcceptableValueRange<int>(1, 8));
        PackMax = WhiteHiltConfig.BindAdminOnly(Section, "PackMax", 5,
            "Most Black Wolves in a pack.", new AcceptableValueRange<int>(1, 8));
        BeastChanceMultiplier = WhiteHiltConfig.BindAdminOnly(Section, "BeastChanceMultiplier", 2f,
            "Multiplier of the beast chance in the Deep North.", new AcceptableValueRange<float>(1f, 5f));
        PackReturnSeconds = WhiteHiltConfig.BindAdminOnly(Section, "PackReturnSeconds", 30f,
            "A pack that lost you to a fire or a roof finds your scent again after this many seconds back out in the open, the same night.", new AcceptableValueRange<float>(5f, 300f));

        Translations.AddEnglish("msg_whitehilt_north_cold", "The night cold bites through everything. Find a fire or a roof!");
        Translations.AddEnglish("msg_whitehilt_north_whiteout", "A white storm sweeps in. Wolves howl in it.");
        Translations.AddEnglish("msg_whitehilt_north_whiteout_ends", "The white storm passes");
        Translations.AddEnglish("msg_whitehilt_north_pack_lost", "The pack circles out in the dark. Stay in the warmth...");
        Translations.AddEnglish("msg_whitehilt_north_pack_returns", "The pack has found your scent again!");
        Translations.AddEnglish("se_whitehiltbitingcold", "Biting cold");
        Translations.AddEnglish("se_whitehiltbitingcold_tooltip", "The night of the Deep North. No frost resistance keeps it out; only a fire, a roof or a warm place.");
    }
}
