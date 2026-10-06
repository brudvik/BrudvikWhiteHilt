using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items;
using BrudvikWhiteHilt.Items.Indestructible;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Decor;

/// <summary>
/// The White Hilt Decor Hammer: an everlasting build tool, like the White Hilt Hammer, whose build menu holds only
/// decorations: plants that sway in the wind, stones and stumps, kitchen and workshop things, furniture, cloth, lights
/// and Norse pieces. The pieces come from <see cref="DecorEntry.All"/>.
/// </summary>
/// <remarks>
/// A piece appears in the menu once the player knows all its materials, as vanilla pieces do, so the catalogue gates
/// itself by biome without a progression entry of its own per decoration.
/// </remarks>
public class DecorHammer : IWhiteHiltCustomItem, IWhiteHiltConfigurable
{
    /// <summary>Prefab name of the hammer.</summary>
    public const string PrefabName = "WhiteHiltDecorHammer";

    /// <summary>Name of the hammer's piece table.</summary>
    public const string TableName = "_WhiteHiltDecorHammerPieceTable";

    /// <summary>
    /// The build menu tabs. They are created as the catalogue's pieces are added, so decor.json lists them in this
    /// order; Jotunn names their texts jotunn_cat_&lt;name&gt; (translated in Norwegian.json).
    /// </summary>
    public static readonly string[] Categories = { "Garden", "Wilds", "Hearth", "Workshop", "Home", "Textiles", "Lights", "Norse" };

    private const string FullName = "White Hilt Decor Hammer";
    private const string Description = "An everlasting hammer for decorating: plants that sway in the wind, stones and stumps, barrels, baskets, tools, furniture, cloth, lights and Norse pieces. A decoration shows up once you know its materials.";

    private readonly ItemManager instance;
    private IndestructibleItem added;

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Start;

    /// <inheritdoc/>
    public string Id => PrefabName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(Translations.ItemKey(PrefabName));

    /// <inheritdoc/>
    public string GatedPrefabName => PrefabName;

    /// <summary>
    /// Creates the definition and registers the English texts of the hammer and every decoration. The plugin does
    /// this for every such class by reflection when it starts; the game sees the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public DecorHammer(ItemManager instance)
    {
        this.instance = instance;
        IndestructibleItem.BindConfig();
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(PrefabName), FullName, Description);
        DecorEntry.RegisterTranslations();
    }

    /// <summary>
    /// Applies the shared indestructible item config to the hammer.
    /// </summary>
    public void ApplyConfig()
    {
        added?.ApplyConfig();
    }

    /// <summary>
    /// Adds the hammer, its piece table and every decoration whose look can be found.
    /// </summary>
    public void Add()
    {
        try
        {
            // Since Valheim 1.0 the tabs come from the pieces' own categories, created in the order they first appear.
            PieceManager.Instance.AddPieceTable(new CustomPieceTable(TableName, new PieceTableConfig { CanRemovePieces = true }));

            IndestructibleItem hammer = new(PrefabName, "Hammer", new ItemConfig
            {
                Name = NameToken,
                Description = Translations.Token($"{Translations.ItemKey(PrefabName)}_description"),
                CraftingStation = CraftingStations.Workbench,
                PieceTable = TableName,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "Wood", Amount = 4 },
                    new() { Item = "Stone", Amount = 2 },
                    new() { Item = "Resin", Amount = 2 }
                }
            });
            hammer.ItemData.m_homeItemsStaminaModifier -= 1.0f;
            TryApplyVisual(hammer);
            instance.AddItem(hammer);
            added = hammer;

            int count = DecorEntry.All.Count(entry => DecorPieceFactory.Add(entry, TableName));
            Jotunn.Logger.LogInfo($"{FullName} added with {count} of {DecorEntry.All.Count} decorations!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    // A green tint, so it is told apart from the building hammer at a glance.
    private static void TryApplyVisual(IndestructibleItem hammer)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            VisualHelper.Tint(hammer.ItemPrefab, new Color(0.78f, 0.9f, 0.7f));
            Sprite icon = VisualHelper.RenderIcon(hammer.ItemPrefab);
            if (icon != null)
            {
                ItemDrop.ItemData.SharedData shared = hammer.ItemDrop.m_itemData.m_shared;
                shared.m_icons = Enumerable.Repeat(icon, Math.Max(1, shared.m_icons.Length)).ToArray();
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: keeping the hammer's look: {ex.Message}");
        }
    }
}
