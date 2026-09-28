using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Smithing.RuneForge;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Runes;

/// <summary>
/// Base class for the iron runes smithed at the <see cref="RuneForge"/>. Hung on a rune post next to a portal,
/// each rune lets that portal carry one tier of metal. All runes on one post let it carry everything.
/// </summary>
public abstract class WhiteHiltRuneBase : IWhiteHiltCustomItem
{
    /// <summary>
    /// Number of runes, and of hooks on the rune post.
    /// </summary>
    public const int Count = 6;

    /// <summary>
    /// Bit mask with every rune set.
    /// </summary>
    public const int FullMask = (1 << Count) - 1;

    private const float RingSize = 0.2f;

    private static readonly WhiteHiltRuneBase[] all = new WhiteHiltRuneBase[Count];

    private readonly ItemManager instance;

    /// <summary>
    /// Position of the rune on the post, from 0 to <see cref="Count"/> - 1. Also its bit in the post's mask.
    /// </summary>
    public abstract int Index { get; }

    /// <summary>
    /// Prefab name of the rune.
    /// </summary>
    protected abstract string BaseName { get; }

    /// <summary>
    /// Name shown to players, in English. Other languages come from the embedded translation files.
    /// </summary>
    protected abstract string FullName { get; }

    /// <summary>
    /// Item description, in English.
    /// </summary>
    protected abstract string Description { get; }

    /// <summary>
    /// Shared names (<c>$item_...</c>) of the items the rune lets through portals.
    /// </summary>
    protected abstract string[] UnlockedItems { get; }

    /// <summary>
    /// Colour of the ring.
    /// </summary>
    protected abstract Color BodyColor { get; }

    /// <summary>
    /// Colour of the carved runes.
    /// </summary>
    protected abstract Color RuneColor { get; }

    /// <summary>
    /// Indicates whether the rune is enabled.
    /// </summary>
    public abstract bool Enabled { get; }

    /// <summary>
    /// Iron needed to smith the rune. Runes for later metals cost more.
    /// </summary>
    protected virtual int IronCost => 2 + Index * 2;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Swamp;

    /// <inheritdoc/>
    public string Id => BaseName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(NameKey);

    /// <inheritdoc/>
    public string GatedPrefabName => BaseName;

    /// <summary>
    /// Prefab name of the rune item.
    /// </summary>
    public string PrefabName => BaseName;

    /// <summary>
    /// Recoloured ring texture, for showing the rune on the post. Null on a dedicated server.
    /// </summary>
    public Texture2D RingTexture { get; private set; }

    private string NameKey => Translations.ItemKey(BaseName);

    /// <summary>
    /// Constructor for the WhiteHiltRuneBase class. Registers the rune and its English text.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    protected WhiteHiltRuneBase(ItemManager instance)
    {
        this.instance = instance;
        all[Index] = this;
        Translations.AddEnglishNameAndDescription(NameKey, FullName, Description);
    }

    /// <summary>
    /// Returns the rune at a position on the post.
    /// </summary>
    /// <param name="index">Position, from 0 to <see cref="Count"/> - 1.</param>
    /// <returns>The rune, or null if it is disabled.</returns>
    public static WhiteHiltRuneBase Get(int index)
    {
        return index >= 0 && index < Count ? all[index] : null;
    }

    /// <summary>
    /// Returns the rune an inventory item is, if any.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The rune, or null if the item is not a rune.</returns>
    public static WhiteHiltRuneBase FromItem(ItemDrop.ItemData item)
    {
        return all.FirstOrDefault(rune => rune != null && item?.m_shared.m_name == rune.NameToken);
    }

    /// <summary>
    /// Checks whether the runes in <paramref name="mask"/> let an item through a portal.
    /// </summary>
    /// <param name="mask">Bit mask of runes.</param>
    /// <param name="sharedName">Shared name (<c>$item_...</c>) of the item.</param>
    /// <returns>True if one of the runes unlocks the item.</returns>
    public static bool Unlocks(int mask, string sharedName)
    {
        return all.Any(rune => rune != null && (mask & (1 << rune.Index)) != 0 && rune.UnlockedItems.Contains(sharedName));
    }

    /// <summary>
    /// Adds the rune and its rune forge recipe to the game.
    /// </summary>
    public void Add()
    {
        try
        {
            CustomItem rune = new(BaseName, "Amber", new ItemConfig
            {
                Name = Translations.Token(NameKey),
                Description = Translations.Token($"{NameKey}_description"),
                CraftingStation = RuneForge.PrefabName,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "Iron", Amount = IronCost, Recover = false }
                }
            });

            ItemDrop.ItemData.SharedData shared = rune.ItemDrop.m_itemData.m_shared;
            shared.m_maxStackSize = 10;
            shared.m_weight = 1f;
            shared.m_value = 0;
            shared.m_teleportable = true;

            TryApplyVisual(rune);
            instance.AddItem(rune);

            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    private void TryApplyVisual(CustomItem rune)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            RingTexture = VisualHelper.RecolorTexture(ForagingAssets.LoadTexture("runering_albedo"), Recolor);
            RingTexture.name = $"{BaseName}_ring";
            VisualHelper.ReplaceMesh(rune.ItemPrefab, ForagingAssets.LoadMesh("runering"), RingTexture, size: RingSize);

            Sprite icon = VisualHelper.RenderIcon(rune.ItemPrefab);
            if (icon != null)
            {
                rune.ItemDrop.m_itemData.m_shared.m_icons = new[] { icon };
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: keeping the vanilla look: {ex.Message}");
        }
    }

    // The ring texture is dark grey metal with yellow runes on a black background.
    private Color32 Recolor(Color32 pixel)
    {
        if (pixel.r > 150 && pixel.b < 90)
        {
            Color32 rune = RuneColor;
            rune.a = pixel.a;
            return rune;
        }

        float brightness = (pixel.r + pixel.g + pixel.b) / (3f * 255f);
        if (brightness < 0.02f)
        {
            return pixel;
        }

        Color32 body = BodyColor * Mathf.Clamp(brightness / 0.16f, 0.5f, 2f);
        body.a = pixel.a;
        return body;
    }
}
