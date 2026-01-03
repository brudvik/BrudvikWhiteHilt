using BepInEx;
using BrudvikWhiteHilt.Items;
using BrudvikWhiteHilt.Pieces;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
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
internal class BrudvikWhiteHilt : BaseUnityPlugin
{
    /// <summary>
    /// Constants for the plugin's GUID, name, and version.
    /// </summary>
    public const string PluginGUID = "com.jotunn.BrudvikWhiteHilt";
    public const string PluginName = "BrudvikWhiteHilt";
    public const string PluginVersion = "0.0.4";

    /// <summary>
    /// Awake method is called when the script instance is being loaded.
    /// This method sets up event handlers, applies Harmony patches, and logs the plugin load message.
    /// </summary>
    private void Awake()
    {
        // Register a callback to add cloned items when prefabs are registered
        PrefabManager.OnPrefabsRegistered += AddClonedItems;

        // Apply Harmony patches using the plugin's GUID
        var harmony = new Harmony(PluginGUID);
        harmony.PatchAll();

        Jotunn.Logger.LogInfo($"{PluginName} v{PluginVersion} has loaded!");
    }

    /// <summary>
    /// Adds cloned items to the custom pieces list.
    /// This method creates a new custom chest model for various prefabs and adds it to the custom pieces list.
    /// </summary>
    private void AddClonedItems()
    {
        try
        {
            // Get all types that implement ICustomItem
            var customItemTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(IWhiteHiltCustomItem).IsAssignableFrom(type) && !type.IsAbstract && type.IsClass);

            // Instantiate and call Add on each ICustomItem implementation
            foreach (var type in customItemTypes)
            {
                if (Activator.CreateInstance(type, ItemManager.Instance) is IWhiteHiltCustomItem customItem)
                {
                    if (customItem.Enabled)
                    {
                        customItem.Add();
                    }
                }
            }

            // Get all types that implement ICustomPiece
            var customPieceTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(IWhiteHiltCustomPiece).IsAssignableFrom(type) && !type.IsAbstract && type.IsClass);

            // Instantiate and call Add on each ICustomItem implementation
            foreach (var type in customPieceTypes)
            {
                if (Activator.CreateInstance(type, PieceManager.Instance) is IWhiteHiltCustomPiece customPiece)
                {
                    if (customPiece.Enabled)
                    {
                        customPiece.Add();
                    }
                }
            }

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

