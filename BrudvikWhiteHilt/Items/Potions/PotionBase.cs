using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions;

/// <summary>
/// Base class for the Gifts of the gods, the White Hilt potions. A potion is made like a vanilla mead: a mead base
/// cooked at the Cauldron, then fermented into the potion. Both are clones of the vanilla minor healing mead and its
/// base, tinted or given an icon of their own; drinking the potion applies the status effect from
/// <see cref="CreateEffect"/>, whose numbers come from <see cref="PotionSettings"/>. The plugin finds every subclass by
/// reflection and calls <see cref="Add"/>.
/// </summary>
public abstract class PotionBase : IWhiteHiltCustomItem
{
    /// <summary>
    /// Base of the potion's prefab names: the potion is <c>&lt;name&gt;Mead</c> and its base
    /// <c>&lt;name&gt;MeadBase</c>, and
    /// their translation keys follow (<c>$item_&lt;name&gt;mead</c>). Saved worlds and inventories refer to the items
    /// by
    /// these names, so it must never change once released.
    /// </summary>
    protected abstract string BaseName { get; }

    /// <summary>
    /// Name shown to players, in English. Other languages come from the embedded translation files
    /// (Translations/*.json), keyed by the prefab name.
    /// </summary>
    protected abstract string FullName { get; }

    /// <summary>
    /// Item description, in English; translated the same way as the name.
    /// </summary>
    protected abstract string Description { get; }

    /// <summary>
    /// The path to the embedded icon of the potion, or null to render the tinted mead as its icon.
    /// </summary>
    protected virtual string IconPath => null;

    /// <summary>
    /// Colour multiplied into the mead and its base when the potion has no embedded icon.
    /// </summary>
    protected virtual Color Tint => Color.white;

    /// <summary>
    /// Whether the potion is added to the game at all. A disabled one is skipped when the plugin starts, so it never
    /// reaches ObjectDB or a recipe.
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
    /// Ingredients of the mead base, cooked at the Cauldron; the fermenter then turns it into the potion, as with the
    /// vanilla meads.
    /// </summary>
    protected abstract RequirementConfig[] MeadBaseRequirements { get; }

    private readonly ItemManager instance;
    private bool effectTextRegistered;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run. Registers the English text.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    protected PotionBase(ItemManager instance)
    {
        PotionSettings.Initialize();
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
            if (IconPath == null)
            {
                ApplyRenderedLook(meadBase);
            }

            instance.AddItem(meadBase);

            // Create Mead
            ItemConfig meadConfig = new()
            {
                Name = Translations.Token(MeadKey),
                Description = Translations.Token($"{MeadKey}_description")
            };

            CustomItem mead = new($"{BaseName}Mead", "MeadHealthMinor", meadConfig);
            mead.ItemDrop.m_itemData.m_shared.m_description = Translations.Token($"{MeadKey}_description");
            if (IconPath != null)
            {
                mead.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtilsExtended.LoadTextureFromEmbeddedResource(IconPath).ConvertToSprite();
            }
            else
            {
                ApplyRenderedLook(mead);
            }

            var effect = CreateEffect();
            ApplyEffectTokens(effect);
            if (effect.m_icon == null)
            {
                effect.m_icon = mead.ItemDrop.m_itemData.m_shared.m_icons[0];
            }
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
    /// Creates the status effect drinking the potion gives. Called once, when the potion is added; its numbers come
    /// from <see cref="PotionSettings"/>, so a config change applies to the next drink.
    /// </summary>
    /// <returns>The effect, which the base registers with Jotunn and sets as the mead's consume effect.</returns>
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

    // Points the status effect's texts (start and stop messages, tooltip, name) at this mod's translation keys, only
    // where the cloned effect had such a text.
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

    private void ApplyRenderedLook(CustomItem item)
    {
        VisualHelper.Tint(item.ItemPrefab, Tint);
        Sprite icon = VisualHelper.RenderIcon(item.ItemPrefab);
        if (icon != null)
        {
            item.ItemDrop.m_itemData.m_shared.m_icons[0] = icon;
        }
    }
}
