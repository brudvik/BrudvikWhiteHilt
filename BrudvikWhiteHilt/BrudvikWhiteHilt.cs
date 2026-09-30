using BepInEx;
using BrudvikWhiteHilt.Building;
using BrudvikWhiteHilt.Crafting;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items;
using BrudvikWhiteHilt.Items.Food;
using BrudvikWhiteHilt.Items.Foraging;
using BrudvikWhiteHilt.Navigation;
using BrudvikWhiteHilt.Pieces;
using BrudvikWhiteHilt.Pieces.EternalFire;
using BrudvikWhiteHilt.Pieces.Portals.WhiteHiltPortal;
using BrudvikWhiteHilt.Progression;
using BrudvikWhiteHilt.Ranching;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrudvikWhiteHilt;

/// <summary>
/// Main class for the BrudvikWhiteHilt plugin.
/// This class initializes the plugin, sets up custom chests, and applies Harmony patches.
/// 
/// Plugins not compatible with this:
/// - AAA_Crafting by Azumatt
/// </summary>
[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[BepInDependency(Jotunn.Main.ModGuid)]
[BepInDependency(Patches.Portals.PortalStationsRunePatch.ModGuid, BepInDependency.DependencyFlags.SoftDependency)]
[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
[SynchronizationMode(AdminOnlyStrictness.IfOnServer)]
internal class BrudvikWhiteHilt : BaseUnityPlugin
{
    /// <summary>
    /// Constants for the plugin's GUID, name, and version.
    /// </summary>
    public const string PluginGUID = "com.jotunn.BrudvikWhiteHilt";
    public const string PluginName = "BrudvikWhiteHilt";
    public const string PluginVersion = "0.15.0";

    private readonly List<IWhiteHiltCustomItem> customItems = new();
    private readonly List<IWhiteHiltCustomPiece> customPieces = new();
    private readonly List<ForageableBase> forageables = new();

    /// <summary>
    /// Awake method is called when the script instance is being loaded.
    /// This method sets up event handlers, applies Harmony patches, and logs the plugin load message.
    /// </summary>
    private void Awake()
    {
        WhiteHiltConfig.Initialize(Config);
        Translations.LoadEmbedded();
        ProgressionManager.RegisterTranslations();
        ExplorationSkill.Register();
        HusbandrySkill.Register();
        FavoriteFoods.RegisterTranslations();
        AnimalCare.RegisterTranslations();
        NearbyContainers.Initialize();
        EternalFireRules.Initialize();
        PortalSettings.Initialize();
        BuildToolSettings.Initialize();
        Building.Media.MediaSettings.Initialize();
        Building.Groups.GroupSettings.Initialize();
        Building.Terrain.TerrainSettings.Initialize();
        Planting.PlantingSettings.Initialize();
        Backpack.BackpackSettings.Initialize();
        Pieces.Ships.ShipSettings.Initialize();
        Clock.ClockSettings.Initialize();
        Painting.PaintSettings.Initialize();
        Branding.BrandingSettings.Initialize();
        Sound.IndoorSoundSettings.Initialize();
        Drops.FloatingItems.Initialize();
        Pieces.Waste.WasteWellSettings.Initialize();
        Pieces.Smithing.RepairAnvil.RepairAnvilSettings.Initialize();
        Items.Accessories.MegingjordUpgrade.Initialize();
        Difficulty.DifficultySettings.Initialize();
        Difficulty.DifficultyCommands.Register();
        Difficulty.Beasts.BeastRegistry.Initialize();
        Companions.CompanionRest.Initialize();
        Companions.DogSettings.Initialize();
        Companions.DogRegistry.Initialize();

        // Entries are discovered here, not when prefabs register, so their config entries exist before server sync.
        DiscoverCustomEntries();

        Config.SettingChanged += (_, _) => RefreshConfig();
        SynchronizationManager.OnConfigurationSynchronized += (_, _) => RefreshConfig();

        // Items must exist before the main menu's ObjectDB copy, or the character preview drops White Hilt gear.
        PrefabManager.OnVanillaPrefabsAvailable += AddClonedItems;

        // Pickables must exist before the first ZNetScene and ZoneSystem, or their vegetation is missing in that session.
        PrefabManager.OnVanillaPrefabsAvailable += AddForageables;
        PrefabManager.OnVanillaPrefabsAvailable += AddSaplings;
        PrefabManager.OnPrefabsRegistered += AddForageableCreatureDrops;
        PrefabManager.OnPrefabsRegistered += FavoriteFoods.AddToDiets;
        PrefabManager.OnPrefabsRegistered += Planting.Plantables.Apply;

        // Apply Harmony patches using the plugin's GUID
        var harmony = new Harmony(PluginGUID);
        harmony.PatchAll();

        Jotunn.Logger.LogInfo($"{PluginName} v{PluginVersion} has loaded!");
    }

    /// <summary>
    /// Instantiates every enabled custom item and piece and registers it for progression.
    /// </summary>
    private void DiscoverCustomEntries()
    {
        try
        {
            var types = typeof(BrudvikWhiteHilt).Assembly.GetTypes()
                .Where(type => !type.IsAbstract && type.IsClass)
                .ToList();

            foreach (var type in types.Where(type => typeof(IWhiteHiltCustomItem).IsAssignableFrom(type)))
            {
                if (Activator.CreateInstance(type, ItemManager.Instance) is IWhiteHiltCustomItem customItem && customItem.Enabled)
                {
                    customItems.Add(customItem);
                    ProgressionManager.RegisterItem(customItem);
                    Difficulty.WhiteHiltGear.Register(customItem);
                }
            }

            foreach (var type in types.Where(type => typeof(IWhiteHiltCustomPiece).IsAssignableFrom(type)))
            {
                if (Activator.CreateInstance(type, PieceManager.Instance) is IWhiteHiltCustomPiece customPiece && customPiece.Enabled)
                {
                    customPieces.Add(customPiece);
                    ProgressionManager.RegisterPiece(customPiece);
                }
            }

            foreach (var type in types.Where(type => typeof(ForageableBase).IsAssignableFrom(type)))
            {
                if (Activator.CreateInstance(type) is ForageableBase forageable && forageable.Enabled)
                {
                    forageables.Add(forageable);
                }
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError("Failed to discover custom items!");
            Jotunn.Logger.LogError(ex);
        }
    }

    /// <summary>
    /// Adds the discovered custom items and pieces to the game.
    /// </summary>
    private void AddClonedItems()
    {
        try
        {
            customItems.ForEach(customItem => customItem.Add());
            customPieces.ForEach(customPiece => customPiece.Add());
            ExplorationSkill.SetIconFromMapTable();
            HusbandrySkill.SetIconFromBoarTrophy();

            Jotunn.Logger.LogInfo("All custom items have been added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError("Failed to add custom items!");
            Jotunn.Logger.LogError(ex);
        }

        // Unregister the callback to prevent duplicate items
        PrefabManager.OnVanillaPrefabsAvailable -= AddClonedItems;
    }

    /// <summary>
    /// Adds the forageable items, their pickables and their vegetation.
    /// </summary>
    private void AddForageables()
    {
        forageables.ForEach(forageable => forageable.Add());
        PrefabManager.OnVanillaPrefabsAvailable -= AddForageables;
    }

    /// <summary>
    /// Adds the sapling prefabs that grow into other vanilla trees.
    /// </summary>
    private void AddSaplings()
    {
        Planting.Saplings.Create();
        PrefabManager.OnVanillaPrefabsAvailable -= AddSaplings;
    }

    /// <summary>
    /// Adds the forageables as drops on vanilla creatures. Runs for every ZNetScene, since it edits vanilla prefabs.
    /// </summary>
    private void AddForageableCreatureDrops()
    {
        forageables.ForEach(forageable => forageable.AddCreatureDrop());
    }

    /// <summary>
    /// Applies changed or server-synced config values.
    /// </summary>
    private void RefreshConfig()
    {
        ProgressionManager.Refresh();
        Backpack.BackpackLayout.Refresh();
        Planting.Plantables.Apply();
        forageables.ForEach(forageable => forageable.ApplyConfig());
        foreach (WhiteHiltFoodBase food in customItems.OfType<WhiteHiltFoodBase>())
        {
            food.ApplyConfig();
        }
    }

}

