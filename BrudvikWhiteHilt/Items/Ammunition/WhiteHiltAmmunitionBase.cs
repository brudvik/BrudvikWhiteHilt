using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;

namespace BrudvikWhiteHilt.Items.Ammunition;

/// <summary>
/// Base class for the White Hilt arrows and bolts. Each is a clone of vanilla ammunition (<see cref="CopyFrom"/>),
/// which brings the projectile and its flight; this class sets the stack crafted at once and applies the config's
/// damage. Ammunition is used up, so unlike the other White Hilt gear it is not indestructible. The plugin finds every
/// subclass by reflection and calls <see cref="Add"/>.
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
    /// Prefab name of the ammunition. It is also the key of its name and description in the translation files
    /// (<c>$item_&lt;name&gt;</c>), and saved worlds and inventories refer to the item by it, so it must never change
    /// once released.
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
    /// Vanilla ammunition it is cloned from; it brings the projectile, its flight and its hit effects.
    /// </summary>
    protected abstract string CopyFrom { get; }

    /// <summary>
    /// The amount of ammunition crafted per recipe.
    /// </summary>
    protected virtual int CraftAmount => 200;

    /// <summary>
    /// Ingredients of the recipe. The progression tier may add its own cost on top (see ProgressionManager).
    /// </summary>
    protected abstract RequirementConfig[] Requirements { get; }

    /// <summary>
    /// Whether the ammunition is added to the game at all. A disabled one is skipped when the plugin starts, so it
    /// never reaches ObjectDB or a recipe.
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
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
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
