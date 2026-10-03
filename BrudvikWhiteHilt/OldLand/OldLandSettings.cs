using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.OldLand;

/// <summary>
/// Config of filling land generated before the mod's vegetation came. Admin only, synced from the server.
/// </summary>
public static class OldLandSettings
{
    private const string Section = "OldLand";

    /// <summary>Whether the server fills old land once per world and per kind.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>Metres anything placed in old land keeps from anything built.</summary>
    public static ConfigEntry<float> BuildingDistance { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true,
            "When the server starts, land generated before a spider nest, slate outcrop or forageable came gets its share, once per world and kind, with the same chance as new land. Off: old land keeps what it has.");
        BuildingDistance = WhiteHiltConfig.BindAdminOnly(Section, "BuildingDistance", 50f,
            "Metres anything placed in old land keeps from anything built.", new AcceptableValueRange<float>(0f, 200f));
    }
}
