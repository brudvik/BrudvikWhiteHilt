using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;

namespace BrudvikWhiteHilt.Items.Potions;

/// <summary>
/// This class defines the base for all potions.
/// </summary>
public abstract class PotionBase : IWhiteHiltCustomItem
{
    /// <summary>
    /// The base name of the potion.
    /// </summary>
    protected abstract string BaseName { get; }

    /// <summary>
    /// The full name of the potion.
    /// </summary>
    protected abstract string FullName { get; }

    /// <summary>
    /// The description of the potion.
    /// </summary>
    protected abstract string Description { get; }

    /// <summary>
    /// The path to the icon of the potion.
    /// </summary>
    protected abstract string IconPath { get; }

    /// <summary>
    /// Indicates whether the potion is enabled or not.
    /// </summary>
    public abstract bool Enabled { get; }

    /// <inheritdoc/>
    public abstract ProgressionTier DefaultTier { get; }

    /// <inheritdoc/>
    public string Id => BaseName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(MeadKey);

    /// <inheritdoc/>
    public string GatedPrefabName => $"{BaseName}MeadBase";

    /// <summary>
    /// The requirements for crafting the potion.
    /// </summary>
    protected abstract RequirementConfig[] MeadBaseRequirements { get; }

    private readonly ItemManager instance;
    private bool effectTextRegistered;

    /// <summary>
    /// Constructor for the PotionBase class. Registers the English text.
    /// </summary>
    /// <param name="instance"></param>
    protected PotionBase(ItemManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(MeadKey, FullName, Description);
        Translations.AddEnglish(MeadBaseKey, $"Mead Base: {FullName}");
        RegisterEffectText();
    }

    private string MeadKey => Translations.ItemKey($"{BaseName}Mead");

    private string MeadBaseKey => Translations.ItemKey($"{BaseName}MeadBase");

    private string EffectKey => $"se_{BaseName.ToLowerInvariant()}";

    /// <summary>
    /// Adds the potion to the game.
    /// </summary>
    public void Add()
    {
        try
        {
            // Create Mead Base
            ItemConfig meadBaseConfig = new()
            {
                Name = Translations.Token(MeadBaseKey),
                Description = "$item_meadbase_description",
                CraftingStation = CraftingStations.Cauldron,
                Requirements = MeadBaseRequirements
            };

            CustomItem meadBase = new($"{BaseName}MeadBase", "MeadBaseHealthMinor", meadBaseConfig);
            instance.AddItem(meadBase);

            // Create Mead
            ItemConfig meadConfig = new()
            {
                Name = Translations.Token(MeadKey),
                Description = Translations.Token($"{MeadKey}_description")
            };

            CustomItem mead = new($"{BaseName}Mead", "MeadHealthMinor", meadConfig);
            mead.ItemDrop.m_itemData.m_shared.m_description = Translations.Token($"{MeadKey}_description");
            mead.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtilsExtended.LoadTextureFromEmbeddedResource(IconPath).ConvertToSprite();

            var effect = CreateEffect();
            ApplyEffectTokens(effect);
            var customEffect = new CustomStatusEffect(effect, fixReference: false);
            instance.AddStatusEffect(customEffect);
            mead.ItemDrop.m_itemData.m_shared.m_consumeStatusEffect = customEffect.StatusEffect;

            instance.AddItem(mead);

            // Add Fermenter Conversion
            var fermentConfig = new FermenterConversionConfig
            {
                FromItem = $"{BaseName}MeadBase",
                ToItem = $"{BaseName}Mead",
                Station = Fermenters.Fermenter,
                ProducedItems = 1
            };
            instance.AddItemConversion(new CustomItemConversion(fermentConfig));

            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    /// <summary>
    /// Creates the effect for the potion.
    /// </summary>
    /// <returns></returns>
    protected abstract SE_Stats CreateEffect();

    // Effects set their English text in Initialize, which runs after Valheim has loaded its languages,
    // so a throwaway effect is created here, in Awake, to register that text in time.
    private void RegisterEffectText()
    {
        SE_Stats effect = null;
        try
        {
            effect = CreateEffect();
            AddEnglishIfSet($"{EffectKey}_start", effect.m_startMessage);
            AddEnglishIfSet($"{EffectKey}_stop", effect.m_stopMessage);
            if (effect.m_tooltip != FullName)
            {
                AddEnglishIfSet($"{EffectKey}_tooltip", effect.m_tooltip);
            }

            effectTextRegistered = true;
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: effect text stays untranslated: {ex.Message}");
        }
        finally
        {
            if (effect != null)
            {
                UnityEngine.Object.Destroy(effect);
            }
        }
    }

    private void ApplyEffectTokens(SE_Stats effect)
    {
        if (!effectTextRegistered)
        {
            return;
        }

        if (!string.IsNullOrEmpty(effect.m_startMessage))
        {
            effect.m_startMessage = Translations.Token($"{EffectKey}_start");
        }

        if (!string.IsNullOrEmpty(effect.m_stopMessage))
        {
            effect.m_stopMessage = Translations.Token($"{EffectKey}_stop");
        }

        if (effect.m_tooltip == FullName)
        {
            effect.m_tooltip = Translations.Token(MeadKey);
        }
        else if (!string.IsNullOrEmpty(effect.m_tooltip))
        {
            effect.m_tooltip = Translations.Token($"{EffectKey}_tooltip");
        }

        effect.m_name = Translations.Token(MeadKey);
    }

    private static void AddEnglishIfSet(string key, string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            Translations.AddEnglish(key, text);
        }
    }
}
