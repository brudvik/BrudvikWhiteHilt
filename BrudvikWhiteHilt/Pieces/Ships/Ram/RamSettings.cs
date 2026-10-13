using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Pieces.Ships.Ram;

/// <summary>
/// Config of the Ram Prow, section <c>Ships.Ram</c>. Admin only, synced from the server. The prow's name and
/// description are registered in <see cref="SeaBattles.Weapons.WeaponSettings"/>.
/// </summary>
public static class RamSettings
{
    /// <summary>Prefab name, and feature-gate id, of the Ram Prow upgrade.</summary>
    public const string UpgradeName = "WhiteHiltShipRam";

    private const string Section = "Ships.Ram";

    /// <summary>The Ram Prow can be made and works.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>Least closing speed for a ram, in metres per second.</summary>
    public static ConfigEntry<float> MinSpeed { get; private set; }

    /// <summary>Blunt and chop damage per metre per second of closing speed.</summary>
    public static ConfigEntry<float> DamagePerSpeed { get; private set; }

    /// <summary>Share of the ram's damage that the ramming ship takes; destructible ships only.</summary>
    public static ConfigEntry<float> SelfDamageShare { get; private set; }

    /// <summary>Push on a rammed creature per metre per second of closing speed.</summary>
    public static ConfigEntry<float> PushPerSpeed { get; private set; }

    /// <summary>Seconds before the same target can be rammed again.</summary>
    public static ConfigEntry<float> Cooldown { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true,
            "The Ram Prow can be made and works. Off: it cannot be made and a ship with one rams nothing; ones already made are kept.");
        WhiteHiltConfig.AddFeatureGate(UpgradeName, Enabled);
        MinSpeed = WhiteHiltConfig.BindAdminOnly(Section, "MinSpeed", 3f,
            "Least closing speed for a ram, in metres per second.", new AcceptableValueRange<float>(0f, 20f));
        DamagePerSpeed = WhiteHiltConfig.BindAdminOnly(Section, "DamagePerSpeed", 40f,
            "Blunt and chop damage per metre per second of closing speed.", new AcceptableValueRange<float>(0f, 1000f));
        SelfDamageShare = WhiteHiltConfig.BindAdminOnly(Section, "SelfDamageShare", 0.1f,
            "Share of the ram's damage that the ramming ship takes. White Hilt ships are indestructible and take none.",
            new AcceptableValueRange<float>(0f, 1f));
        PushPerSpeed = WhiteHiltConfig.BindAdminOnly(Section, "PushPerSpeed", 20f,
            "Push on a rammed creature per metre per second of closing speed. A heavy creature is moved less.",
            new AcceptableValueRange<float>(0f, 500f));
        Cooldown = WhiteHiltConfig.BindAdminOnly(Section, "Cooldown", 1.5f,
            "Seconds before the same target can be rammed again.", new AcceptableValueRange<float>(0.1f, 10f));
    }
}
