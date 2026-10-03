using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Items.Curing;

/// <summary>
/// Config for curing: how many game days meat and fish hang on the drying rack, and how much rakfisk a tub gives.
/// </summary>
public static class CuringSettings
{
    /// <summary>Seconds in a game day.</summary>
    public const float DaySeconds = 1800f;

    private const string Section = "Curing";

    /// <summary>Game days a seasoned ham hangs before it is cured.</summary>
    public static ConfigEntry<float> HamDays { get; private set; }

    /// <summary>Game days a raw sausage hangs before it is cured.</summary>
    public static ConfigEntry<float> SausageDays { get; private set; }

    /// <summary>Game days a coral cod hangs before it is stockfish.</summary>
    public static ConfigEntry<float> StockfishDays { get; private set; }

    /// <summary>Rakfisk from one tub in the fermenter.</summary>
    public static ConfigEntry<int> RakfiskYield { get; private set; }

    /// <summary>
    /// Binds the config entries and adds the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        HamDays = WhiteHiltConfig.BindAdminOnly(Section, "HamDays", 3f, "Game days a seasoned ham hangs on the drying rack before it is cured.",
            new AcceptableValueRange<float>(0.1f, 20f));
        SausageDays = WhiteHiltConfig.BindAdminOnly(Section, "SausageDays", 2f, "Game days a raw sausage hangs on the drying rack before it is cured.",
            new AcceptableValueRange<float>(0.1f, 20f));
        StockfishDays = WhiteHiltConfig.BindAdminOnly(Section, "StockfishDays", 2f, "Game days a coral cod hangs on the drying rack before it is stockfish.",
            new AcceptableValueRange<float>(0.1f, 20f));
        RakfiskYield = WhiteHiltConfig.BindAdminOnly(Section, "RakfiskYield", 4, "Rakfisk from one tub in the fermenter. Takes effect after a restart.",
            new AcceptableValueRange<int>(1, 12));
        Translations.AddEnglish("whitehilt_dryingrack_hang", "Hang up");
        Translations.AddEnglish("whitehilt_dryingrack_take", "Take down");
        Translations.AddEnglish("msg_whitehilt_dryingrack_nothing", "Nothing to hang up");
    }
}
