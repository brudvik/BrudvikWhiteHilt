using BepInEx.Configuration;
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
public abstract class WhiteHiltRuneBase : IWhiteHiltCustomItem, IWhiteHiltConfigurable
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
    private const string Section = "Gear.Runes";

    private static readonly WhiteHiltRuneBase[] all = new WhiteHiltRuneBase[Count];
    private static ConfigEntry<float> weight;
    private static ConfigEntry<int> maxStackSize;

    private readonly ItemManager instance;
    private ItemDrop.ItemData.SharedData shared;

    /// <summary>
    /// Position of the rune on the post, from 0 to <see cref="Count"/> - 1. Also its bit in the post's mask. -1 for a
    /// rune
    /// that is only etched into weapons and never hung on a post.
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

    /// <summary>
    /// Materials the rune forge takes besides the iron.
    /// </summary>
    protected virtual RequirementConfig[] ExtraRequirements => Array.Empty<RequirementConfig>();

    /// <inheritdoc/>
    public virtual ProgressionTier DefaultTier => ProgressionTier.Swamp;

    /// <summary>
    /// How many of the rune stack in one slot; by default the config's <c>MaxStackSize</c>.
    /// </summary>
    protected virtual int MaxStackSize => maxStackSize.Value;

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

    /// <summary>
    /// Emission texture with only the carved runes lit, for the glow of a full post. Null on a dedicated server.
    /// </summary>
    public Texture2D GlowTexture { get; private set; }

    /// <summary>
    /// Colour of the carved runes, also used for their glow.
    /// </summary>
    public Color GlowColor => RuneColor;

    private string NameKey => Translations.ItemKey(BaseName);

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run. Registers the rune and its English text.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    protected WhiteHiltRuneBase(ItemManager instance)
    {
        this.instance = instance;
        if (Index >= 0)
        {
            all[Index] = this;
        }

        BindConfig();
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
                }.Concat(ExtraRequirements).ToArray()
            });

            ItemDrop.ItemData.SharedData runeShared = rune.ItemDrop.m_itemData.m_shared;
            runeShared.m_value = 0;
            runeShared.m_teleportable = true;
            shared = runeShared;
            ApplyConfig();

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

    /// <summary>
    /// Applies the configured weight and stack size.
    /// </summary>
    public void ApplyConfig()
    {
        if (shared == null)
        {
            return;
        }

        shared.m_weight = weight.Value;
        shared.m_maxStackSize = MaxStackSize;
    }

    private static void BindConfig()
    {
        if (weight != null)
        {
            return;
        }

        weight = WhiteHiltConfig.BindAdminOnly(Section, "Weight", 1f, "Weight of each rune.", new AcceptableValueRange<float>(0f, 50f));
        maxStackSize = WhiteHiltConfig.BindAdminOnly(Section, "MaxStackSize", 10, "How many runes of a kind stack in one slot.",
            new AcceptableValueRange<int>(1, 100));
    }

    // Gives the rune ring its colours: the stone in the rune's body colour and the carved rune in its glow colour, plus
    // a separate glow texture with only the rune lit for the rune rack.
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
            GlowTexture = VisualHelper.RecolorTexture(ForagingAssets.LoadTexture("runering_albedo"), pixel => IsCarvedRune(pixel) ? (Color32)RuneColor : new Color32(0, 0, 0, 255));
            GlowTexture.name = $"{BaseName}_glow";
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
    private static bool IsCarvedRune(Color32 pixel)
    {
        return pixel.r > 150 && pixel.b < 90;
    }

    // Recolours one pixel of the ring's texture: the carved rune takes the rune's colour, the rest the body colour,
    // keeping the texture's light and shade. Near-black pixels stay as they are.
    private Color32 Recolor(Color32 pixel)
    {
        if (IsCarvedRune(pixel))
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
