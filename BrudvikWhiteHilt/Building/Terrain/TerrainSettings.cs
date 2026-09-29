using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using UnityEngine;

namespace BrudvikWhiteHilt.Building.Terrain;

/// <summary>
/// Config and texts for the White Hilt hoe and cultivator tools: roads, levelling, ramps, painting, resetting, big brush,
/// grid planting, cultivating and harvesting areas.
/// </summary>
public static class TerrainSettings
{
    private const string Section = "BuildTools.Terrain";
    private const string KeySection = "BuildTools.Terrain.Keys";
    private const string HoePrefab = "WhiteHiltHoe";
    private const string CultivatorPrefab = "WhiteHiltCultivator";

    /// <summary>Whether the hoe and cultivator tools may be used.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>Multiplier on the stone that paving and raising ground cost, compared with the vanilla hoe.</summary>
    public static ConfigEntry<float> CostMultiplier { get; private set; }

    /// <summary>Whether ripe crops in an area may be harvested at once.</summary>
    public static ConfigEntry<bool> HarvestArea { get; private set; }

    /// <summary>Largest area one tool may change, in square metres.</summary>
    public static ConfigEntry<float> MaxArea { get; private set; }

    /// <summary>How long terrain changes may be undone, in seconds.</summary>
    public static ConfigEntry<float> UndoSeconds { get; private set; }

    /// <summary>Whether growth markers show over plants while holding the cultivator.</summary>
    public static ConfigEntry<bool> ShowGrowth { get; private set; }

    /// <summary>Road width in metres when a road is planned.</summary>
    public static ConfigEntry<float> RoadWidth { get; private set; }

    /// <summary>Road planning on the map.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyRoad { get; private set; }

    /// <summary>Level an area (hoe) or cultivate an area (cultivator).</summary>
    public static ConfigEntry<KeyboardShortcut> KeyArea { get; private set; }

    /// <summary>Ramp (hoe) or grid planting (cultivator).</summary>
    public static ConfigEntry<KeyboardShortcut> KeyShape { get; private set; }

    /// <summary>Paint an area.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyPaint { get; private set; }

    /// <summary>Reset an area to the original terrain.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyReset { get; private set; }

    /// <summary>Sets the height reference.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyReference { get; private set; }

    /// <summary>Harvest an area.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyHarvest { get; private set; }

    /// <summary>
    /// True if the player holds the White Hilt hoe and the tools are on.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>True if so.</returns>
    public static bool HoldingHoe(Player player)
    {
        return Holding(player, HoePrefab);
    }

    /// <summary>
    /// True if the player holds the White Hilt cultivator and the tools are on.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>True if so.</returns>
    public static bool HoldingCultivator(Player player)
    {
        return Holding(player, CultivatorPrefab);
    }

    /// <summary>
    /// Binds the config entries and adds the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        AddTranslations();
        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true, "Allow the White Hilt hoe and cultivator tools.");
        CostMultiplier = WhiteHiltConfig.BindAdminOnly(Section, "CostMultiplier", 1f,
            "Stone for paving and raising ground, as a multiple of what the vanilla hoe costs for the same area and height.",
            new AcceptableValueRange<float>(0f, 10f));
        HarvestArea = WhiteHiltConfig.BindAdminOnly(Section, "HarvestArea", true, "Allow harvesting every ripe crop in an area at once with the cultivator.");
        MaxArea = WhiteHiltConfig.BindAdminOnly(Section, "MaxArea", 2500f, "Largest area one tool may change at once, in square metres.",
            new AcceptableValueRange<float>(16f, 40000f));
        UndoSeconds = WhiteHiltConfig.BindAdminOnly(Section, "UndoSeconds", 60f, "How long terrain changes may be undone, in seconds. 0 turns it off.",
            new AcceptableValueRange<float>(0f, 600f));
        ShowGrowth = WhiteHiltConfig.BindLocal(Section, "ShowGrowth", true, "Show growth markers over plants while holding the White Hilt cultivator.");
        RoadWidth = WhiteHiltConfig.BindLocal(Section, "RoadWidth", 4f, "Width of new roads in metres.");

        KeyRoad = BindKey("Road", KeyCode.R, "Hoe: plan a road on the map. Cultivator: replant the empty places of the last grid.");
        KeyArea = BindKey("Area", KeyCode.F, "Hoe: level an area. Cultivator: cultivate an area.");
        KeyShape = BindKey("Shape", KeyCode.G, "Hoe: ramp with an even slope. Cultivator: plant in a grid.");
        KeyPaint = BindKey("Paint", KeyCode.P, "Hoe: paint an area with stone, dirt or grass.");
        KeyReset = BindKey("Reset", KeyCode.T, "Hoe: put an area back to the original terrain.");
        KeyReference = BindKey("Reference", KeyCode.M, "Hoe: set the height reference to where you aim.");
        KeyHarvest = BindKey("Harvest", KeyCode.H, "Cultivator: harvest every ripe crop in an area.");
    }

    private static bool Holding(Player player, string prefab)
    {
        ItemDrop.ItemData item = player != null ? player.GetRightItem() : null;
        return Enabled != null && Enabled.Value && item?.m_dropPrefab != null && item.m_dropPrefab.name == prefab;
    }

    private static ConfigEntry<KeyboardShortcut> BindKey(string key, KeyCode main, string description)
    {
        return WhiteHiltConfig.BindLocal(KeySection, key, new KeyboardShortcut(main, KeyCode.LeftControl), description);
    }

    private static void AddTranslations()
    {
        Translations.AddEnglish("whitehilt_terrain", "Terrain");
        Translations.AddEnglish("whitehilt_farm", "Field");
        Translations.AddEnglish("whitehilt_terrain_road", "Road");
        Translations.AddEnglish("whitehilt_terrain_level", "Level");
        Translations.AddEnglish("whitehilt_terrain_ramp", "Ramp");
        Translations.AddEnglish("whitehilt_terrain_paint", "Paint");
        Translations.AddEnglish("whitehilt_terrain_reset", "Reset");
        Translations.AddEnglish("whitehilt_terrain_reference", "Reference");
        Translations.AddEnglish("whitehilt_terrain_surface", "Surface");
        Translations.AddEnglish("whitehilt_terrain_surface_none", "none");
        Translations.AddEnglish("whitehilt_terrain_surface_stone", "stone");
        Translations.AddEnglish("whitehilt_terrain_surface_dirt", "dirt");
        Translations.AddEnglish("whitehilt_terrain_surface_grass", "grass");
        Translations.AddEnglish("whitehilt_terrain_width", "Width");
        Translations.AddEnglish("whitehilt_terrain_grade", "Slope");
        Translations.AddEnglish("whitehilt_terrain_grade_free", "free");
        Translations.AddEnglish("whitehilt_terrain_follow", "Road height");
        Translations.AddEnglish("whitehilt_terrain_follow_terrain", "Road follows ground");
        Translations.AddEnglish("whitehilt_terrain_follow_even", "Road with even slope");
        Translations.AddEnglish("whitehilt_terrain_pause", "Pause road");
        Translations.AddEnglish("whitehilt_terrain_resume", "Resume road");
        Translations.AddEnglish("whitehilt_terrain_cancel", "Cancel road");
        Translations.AddEnglish("whitehilt_terrain_height", "Height {0}");
        Translations.AddEnglish("whitehilt_terrain_height_ref", "Height {0}   from reference {1}");
        Translations.AddEnglish("whitehilt_terrain_brush", "Brush {0}   [{1}]+wheel");
        Translations.AddEnglish("whitehilt_terrain_road_status", "Road {0}%   {1}");
        Translations.AddEnglish("whitehilt_terrain_road_building", "being built as you walk along it");
        Translations.AddEnglish("whitehilt_terrain_road_paused", "paused");
        Translations.AddEnglish("whitehilt_terrain_road_water", "stops at water");
        Translations.AddEnglish("whitehilt_terrain_road_done", "done");
        Translations.AddEnglish("whitehilt_terrain_hint_rect", "{0}: click the first corner   Right mouse: stop");
        Translations.AddEnglish("whitehilt_terrain_hint_rect2", "{0}: {1} x {2} m   click the second corner   Arrows: fixed size   Right mouse: back");
        Translations.AddEnglish("whitehilt_terrain_hint_level",
            "Level: {0} x {1} m at {2}   PgUp/PgDn: height   Shift+click: height from where you aim   Click: level   Right mouse: back\nDig {3} m³   fill {4} m³   {5}");
        Translations.AddEnglish("whitehilt_terrain_hint_ramp",
            "Ramp: click the {0}   Arrows up/down: width {1} m   Right mouse: back\n{2}");
        Translations.AddEnglish("whitehilt_terrain_ramp_start", "bottom");
        Translations.AddEnglish("whitehilt_terrain_ramp_end", "top");
        Translations.AddEnglish("whitehilt_terrain_ramp_info", "Length {0} m   rise {1} m   slope {2}% ({3})   {4}");
        Translations.AddEnglish("whitehilt_terrain_hint_road",
            "Road: click points on the map, double-click or close the map to finish   {0} points, {1} m");
        Translations.AddEnglish("whitehilt_terrain_cost", "Cost: {0}");
        Translations.AddEnglish("whitehilt_terrain_free", "Free");
        Translations.AddEnglish("whitehilt_farm_grid", "Grid");
        Translations.AddEnglish("whitehilt_farm_cultivate", "Cultivate area");
        Translations.AddEnglish("whitehilt_farm_refill", "Refill");
        Translations.AddEnglish("whitehilt_farm_harvest", "Harvest area");
        Translations.AddEnglish("whitehilt_farm_auto", "Cultivate under");
        Translations.AddEnglish("whitehilt_farm_growth", "Growth");
        Translations.AddEnglish("whitehilt_farm_hint_grid",
            "{0}: {1} rows x {2} columns, {3} m apart   Arrows: rows/columns   PgUp/PgDn: spacing   Wheel: turn   Click: plant   Right mouse: stop\n{4}");
        Translations.AddEnglish("whitehilt_farm_grid_status", "{0} of {1} places can be planted   {2}");
        Translations.AddEnglish("whitehilt_farm_ready_in", "{0}");
        Translations.AddEnglish("whitehilt_farm_ready", "ready soon");
        Translations.AddEnglish("msg_whitehilt_terrain_disabled", "The terrain tools are switched off on this server");
        Translations.AddEnglish("msg_whitehilt_terrain_too_big", "Too big: {0} m² (the server allows {1} m²)");
        Translations.AddEnglish("msg_whitehilt_terrain_limit", "Some ground cannot go more than 8 m above or below where it started");
        Translations.AddEnglish("msg_whitehilt_terrain_missing", "Not enough {0}: {1} needed");
        Translations.AddEnglish("msg_whitehilt_terrain_reference", "Height reference set to {0}");
        Translations.AddEnglish("msg_whitehilt_terrain_road_started", "Road planned: {0} m. It is built as you walk along it.");
        Translations.AddEnglish("msg_whitehilt_terrain_road_paused", "Road paused: {0}");
        Translations.AddEnglish("msg_whitehilt_terrain_undone", "Terrain change taken back");
        Translations.AddEnglish("msg_whitehilt_farm_no_plant", "Pick a plant in the cultivator first");
        Translations.AddEnglish("msg_whitehilt_farm_no_grid", "Plant a grid first");
        Translations.AddEnglish("msg_whitehilt_farm_harvested", "{0} crops harvested");
        Translations.AddEnglish("msg_whitehilt_farm_harvest_off", "Harvesting areas is switched off on this server");
        Translations.AddEnglish("msg_whitehilt_farm_nothing", "Nowhere to plant here");
    }
}
