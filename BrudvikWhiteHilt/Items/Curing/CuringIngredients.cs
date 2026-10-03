using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Cooking.StonePot;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Curing;

/// <summary>
/// Base class for food made ready for curing in the Stone Pot, e.g. a seasoned ham that is then hung on the drying rack.
/// Not food itself.
/// </summary>
public abstract class CuringIngredientBase : IWhiteHiltCustomItem
{
    private readonly ItemManager instance;

    /// <summary>Prefab name of the item.</summary>
    protected abstract string BaseName { get; }

    /// <summary>Name shown to players, in English.</summary>
    protected abstract string FullName { get; }

    /// <summary>Item description, in English.</summary>
    protected abstract string Description { get; }

    /// <summary>Vanilla item to clone the model from.</summary>
    protected abstract string CopyFrom { get; }

    /// <summary>Ingredients.</summary>
    protected abstract RequirementConfig[] Requirements { get; }

    /// <summary>Colour multiplied into the cloned model.</summary>
    protected abstract Color Tint { get; }

    /// <inheritdoc/>
    public virtual bool Enabled => true;

    /// <inheritdoc/>
    public virtual ProgressionTier DefaultTier => ProgressionTier.Start;

    /// <inheritdoc/>
    public string Id => BaseName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(Translations.ItemKey(BaseName));

    /// <inheritdoc/>
    public string GatedPrefabName => BaseName;

    /// <summary>
    /// Constructor for the CuringIngredientBase class. Registers the English text.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    protected CuringIngredientBase(ItemManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(BaseName), FullName, Description);
    }

    /// <summary>
    /// Adds the item and its Stone Pot recipe.
    /// </summary>
    public void Add()
    {
        try
        {
            string key = Translations.ItemKey(BaseName);
            CustomItem item = new(BaseName, CopyFrom, new ItemConfig
            {
                Name = Translations.Token(key),
                Description = Translations.Token($"{key}_description"),
                CraftingStation = StonePot.PrefabName,
                Requirements = Requirements
            });
            ItemDrop.ItemData.SharedData shared = item.ItemDrop.m_itemData.m_shared;
            shared.m_itemType = ItemDrop.ItemData.ItemType.Material;
            shared.m_food = 0f;
            shared.m_foodStamina = 0f;
            shared.m_foodEitr = 0f;
            shared.m_consumeStatusEffect = null;
            VisualHelper.Tint(item.ItemPrefab, Tint);
            Sprite icon = VisualHelper.RenderIcon(item.ItemPrefab);
            if (icon != null)
            {
                shared.m_icons = new[] { icon };
            }

            instance.AddItem(item);
            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }
}

/// <summary>
/// A boar ham rubbed with wild garlic and thistle, ready to hang on the drying rack.
/// </summary>
public class SeasonedHam : CuringIngredientBase
{
    /// <summary>Prefab name.</summary>
    public const string PrefabName = "WhiteHiltSeasonedHam";

    /// <summary>
    /// Constructor for the SeasonedHam class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public SeasonedHam(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => PrefabName;

    /// <inheritdoc/>
    protected override string FullName => "Seasoned Ham";

    /// <inheritdoc/>
    protected override string Description => "A boar ham rubbed with wild garlic and thistle. Hang it on a drying rack to cure.";

    /// <inheritdoc/>
    protected override string CopyFrom => "RawMeat";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "RawMeat", Amount = 2, Recover = false },
        new() { Item = Foraging.WildGarlic.WildGarlic.PrefabName, Amount = 1, Recover = false },
        new() { Item = "Thistle", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override Color Tint => new(0.9f, 0.75f, 0.7f);

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;
}

/// <summary>
/// Minced boar and deer in a casing, ready to hang on the drying rack.
/// </summary>
public class RawSausage : CuringIngredientBase
{
    /// <summary>Prefab name.</summary>
    public const string PrefabName = "WhiteHiltRawSausage";

    /// <summary>
    /// Constructor for the RawSausage class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public RawSausage(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => PrefabName;

    /// <inheritdoc/>
    protected override string FullName => "Raw Sausage";

    /// <inheritdoc/>
    protected override string Description => "Minced boar and deer with thistle seeds, stuffed in a casing. Hang it on a drying rack to cure.";

    /// <inheritdoc/>
    protected override string CopyFrom => "RawMeat";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "RawMeat", Amount = 1, Recover = false },
        new() { Item = "DeerMeat", Amount = 1, Recover = false },
        new() { Item = "Thistle", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override Color Tint => new(0.75f, 0.45f, 0.4f);

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;
}

/// <summary>
/// Pike gutted and laid in a tub with honey, ready to ferment into rakfisk.
/// </summary>
public class RakfiskTub : CuringIngredientBase
{
    /// <summary>Prefab name.</summary>
    public const string PrefabName = "WhiteHiltRakfiskTub";

    /// <summary>
    /// Constructor for the RakfiskTub class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public RakfiskTub(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => PrefabName;

    /// <inheritdoc/>
    protected override string FullName => "Rakfisk Tub";

    /// <inheritdoc/>
    protected override string Description => "Gutted pike packed tight in a tub with a little honey. Put it in a fermenter and it turns to rakfisk.";

    /// <inheritdoc/>
    protected override string CopyFrom => "MeadBaseStaminaMedium";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Fish2", Amount = 2, Recover = false },
        new() { Item = "Honey", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override Color Tint => new(0.6f, 0.65f, 0.55f);

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;
}
