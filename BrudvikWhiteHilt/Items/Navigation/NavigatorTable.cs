using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Navigation;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Navigation;

/// <summary>
/// The Navigator's Table: a small table with a sea chart and a sextant, set up on a ship's deck by using it on the helm.
/// Aboard, it widens the circle of map the crew uncovers, more so the higher their Exploration skill.
/// </summary>
public class NavigatorTable : IWhiteHiltCustomItem, IWhiteHiltConfigurable
{
    /// <summary>
    /// Prefab name of the item.
    /// </summary>
    public const string PrefabName = "WhiteHiltChartTable";

    private const string FullName = "Navigator's Table";
    private const string Description = "A small plank table with a sea chart, a sextant and map scrolls. Use it on the helm of a karve, longship, drakkar or White Hilt Ship to set it up on deck; on the White Hilt Ship the mast takes it too. Aboard, the map uncovers further around you, up to {0} m as your Exploration skill rises.";
    private const float Size = 0.45f;

    private readonly ItemManager instance;
    private readonly ConfigEntry<float> weight;
    private ItemDrop.ItemData.SharedData shared;

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.BlackForest;

    /// <inheritdoc/>
    public string Id => PrefabName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(Translations.ItemKey(PrefabName));

    /// <inheritdoc/>
    public string GatedPrefabName => PrefabName;

    /// <summary>
    /// Constructor for the NavigatorTable class. Registers the English text.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public NavigatorTable(ItemManager instance)
    {
        this.instance = instance;
        weight = WhiteHiltConfig.BindAdminOnly($"Gear.{PrefabName}", "Weight", 10f, "Weight of the Navigator's Table item.", new AcceptableValueRange<float>(0f, 100f));
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(PrefabName), FullName, Description);
        Translations.AddEnglish("whitehilt_charttable_take", "Take the Navigator's Table");
        Translations.AddEnglish("whitehilt_charttable_sight", "Sight");
        Translations.AddEnglish("msg_whitehilt_charttable_added", "The Navigator's Table is set up on deck");
        Translations.AddEnglish("msg_whitehilt_charttable_already", "The ship already has a Navigator's Table");
    }

    /// <summary>
    /// Returns true if an inventory item is a Navigator's Table.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>True for the table.</returns>
    public static bool IsTable(ItemDrop.ItemData item)
    {
        return item?.m_shared.m_name == Translations.Token(Translations.ItemKey(PrefabName));
    }

    /// <summary>
    /// Adds the item and its recipe at the Cartographer's Desk.
    /// </summary>
    public void Add()
    {
        try
        {
            CustomItem table = new(PrefabName, "BronzeNails", new ItemConfig
            {
                Name = NameToken,
                Description = Translations.Token($"{Translations.ItemKey(PrefabName)}_description"),
                CraftingStation = CartographerDesk.StationPrefabName,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "FineWood", Amount = 6 },
                    new() { Item = "Bronze", Amount = 3 },
                    new() { Item = "LeatherScraps", Amount = 4 }
                }
            });

            ItemDrop.ItemData.SharedData shared = table.ItemDrop.m_itemData.m_shared;
            shared.m_itemType = ItemDrop.ItemData.ItemType.Material;
            shared.m_maxStackSize = 1;
            shared.m_value = 0;
            shared.m_teleportable = true;
            this.shared = shared;
            ApplyConfig();

            TryApplyVisual(table);
            instance.AddItem(table);

            // The ships are edited once their prefabs, including the White Hilt Ship, are all registered.
            PrefabManager.OnPrefabsRegistered += PrepareShips;
            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    /// <summary>
    /// Applies the configured weight.
    /// </summary>
    public void ApplyConfig()
    {
        if (shared != null)
        {
            shared.m_weight = weight.Value;
        }
    }

    // Runs for every ZNetScene and skips ships that already have the table.
    private static void PrepareShips()
    {
        foreach ((string ship, string mount) in ShipChartTable.Ships)
        {
            try
            {
                ShipChartTable.Prepare(PrefabManager.Instance.GetPrefab(ship), mount);
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogWarning($"{FullName}: not available on {ship}: {ex.Message}");
            }
        }
    }

    private static void TryApplyVisual(CustomItem table)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            VisualHelper.ReplaceMesh(table.ItemPrefab, ForagingAssets.LoadMesh("sextant"), ForagingAssets.LoadTexture("sextant_albedo"), size: Size);
            Sprite icon = VisualHelper.RenderIcon(table.ItemPrefab);
            if (icon != null)
            {
                table.ItemDrop.m_itemData.m_shared.m_icons = new[] { icon };
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: keeping the vanilla look: {ex.Message}");
        }
    }
}
