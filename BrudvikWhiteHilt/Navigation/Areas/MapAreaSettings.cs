using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Navigation.Areas;

/// <summary>
/// Config for built areas, fields, pastures and wards on the map, section "Map.Areas". What is scanned and shared is
/// the server's; what is drawn is each player's.
/// </summary>
public static class MapAreaSettings
{
    private const string Section = "Map.Areas";

    /// <summary>Whether the server finds built areas, fields, pastures and wards at all.</summary>
    public static ConfigEntry<bool> AllowAreas { get; private set; }

    /// <summary>Whether players see what others have built, or only their own.</summary>
    public static ConfigEntry<bool> ShowOthers { get; private set; }

    /// <summary>Fewest building pieces for a built area.</summary>
    public static ConfigEntry<int> MinPieces { get; private set; }

    /// <summary>Fewest growing or planted crops for a field.</summary>
    public static ConfigEntry<int> MinPlants { get; private set; }

    /// <summary>Fewest tamed animals for a pasture.</summary>
    public static ConfigEntry<int> MinAnimals { get; private set; }

    /// <summary>Whether the player draws built areas.</summary>
    public static ConfigEntry<bool> ShowBuildings { get; private set; }

    /// <summary>Whether the player draws fields.</summary>
    public static ConfigEntry<bool> ShowFields { get; private set; }

    /// <summary>Whether the player draws pastures.</summary>
    public static ConfigEntry<bool> ShowPastures { get; private set; }

    /// <summary>Whether the player draws the reach of wards.</summary>
    public static ConfigEntry<bool> ShowWards { get; private set; }

    /// <summary>Whether names are shown on the large map when zoomed in.</summary>
    public static ConfigEntry<bool> ShowLabels { get; private set; }

    /// <summary>
    /// Binds the config entries and adds the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        AllowAreas = WhiteHiltConfig.BindAdminOnly(Section, "AllowAreas", true,
            "Find built areas, fields, pastures and wards in the world and show them on the players' maps.");
        ShowOthers = WhiteHiltConfig.BindAdminOnly(Section, "ShowOthers", true,
            "Players see what everyone has built. Off: each player sees only their own buildings and wards, and the fields and pastures beside them.");
        MinPieces = WhiteHiltConfig.BindAdminOnly(Section, "MinPieces", 5, "Fewest building pieces for a built area; fewer, e.g. a lone campfire, are left out.",
            new AcceptableValueRange<int>(1, 1000));
        MinPlants = WhiteHiltConfig.BindAdminOnly(Section, "MinPlants", 4, "Fewest planted crops for a field.", new AcceptableValueRange<int>(1, 1000));
        MinAnimals = WhiteHiltConfig.BindAdminOnly(Section, "MinAnimals", 2, "Fewest tamed animals for a pasture.", new AcceptableValueRange<int>(1, 1000));

        ShowBuildings = WhiteHiltConfig.BindLocal(Section, "ShowBuildings", true,
            "Draw built areas on the map: filled in the builder's colour, bordered gold for a base (bed and fire), blue for an outpost (workbench or portal), white for other buildings.");
        ShowFields = WhiteHiltConfig.BindLocal(Section, "ShowFields", true, "Draw fields of planted crops on the map in green.");
        ShowPastures = WhiteHiltConfig.BindLocal(Section, "ShowPastures", true, "Draw pastures with tamed animals on the map in brown, with the number of animals.");
        ShowWards = WhiteHiltConfig.BindLocal(Section, "ShowWards", true, "Draw the reach of wards on the map as a dashed ring: green when on, grey when off.");
        ShowLabels = WhiteHiltConfig.BindLocal(Section, "ShowLabels", true,
            "Show names on the large map when zoomed in. A sign in the area whose text starts with # names it, e.g. #Brudvik.");

        ShowBuildings.SettingChanged += (_, _) => MapAreaOverlay.MarkDirty();
        ShowFields.SettingChanged += (_, _) => MapAreaOverlay.MarkDirty();
        ShowPastures.SettingChanged += (_, _) => MapAreaOverlay.MarkDirty();
        ShowWards.SettingChanged += (_, _) => MapAreaOverlay.MarkDirty();

        Translations.AddEnglish("whitehilt_area_base", "Base");
        Translations.AddEnglish("whitehilt_area_outpost", "Outpost");
        Translations.AddEnglish("whitehilt_area_building", "Building");
        Translations.AddEnglish("whitehilt_area_field", "Field");
        Translations.AddEnglish("whitehilt_area_pasture", "Pasture");
        Translations.AddEnglish("whitehilt_area_plants", "{0} plants");
        Translations.AddEnglish("whitehilt_area_animals", "{0} animals");
    }
}
