using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Textiles;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Textiles;

/// <summary>
/// Linen cloth woven on the loom; a dyed sail needs it.
/// </summary>
public class LinenCloth : IWhiteHiltCustomItem
{
    /// <summary>Prefab name of the cloth.</summary>
    public const string PrefabName = "WhiteHiltLinenCloth";

    private const string FullName = "Linen Cloth";
    private const string Description = "A bolt of plain linen woven on the loom. A dyed sail needs it.";

    private readonly ItemManager instance;

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Plains;

    /// <inheritdoc/>
    public string Id => PrefabName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(Translations.ItemKey(PrefabName));

    /// <inheritdoc/>
    public string GatedPrefabName => PrefabName;

    /// <summary>
    /// Registers the English text.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public LinenCloth(ItemManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(PrefabName), FullName, Description);
    }

    /// <summary>
    /// Adds the cloth and its loom recipe.
    /// </summary>
    public void Add()
    {
        try
        {
            CustomItem cloth = new(PrefabName, "LinenThread", new ItemConfig
            {
                Name = NameToken,
                Description = Translations.Token($"{Translations.ItemKey(PrefabName)}_description"),
                CraftingStation = Loom.StationPrefabName,
                Requirements = new RequirementConfig[] { new() { Item = "LinenThread", Amount = 3, Recover = false } }
            });
            VisualHelper.Tint(cloth.ItemPrefab, new Color(0.95f, 0.92f, 0.82f));
            Sprite icon = VisualHelper.RenderIcon(cloth.ItemPrefab);
            if (icon != null)
            {
                cloth.ItemDrop.m_itemData.m_shared.m_icons = new[] { icon };
            }

            instance.AddItem(cloth);
            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }
}
