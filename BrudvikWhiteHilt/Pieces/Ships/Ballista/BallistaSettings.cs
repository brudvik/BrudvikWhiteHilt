using BepInEx.Configuration;
using BrudvikWhiteHilt.Items.ShipUpgrades;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Pieces.Ships.Ballista;

/// <summary>
/// Config of the Ship Ballista and the Ship Bolts, section <c>Ships.Ballista</c>. Admin only, synced from the server.
/// </summary>
public static class BallistaSettings
{
    private const string Section = "Ships.Ballista";

    /// <summary>The ballista upgrade and the bolts can be made, and the ballista used.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>Pierce damage of a Ship Bolt.</summary>
    public static ConfigEntry<float> Damage { get; private set; }

    /// <summary>Seconds between shots.</summary>
    public static ConfigEntry<float> ReloadSeconds { get; private set; }

    /// <summary>Speed of a bolt, in metres per second.</summary>
    public static ConfigEntry<float> BoltSpeed { get; private set; }

    /// <summary>Random spread of a shot, in degrees.</summary>
    public static ConfigEntry<float> Accuracy { get; private set; }

    /// <summary>Degrees the ballista can turn either side of straight ahead.</summary>
    public static ConfigEntry<float> YawArc { get; private set; }

    /// <summary>Degrees the ballista can aim down, as a negative number.</summary>
    public static ConfigEntry<float> PitchMin { get; private set; }

    /// <summary>Degrees the ballista can aim up.</summary>
    public static ConfigEntry<float> PitchMax { get; private set; }

    /// <summary>Bolts made at once.</summary>
    public static ConfigEntry<int> BoltsPerCraft { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake, after <see cref="ShipSettings.Initialize"/>.
    /// </summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true,
            "The Ship Ballista upgrade and Ship Bolts can be made, and the ballista used. Off: none of them can be made " +
            "and the ballista cannot be manned; ones already made are kept.");
        WhiteHiltConfig.AddFeatureGate(ShipBallistaUpgrade.ItemName, Enabled);
        WhiteHiltConfig.AddFeatureGate(ShipBolts.PrefabName, Enabled);
        Damage = WhiteHiltConfig.BindAdminOnly(Section, "Damage", 80f,
            "Pierce damage of a Ship Bolt.", new AcceptableValueRange<float>(1f, 1000f));
        ReloadSeconds = WhiteHiltConfig.BindAdminOnly(Section, "ReloadSeconds", 4f,
            "Seconds between shots.", new AcceptableValueRange<float>(0.5f, 30f));
        BoltSpeed = WhiteHiltConfig.BindAdminOnly(Section, "BoltSpeed", 60f,
            "Speed of a bolt, in metres per second.", new AcceptableValueRange<float>(10f, 150f));
        Accuracy = WhiteHiltConfig.BindAdminOnly(Section, "Accuracy", 1f,
            "Random spread of a shot, in degrees. 0 is dead on.", new AcceptableValueRange<float>(0f, 10f));
        YawArc = WhiteHiltConfig.BindAdminOnly(Section, "YawArc", 70f,
            "Degrees the ballista can turn to either side of straight ahead.", new AcceptableValueRange<float>(10f, 170f));
        PitchMin = WhiteHiltConfig.BindAdminOnly(Section, "PitchMin", -10f,
            "Degrees the ballista can aim down, as a negative number.", new AcceptableValueRange<float>(-45f, 60f));
        PitchMax = WhiteHiltConfig.BindAdminOnly(Section, "PitchMax", 25f,
            "Degrees the ballista can aim up.", new AcceptableValueRange<float>(-45f, 60f));
        BoltsPerCraft = WhiteHiltConfig.BindAdminOnly(Section, "BoltsPerCraft", 10,
            "Ship Bolts made at once at the Shipwright's Bench.", new AcceptableValueRange<int>(1, 50));
    }
}
