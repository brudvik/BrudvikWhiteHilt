using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Cooking.StonePot;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food;

/// <summary>
/// Base class for foods cooked in the <see cref="StonePot"/>.
/// </summary>
public abstract class WhiteHiltFoodBase : IWhiteHiltCustomItem
{
    private readonly ItemManager instance;
    private readonly ConfigEntry<float> health;
    private readonly ConfigEntry<float> stamina;
    private readonly ConfigEntry<float> durationMinutes;
    private readonly ConfigEntry<float> regen;
    private ItemDrop.ItemData.SharedData shared;

    /// <summary>
    /// Prefab name of the food.
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
    /// Vanilla food to clone the model from.
    /// </summary>
    protected abstract string CopyFrom { get; }

    /// <summary>
    /// Ingredients.
    /// </summary>
    protected abstract RequirementConfig[] Requirements { get; }

    /// <summary>
    /// Default maximum health gained while the food is active.
    /// </summary>
    protected abstract float Health { get; }

    /// <summary>
    /// Default maximum stamina gained while the food is active.
    /// </summary>
    protected abstract float Stamina { get; }

    /// <summary>
    /// Colour multiplied into the cloned model, so the food does not look like the vanilla one.
    /// </summary>
    protected abstract Color Tint { get; }

    /// <summary>
    /// Indicates whether the food is enabled.
    /// </summary>
    public abstract bool Enabled { get; }

    /// <summary>
    /// Default duration of the food, in seconds.
    /// </summary>
    protected virtual float DurationSeconds => 1500f;

    /// <summary>
    /// Default health regenerated per tick while the food is active.
    /// </summary>
    protected virtual float Regen => 2f;

    /// <summary>
    /// Stone Pot level needed to cook the food. Each extension next to the pot, such as the Herb Tray, adds a level.
    /// </summary>
    protected virtual int MinStationLevel => 1;

    /// <inheritdoc/>
    public virtual ProgressionTier DefaultTier => ProgressionTier.Start;

    /// <inheritdoc/>
    public string Id => BaseName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(NameKey);

    /// <inheritdoc/>
    public string GatedPrefabName => BaseName;

    private string NameKey => $"item_{BaseName.ToLowerInvariant()}";

    /// <summary>
    /// Constructor for the WhiteHiltFoodBase class. Binds the config entries and registers the English text.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    protected WhiteHiltFoodBase(ItemManager instance)
    {
        this.instance = instance;

        string section = $"Food.{FullName.Replace(" ", string.Empty)}";
        health = WhiteHiltConfig.BindAdminOnly(section, "Health", Health, "Maximum health gained.", new AcceptableValueRange<float>(0f, 200f));
        stamina = WhiteHiltConfig.BindAdminOnly(section, "Stamina", Stamina, "Maximum stamina gained.", new AcceptableValueRange<float>(0f, 200f));
        durationMinutes = WhiteHiltConfig.BindAdminOnly(section, "DurationMinutes", DurationSeconds / 60f, "How long the food lasts.",
            new AcceptableValueRange<float>(1f, 120f));
        regen = WhiteHiltConfig.BindAdminOnly(section, "Regen", Regen, "Health regenerated per tick.", new AcceptableValueRange<float>(0f, 20f));

        Translations.AddEnglish(NameKey, FullName);
        Translations.AddEnglish($"{NameKey}_description", Description);
    }

    /// <summary>
    /// Adds the food and its stone pot recipe to the game.
    /// </summary>
    public void Add()
    {
        try
        {
            ItemConfig itemConfig = new()
            {
                Name = Translations.Token(NameKey),
                Description = Translations.Token($"{NameKey}_description"),
                CraftingStation = StonePot.PrefabName,
                MinStationLevel = MinStationLevel,
                Requirements = Requirements
            };

            CustomItem food = new(BaseName, CopyFrom, itemConfig);
            shared = food.ItemDrop.m_itemData.m_shared;
            shared.m_foodEitr = 0f;
            ApplyConfig();

            VisualHelper.Tint(food.ItemPrefab, Tint);
            Sprite icon = VisualHelper.RenderIcon(food.ItemPrefab);
            if (icon != null)
            {
                shared.m_icons = new[] { icon };
            }

            instance.AddItem(food);

            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    /// <summary>
    /// Applies changed or server-synced food values. Items in inventories share this data, so they change too.
    /// </summary>
    public void ApplyConfig()
    {
        if (shared == null)
        {
            return;
        }

        shared.m_food = health.Value;
        shared.m_foodStamina = stamina.Value;
        shared.m_foodBurnTime = durationMinutes.Value * 60f;
        shared.m_foodRegen = regen.Value;
    }
}
