using UnityEngine;

namespace BrudvikWhiteHilt.Crafting.Refinement;

/// <summary>
/// How the roll at the Forge of Potential is made.
/// </summary>
public enum RefinementRoll
{
    /// <summary>As the game: the first try of each item and level is fixed by the character's name.</summary>
    Vanilla,

    /// <summary>A fresh, true roll every try.</summary>
    Fair,

    /// <summary>A fresh, true roll every try, and every failed try on the same item and level adds to the next.</summary>
    FairPity
}

/// <summary>
/// What a failed refinement does to the item. The idol is spent in every case.
/// </summary>
public enum RefinementFailure
{
    /// <summary>As the game: the item may break by the idol's break chance, else it loses a level.</summary>
    Vanilla,

    /// <summary>The item keeps its level; only the idol is lost.</summary>
    LoseIdol,

    /// <summary>The item loses a level, and never breaks.</summary>
    Downgrade,

    /// <summary>The item breaks.</summary>
    Break
}

/// <summary>
/// What came of a refinement.
/// </summary>
public enum RefinementOutcome
{
    /// <summary>The item went up a level.</summary>
    Success,

    /// <summary>The item kept its level.</summary>
    KeepLevel,

    /// <summary>The item went down a level.</summary>
    Downgrade,

    /// <summary>The item broke.</summary>
    Break
}

/// <summary>
/// A player's skills that count at the forge, each from 0 (level 0) to 1 (level 100).
/// </summary>
public readonly struct RefinementSkills
{
    /// <summary>
    /// Creates the skills.
    /// </summary>
    /// <param name="crafting">Crafting.</param>
    /// <param name="weapon">The skill of the item's own kind, such as Swords or Blocking.</param>
    /// <param name="overall">The mean of all the player's skills.</param>
    public RefinementSkills(float crafting, float weapon, float overall)
    {
        Crafting = Mathf.Clamp01(crafting);
        Weapon = Mathf.Clamp01(weapon);
        Overall = Mathf.Clamp01(overall);
    }

    /// <summary>Crafting.</summary>
    public float Crafting { get; }

    /// <summary>The skill of the item's own kind.</summary>
    public float Weapon { get; }

    /// <summary>The mean of all the player's skills.</summary>
    public float Overall { get; }
}

/// <summary>
/// The server's numbers for the forge: what skills, failed tries and extra idols add to the chance of success.
/// </summary>
public readonly struct RefinementBonuses
{
    /// <summary>
    /// Creates the numbers.
    /// </summary>
    /// <param name="crafting">Chance added at Crafting 100.</param>
    /// <param name="weapon">Chance added at the item's own skill 100.</param>
    /// <param name="overall">Chance added with every skill at 100.</param>
    /// <param name="perFailure">Chance added by each failed try on the same item and level.</param>
    /// <param name="perExtraIdol">Chance added by each extra idol offered.</param>
    /// <param name="maxChance">Most the bonuses raise the chance to.</param>
    /// <param name="breakReduction">Share of the break chance the skills take away when they are all at 100.</param>
    public RefinementBonuses(float crafting, float weapon, float overall, float perFailure, float perExtraIdol, float maxChance, float breakReduction)
    {
        Crafting = crafting;
        Weapon = weapon;
        Overall = overall;
        PerFailure = perFailure;
        PerExtraIdol = perExtraIdol;
        MaxChance = maxChance;
        BreakReduction = breakReduction;
    }

    /// <summary>Chance added at Crafting 100.</summary>
    public float Crafting { get; }

    /// <summary>Chance added at the item's own skill 100.</summary>
    public float Weapon { get; }

    /// <summary>Chance added with every skill at 100.</summary>
    public float Overall { get; }

    /// <summary>Chance added by each failed try on the same item and level.</summary>
    public float PerFailure { get; }

    /// <summary>Chance added by each extra idol offered.</summary>
    public float PerExtraIdol { get; }

    /// <summary>Most the bonuses raise the chance to.</summary>
    public float MaxChance { get; }

    /// <summary>Share of the break chance the skills take away when they are all at 100.</summary>
    public float BreakReduction { get; }
}

/// <summary>
/// The odds at the Forge of Potential. Pure numbers, so the rules can be tested without the game.
/// </summary>
public static class RefinementOdds
{
    /// <summary>
    /// The chance of success: the idol's own, raised by the skills, the failed tries and the extra idols, up to the
    /// most the bonuses may give. An idol that already gives more keeps its own chance.
    /// </summary>
    /// <param name="idolChance">The idol's chance of success.</param>
    /// <param name="skills">The player's skills.</param>
    /// <param name="failures">Failed tries on the same item and level since the last success.</param>
    /// <param name="extraIdols">Extra idols offered.</param>
    /// <param name="bonuses">The server's numbers.</param>
    /// <returns>The chance, from 0 to 1.</returns>
    public static float SuccessChance(float idolChance, RefinementSkills skills, int failures, int extraIdols, RefinementBonuses bonuses)
    {
        float raised = idolChance + skills.Crafting * bonuses.Crafting + skills.Weapon * bonuses.Weapon + skills.Overall * bonuses.Overall
            + Mathf.Max(0, failures) * bonuses.PerFailure + Mathf.Max(0, extraIdols) * bonuses.PerExtraIdol;
        return Mathf.Clamp01(Mathf.Max(idolChance, Mathf.Min(raised, bonuses.MaxChance)));
    }

    /// <summary>
    /// The chance that a failed try breaks the item, as the game rolls it: the idol's own, lowered by the player's
    /// experience.
    /// </summary>
    /// <param name="idolBreakChance">The idol's break chance.</param>
    /// <param name="skills">The player's skills.</param>
    /// <param name="bonuses">The server's numbers.</param>
    /// <returns>The chance, from 0 to 1.</returns>
    public static float BreakChance(float idolBreakChance, RefinementSkills skills, RefinementBonuses bonuses)
    {
        return Mathf.Clamp01(idolBreakChance * (1f - Mathf.Clamp01(bonuses.BreakReduction) * Experience(skills, bonuses)));
    }

    /// <summary>
    /// How experienced the player is at the forge, from 0 to 1: the skills' mean, weighed as they raise the chance.
    /// </summary>
    /// <param name="skills">The player's skills.</param>
    /// <param name="bonuses">The server's numbers.</param>
    /// <returns>The experience.</returns>
    public static float Experience(RefinementSkills skills, RefinementBonuses bonuses)
    {
        float crafting = Mathf.Max(0f, bonuses.Crafting);
        float weapon = Mathf.Max(0f, bonuses.Weapon);
        float overall = Mathf.Max(0f, bonuses.Overall);
        float weights = crafting + weapon + overall;
        return weights > 0f ? (skills.Crafting * crafting + skills.Weapon * weapon + skills.Overall * overall) / weights : 0f;
    }

    /// <summary>
    /// What a roll comes to. Low rolls succeed, as in the game; a failure follows the server's rule, and the game's own
    /// rule breaks the item on the highest rolls.
    /// </summary>
    /// <param name="roll">The roll, from 0 to 1.</param>
    /// <param name="successChance">The chance of success.</param>
    /// <param name="breakChance">The chance of breaking, for the game's own rule.</param>
    /// <param name="failure">The server's rule for a failure.</param>
    /// <returns>The outcome.</returns>
    public static RefinementOutcome Decide(double roll, float successChance, float breakChance, RefinementFailure failure)
    {
        if (roll < successChance)
        {
            return RefinementOutcome.Success;
        }

        return failure switch
        {
            RefinementFailure.LoseIdol => RefinementOutcome.KeepLevel,
            RefinementFailure.Downgrade => RefinementOutcome.Downgrade,
            RefinementFailure.Break => RefinementOutcome.Break,
            _ => roll >= 1.0 - breakChance ? RefinementOutcome.Break : RefinementOutcome.Downgrade
        };
    }

    /// <summary>
    /// The coins a trader asks for an idol of a biome's tier.
    /// </summary>
    /// <param name="basePrice">The price of the first tier.</param>
    /// <param name="biomeWorth">What a plain material of the biome is worth against the Meadows'.</param>
    /// <returns>Coins, at least 1.</returns>
    public static int IdolPrice(int basePrice, float biomeWorth)
    {
        return Mathf.Max(1, Mathf.RoundToInt(basePrice * Mathf.Max(1f, biomeWorth)));
    }
}
