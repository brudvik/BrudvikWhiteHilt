using BepInEx;
using BrudvikWhiteHilt.Items;
using BrudvikWhiteHilt.Pieces;
using BrudvikWhiteHilt.Progression;
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
[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
[SynchronizationMode(AdminOnlyStrictness.IfOnServer)]
internal class BrudvikWhiteHilt : BaseUnityPlugin
{
    /// <summary>
    /// Constants for the plugin's GUID, name, and version.
    /// </summary>
    public const string PluginGUID = "com.jotunn.BrudvikWhiteHilt";
    public const string PluginName = "BrudvikWhiteHilt";
    public const string PluginVersion = "0.1.0";

    private readonly List<IWhiteHiltCustomItem> customItems = new();
    private readonly List<IWhiteHiltCustomPiece> customPieces = new();

    /// <summary>
    /// Awake method is called when the script instance is being loaded.
    /// This method sets up event handlers, applies Harmony patches, and logs the plugin load message.
    /// </summary>
    private void Awake()
    {
        WhiteHiltConfig.Initialize(Config);

        // Entries are discovered here, not when prefabs register, so their config entries exist before server sync.
        DiscoverCustomEntries();

        Config.SettingChanged += (_, _) => ProgressionManager.Refresh();
        SynchronizationManager.OnConfigurationSynchronized += (_, _) => ProgressionManager.Refresh();

        // Register a callback to add cloned items when prefabs are registered
        PrefabManager.OnPrefabsRegistered += AddClonedItems;

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

            Jotunn.Logger.LogInfo("All custom items have been added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError("Failed to add custom items!");
            Jotunn.Logger.LogError(ex);
        }

        // Unregister the callback to prevent duplicate items
        PrefabManager.OnPrefabsRegistered -= AddClonedItems;
    }

}

