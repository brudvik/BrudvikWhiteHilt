using System.Collections.Generic;

namespace BrudvikWhiteHilt.Performance;

/// <summary>
/// Finds a recipe or a status effect in ObjectDB by name instead of searching the whole list. <c>ObjectDB.GetRecipe</c>
/// compares the item's name with every recipe's, and the crafting window asks it for every worn item in the inventory
/// every frame to light the repair button; <c>ObjectDB.GetStatusEffect</c> walks every status effect each time one is
/// added to a character. Mods add recipes and status effects late and may replace them, so a table is trusted only
/// while the list is the same list of the same length, every hit is checked against the list, and anything not found
/// is left to vanilla's own search. The answer is always the one vanilla would give, except that of two recipes for
/// one item swapped in place, the one first when the table was built is kept.
/// </summary>
public static class ObjectDBLookups
{
    private static readonly Dictionary<string, int> recipeIndex = new();
    private static readonly Dictionary<int, int> statusEffectIndex = new();
    private static List<Recipe> recipeList;
    private static int recipeCount = -1;
    private static List<StatusEffect> statusEffectList;
    private static int statusEffectCount = -1;

    /// <summary>
    /// The first recipe that makes the item, as <c>ObjectDB.GetRecipe</c> finds it.
    /// </summary>
    /// <param name="db">The object database.</param>
    /// <param name="item">The item.</param>
    /// <param name="recipe">The recipe, when found.</param>
    /// <returns>False when the table does not know it, and vanilla should search.</returns>
    public static bool TryGetRecipe(ObjectDB db, ItemDrop.ItemData item, out Recipe recipe)
    {
        recipe = null;
        string name = item?.m_shared?.m_name;
        List<Recipe> recipes = db.m_recipes;
        if (name == null || recipes == null)
        {
            return false;
        }

        if (!ReferenceEquals(recipes, recipeList) || recipes.Count != recipeCount)
        {
            BuildRecipes(recipes);
        }

        if (!recipeIndex.TryGetValue(name, out int index))
        {
            return false;
        }

        if (index < recipes.Count && MakesItem(recipes[index], name))
        {
            recipe = recipes[index];
            return true;
        }

        // The list was changed in place; build again and let vanilla answer this time.
        BuildRecipes(recipes);
        return false;
    }

    /// <summary>
    /// The status effect with the name hash, as <c>ObjectDB.GetStatusEffect</c> finds it.
    /// </summary>
    /// <param name="db">The object database.</param>
    /// <param name="nameHash">The status effect's name hash.</param>
    /// <param name="effect">The status effect, when found.</param>
    /// <returns>False when the table does not know it, and vanilla should search.</returns>
    public static bool TryGetStatusEffect(ObjectDB db, int nameHash, out StatusEffect effect)
    {
        effect = null;
        List<StatusEffect> effects = db.m_StatusEffects;
        if (effects == null)
        {
            return false;
        }

        if (!ReferenceEquals(effects, statusEffectList) || effects.Count != statusEffectCount)
        {
            BuildStatusEffects(effects);
        }

        if (!statusEffectIndex.TryGetValue(nameHash, out int index))
        {
            return false;
        }

        if (index < effects.Count && effects[index] != null && effects[index].NameHash() == nameHash)
        {
            effect = effects[index];
            return true;
        }

        BuildStatusEffects(effects);
        return false;
    }

    private static bool MakesItem(Recipe recipe, string name)
    {
        return recipe != null && recipe.m_item != null && recipe.m_item.m_itemData.m_shared.m_name == name;
    }

    // Maps each item name to the first recipe that makes it, as vanilla's search finds the first.
    private static void BuildRecipes(List<Recipe> recipes)
    {
        recipeIndex.Clear();
        for (int i = 0; i < recipes.Count; i++)
        {
            Recipe recipe = recipes[i];
            if (recipe != null && recipe.m_item != null && recipe.m_item.m_itemData?.m_shared?.m_name is string name
                && !recipeIndex.ContainsKey(name))
            {
                recipeIndex.Add(name, i);
            }
        }

        recipeList = recipes;
        recipeCount = recipes.Count;
    }

    private static void BuildStatusEffects(List<StatusEffect> effects)
    {
        statusEffectIndex.Clear();
        for (int i = 0; i < effects.Count; i++)
        {
            StatusEffect effect = effects[i];
            if (effect != null)
            {
                int hash = effect.NameHash();
                if (!statusEffectIndex.ContainsKey(hash))
                {
                    statusEffectIndex.Add(hash, i);
                }
            }
        }

        statusEffectList = effects;
        statusEffectCount = effects.Count;
    }
}
