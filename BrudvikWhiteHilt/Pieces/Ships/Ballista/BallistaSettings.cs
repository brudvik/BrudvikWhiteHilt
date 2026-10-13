using BepInEx.Configuration;
using BrudvikWhiteHilt.Items.ShipUpgrades;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Pieces.Ships.Ballista;

/// <summary>
/// Config of the Ship Ballista and its ammunition (Ship Bolts, Chain Shots and Grapnel Bolts), section
/// <c>Ships.Ballista</c>. Admin only, synced from the server.
/// </summary>
public static class BallistaSettings
{
    /// <summary>Prefab name, and feature-gate id, of the Chain Shot.</summary>
    public const string ChainShotName = "WhiteHiltChainShot";

    /// <summary>Prefab name, and feature-gate id, of the Grapnel Bolt.</summary>
    public const string GrapnelBoltName = "WhiteHiltGrapnelBolt";

    private const string Section = "Ships.Ballista";

    /// <summary>The ballista upgrade and its ammunition can be made, and the ballista used.</summary>
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

    /// <summary>Pierce damage of a Chain Shot.</summary>
    public static ConfigEntry<float> ChainShotDamage { get; private set; }

    /// <summary>Share of its sail force a torn sail loses.</summary>
    public static ConfigEntry<float> SailTearShare { get; private set; }

    /// <summary>Seconds a sail stays torn.</summary>
    public static ConfigEntry<float> SailTearSeconds { get; private set; }

    /// <summary>Chain Shots made at once.</summary>
    public static ConfigEntry<int> ChainShotsPerCraft { get; private set; }

    /// <summary>Pierce damage of a Grapnel Bolt.</summary>
    public static ConfigEntry<float> GrapnelDamage { get; private set; }

    /// <summary>Metres the rope pulls the two ships to.</summary>
    public static ConfigEntry<float> GrappleLength { get; private set; }

    /// <summary>Metres between the ships at which the rope snaps.</summary>
    public static ConfigEntry<float> GrappleBreak { get; private set; }

    /// <summary>Seconds the rope holds before it frays.</summary>
    public static ConfigEntry<float> GrappleSeconds { get; private set; }

    /// <summary>Pull of the rope, in metres per second squared per metre of slack.</summary>
    public static ConfigEntry<float> GrapplePull { get; private set; }

    /// <summary>How much of the pull a ship's speed towards the other ship takes off, per second.</summary>
    public static ConfigEntry<float> GrappleDamping { get; private set; }

    /// <summary>Grapnel Bolts made at once.</summary>
    public static ConfigEntry<int> GrapnelsPerCraft { get; private set; }

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
        WhiteHiltConfig.AddFeatureGate(ChainShotName, Enabled);
        WhiteHiltConfig.AddFeatureGate(GrapnelBoltName, Enabled);
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
        ChainShotDamage = WhiteHiltConfig.BindAdminOnly(Section, "ChainShotDamage", 20f,
            "Pierce damage of a Chain Shot.", new AcceptableValueRange<float>(0f, 1000f));
        SailTearShare = WhiteHiltConfig.BindAdminOnly(Section, "SailTearShare", 0.5f,
            "Share of its sail force a ship loses while its sail is torn by a Chain Shot.", new AcceptableValueRange<float>(0f, 1f));
        SailTearSeconds = WhiteHiltConfig.BindAdminOnly(Section, "SailTearSeconds", 45f,
            "Seconds a sail stays torn.", new AcceptableValueRange<float>(1f, 600f));
        ChainShotsPerCraft = WhiteHiltConfig.BindAdminOnly(Section, "ChainShotsPerCraft", 5,
            "Chain Shots made at once at the Shipwright's Bench.", new AcceptableValueRange<int>(1, 50));
        GrapnelDamage = WhiteHiltConfig.BindAdminOnly(Section, "GrapnelDamage", 10f,
            "Pierce damage of a Grapnel Bolt.", new AcceptableValueRange<float>(0f, 1000f));
        GrappleLength = WhiteHiltConfig.BindAdminOnly(Section, "GrappleLength", 6f,
            "Metres the rope pulls the two ships to.", new AcceptableValueRange<float>(2f, 30f));
        GrappleBreak = WhiteHiltConfig.BindAdminOnly(Section, "GrappleBreak", 60f,
            "Metres between the ships at which the rope snaps.", new AcceptableValueRange<float>(10f, 200f));
        GrappleSeconds = WhiteHiltConfig.BindAdminOnly(Section, "GrappleSeconds", 90f,
            "Seconds the rope holds before it frays.", new AcceptableValueRange<float>(5f, 600f));
        GrapplePull = WhiteHiltConfig.BindAdminOnly(Section, "GrapplePull", 1.5f,
            "Pull of the rope: acceleration in metres per second squared for each metre of slack.", new AcceptableValueRange<float>(0f, 10f));
        GrappleDamping = WhiteHiltConfig.BindAdminOnly(Section, "GrappleDamping", 0.5f,
            "How hard the rope holds back a ship that closes in fast on the other one, per second. 0 lets the ships swing past each other.",
            new AcceptableValueRange<float>(0f, 5f));
        GrapnelsPerCraft = WhiteHiltConfig.BindAdminOnly(Section, "GrapnelsPerCraft", 3,
            "Grapnel Bolts made at once at the Shipwright's Bench.", new AcceptableValueRange<int>(1, 50));
    }
}
