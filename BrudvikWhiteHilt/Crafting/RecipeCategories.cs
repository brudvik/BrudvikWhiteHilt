namespace BrudvikWhiteHilt.Crafting;

/// <summary>
/// The tabs above the recipe list. Every recipe falls in one of them, read from what its item is, so the recipes of
/// other mods are sorted too without a list to keep up.
/// </summary>
public enum RecipeCategory
{
    All,
    Weapons,
    Armor,
    Tools,
    Ammo,
    Food,
    Materials
}

/// <summary>
/// Puts recipes in the tabs above the recipe list.
/// </summary>
public static class RecipeCategories
{
    /// <summary>
    /// The tab an item belongs in. Pickaxes and fishing rods are held like weapons but are tools; axes stay weapons.
    /// </summary>
    /// <param name="type">The item's type.</param>
    /// <param name="skill">The skill the item uses.</param>
    /// <returns>The tab, never All.</returns>
    public static RecipeCategory Of(ItemDrop.ItemData.ItemType type, Skills.SkillType skill)
    {
        switch (type)
        {
            case ItemDrop.ItemData.ItemType.OneHandedWeapon:
            case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
            case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
            case ItemDrop.ItemData.ItemType.Bow:
            case ItemDrop.ItemData.ItemType.Attach_Atgeir:
                return skill is Skills.SkillType.Pickaxes or Skills.SkillType.Fishing ? RecipeCategory.Tools : RecipeCategory.Weapons;
            case ItemDrop.ItemData.ItemType.Tool:
            case ItemDrop.ItemData.ItemType.Torch:
                return RecipeCategory.Tools;
            case ItemDrop.ItemData.ItemType.Shield:
            case ItemDrop.ItemData.ItemType.Helmet:
            case ItemDrop.ItemData.ItemType.Chest:
            case ItemDrop.ItemData.ItemType.Legs:
            case ItemDrop.ItemData.ItemType.Hands:
            case ItemDrop.ItemData.ItemType.Shoulder:
            case ItemDrop.ItemData.ItemType.Utility:
            case ItemDrop.ItemData.ItemType.Trinket:
            case ItemDrop.ItemData.ItemType.Customization:
                return RecipeCategory.Armor;
            case ItemDrop.ItemData.ItemType.Ammo:
            case ItemDrop.ItemData.ItemType.AmmoNonEquipable:
                return RecipeCategory.Ammo;
            case ItemDrop.ItemData.ItemType.Consumable:
            case ItemDrop.ItemData.ItemType.Fish:
                return RecipeCategory.Food;
            default:
                return RecipeCategory.Materials;
        }
    }

    /// <summary>
    /// The tab a recipe belongs in.
    /// </summary>
    /// <param name="recipe">The recipe.</param>
    /// <returns>The tab, Materials when the recipe has no item.</returns>
    public static RecipeCategory Of(Recipe recipe)
    {
        ItemDrop.ItemData.SharedData shared = recipe != null && recipe.m_item != null ? recipe.m_item.m_itemData.m_shared : null;
        return shared == null ? RecipeCategory.Materials : Of(shared.m_itemType, shared.m_skillType);
    }
}
