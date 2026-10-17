using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Crafting.Refinement;

/// <summary>
/// Config for the Forge of Potential, section "Refinement". Server-synced. Every number is read when used, so changes
/// apply at once, except the exchange's station.
/// </summary>
public static class RefinementSettings
{
    private const string Section = "Refinement";

    /// <summary>Whether White Hilt's rules apply at the forge; off, the game's own do.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>How the roll is made.</summary>
    public static ConfigEntry<RefinementRoll> RollMode { get; private set; }

    /// <summary>Chance added by each failed try on the same item and level, with <see cref="RefinementRoll.FairPity"/>.</summary>
    public static ConfigEntry<float> PityPerFailure { get; private set; }

    /// <summary>What a failed try does to the item.</summary>
    public static ConfigEntry<RefinementFailure> FailureMode { get; private set; }

    /// <summary>Chance added at Crafting 100.</summary>
    public static ConfigEntry<float> CraftingBonus { get; private set; }

    /// <summary>Chance added at the item's own skill 100.</summary>
    public static ConfigEntry<float> ItemSkillBonus { get; private set; }

    /// <summary>Chance added with every skill at 100.</summary>
    public static ConfigEntry<float> OverallSkillBonus { get; private set; }

    /// <summary>Most the bonuses raise the chance of success to.</summary>
    public static ConfigEntry<float> MaxSuccessChance { get; private set; }

    /// <summary>Share of the break chance the skills take away when they are all at 100.</summary>
    public static ConfigEntry<float> BreakReduction { get; private set; }

    /// <summary>Most extra idols one try may take.</summary>
    public static ConfigEntry<int> MaxExtraIdols { get; private set; }

    /// <summary>Chance added by each extra idol.</summary>
    public static ConfigEntry<float> ExtraIdolBonus { get; private set; }

    /// <summary>Whether the trading posts sell the idols of their biome.</summary>
    public static ConfigEntry<bool> IdolsAtTraders { get; private set; }

    /// <summary>Coins the first tier's idol costs; each biome's costs as much more as its materials are worth.</summary>
    public static ConfigEntry<int> IdolPrice { get; private set; }

    /// <summary>Whether idols can be exchanged for idols of the next tier.</summary>
    public static ConfigEntry<bool> Exchange { get; private set; }

    /// <summary>Idols of one tier that make one of the next.</summary>
    public static ConfigEntry<int> ExchangeCost { get; private set; }

    /// <summary>Whether a Battle idol can be exchanged for a Protection idol of the same tier, and back.</summary>
    public static ConfigEntry<bool> ExchangeKind { get; private set; }

    /// <summary>The crafting station the exchange is made at.</summary>
    public static ConfigEntry<string> ExchangeStation { get; private set; }

    /// <summary>
    /// The server's numbers, as the odds take them.
    /// </summary>
    public static RefinementBonuses Bonuses => new(CraftingBonus.Value, ItemSkillBonus.Value, OverallSkillBonus.Value,
        RollMode.Value == RefinementRoll.FairPity ? PityPerFailure.Value : 0f, ExtraIdolBonus.Value, MaxSuccessChance.Value, BreakReduction.Value);

    /// <summary>
    /// Binds the config entries and registers the texts. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        AcceptableValueRange<float> chance = new(0f, 1f);
        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true,
            "Use White Hilt's rules at the Forge of Potential: the roll, the skills, what a failure does and the extra idols below. Off: the game's own.");
        RollMode = WhiteHiltConfig.BindAdminOnly(Section, "RollMode", RefinementRoll.FairPity,
            "How the roll is made. Vanilla: as the game, where the first try of each item and level is fixed by the character's name. " +
            "Fair: a fresh, true roll every try. FairPity: as Fair, and every failed try on the same item and level adds PityPerFailure to the next, until it succeeds.");
        PityPerFailure = WhiteHiltConfig.BindAdminOnly(Section, "PityPerFailure", 0.1f,
            "Chance of success added by each failed try on the same item and level, with RollMode FairPity (0.1 = +10 %).", chance);
        FailureMode = WhiteHiltConfig.BindAdminOnly(Section, "FailureMode", RefinementFailure.LoseIdol,
            "What a failed try does to the item; the idol is spent in every case. Vanilla: it may break by the idol's break chance, else it loses a level. " +
            "LoseIdol: it keeps its level. Downgrade: it loses a level and never breaks. Break: it breaks, giving back the share of its materials the idol gives back.");
        CraftingBonus = WhiteHiltConfig.BindAdminOnly(Section, "CraftingBonus", 0.15f,
            "Chance of success added at Crafting 100, less at lower levels (0.15 = +15 %).", chance);
        ItemSkillBonus = WhiteHiltConfig.BindAdminOnly(Section, "ItemSkillBonus", 0.1f,
            "Chance of success added at 100 in the item's own skill, such as Swords for a sword or Blocking for a shield. " +
            "Armour, which has no skill of its own, counts the mean of all skills instead.", chance);
        OverallSkillBonus = WhiteHiltConfig.BindAdminOnly(Section, "OverallSkillBonus", 0.1f,
            "Chance of success added with every skill at 100, by the mean of all the player's skills, so a seasoned player does better.", chance);
        MaxSuccessChance = WhiteHiltConfig.BindAdminOnly(Section, "MaxSuccessChance", 0.95f,
            "Most the skills, failed tries and extra idols raise the chance of success to. An idol that gives more on its own keeps its chance.", chance);
        BreakReduction = WhiteHiltConfig.BindAdminOnly(Section, "BreakReduction", 0.5f,
            "Share of the idol's break chance the skills take away when they are all at 100, with FailureMode Vanilla (0.5 = half).", chance);
        MaxExtraIdols = WhiteHiltConfig.BindAdminOnly(Section, "MaxExtraIdols", 3,
            "Most extra idols of the same kind one try may take, when Shift is held as the refinement is begun. 0 turns it off.",
            new AcceptableValueRange<int>(0, 10));
        ExtraIdolBonus = WhiteHiltConfig.BindAdminOnly(Section, "ExtraIdolBonus", 0.1f,
            "Chance of success added by each extra idol (0.1 = +10 %).", chance);
        IdolsAtTraders = WhiteHiltConfig.BindAdminOnly(Section, "IdolsAtTraders", true,
            "The trading posts sell the idols of their biome's tier (Wooden in the Meadows, Bronze in the Black Forest and so on), once the world has reached the biome.");
        IdolPrice = WhiteHiltConfig.BindAdminOnly(Section, "IdolPrice", 200,
            "Coins a Wooden idol costs at the Meadows' post. Each biome's idols cost as much more as its materials are worth (up to ten times in the Deep North), less the player's Trading discount.",
            new AcceptableValueRange<int>(1, 100000));
        Exchange = WhiteHiltConfig.BindAdminOnly(Section, "Exchange", true,
            "Idols of one tier can be exchanged for one of the next tier, at ExchangeStation.");
        ExchangeCost = WhiteHiltConfig.BindAdminOnly(Section, "ExchangeCost", 3,
            "Idols of one tier that make one of the next.", new AcceptableValueRange<int>(1, 20));
        ExchangeKind = WhiteHiltConfig.BindAdminOnly(Section, "ExchangeKind", true,
            "A Battle idol can be exchanged for a Protection idol of the same tier, and back, one for one.");
        ExchangeStation = WhiteHiltConfig.BindAdminOnly(Section, "ExchangeStation", "forge",
            "Prefab name of the crafting station the idols are exchanged at. The Forge of Potential itself only refines. Needs a restart.");

        Translations.AddEnglish("whitehilt_cfgsec_refinement", "Forge of Potential");
        Translations.AddEnglish("whitehilt_cfgenum_refinementroll_fairpity", "Fair with pity");
        Translations.AddEnglish("whitehilt_cfgenum_refinementfailure_loseidol", "Lose only the idol");
        Translations.AddEnglish("msg_whitehilt_refine_kept", "Refining $1 failed; it keeps level $2. The idol is spent.");
        Translations.AddEnglish("msg_whitehilt_refine_downgraded", "Refining $1 failed, downgraded to level $2");
        Translations.AddEnglish("msg_whitehilt_refine_extra", "$1 extra idols offered");
        Translations.AddEnglish("whitehilt_refine_extra_hint", "Hold Shift as you begin to offer up to $1 more idols, +$2 % each.");
    }
}
