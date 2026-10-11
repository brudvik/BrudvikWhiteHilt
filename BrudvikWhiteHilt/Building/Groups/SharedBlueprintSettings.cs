using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Building.Groups;

/// <summary>
/// Config and texts for blueprints shared on the server: whether they may be shared, where, and how many and how
/// large they may be.
/// </summary>
public static class SharedBlueprintSettings
{
    private const string Section = "BuildTools.SharedBlueprints";

    /// <summary>Whether players may share blueprints on the server.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>Whether sharing and fetching needs a School Bench nearby.</summary>
    public static ConfigEntry<bool> NeedSchoolBench { get; private set; }

    /// <summary>Most blueprints one player may share in a world.</summary>
    public static ConfigEntry<int> MaxPerPlayer { get; private set; }

    /// <summary>Most pieces in a shared blueprint.</summary>
    public static ConfigEntry<int> MaxPieces { get; private set; }

    /// <summary>Largest shared blueprint, packed, in kilobytes.</summary>
    public static ConfigEntry<int> MaxKilobytes { get; private set; }

    /// <summary>Seconds a player must wait between two shares.</summary>
    public static ConfigEntry<float> ShareCooldownSeconds { get; private set; }

    /// <summary>
    /// Binds the config entries and adds the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true, "Players can share blueprints on the server, for everyone in the world to fetch.");
        NeedSchoolBench = WhiteHiltConfig.BindAdminOnly(Section, "NeedSchoolBench", true, "Sharing and fetching blueprints needs a School Bench nearby.");
        MaxPerPlayer = WhiteHiltConfig.BindAdminOnly(Section, "MaxPerPlayer", 25, "Most blueprints one player may share in a world.",
            new AcceptableValueRange<int>(1, 500));
        MaxPieces = WhiteHiltConfig.BindAdminOnly(Section, "MaxPieces", 5000, "Most pieces in a shared blueprint.", new AcceptableValueRange<int>(10, 50000));
        MaxKilobytes = WhiteHiltConfig.BindAdminOnly(Section, "MaxKilobytes", 256, "Largest shared blueprint, packed, in kilobytes.",
            new AcceptableValueRange<int>(16, 400));
        ShareCooldownSeconds = WhiteHiltConfig.BindAdminOnly(Section, "ShareCooldownSeconds", 10f, "Seconds a player must wait between two shares.",
            new AcceptableValueRange<float>(0f, 600f));
        AddTranslations();
    }

    // The English texts of shared blueprints; Norwegian.json has the translations.
    private static void AddTranslations()
    {
        Translations.AddEnglish("whitehilt_blueprint_mine", "Mine");
        Translations.AddEnglish("whitehilt_blueprint_shared", "Shared");
        Translations.AddEnglish("whitehilt_blueprint_share", "Share");
        Translations.AddEnglish("whitehilt_blueprint_fetch", "Fetch");
        Translations.AddEnglish("whitehilt_blueprint_unshare", "Unshare");
        Translations.AddEnglish("whitehilt_blueprint_yours", "(yours)");
        Translations.AddEnglish("whitehilt_blueprint_by", "Shared by {0}");
        Translations.AddEnglish("whitehilt_blueprint_waiting", "Asking the server for its blueprints...");
        Translations.AddEnglish("whitehilt_blueprint_noserver", "The server does not answer: it needs this mod to share blueprints.");
        Translations.AddEnglish("whitehilt_blueprint_none", "Nobody has shared a blueprint in this world yet. Choose one of yours under Mine and share it.");
        Translations.AddEnglish("whitehilt_blueprint_fetch_hint", "Fetch puts a copy among your own blueprints, to place with the hammer.");
        Translations.AddEnglish("msg_whitehilt_blueprint_disabled", "Sharing blueprints is switched off on this server");
        Translations.AddEnglish("msg_whitehilt_blueprint_needbench", "Go to a School Bench to share and fetch blueprints");
        Translations.AddEnglish("msg_whitehilt_blueprint_shared", "Blueprint shared: {0}");
        Translations.AddEnglish("msg_whitehilt_blueprint_unshared", "No longer shared: {0}");
        Translations.AddEnglish("msg_whitehilt_blueprint_fetched", "Blueprint fetched: {0}");
        Translations.AddEnglish("msg_whitehilt_blueprint_wait", "Wait a little before sharing again");
        Translations.AddEnglish("msg_whitehilt_blueprint_toobig", "The blueprint is too large to share");
        Translations.AddEnglish("msg_whitehilt_blueprint_toomany", "You have shared as many blueprints as the server allows ({0})");
        Translations.AddEnglish("msg_whitehilt_blueprint_empty", "The server knows none of the blueprint's pieces");
        Translations.AddEnglish("msg_whitehilt_blueprint_gone", "That blueprint is no longer shared");
        Translations.AddEnglish("msg_whitehilt_blueprint_notyours", "Only the one who shared a blueprint can unshare it");
        Translations.AddEnglish("msg_whitehilt_blueprint_failed", "The server could not save the blueprint");
        Translations.AddEnglish("msg_whitehilt_blueprint_hammer", "Take out the hammer to place a blueprint");
    }
}
