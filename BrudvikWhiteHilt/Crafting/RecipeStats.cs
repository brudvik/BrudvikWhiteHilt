using System;
using System.Globalization;
using System.Text;

namespace BrudvikWhiteHilt.Crafting;

/// <summary>
/// How the recipe list is sorted: by name, or by what the made item gives.
/// </summary>
public enum RecipeSort
{
    /// <summary>By name, favourites first (or the game's order with SortByName off).</summary>
    Name,

    /// <summary>Food by the health it gives.</summary>
    Health,

    /// <summary>Food by the stamina it gives.</summary>
    Stamina,

    /// <summary>Food by the eitr it gives.</summary>
    Eitr,

    /// <summary>Food by health, stamina and eitr together.</summary>
    Total,

    /// <summary>Food by the player's profile: health, stamina and eitr weighted.</summary>
    BestForMe,

    /// <summary>Gear by its strength: a weapon's or ammunition's damage, armour's armour, a shield's block.</summary>
    Strength
}

/// <summary>
/// How the player weighs health, stamina and eitr when food is sorted Best for me.
/// </summary>
public enum FoodProfile
{
    /// <summary>All three alike.</summary>
    Balanced,

    /// <summary>Health first, then stamina.</summary>
    Warrior,

    /// <summary>Stamina first, then health.</summary>
    Archer,

    /// <summary>Eitr first, then health.</summary>
    Mage,

    /// <summary>The weights in the settings.</summary>
    Custom
}

/// <summary>
/// What kind of strength a piece of gear has.
/// </summary>
public enum StrengthKind
{
    /// <summary>No strength to show.</summary>
    None,

    /// <summary>Damage, of a weapon or ammunition.</summary>
    Damage,

    /// <summary>Armour.</summary>
    Armor,

    /// <summary>Block power, of a shield.</summary>
    Block
}

/// <summary>
/// What a recipe's item gives, read from the item: a food's health, stamina, eitr and time; a potion's or mead's effect
/// and time; a piece of gear's damage, armour or block.
/// </summary>
public readonly struct ItemStats
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ItemStats"/> struct.
    /// </summary>
    /// <param name="health">Health a food gives.</param>
    /// <param name="stamina">Stamina a food gives.</param>
    /// <param name="eitr">Eitr a food gives.</param>
    /// <param name="minutes">How long a food or an effect lasts, in minutes.</param>
    /// <param name="strength">A piece of gear's strength.</param>
    /// <param name="kind">What kind of strength it is.</param>
    /// <param name="effect">The name of a potion's or mead's effect, as shown, or null.</param>
    public ItemStats(float health, float stamina, float eitr, float minutes, float strength = 0f, StrengthKind kind = StrengthKind.None, string effect = null)
    {
        Health = health;
        Stamina = stamina;
        Eitr = eitr;
        Minutes = minutes;
        Strength = strength;
        Kind = kind;
        Effect = effect;
    }

    /// <summary>Gets the health a food gives.</summary>
    public float Health { get; }

    /// <summary>Gets the stamina a food gives.</summary>
    public float Stamina { get; }

    /// <summary>Gets the eitr a food gives.</summary>
    public float Eitr { get; }

    /// <summary>Gets how long a food or an effect lasts, in minutes.</summary>
    public float Minutes { get; }

    /// <summary>Gets a piece of gear's strength.</summary>
    public float Strength { get; }

    /// <summary>Gets what kind of strength it is.</summary>
    public StrengthKind Kind { get; }

    /// <summary>Gets the name of a potion's or mead's effect, or null.</summary>
    public string Effect { get; }

    /// <summary>Gets whether the item is food.</summary>
    public bool IsFood => Health > 0f || Stamina > 0f || Eitr > 0f;

    /// <summary>Gets whether there is anything to show.</summary>
    public bool IsEmpty => !IsFood && Kind == StrengthKind.None && string.IsNullOrEmpty(Effect);
}

/// <summary>
/// The numbers behind sorting the recipe list by what the items give, and the short line shown beside each recipe.
/// Pure, so the rules can be tested without the game.
/// </summary>
public static class RecipeStats
{
    // The game's own colours for health, stamina and eitr in a food's tooltip.
    private const string HealthColour = "#FF8080";
    private const string StaminaColour = "#FFFF80";
    private const string EitrColour = "#9090FF";
    private const string StrengthColour = "#E8B04B";
    private const string EffectColour = "#C8A8FF";
    private const string TimeColour = "#A0A0A0";

    /// <summary>
    /// The weights a profile gives health, stamina and eitr.
    /// </summary>
    /// <param name="profile">The profile.</param>
    /// <param name="custom">The weights in the settings, for <see cref="FoodProfile.Custom"/>.</param>
    /// <returns>The weights.</returns>
    public static (float Health, float Stamina, float Eitr) Weights(FoodProfile profile, (float Health, float Stamina, float Eitr) custom)
    {
        return profile switch
        {
            FoodProfile.Warrior => (1.5f, 1f, 0.5f),
            FoodProfile.Archer => (1f, 1.5f, 0.5f),
            FoodProfile.Mage => (1f, 0.5f, 1.5f),
            FoodProfile.Custom => custom,
            _ => (1f, 1f, 1f)
        };
    }

    /// <summary>
    /// What an item is sorted by, or null when the sort says nothing about it (it then goes after those it does).
    /// </summary>
    /// <param name="stats">The item's stats.</param>
    /// <param name="sort">The sort.</param>
    /// <param name="weights">The profile's weights, for <see cref="RecipeSort.BestForMe"/>.</param>
    /// <returns>The value, higher first.</returns>
    public static float? Key(ItemStats stats, RecipeSort sort, (float Health, float Stamina, float Eitr) weights)
    {
        switch (sort)
        {
            case RecipeSort.Health:
            case RecipeSort.Stamina:
            case RecipeSort.Eitr:
            case RecipeSort.Total:
            case RecipeSort.BestForMe:
                if (!stats.IsFood)
                {
                    return null;
                }

                return sort switch
                {
                    RecipeSort.Health => stats.Health,
                    RecipeSort.Stamina => stats.Stamina,
                    RecipeSort.Eitr => stats.Eitr,
                    RecipeSort.Total => stats.Health + stats.Stamina + stats.Eitr,
                    _ => stats.Health * weights.Health + stats.Stamina * weights.Stamina + stats.Eitr * weights.Eitr
                };
            case RecipeSort.Strength:
                return stats.Kind == StrengthKind.None ? null : stats.Strength;
            default:
                return null;
        }
    }

    /// <summary>
    /// Compares two sort values, higher first and none last.
    /// </summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns>Less than 0 when a goes first.</returns>
    public static int Compare(float? a, float? b)
    {
        if (a.HasValue != b.HasValue)
        {
            return a.HasValue ? -1 : 1;
        }

        return a.HasValue ? b.Value.CompareTo(a.Value) : 0;
    }

    /// <summary>
    /// The short line shown beside a recipe: a food's health, stamina and eitr in the game's colours and its time; a
    /// potion's effect and time; a piece of gear's damage, armour or block. The number sorted by is in bold, and a
    /// sort by total or profile adds the score.
    /// </summary>
    /// <param name="stats">The item's stats.</param>
    /// <param name="sort">The list's sort.</param>
    /// <param name="weights">The profile's weights.</param>
    /// <param name="labels">The words for damage, armour and block, in the player's language.</param>
    /// <returns>Rich text, empty when there is nothing to show.</returns>
    public static string Format(ItemStats stats, RecipeSort sort, (float Health, float Stamina, float Eitr) weights, (string Damage, string Armor, string Block) labels)
    {
        StringBuilder text = new();
        if (stats.IsFood)
        {
            text.Append(Number(stats.Health, HealthColour, sort == RecipeSort.Health));
            text.Append(' ').Append(Number(stats.Stamina, StaminaColour, sort == RecipeSort.Stamina));
            if (stats.Eitr > 0f || sort == RecipeSort.Eitr)
            {
                text.Append(' ').Append(Number(stats.Eitr, EitrColour, sort == RecipeSort.Eitr));
            }

            if (sort is RecipeSort.Total or RecipeSort.BestForMe)
            {
                text.Append(' ').Append(Number(Key(stats, sort, weights) ?? 0f, StrengthColour, true, "(", ")"));
            }

            AppendTime(text, stats.Minutes);
        }
        else if (stats.Kind != StrengthKind.None)
        {
            string label = stats.Kind switch
            {
                StrengthKind.Armor => labels.Armor,
                StrengthKind.Block => labels.Block,
                _ => labels.Damage
            };
            text.Append(Number(stats.Strength, StrengthColour, sort == RecipeSort.Strength));
            text.Append(' ').Append($"<color={TimeColour}>{label}</color>");
        }
        else if (!string.IsNullOrEmpty(stats.Effect))
        {
            text.Append($"<color={EffectColour}>{stats.Effect}</color>");
            AppendTime(text, stats.Minutes);
        }

        return text.ToString();
    }

    private static string Number(float value, string colour, bool bold, string before = "", string after = "")
    {
        string number = before + Math.Round(value).ToString("0", CultureInfo.InvariantCulture) + after;
        return bold ? $"<color={colour}><b>{number}</b></color>" : $"<color={colour}>{number}</color>";
    }

    private static void AppendTime(StringBuilder text, float minutes)
    {
        if (minutes > 0f)
        {
            text.Append(' ').Append($"<color={TimeColour}>{Math.Round(minutes).ToString("0", CultureInfo.InvariantCulture)}m</color>");
        }
    }
}
