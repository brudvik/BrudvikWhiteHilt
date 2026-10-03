using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Building.Moats;

/// <summary>
/// The kinds of moat the White Hilt hoe digs.
/// </summary>
public enum MoatProfile
{
    /// <summary>A dry V-shaped ditch.</summary>
    Spiked,

    /// <summary>A flat-bottomed ditch with water in it.</summary>
    Wet,

    /// <summary>A V-shaped ditch with sharp stakes in the bottom.</summary>
    Staked
}

/// <summary>
/// Where the earth dug out of a moat goes.
/// </summary>
public enum MoatBank
{
    /// <summary>A bank on the far side of the ditch.</summary>
    Outside,

    /// <summary>A bank on the near side of the ditch.</summary>
    Inside,

    /// <summary>No bank.</summary>
    None
}

/// <summary>
/// Config and texts for moats: the sizes of the three profiles, the bank, the causeways and what a ditch does to creatures.
/// </summary>
public static class MoatSettings
{
    private const string Section = "BuildTools.Moats";

    /// <summary>Whether the hoe may dig moats.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>Distance from the wall to the ditch when following a wall, in metres.</summary>
    public static ConfigEntry<float> Berm { get; private set; }

    /// <summary>Width of the dry V-ditch.</summary>
    public static ConfigEntry<float> SpikedWidth { get; private set; }

    /// <summary>Depth of the dry V-ditch.</summary>
    public static ConfigEntry<float> SpikedDepth { get; private set; }

    /// <summary>Width of the wet moat at the top.</summary>
    public static ConfigEntry<float> WetWidth { get; private set; }

    /// <summary>Width of the wet moat's flat bottom.</summary>
    public static ConfigEntry<float> WetBottom { get; private set; }

    /// <summary>Depth of the wet moat from the lowest edge to the bottom.</summary>
    public static ConfigEntry<float> WetDepth { get; private set; }

    /// <summary>How far the water stays below the lowest edge of the wet moat.</summary>
    public static ConfigEntry<float> Freeboard { get; private set; }

    /// <summary>Whether wet moats get water.</summary>
    public static ConfigEntry<bool> Water { get; private set; }

    /// <summary>Width of the staked ditch.</summary>
    public static ConfigEntry<float> StakedWidth { get; private set; }

    /// <summary>Depth of the staked ditch.</summary>
    public static ConfigEntry<float> StakedDepth { get; private set; }

    /// <summary>Distance between the sharp stakes along the bottom of the staked ditch.</summary>
    public static ConfigEntry<float> StakeSpacing { get; private set; }

    /// <summary>Width of the bank.</summary>
    public static ConfigEntry<float> BankWidth { get; private set; }

    /// <summary>Height of the bank.</summary>
    public static ConfigEntry<float> BankHeight { get; private set; }

    /// <summary>Width of the undug ground left in front of gates.</summary>
    public static ConfigEntry<float> CausewayWidth { get; private set; }

    /// <summary>How far from the clicked piece the connected walls are followed.</summary>
    public static ConfigEntry<float> FollowReach { get; private set; }

    /// <summary>Speed of creatures down in a ditch, as a share of their normal speed.</summary>
    public static ConfigEntry<float> CreatureSpeed { get; private set; }

    /// <summary>Slope at which creatures in a ditch slide, in degrees.</summary>
    public static ConfigEntry<float> CreatureSlideAngle { get; private set; }

    /// <summary>Seconds a creature slides in a ditch before it claws its way out.</summary>
    public static ConfigEntry<float> ClimbOutSeconds { get; private set; }

    /// <summary>The profile picked on the panel.</summary>
    public static MoatProfile Profile { get; set; } = MoatProfile.Spiked;

    /// <summary>Where the earth goes, picked on the panel.</summary>
    public static MoatBank Bank { get; set; } = MoatBank.Outside;

    /// <summary>Whether ground is left in front of gates, picked on the panel.</summary>
    public static bool Causeways { get; set; } = true;

    /// <summary>
    /// Binds the config entries and adds the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        AddTranslations();
        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true, "Allow digging moats with the White Hilt hoe.");
        Berm = Bind("Berm", 3f, "Ground left between a followed wall and the ditch, in metres.", 0f, 15f);
        SpikedWidth = Bind("SpikedWidth", 6f, "Width of the dry V-ditch, in metres.", 2f, 16f);
        SpikedDepth = Bind("SpikedDepth", 2.5f, "Depth of the dry V-ditch, in metres.", 0.5f, 7f);
        WetWidth = Bind("WetWidth", 8f, "Width of the wet moat at the top, in metres.", 3f, 20f);
        WetBottom = Bind("WetBottom", 4f, "Width of the wet moat's flat bottom, in metres.", 1f, 16f);
        WetDepth = Bind("WetDepth", 2.4f, "Depth of the wet moat from its lowest edge to the bottom, in metres.", 1f, 7f);
        Freeboard = Bind("Freeboard", 0.6f, "How far the water stays below the lowest edge of the wet moat, in metres.", 0f, 3f);
        Water = WhiteHiltConfig.BindAdminOnly(Section, "Water", true, "Fill wet moats with water you can swim in.");
        StakedWidth = Bind("StakedWidth", 5f, "Width of the staked ditch, in metres.", 2f, 16f);
        StakedDepth = Bind("StakedDepth", 2f, "Depth of the staked ditch, in metres.", 0.5f, 7f);
        StakeSpacing = Bind("StakeSpacing", 2.6f, "Distance between the sharp stakes along the bottom of the staked ditch, in metres.", 1f, 10f);
        BankWidth = Bind("BankWidth", 5f, "Width of the bank made of the dug earth, in metres.", 1f, 15f);
        BankHeight = Bind("BankHeight", 1.2f, "Height of the bank made of the dug earth, in metres.", 0.2f, 4f);
        CausewayWidth = Bind("CausewayWidth", 5f, "Width of the undug ground left in front of gates, in metres.", 2f, 15f);
        FollowReach = Bind("FollowReach", 60f, "How far from the clicked piece connected walls are followed, in metres.", 10f, 200f);
        CreatureSpeed = Bind("CreatureSpeed", 0.5f, "Speed of creatures down in a ditch, as a share of their normal speed. 1 leaves them alone.", 0.1f, 1f);
        CreatureSlideAngle = Bind("CreatureSlideAngle", 38f,
            "Creatures in a ditch slide down slopes steeper than this, in degrees, like players do everywhere. 90 turns it off.", 10f, 90f);
        ClimbOutSeconds = Bind("ClimbOutSeconds", 10f, "Seconds a creature slides in a ditch before it claws its way out.", 0f, 120f);
    }

    /// <summary>
    /// Width of a profile's ditch.
    /// </summary>
    /// <param name="profile">The profile.</param>
    /// <returns>Metres.</returns>
    public static float Width(MoatProfile profile)
    {
        return profile switch
        {
            MoatProfile.Wet => WetWidth.Value,
            MoatProfile.Staked => StakedWidth.Value,
            _ => SpikedWidth.Value
        };
    }

    /// <summary>
    /// Depth of a profile's ditch.
    /// </summary>
    /// <param name="profile">The profile.</param>
    /// <returns>Metres.</returns>
    public static float Depth(MoatProfile profile)
    {
        return profile switch
        {
            MoatProfile.Wet => WetDepth.Value,
            MoatProfile.Staked => StakedDepth.Value,
            _ => SpikedDepth.Value
        };
    }

    /// <summary>
    /// Name of a profile.
    /// </summary>
    /// <param name="profile">The profile.</param>
    /// <returns>Localized text.</returns>
    public static string ProfileName(MoatProfile profile)
    {
        return Localization.instance.Localize("$whitehilt_moat_profile_" + profile.ToString().ToLowerInvariant());
    }

    /// <summary>
    /// Name of a bank choice.
    /// </summary>
    /// <param name="bank">The choice.</param>
    /// <returns>Localized text.</returns>
    public static string BankName(MoatBank bank)
    {
        return Localization.instance.Localize("$whitehilt_moat_bank_" + bank.ToString().ToLowerInvariant());
    }

    private static ConfigEntry<float> Bind(string key, float value, string description, float min, float max)
    {
        return WhiteHiltConfig.BindAdminOnly(Section, key, value, description, new AcceptableValueRange<float>(min, max));
    }

    private static void AddTranslations()
    {
        Translations.AddEnglish("whitehilt_moat", "Moat");
        Translations.AddEnglish("whitehilt_moat_profile", "Ditch");
        Translations.AddEnglish("whitehilt_moat_profile_spiked", "V-ditch");
        Translations.AddEnglish("whitehilt_moat_profile_wet", "wet moat");
        Translations.AddEnglish("whitehilt_moat_profile_staked", "staked ditch");
        Translations.AddEnglish("whitehilt_moat_bank", "Earth");
        Translations.AddEnglish("whitehilt_moat_bank_outside", "bank outside");
        Translations.AddEnglish("whitehilt_moat_bank_inside", "bank inside");
        Translations.AddEnglish("whitehilt_moat_bank_none", "carried away");
        Translations.AddEnglish("whitehilt_moat_causeways", "Causeways");
        Translations.AddEnglish("whitehilt_moat_pause", "Pause moat");
        Translations.AddEnglish("whitehilt_moat_resume", "Resume moat");
        Translations.AddEnglish("whitehilt_moat_cancel", "Cancel moat");
        Translations.AddEnglish("whitehilt_moat_status", "Moat {0}%   {1}");
        Translations.AddEnglish("whitehilt_moat_hint_start",
            "Moat ({0}): click a wall to follow it, or click points on the ground   Right mouse: stop");
        Translations.AddEnglish("whitehilt_moat_hint_wall",
            "Moat ({0}) round this wall: {1} m in {2} stretches, {3} causeways   Click: dig   Right mouse: back");
        Translations.AddEnglish("whitehilt_moat_hint_points",
            "Moat ({0}): {1} points, {2} m   Click: next point   Click the first point: close the ring   Click the last point: finish   Right mouse: back");
        Translations.AddEnglish("msg_whitehilt_moat_started", "Moat planned: {0} m. It is dug as you walk along it.");
        Translations.AddEnglish("msg_whitehilt_moat_no_wall", "No wall to follow here");
        Translations.AddEnglish("msg_whitehilt_moat_paused", "Moat paused: {0}");
        Translations.AddEnglish("msg_whitehilt_moat_stakes", "Not enough materials for the sharp stakes");
        Translations.AddEnglish("msg_whitehilt_moat_disabled", "Moats are switched off on this server");
    }
}
