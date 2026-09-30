using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using System.Collections.Generic;

namespace BrudvikWhiteHilt.Planting;

/// <summary>
/// Config and texts for planting vanilla berry bushes, mushrooms, flowers, debris and decorative flora with the cultivator.
/// </summary>
public static class PlantingSettings
{
    private const string Section = "Planting";
    private const string CostSection = "Planting.Costs";
    private const string SaplingSection = "Planting.Saplings";

    /// <summary>
    /// Everything the cultivator can plant. Growth, respawn times and yields stay vanilla.
    /// </summary>
    public static readonly IReadOnlyList<PlantableDefinition> Definitions = new List<PlantableDefinition>
    {
        new("RaspberryBush", PlantableKind.Pickable, "Raspberry bush", 5),
        new("BlueberryBush", PlantableKind.Pickable, "Blueberry bush", 5),
        new("CloudberryBush", PlantableKind.Pickable, "Cloudberry bush", 5),
        new("LingonberryBush", PlantableKind.Pickable, "Lingonberry bush", 5),
        new("Pickable_Mushroom", PlantableKind.Pickable, "Mushrooms", 5),
        new("Pickable_Mushroom_yellow", PlantableKind.Pickable, "Yellow mushrooms", 5),
        new("Pickable_Mushroom_blue", PlantableKind.Pickable, "Blue mushrooms", 5),
        new("Pickable_Thistle", PlantableKind.Pickable, "Thistle", 5),
        new("Pickable_Dandelion", PlantableKind.Pickable, "Dandelion", 5),
        new("Pickable_SmokePuff", PlantableKind.Pickable, "Smoke puffs", 5),
        new("Pickable_Fiddlehead", PlantableKind.Pickable, "Fiddlehead fern", 15),
        new("Pickable_Branch", PlantableKind.Debris, "Branch", 5),
        new("Pickable_Stone", PlantableKind.Debris, "Stone", 1),
        new("Pickable_Flint", PlantableKind.Debris, "Flint", 5),
        new("Beech_small1", PlantableKind.Flora, "Small beech", 1, "BeechSeeds"),
        new("FirTree_small", PlantableKind.Flora, "Small fir", 1, "FirCone"),
        new("FirTree_small_dead", PlantableKind.Flora, "Small dead fir", 1, "FirCone"),
        new("Bush01", PlantableKind.Flora, "Bush", 2, "Wood"),
        new("Bush01_heath", PlantableKind.Flora, "Heath bush", 2, "Wood"),
        new("Bush02_en", PlantableKind.Flora, "Plains bush", 3, "Wood"),
        new("shrub_2", PlantableKind.Flora, "Shrub", 2, "Wood"),
        new("shrub_2_heath", PlantableKind.Flora, "Heath shrub", 2, "Wood"),
        new("YggaShoot_small1", PlantableKind.Flora, "Small Yggdrasil shoot", 1, "YggdrasilWood", extraResource: "Wood", extraAmount: 2),
        new("vines", PlantableKind.Flora, "Vines", 2, "Wood", grounded: false),
        new("FernAshlands", PlantableKind.Flora, "Ashlands fern", 2, "Wood"),
        new SaplingDefinition("Ancient_Sapling", "Ancient sapling", "AncientSeed", SaplingLook.AncientBark, new[] { "SwampTree1" },
            Heightmap.Biome.Meadows | Heightmap.Biome.Swamp | Heightmap.Biome.BlackForest | Heightmap.Biome.Plains, true, 2f),
        new SaplingDefinition("Autumn_Birch_Sapling", "Autumn birch sapling", "BirchSeeds", SaplingLook.AutumnLeaves, new[] { "Birch1_aut", "Birch2_aut" },
            Heightmap.Biome.Meadows | Heightmap.Biome.BlackForest | Heightmap.Biome.Plains, true, 1f),
        new SaplingDefinition("Ygga_Sapling", "Yggdrasil sapling", "Sap", SaplingLook.YggaShoot, new[] { "YggaShoot1", "YggaShoot2", "YggaShoot3" },
            Heightmap.Biome.Meadows | Heightmap.Biome.BlackForest | Heightmap.Biome.Plains | Heightmap.Biome.Mistlands, false, 2f),
        new SaplingDefinition("Ashwood_Sapling", "Ashwood sapling", "BeechSeeds", SaplingLook.Ashwood,
            new[] { "AshlandsTree3", "AshlandsTree4", "AshlandsTree5", "AshlandsTree6_big" },
            Heightmap.Biome.Meadows | Heightmap.Biome.BlackForest | Heightmap.Biome.Plains | Heightmap.Biome.AshLands, false, 2f,
            extraResource: "SulfurStone", tolerateHeat: true)
    };

    /// <summary>Whether the cultivator can plant these at all.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>Whether debris (branches, stones, flint) can be planted.</summary>
    public static ConfigEntry<bool> Debris { get; private set; }

    /// <summary>Whether decorative flora can be planted.</summary>
    public static ConfigEntry<bool> Flora { get; private set; }

    /// <summary>Whether berry bushes, mushrooms, flowers and debris need cultivated ground.</summary>
    public static ConfigEntry<bool> RequireCultivation { get; private set; }

    /// <summary>Whether removing something you planted gives back what it cost.</summary>
    public static ConfigEntry<bool> RecoverResources { get; private set; }

    /// <summary>Whether wild berry bushes, mushrooms, flowers and debris can be removed with the cultivator.</summary>
    public static ConfigEntry<bool> RemoveWild { get; private set; }

    /// <summary>Whether planted decorative flora can be removed with the cultivator.</summary>
    public static ConfigEntry<bool> RemoveFlora { get; private set; }

    /// <summary>Whether enemies attack what players have planted.</summary>
    public static ConfigEntry<bool> EnemiesTarget { get; private set; }

    /// <summary>Whether vines snap to each other.</summary>
    public static ConfigEntry<bool> SnappableVines { get; private set; }

    /// <summary>Whether picked bushes, mushrooms and flowers show when they grow back while the cultivator is held.</summary>
    public static ConfigEntry<bool> ShowRegrowTimers { get; private set; }

    /// <summary>Whether saplings grow in every biome, instead of only where their tree grows.</summary>
    public static ConfigEntry<bool> SaplingsAnyBiome { get; private set; }

    /// <summary>
    /// Binds the config entries and adds the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        AddTranslations();
        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true,
            "Let the cultivator plant berry bushes, mushrooms, flowers, debris and decorative flora. " +
            "Each unlocks when you have had one of every item it costs. Off while PlantEverything is installed.");
        Debris = WhiteHiltConfig.BindAdminOnly(Section, "Debris", true, "Let the cultivator place branches, stones and flint.");
        Flora = WhiteHiltConfig.BindAdminOnly(Section, "Flora", true, "Let the cultivator plant small trees, bushes, shrubs, vines and ferns.");
        RequireCultivation = WhiteHiltConfig.BindAdminOnly(Section, "RequireCultivation", false,
            "Berry bushes, mushrooms, flowers and debris can only be planted on cultivated ground.");
        RecoverResources = WhiteHiltConfig.BindAdminOnly(Section, "RecoverResources", false,
            "Removing something a player planted gives back what it cost. What it holds is then not picked.");
        RemoveWild = WhiteHiltConfig.BindAdminOnly(Section, "RemoveWild", true,
            "The cultivator can remove wild berry bushes, mushrooms, flowers and debris (they are picked first).");
        RemoveFlora = WhiteHiltConfig.BindAdminOnly(Section, "RemoveFlora", false,
            "The cultivator can remove decorative flora that a player planted. Wild trees and bushes can never be removed this way.");
        EnemiesTarget = WhiteHiltConfig.BindAdminOnly(Section, "EnemiesTarget", true,
            "Enemies may attack what players have planted. Takes effect for planted things when the area loads again.");
        SnappableVines = WhiteHiltConfig.BindAdminOnly(Section, "SnappableVines", true, "Vines snap to each other when placed side by side.");
        ShowRegrowTimers = WhiteHiltConfig.BindLocal(Section, "ShowRegrowTimers", true,
            "Show when picked berry bushes, mushrooms and flowers grow back, next to the growth markers of the White Hilt cultivator.");
        SaplingsAnyBiome = WhiteHiltConfig.BindAdminOnly(Section, "SaplingsAnyBiome", true,
            "The ancient, autumn birch, Yggdrasil and ashwood saplings grow in every biome. Off: only where their tree grows, and in the Meadows, Black Forest and Plains.");

        foreach (PlantableDefinition definition in Definitions)
        {
            string item = definition.Resource ?? "what it gives when picked";
            string extra = definition.ExtraResource != null ? $", plus {definition.ExtraAmount} {definition.ExtraResource}" : string.Empty;
            definition.Cost = WhiteHiltConfig.BindAdminOnly(CostSection, definition.Prefab, definition.DefaultCost,
                $"{definition.Name}: amount of {item} it costs to plant{extra}. 0 leaves it out of the cultivator.",
                new AcceptableValueRange<int>(0, 100));
            if (definition is SaplingDefinition sapling)
            {
                BindSapling(sapling);
            }
        }
    }

    private static void BindSapling(SaplingDefinition sapling)
    {
        string section = $"{SaplingSection}.{sapling.Prefab}";
        sapling.Enabled = WhiteHiltConfig.BindAdminOnly(section, "Enabled", sapling.DefaultEnabled,
            $"{sapling.Name}: in the cultivator once you have had one of every item it costs.");
        sapling.GrowthTime = WhiteHiltConfig.BindAdminOnly(section, "GrowthTime", 3000f, "Seconds it takes to grow into a tree.",
            new AcceptableValueRange<float>(10f, 100000f));
        sapling.MinScale = WhiteHiltConfig.BindAdminOnly(section, "MinScale", 0.5f, "Smallest size of the grown tree.",
            new AcceptableValueRange<float>(0.1f, 5f));
        sapling.MaxScale = WhiteHiltConfig.BindAdminOnly(section, "MaxScale", sapling.DefaultMaxScale, "Largest size of the grown tree.",
            new AcceptableValueRange<float>(0.1f, 5f));
        sapling.GrowRadius = WhiteHiltConfig.BindAdminOnly(section, "GrowRadius", 2f, "Free space in metres it needs around it to grow.",
            new AcceptableValueRange<float>(0f, 10f));
    }

    private static void AddTranslations()
    {
        foreach (PlantableDefinition definition in Definitions)
        {
            Translations.AddEnglish(definition.NameKey, definition.Name);
        }

        Translations.AddEnglish("piece_whitehilt_plant_pickable_description", "Plant it, and pick it again when it has grown back.");
        Translations.AddEnglish("piece_whitehilt_plant_debris_description", "Put it where you want it, and pick it up again.");
        Translations.AddEnglish("piece_whitehilt_plant_flora_description", "Decoration. It stays as it is planted.");
        Translations.AddEnglish("piece_whitehilt_plant_sapling_description", "Grows into a tree.");
        Translations.AddEnglish("msg_whitehilt_plant_cantremove", "That cannot be removed with the cultivator here");
    }
}
