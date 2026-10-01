using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;

namespace BrudvikWhiteHilt.Items.Ammunition;

/// <summary>
/// This class defines the base for all White Hilt ammunition items.
/// </summary>
public abstract class WhiteHiltAmmunitionBase : IWhiteHiltCustomItem, IWhiteHiltConfigurable
{
    private const string Section = "Gear.Ammunition";

    private static ConfigEntry<float> pierceMultiplier;
    private static ConfigEntry<float> bonusFire;
    private static ConfigEntry<float> bonusSpirit;

    private ItemDrop.ItemData.SharedData shared;
    private HitData.DamageTypes baseDamages;

    /// <summary>
    /// The base name of the ammunition item.
    /// </summary>
    protected abstract string BaseName { get; }

    /// <summary>
    /// The full name of the ammunition item.
    /// </summary>
    protected abstract string FullName { get; }

    /// <summary>
    /// The description of the ammunition item.
    /// </summary>
    protected abstract string Description { get; }

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected abstract string CopyFrom { get; }

    /// <summary>
    /// The amount of ammunition crafted per recipe.
    /// </summary>
    protected virtual int CraftAmount => 200;

    /// <summary>
    /// The requirements for crafting the ammunition item.
    /// </summary>
    protected abstract RequirementConfig[] Requirements { get; }

    /// <summary>
    /// Indicates whether the ammunition is enabled or not.
    /// </summary>
    public abstract bool Enabled { get; }

    /// <inheritdoc/>
    public virtual ProgressionTier DefaultTier => ProgressionTier.Swamp;

    /// <inheritdoc/>
    public string Id => BaseName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(Translations.ItemKey(BaseName));

    /// <inheritdoc/>
    public string GatedPrefabName => BaseName;

    private readonly ItemManager instance;

    /// <summary>
    /// Constructor for the WhiteHiltAmmunitionBase class.
    /// </summary>
    /// <param name="instance"></param>
    protected WhiteHiltAmmunitionBase(ItemManager instance)
    {
        this.instance = instance;
        BindConfig();
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(BaseName), FullName, Description);
    }

    /// <summary>
    /// Adds the ammunition item to the game.
    /// </summary>
    public void Add()
    {
        try
        {
            ItemConfig config = new()
            {
                Name = Translations.Token(Translations.ItemKey(BaseName)),
                Description = Translations.Token($"{Translations.ItemKey(BaseName)}_description"),
                CraftingStation = CraftingStations.Forge,
                MinStationLevel = 2,
                Amount = CraftAmount,
                Requirements = Requirements
            };

            CustomItem item = new(BaseName, CopyFrom, config);
            ItemDrop.ItemData.SharedData itemData = item.ItemDrop.m_itemData.m_shared;
            baseDamages = itemData.m_damages.Clone();
            shared = itemData;
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

    /// <summary>
    /// Applies the configured damage on top of the cloned vanilla values.
    /// </summary>
    public void ApplyConfig()
    {
        if (shared == null)
        {
            return;
        }

        HitData.DamageTypes damages = baseDamages.Clone();
        damages.m_pierce *= pierceMultiplier.Value;
        damages.m_fire += bonusFire.Value;
        damages.m_spirit += bonusSpirit.Value;
        shared.m_damages = damages;
    }

    private static void BindConfig()
    {
        if (pierceMultiplier != null)
        {
            return;
        }

        pierceMultiplier = WhiteHiltConfig.BindAdminOnly(Section, "PierceMultiplier", 2f,
            "Multiplies the pierce damage of White Hilt arrows and bolts.", new AcceptableValueRange<float>(0f, 10f));
        bonusFire = WhiteHiltConfig.BindAdminOnly(Section, "BonusFireDamage", 30f,
            "Fire damage added to White Hilt arrows and bolts.", new AcceptableValueRange<float>(0f, 500f));
        bonusSpirit = WhiteHiltConfig.BindAdminOnly(Section, "BonusSpiritDamage", 20f,
            "Spirit damage added to White Hilt arrows and bolts.", new AcceptableValueRange<float>(0f, 500f));
    }
}
