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
/// The plugin. BepInEx creates it once when the game starts. It binds every setting and registers the texts, finds
/// the mod's items and pieces by reflection, adds them when Jotunn has the vanilla prefabs ready, starts the chest
/// module and applies the Harmony patches. docs/architecture.md describes the start-up order and why it is so.
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
    /// <summary>The plugin's unique ID, which BepInEx and other mods know it by. Never change it.</summary>
    public const string PluginGUID = "com.jotunn.BrudvikWhiteHilt";

    /// <summary>The plugin's name.</summary>
    public const string PluginName = "BrudvikWhiteHilt";

    /// <summary>The version; it must match Package/manifest.json and Properties/AssemblyInfo.cs.</summary>
    public const string PluginVersion = "0.107.0";

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
        LoxMilking.Initialize();
        NavigationSettings.Initialize();
        Navigation.Waypoints.WaypointSettings.Initialize();
        Navigation.Weather.ForecastSettings.Initialize();
        Navigation.Overview.OverviewSettings.Initialize();
        Navigation.Dowsing.StoneDowsingSettings.Initialize();
        Navigation.Dowsing.RootDowsingSettings.Initialize();
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
        CraftingPanelSettings.Initialize();
        EternalFireRules.Initialize();
        PortalSettings.Initialize();
        Pieces.Portals.Effects.PortalFxSettings.Initialize();
        BuildToolSettings.Initialize();
        Building.Media.MediaSettings.Initialize();
        Building.Groups.GroupSettings.Initialize();
        Building.Terrain.TerrainSettings.Initialize();
        Building.Moats.MoatSettings.Initialize();
        Items.Curing.CuringSettings.Initialize();
        Items.Ammunition.BurningFlames.Initialize();
        Difficulty.BigHitLog.Initialize();
        Party.PartySettings.Initialize();
        Textiles.TextileSettings.Initialize();
        Saga.SagaSettings.Initialize();
        Storage.StorageSearch.Initialize();
        Guestbook.GuestbookSettings.Initialize();
        Quartermaster.QuartermasterSettings.Initialize();
        Bestiary.BeastBookSettings.Initialize();
        Bestiary.BeastCounter.Initialize();
        Building.Doors.AutoDoorSettings.Initialize();
        Planting.PlantingSettings.Initialize();
        Backpack.BackpackSettings.Initialize();
        Pieces.Ships.ShipSettings.Initialize();
        Pieces.Ships.Skidbladnir.SkidbladnirSettings.Initialize();
        Clock.ClockSettings.Initialize();
        Production.ProductionSettings.Initialize();
        Navigation.Portraits.PortraitSettings.Initialize();
        Navigation.Compass.CompassSettings.Initialize();
        Navigation.Compass.HudCompassSettings.Initialize();
        Navigation.Areas.MapAreaSettings.Initialize();
        Navigation.Discoveries.DiscoverySettings.Initialize();
        Navigation.Discoveries.DiscoveryCatalog.RegisterTranslations();
        Painting.PaintSettings.Initialize();
        Branding.BrandingSettings.Initialize();
        Sound.IndoorSoundSettings.Initialize();
        Drops.FloatingItems.Initialize();
        Pieces.Waste.WasteWellSettings.Initialize();
        Pieces.Smithing.RepairAnvil.RepairAnvilSettings.Initialize();
        Pieces.Trophies.TrophyAltarSettings.Initialize();
        Pieces.Defenses.DefenseSettings.Initialize();
        Pieces.Defenses.GateControl.GateMechanisms.Initialize();
        Pieces.Defenses.Siege.SiegeSettings.Initialize();
        Necromancy.Raising.Initialize();
        Items.Accessories.MegingjordUpgrade.Initialize();
        Difficulty.DifficultySettings.Initialize();
        Difficulty.DifficultyCommands.Register();
        Items.Binding.BindingSettings.Initialize();
        Items.Binding.GearBinding.RegisterTranslations();
        Items.Binding.DreadEffect.Register();
        Items.Weapons.BleedEffect.Register();
        Items.Weapons.ArmorBreakEffect.Register();
        Patches.Gear.WeaponTraitPatches.RegisterTranslations();
        Items.Weapons.WeaponGlow.RegisterTranslations();
        Items.Weapons.Styles.AttackVarietySettings.Initialize();
        Items.Weapons.Styles.AttackVariety.RegisterTranslations();
        Items.Runes.GlowRune.GlowRune.RegisterTranslations();
        Items.Runes.FlameRune.FlameRune.RegisterTranslations();
        Items.Runes.ColourRunes.RegisterTranslations();
        Items.Weapons.WeaponFlame.RegisterTranslations();
        Kraken.KrakenSettings.Initialize();
        Items.Summoning.SummoningHornService.Initialize();
        Kraken.KrakenCommands.Register();
        Kraken.KrakenRegistry.Initialize();
        Monsters.MonsterSettings.Initialize();
        OldLand.OldLandSettings.Initialize();
        Monsters.MonsterCommands.Register();
        Monsters.MonsterRegistry.Initialize();
        Monsters.DesertDragonRegistry.Initialize();
        // After the monsters: the Black Dragon is cloned from the Desert Dragon.
        Difficulty.Beasts.BeastRegistry.Initialize();
        Companions.CompanionRest.Initialize();
        Companions.DogSettings.Initialize();
        Companions.DogRegistry.Initialize();
        Treasure.TreasureSettings.Initialize();
        Treasure.TreasureRegistry.Initialize();
        Pieces.Roofs.RoofSettings.Initialize();
        Items.Roofing.RoofMaterials.Initialize();
        Pieces.Roofs.RoofCatalog.Initialize();
        chests = Chests.ChestModule.Start();
        Navigation.Discoveries.DiscoveryPanel.Chests = chests;
        Chests.Collection.CollectionSettings.Initialize();
        Chests.Collection.CollectionPostComponent.Module = chests;
        Quartermaster.QuartermasterStore.Module = chests;
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
        Saga.SagaPanel.CheckKey();
        Storage.StorageSearch.Tick();
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
            Pieces.Ships.ShipBell.Create();
            Pieces.Portals.Effects.PortalFx.CreateSounds();
            Building.Moats.MoatSection.CreatePrefab();

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
        Monsters.DesertDragonRegistry.ApplyConfig();
        Treasure.TreasureRegistry.ApplyConfig();
        Items.Roofing.RoofMaterials.ApplyConfig();
        Pieces.Roofs.RoofCatalog.ApplyConfig();
        Pieces.Cooking.DryingRack.ApplyCookTimes();
        foreach (IWhiteHiltConfigurable configurable in customItems.OfType<IWhiteHiltConfigurable>())
        {
            configurable.ApplyConfig();
        }
    }

}

