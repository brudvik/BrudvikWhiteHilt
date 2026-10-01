using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Meads;

/// <summary>
/// Base class for meads brewed from White Hilt forageables. The mead base is cooked in the Cauldron and fermented into
/// six meads, like the vanilla meads. The model is a tinted vanilla mead, so no bought icons are needed.
/// </summary>
public abstract class WhiteHiltMeadBase : IWhiteHiltCustomItem, IWhiteHiltConfigurable
{
    private readonly ItemManager instance;
    private readonly ConfigEntry<float> durationMinutes;
    private SE_Stats effect;

    /// <summary>
    /// Prefab name of the mead. The mead base is named <c>{BaseName}Base</c>.
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
    /// Status effect tooltip, in English.
    /// </summary>
    protected abstract string EffectTooltip { get; }

    /// <summary>
    /// Vanilla mead to clone the model and drinking effects from.
    /// </summary>
    protected abstract string CopyMeadFrom { get; }

    /// <summary>
    /// Vanilla mead base to clone the model from.
    /// </summary>
    protected abstract string CopyMeadBaseFrom { get; }

    /// <summary>
    /// Ingredients of the mead base.
    /// </summary>
    protected abstract RequirementConfig[] Requirements { get; }

    /// <summary>
    /// Colour multiplied into the cloned models, so they do not look like the vanilla ones.
    /// </summary>
    protected abstract Color Tint { get; }

    /// <summary>
    /// Indicates whether the mead is enabled.
    /// </summary>
    public abstract bool Enabled { get; }

    /// <summary>
    /// How long the effect lasts, in seconds.
    /// </summary>
    protected virtual float DurationSeconds => 600f;

    /// <inheritdoc/>
    public virtual ProgressionTier DefaultTier => ProgressionTier.Start;

    /// <inheritdoc/>
    public string Id => BaseName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(NameKey);

    /// <inheritdoc/>
    public string GatedPrefabName => MeadBaseName;

    /// <summary>
    /// Config section of the mead, e.g. <c>Meads.CrowberryWine</c>.
    /// </summary>
    protected string ConfigSection => $"Meads.{FullName.Replace(" ", string.Empty)}";

    private string MeadBaseName => $"{BaseName}Base";

    private string NameKey => Translations.ItemKey(BaseName);

    private string MeadBaseKey => Translations.ItemKey(MeadBaseName);

    private string TooltipKey => $"se_{BaseName.ToLowerInvariant()}_tooltip";

    /// <summary>
    /// Constructor for the WhiteHiltMeadBase class. Registers the English text.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    protected WhiteHiltMeadBase(ItemManager instance)
    {
        this.instance = instance;
        durationMinutes = WhiteHiltConfig.BindAdminOnly(ConfigSection, "DurationMinutes", DurationSeconds / 60f, "How long the effect lasts.",
            new AcceptableValueRange<float>(1f, 120f));
        Translations.AddEnglishNameAndDescription(NameKey, FullName, Description);
        Translations.AddEnglish(MeadBaseKey, $"Mead Base: {FullName}");
        Translations.AddEnglish(TooltipKey, EffectTooltip);
    }

    /// <summary>
    /// Adds the mead base, the mead, its status effect and the fermenter conversion.
    /// </summary>
    public void Add()
    {
        try
        {
            CustomItem meadBase = new(MeadBaseName, CopyMeadBaseFrom, new ItemConfig
            {
                Name = Translations.Token(MeadBaseKey),
                Description = "$item_meadbase_description",
                CraftingStation = CraftingStations.Cauldron,
                Requirements = Requirements
            });
            ApplyLook(meadBase);
            instance.AddItem(meadBase);

            CustomItem mead = new(BaseName, CopyMeadFrom, new ItemConfig
            {
                Name = Translations.Token(NameKey),
                Description = Translations.Token($"{NameKey}_description")
            });
            ItemDrop.ItemData.SharedData shared = mead.ItemDrop.m_itemData.m_shared;
            ApplyLook(mead);

            CustomStatusEffect customEffect = new(CreateEffect(shared), fixReference: false);
            instance.AddStatusEffect(customEffect);
            shared.m_consumeStatusEffect = customEffect.StatusEffect;
            effect = (SE_Stats)customEffect.StatusEffect;
            ApplyConfig();
            instance.AddItem(mead);

            instance.AddItemConversion(new CustomItemConversion(new FermenterConversionConfig
            {
                FromItem = MeadBaseName,
                ToItem = BaseName,
                Station = Fermenters.Fermenter,
                ProducedItems = 6
            }));

            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    /// <summary>
    /// Applies the configured duration and effect values. Drinks already active keep their values until they end.
    /// </summary>
    public void ApplyConfig()
    {
        if (effect == null)
        {
            return;
        }

        effect.m_ttl = durationMinutes.Value * 60f;
        ConfigureEffect(effect);
    }

    /// <summary>
    /// Sets what the effect does. The effect starts empty, apart from its duration. Called again when the config changes,
    /// so it must set values rather than add to them.
    /// </summary>
    /// <param name="effect">The mead's status effect.</param>
    protected abstract void ConfigureEffect(SE_Stats effect);

    private SE_Stats CreateEffect(ItemDrop.ItemData.SharedData shared)
    {
        SE_Stats effect = ScriptableObject.CreateInstance<SE_Stats>();
        effect.name = $"SE_{BaseName}";
        effect.m_name = Translations.Token(NameKey);
        effect.m_tooltip = Translations.Token(TooltipKey);
        effect.m_icon = shared.m_icons[0];
        effect.m_ttl = DurationSeconds;

        // Keep the vanilla drinking animation and effects, but not its category, which would cancel vanilla meads.
        StatusEffect vanilla = shared.m_consumeStatusEffect;
        if (vanilla != null)
        {
            effect.m_startEffects = vanilla.m_startEffects;
            effect.m_stopEffects = vanilla.m_stopEffects;
            effect.m_activationAnimation = vanilla.m_activationAnimation;
        }

        ConfigureEffect(effect);
        return effect;
    }

    private void ApplyLook(CustomItem item)
    {
        VisualHelper.Tint(item.ItemPrefab, Tint);
        Sprite icon = VisualHelper.RenderIcon(item.ItemPrefab);
        if (icon != null)
        {
            item.ItemDrop.m_itemData.m_shared.m_icons = new[] { icon };
        }
    }
}
