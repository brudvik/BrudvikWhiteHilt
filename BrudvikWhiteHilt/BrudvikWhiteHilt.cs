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
// Loads the old chest mod first, so ChestModule can see it and leave its chests alone.
[BepInDependency(Chests.ChestModule.OldModGuid, BepInDependency.DependencyFlags.SoftDependency)]
[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
[SynchronizationMode(AdminOnlyStrictness.IfOnServer)]
internal class BrudvikWhiteHilt : BaseUnityPlugin
{
    /// <summary>
    /// Constants for the plugin's GUID, name, and version.
    /// </summary>
    public const string PluginGUID = "com.jotunn.BrudvikWhiteHilt";
    public const string PluginName = "BrudvikWhiteHilt";
    public const string PluginVersion = "0.31.0";

    private readonly List<IWhiteHiltCustomItem> customItems = new();
    private readonly List<IWhiteHiltCustomPiece> customPieces = new();
    private readonly List<ForageableBase> forageables = new();
    private Chests.ChestModule chests;
    private bool refreshPending;

    /// <summary>
    /// Awake method is called when the script instance is being loaded.
    /// This method sets up event handlers, applies Harmony patches, and logs the plugin load message.
    /// </summary>
    private void Awake()
    {
        WhiteHiltConfig.Initialize(Config);
        RanchingSettings.Initialize();
        NavigationSettings.Initialize();
        Translations.LoadEmbedded();
        DynamicTexts.Register();
        Settings.ConfigWindow.RegisterTranslations();
        Settings.ConfigWindow.BindKey();
        Settings.ConfigText.RegisterCommand();
        ProgressionManager.RegisterTranslations();
        ExplorationSkill.Register();
        HusbandrySkill.Register();
        Mastery.MasterySettings.Initialize();
        Mastery.ForagingSkill.Register();
        RegisterMasteryTranslations();
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
        Production.ProductionSettings.Initialize();
        Navigation.Portraits.PortraitSettings.Initialize();
        Navigation.Areas.MapAreaSettings.Initialize();
        Painting.PaintSettings.Initialize();
        Branding.BrandingSettings.Initialize();
        Sound.IndoorSoundSettings.Initialize();
        Drops.FloatingItems.Initialize();
        Pieces.Waste.WasteWellSettings.Initialize();
        Pieces.Smithing.RepairAnvil.RepairAnvilSettings.Initialize();
        Pieces.Defenses.DefenseSettings.Initialize();
        Items.Accessories.MegingjordUpgrade.Initialize();
        Difficulty.DifficultySettings.Initialize();
        Difficulty.DifficultyCommands.Register();
        Difficulty.Beasts.BeastRegistry.Initialize();
        Kraken.KrakenSettings.Initialize();
        Kraken.KrakenCommands.Register();
        Kraken.KrakenRegistry.Initialize();
        Monsters.MonsterSettings.Initialize();
        Monsters.MonsterCommands.Register();
        Monsters.MonsterRegistry.Initialize();
        Companions.CompanionRest.Initialize();
        Companions.DogSettings.Initialize();
        Companions.DogRegistry.Initialize();
        chests = Chests.ChestModule.Start();
        Chests.ChestCensus.RegisterCommand();

        // Entries are discovered here, not when prefabs register, so their config entries exist before server sync.
        DiscoverCustomEntries();
        WhiteHiltConfig.ApplyMigrations();

        // Saving many settings at once fires one event per setting; refresh once, on the next frame.
        Config.SettingChanged += (_, _) => refreshPending = true;
        SynchronizationManager.OnConfigurationSynchronized += (_, _) => refreshPending = true;

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
    /// Applies pending config changes and creates the settings button once the game's GUI exists.
    /// </summary>
    private void Update()
    {
        Settings.ConfigButton.EnsureCreated();
        Settings.ConfigWindow.CheckKey();
        chests?.Update();
        if (refreshPending)
        {
            refreshPending = false;
            RefreshConfig();
        }
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
            Mastery.ForagingSkill.SetIconFromMushroom();
            Mastery.MasteryEffects.Register();

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
    /// Registers the English text of the skill milestones.
    /// </summary>
    private static void RegisterMasteryTranslations()
    {
        Mastery.Perks.RegisterTranslations();
        Mastery.Stars.RegisterTranslations();
        Mastery.MasteryEffects.RegisterTranslations();
        Mastery.OreEcho.RegisterTranslations();
        Mastery.Lookout.RegisterTranslations();
        Mastery.Gathering.RegisterTranslations();
        Mastery.WildPicks.RegisterTranslations();
        Mastery.Crops.RegisterTranslations();
        Mastery.Angling.RegisterTranslations();
        Mastery.JunkFilter.RegisterTranslations();
        Mastery.SkillBook.RegisterTranslations();
    }

    /// <summary>
    /// Applies changed or server-synced config values.
    /// </summary>
    private void RefreshConfig()
    {
        Translations.RefreshDynamic();
        ProgressionManager.Refresh();
        Backpack.BackpackLayout.Refresh();
        Planting.Plantables.Apply();
        forageables.ForEach(forageable => forageable.ApplyConfig());
        Monsters.MonsterRegistry.ApplyConfig();
        foreach (IWhiteHiltConfigurable configurable in customItems.OfType<IWhiteHiltConfigurable>())
        {
            configurable.ApplyConfig();
        }
    }

}

