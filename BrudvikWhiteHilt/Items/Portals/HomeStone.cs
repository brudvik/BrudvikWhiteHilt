using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Portals.WhiteHiltPortal;
using BrudvikWhiteHilt.Pieces.Smithing.RuneForge;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Globalization;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Portals;

/// <summary>
/// The Home Stone: a small rune stone that takes its bearer to their home portal. It then rests for a while, shown as a
/// status effect with the time left; the rest is saved with the character, so dying or logging out does not end it.
/// The ordinary portal rules apply: no ore or metal.
/// </summary>
public class HomeStone : IWhiteHiltCustomItem
{
    /// <summary>
    /// Prefab name of the stone.
    /// </summary>
    public const string PrefabName = "WhiteHiltHomeStone";

    private const string FullName = "Home Stone";
    private const string Description = "A small stone with a carved rune that remembers your home portal. Use it to go home; set your home at any White Hilt portal. It needs a rest after each journey, and cannot carry ore or metal.";
    private const string RestKey = "whitehilt_homestone_ready";
    private const string EffectKey = "se_whitehilthomestone";
    private const float Size = 0.25f;

    private static HomeStoneRest restEffect;

    private readonly ItemManager instance;

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Swamp;

    /// <inheritdoc/>
    public string Id => PrefabName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(Translations.ItemKey(PrefabName));

    /// <inheritdoc/>
    public string GatedPrefabName => PrefabName;

    /// <summary>
    /// Constructor for the HomeStone class. Registers the English text.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public HomeStone(ItemManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(PrefabName), FullName, Description);
        Translations.AddEnglish(EffectKey, "Home Stone resting");
        Translations.AddEnglish($"{EffectKey}_tooltip", "The Home Stone can take you home again when this ends.");
        Translations.AddEnglish("msg_whitehilt_homestone_nohome", "Set a home at a White Hilt portal first");
        Translations.AddEnglish("msg_whitehilt_homestone_resting", "The Home Stone is still resting");
    }

    /// <summary>
    /// Returns true if the item is a Home Stone.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>True for the stone.</returns>
    public static bool IsHomeStone(ItemDrop.ItemData item)
    {
        return item?.m_shared.m_name == Translations.Token(Translations.ItemKey(PrefabName));
    }

    /// <summary>
    /// Takes the player home if the stone has rested and a home portal is set.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Use(Player player)
    {
        double remaining = RemainingSeconds(player);
        if (remaining > 0)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_homestone_resting");
            ShowRest(player);
            return;
        }

        string home = PortalTravel.GetHome(player);
        if (string.IsNullOrEmpty(home))
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_homestone_nohome");
            return;
        }

        if (!PortalTravel.TryTravel(player, PortalTravel.Find(home), runesFrom: null))
        {
            return;
        }

        DateTime ready = ZNet.instance.GetTime().AddMinutes(PortalSettings.HomeCooldownMinutes);
        player.m_customData[RestKey] = ready.Ticks.ToString(CultureInfo.InvariantCulture);
        ShowRest(player);
    }

    /// <summary>
    /// Shows the rest as a status effect with the time left, e.g. after respawning or logging in.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void ShowRest(Player player)
    {
        double remaining = RemainingSeconds(player);
        if (restEffect == null || remaining <= 0)
        {
            return;
        }

        SEMan seman = player.GetSEMan();
        StatusEffect effect = seman.GetStatusEffect(restEffect.NameHash()) ?? seman.AddStatusEffect(restEffect, resetTime: true);
        if (effect != null)
        {
            effect.m_time = Mathf.Max(0f, effect.m_ttl - (float)remaining);
        }
    }

    /// <summary>
    /// Adds the stone and its recipe at the Rune Forge.
    /// </summary>
    public void Add()
    {
        try
        {
            CustomItem stone = new(PrefabName, "Thunderstone", new ItemConfig
            {
                Name = NameToken,
                Description = Translations.Token($"{Translations.ItemKey(PrefabName)}_description"),
                CraftingStation = RuneForge.PrefabName,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "Stone", Amount = 4 },
                    new() { Item = "Iron", Amount = 2 },
                    new() { Item = "SurtlingCore", Amount = 1 },
                    new() { Item = "GreydwarfEye", Amount = 5 }
                }
            });

            ItemDrop.ItemData.SharedData shared = stone.ItemDrop.m_itemData.m_shared;
            shared.m_itemType = ItemDrop.ItemData.ItemType.Material;
            shared.m_maxStackSize = 1;
            shared.m_weight = 0.5f;
            shared.m_value = 0;
            shared.m_teleportable = true;

            TryApplyVisual(stone);
            restEffect = ScriptableObject.CreateInstance<HomeStoneRest>();
            restEffect.name = "SE_WhiteHiltHomeStone";
            restEffect.m_name = Translations.Token(EffectKey);
            restEffect.m_tooltip = Translations.Token($"{EffectKey}_tooltip");
            restEffect.m_icon = shared.m_icons.Length > 0 ? shared.m_icons[0] : null;

            instance.AddStatusEffect(new CustomStatusEffect(restEffect, fixReference: false));
            instance.AddItem(stone);
            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    private static double RemainingSeconds(Player player)
    {
        if (ZNet.instance == null || !player.m_customData.TryGetValue(RestKey, out string value)
            || !long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long ticks))
        {
            return 0;
        }

        return (new DateTime(ticks) - ZNet.instance.GetTime()).TotalSeconds;
    }

    private static void TryApplyVisual(CustomItem stone)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            VisualHelper.ReplaceMesh(stone.ItemPrefab, ForagingAssets.LoadMesh("homestone"), ForagingAssets.LoadTexture("homestone_albedo"), size: Size);
            Sprite icon = VisualHelper.RenderIcon(stone.ItemPrefab);
            if (icon != null)
            {
                stone.ItemDrop.m_itemData.m_shared.m_icons = new[] { icon };
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: keeping the vanilla look: {ex.Message}");
        }
    }
}

/// <summary>
/// The Home Stone's rest, shown with the time left. Its length follows the config when it starts.
/// </summary>
public class HomeStoneRest : StatusEffect
{
    /// <summary>
    /// Sets the length from the config before the effect starts.
    /// </summary>
    /// <param name="character">The character.</param>
    public override void Setup(Character character)
    {
        m_ttl = PortalSettings.HomeCooldownMinutes * 60f;
        base.Setup(character);
    }
}
