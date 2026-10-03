using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Foraging;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Coatings;

/// <summary>
/// Base class for things used on the weapon in hand: a whetstone or an oil. Using one from the inventory gives a
/// <see cref="WeaponCoatingEffect"/> for a number of hits.
/// </summary>
public abstract class WeaponCoatingBase : IWhiteHiltCustomItem, IWhiteHiltConfigurable
{
    private readonly ItemManager instance;
    private readonly ConfigEntry<int> charges;
    private readonly ConfigEntry<float> strength;
    private WeaponCoatingEffect effect;

    /// <summary>
    /// Prefab name of the item.
    /// </summary>
    protected abstract string BaseName { get; }

    /// <summary>
    /// Name shown to players, in English.
    /// </summary>
    protected abstract string FullName { get; }

    /// <summary>
    /// Item description, in English.
    /// </summary>
    protected abstract string Description { get; }

    /// <summary>
    /// Effect tooltip, in English, with <c>{0}</c> for the strength in percent and <c>{1}</c> for the hits.
    /// </summary>
    protected abstract string EffectTooltip { get; }

    /// <summary>
    /// Bundle model of the item.
    /// </summary>
    protected abstract string ModelName { get; }

    /// <summary>
    /// Colour multiplied into the model.
    /// </summary>
    protected virtual Color Tint => Color.white;

    /// <summary>
    /// Crafting station.
    /// </summary>
    protected abstract string CraftingStation { get; }

    /// <summary>
    /// Ingredients.
    /// </summary>
    protected abstract RequirementConfig[] Requirements { get; }

    /// <summary>
    /// Default strength: extra slash and pierce for a whetstone, share of the physical damage added for an oil.
    /// </summary>
    protected abstract float DefaultStrength { get; }

    /// <summary>
    /// Sets what the coating does with the configured strength.
    /// </summary>
    /// <param name="coating">The effect.</param>
    /// <param name="value">The strength.</param>
    protected abstract void Configure(WeaponCoatingEffect coating, float value);

    /// <summary>
    /// Indicates whether the item is enabled.
    /// </summary>
    public abstract bool Enabled { get; }

    /// <inheritdoc/>
    public abstract ProgressionTier DefaultTier { get; }

    /// <inheritdoc/>
    public string Id => BaseName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(NameKey);

    /// <inheritdoc/>
    public string GatedPrefabName => BaseName;

    private string NameKey => Translations.ItemKey(BaseName);

    private string TooltipKey => $"se_{BaseName.ToLowerInvariant()}_tooltip";

    /// <summary>
    /// Constructor for the WeaponCoatingBase class. Binds the settings and registers the English text.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    protected WeaponCoatingBase(ItemManager instance)
    {
        this.instance = instance;
        string section = $"Coatings.{FullName.Replace(" ", string.Empty)}";
        WhiteHiltConfig.SetSectionLabel(section, Translations.Token(NameKey));
        charges = WhiteHiltConfig.BindAdminOnly(section, "Hits", 30, "How many hits it lasts.", new AcceptableValueRange<int>(1, 500));
        strength = WhiteHiltConfig.BindAdminOnly(section, "Strength", DefaultStrength,
            "Extra damage: for a whetstone more slash and pierce, for an oil a share of the physical damage added as its own kind (0.15 = 15%).",
            new AcceptableValueRange<float>(0f, 2f));
        Translations.AddEnglishNameAndDescription(NameKey, FullName, Description);
        Translations.AddEnglish(TooltipKey, EffectTooltip);
        Translations.AddDynamic(TooltipKey, () => new object[] { Translations.Number(strength.Value * 100f), charges.Value });
    }

    /// <inheritdoc/>
    public void Add()
    {
        try
        {
            CustomItem item = new(BaseName, "MeadTasty", new ItemConfig
            {
                Name = Translations.Token(NameKey),
                Description = Translations.Token($"{NameKey}_description"),
                CraftingStation = CraftingStation,
                Requirements = Requirements
            });
            ItemDrop.ItemData.SharedData shared = item.ItemDrop.m_itemData.m_shared;
            ApplyLook(item);

            effect = ScriptableObject.CreateInstance<WeaponCoatingEffect>();
            effect.name = $"SE_{BaseName}";
            effect.m_name = Translations.Token(NameKey);
            effect.m_tooltip = Translations.Token(TooltipKey);
            effect.m_icon = shared.m_icons[0];
            effect.m_ttl = 3600f;
            instance.AddStatusEffect(new CustomStatusEffect(effect, fixReference: false));
            shared.m_consumeStatusEffect = effect;
            ApplyConfig();
            instance.AddItem(item);

            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    /// <inheritdoc/>
    public void ApplyConfig()
    {
        if (effect == null)
        {
            return;
        }

        effect.Charges = charges.Value;
        Configure(effect, strength.Value);
    }

    private void ApplyLook(CustomItem item)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            Texture2D texture = ForagingAssets.LoadTexture($"{ModelName}_albedo");
            GameObject model = VisualHelper.ReplaceMesh(item.ItemPrefab, ForagingAssets.LoadMesh(ModelName), texture);
            model.GetComponent<MeshRenderer>().sharedMaterial = ForageableBase.PlantMaterial(texture);
            VisualHelper.Tint(model, Tint);
            Sprite icon = VisualHelper.RenderIcon(item.ItemPrefab);
            if (icon != null)
            {
                item.ItemDrop.m_itemData.m_shared.m_icons = new[] { icon };
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: keeping the vanilla look: {ex.Message}");
        }
    }
}
