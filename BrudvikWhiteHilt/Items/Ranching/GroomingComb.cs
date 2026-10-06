using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Ranching;

/// <summary>
/// A bone comb for grooming tame animals. A groomed animal is content for a day: it breeds faster,
/// puts its product in a nearby trough and gives a little more when slaughtered.
/// </summary>
public class GroomingComb : IWhiteHiltCustomItem
{
    /// <summary>
    /// Prefab name of the item.
    /// </summary>
    public const string PrefabName = "WhiteHiltGroomingComb";

    private const string FullName = "Grooming Comb";
    private const string Description = "A comb carved from bone. Put it on your hotbar and use it while looking at a tame animal to groom it. A groomed animal is content for a day: it breeds faster, leaves something in a nearby Feeding Trough and gives a little more when slaughtered.";
    private const float Size = 0.2f;

    private readonly ItemManager instance;

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
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run. Registers the English text.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public GroomingComb(ItemManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(PrefabName), FullName, Description);
        Translations.AddEnglish("msg_whitehilt_groomed", "is groomed and content");
        Translations.AddEnglish("msg_whitehilt_groom_already", "is already groomed today");
        Translations.AddEnglish("msg_whitehilt_groom_wild", "Only tame animals let you groom them");
    }

    /// <summary>
    /// Returns true if an inventory item is a Grooming Comb.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>True for the comb.</returns>
    public static bool IsComb(ItemDrop.ItemData item)
    {
        return item?.m_shared.m_name == Translations.Token(Translations.ItemKey(PrefabName));
    }

    /// <summary>
    /// Adds the item and its recipe at the workbench.
    /// </summary>
    public void Add()
    {
        try
        {
            CustomItem comb = new(PrefabName, "BoneFragments", new ItemConfig
            {
                Name = NameToken,
                Description = Translations.Token($"{Translations.ItemKey(PrefabName)}_description"),
                CraftingStation = CraftingStations.Workbench,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "BoneFragments", Amount = 3 },
                    new() { Item = "DeerHide", Amount = 1 }
                }
            });

            ItemDrop.ItemData.SharedData shared = comb.ItemDrop.m_itemData.m_shared;
            shared.m_itemType = ItemDrop.ItemData.ItemType.Material;
            shared.m_maxStackSize = 1;
            shared.m_weight = 0.5f;
            shared.m_value = 0;
            shared.m_teleportable = true;

            TryApplyVisual(comb);
            instance.AddItem(comb);
            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    // Replaces the cloned item's look with the comb model and renders an icon from it.
    private static void TryApplyVisual(CustomItem comb)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            VisualHelper.ReplaceMesh(comb.ItemPrefab, ForagingAssets.LoadMesh("comb"), ForagingAssets.LoadTexture("comb_albedo"), size: Size);
            Sprite icon = VisualHelper.RenderIcon(comb.ItemPrefab);
            if (icon != null)
            {
                comb.ItemDrop.m_itemData.m_shared.m_icons = new[] { icon };
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: keeping the vanilla look: {ex.Message}");
        }
    }
}
