using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Navigation;
using BrudvikWhiteHilt.Navigation;
using BrudvikWhiteHilt.Progression;
using BrudvikWhiteHilt.Ranching;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Mastery;

/// <summary>
/// A skill milestone: something a player can do from a skill level on.
/// </summary>
public sealed class Perk
{
    private readonly Func<Skills.SkillType> skill;
    private readonly Func<bool> enabled;
    private readonly Func<string[]> words;
    private ConfigEntry<int> levelEntry;

    /// <summary>
    /// Creates a milestone.
    /// </summary>
    /// <param name="skill">The skill, read late since custom skills get their type in the plugin's Awake.</param>
    /// <param name="group">Config group, e.g. <c>Woodcutting</c>.</param>
    /// <param name="level">Default level from which it works.</param>
    /// <param name="key">Translation key without the <c>whitehilt_perk_</c> prefix.</param>
    /// <param name="name">English name.</param>
    /// <param name="description">English description; <c>{0}</c>, <c>{1}</c> are filled from <paramref name="words"/>.</param>
    /// <param name="enabled">Whether the server has it switched on.</param>
    /// <param name="words">The numbers in the description, read when shown.</param>
    public Perk(Func<Skills.SkillType> skill, string group, int level, string key, string name, string description, Func<bool> enabled,
        Func<string[]> words = null)
    {
        this.skill = skill;
        this.enabled = enabled;
        this.words = words;
        Group = group;
        DefaultLevel = level;
        Key = $"whitehilt_perk_{key}";
        Name = name;
        Description = description;
    }

    /// <summary>The skill.</summary>
    public Skills.SkillType Skill => skill();

    /// <summary>Config group, e.g. <c>Woodcutting</c>.</summary>
    public string Group { get; }

    /// <summary>Level from which it works by default.</summary>
    public int DefaultLevel { get; }

    /// <summary>Level from which it works, as configured.</summary>
    public int Level => levelEntry != null ? levelEntry.Value : DefaultLevel;

    /// <summary>Translation key of the name; the description uses the key plus <c>_description</c>.</summary>
    public string Key { get; }

    /// <summary>English name.</summary>
    public string Name { get; }

    /// <summary>English description.</summary>
    public string Description { get; }

    /// <summary>Whether the server has it switched on.</summary>
    public bool Enabled => enabled();

    /// <summary>
    /// Binds the level config entry, e.g. <c>[Skills.Woodcutting] DominoFellingLevel</c>.
    /// </summary>
    /// <param name="section">Parent section.</param>
    public void BindLevel(string section)
    {
        string configName = new(Name.Where(c => char.IsLetter(c)).ToArray());
        levelEntry = WhiteHiltConfig.BindAdminOnly($"{section}.{Group}", configName + "Level", DefaultLevel,
            $"Skill level from which {Name} works.", new AcceptableValueRange<int>(0, 100));
    }

    /// <summary>
    /// The description in the player's language, with the configured numbers.
    /// </summary>
    /// <returns>The text.</returns>
    public string LocalizedDescription()
    {
        string text = Localization.instance.Localize(Translations.Token(Key + "_description"));
        return words == null ? text : string.Format(text, words());
    }

    /// <summary>
    /// True if the milestone is on and reached at a level.
    /// </summary>
    /// <param name="level">Skill level.</param>
    /// <returns>True if it works.</returns>
    public bool ReachedAt(float level)
    {
        return Enabled && level >= Level;
    }

    /// <summary>
    /// True if the milestone is on and the player has reached it.
    /// </summary>
    /// <param name="player">The player, local or remote.</param>
    /// <returns>True if it works for the player.</returns>
    public bool Has(Player player)
    {
        return player != null && ReachedAt(SkillLevels.Get(player, Skill));
    }
}

/// <summary>
/// Every skill milestone and the numbers that grow with the skills.
/// </summary>
public static class Perks
{
    /// <summary>Wild picks can get a star.</summary>
    public static readonly Perk KeenEye = Foraging(25, "keen_eye", "Keen Eye", "Wild berries, mushrooms and herbs you pick can get a star.");

    /// <summary>Best picking times shown and favoured.</summary>
    public static readonly Perk SeasonSense = Foraging(50, "season_sense", "Season Sense",
        "Shows when a plant is at its best: mushrooms at dawn and in the rain, berries and herbs in the afternoon sun. Stars are {0} times as likely then. Up to two stars.",
        () => new[] { Number(MasterySettings.BestTimeStarMultiplier.Value) });

    /// <summary>Picking one plant picks the same plants nearby.</summary>
    public static readonly Perk SweepPicking = Foraging(75, "sweep", "Sweep Picking", "Picking a plant also picks the same plants within {0} m.",
        () => new[] { Number(MasterySettings.SweepRange.Value) });

    /// <summary>Wild picks up to three stars.</summary>
    public static readonly Perk ForagersBounty = Foraging(100, "bounty", "Forager's Bounty", "Up to three stars.");

    /// <summary>Dishes can get stars.</summary>
    public static readonly Perk FineCooking = Cooking(25, "fine_cooking", "Fine Cooking",
        "Dishes you cook can get a star, and starred ingredients give starred dishes.");

    /// <summary>Food near the cook does not burn.</summary>
    public static readonly Perk WatchfulCook = Cooking(50, "watchful", "Watchful Cook", "Food on cooking stations within {0} m of you does not burn. Up to two stars.",
        () => new[] { Number(MasterySettings.CookRange.Value) });

    /// <summary>A second chance at a star.</summary>
    public static readonly Perk ChefsTouch = Cooking(75, "chefs_touch", "Chef's Touch", "One more chance at a star on every dish.");

    /// <summary>Dishes up to three stars.</summary>
    public static readonly Perk MasterChef = Cooking(100, "master_chef", "Master Chef", "Up to three stars.");

    /// <summary>Trees fall the way the player faces.</summary>
    public static readonly Perk AimedFall = Woodcutting(25, "aimed_fall", "Aimed Fall", "Trees you fell come down the way you face.");

    /// <summary>Felled trees are replanted.</summary>
    public static readonly Perk Replanting = Woodcutting(50, "replant", "Replanting", "A tree you fell is replanted next to its stump, if you carry its seed.");

    /// <summary>Falling trees fell the trees they hit.</summary>
    public static readonly Perk DominoFelling = Woodcutting(75, "domino", "Domino Felling", "A tree you fell knocks down the trees it falls on.");

    /// <summary>Some trees give double wood.</summary>
    public static readonly Perk OldGrowth = Woodcutting(100, "old_growth", "Old Growth", "{0}% of the trees you fell are old growth and give twice the wood.",
        () => new[] { Percent(MasterySettings.OldGrowthChance.Value) });

    /// <summary>Some strikes do double damage.</summary>
    public static readonly Perk CleanStrike = Mining(25, "clean_strike", "Clean Strike", "{0}% of your strikes hit the seam and do {1} times the damage.",
        () => new[] { Percent(MasterySettings.CleanStrikeChance.Value), Number(MasterySettings.CleanStrikeMultiplier.Value) });

    /// <summary>Ore deposits show on the map.</summary>
    public static readonly Perk OreEcho = Mining(50, "ore_echo", "Ore Echo", "Striking rock makes ore deposits within {0} m ring out on the map.",
        () => new[] { Number(MasterySettings.OreEchoRange.Value) });

    /// <summary>Some deposits give double ore.</summary>
    public static readonly Perk RichVeins = Mining(75, "rich_veins", "Rich Veins", "{0}% of deposits are rich and give twice the ore.",
        () => new[] { Percent(MasterySettings.RichVeinChance.Value) });

    /// <summary>Wider echo, more finds.</summary>
    public static readonly Perk Prospector = Mining(100, "prospector", "Prospector", "The ore echo reaches {0} m and finds are {1} times as common.",
        () => new[] { Number(MasterySettings.ProspectorRange.Value), Number(MasterySettings.ProspectorFindMultiplier.Value) });

    /// <summary>A strong hit after a perfect parry.</summary>
    public static readonly Perk Riposte = Blocking(25, "riposte", "Riposte", "After a perfect parry, your next hit within {0} s does {1}% more damage.",
        () => new[] { Number(MasterySettings.RiposteSeconds.Value), Percent(MasterySettings.RiposteBonus.Value) });

    /// <summary>Less damage around a blocking tower shield.</summary>
    public static readonly Perk ShieldWall = Blocking(50, "shield_wall", "Shield Wall",
        "While you block with a tower shield, you and the players within {0} m take {1}% less damage.",
        () => new[] { Number(MasterySettings.ShieldWallRange.Value), Percent(MasterySettings.ShieldWallReduction.Value) });

    /// <summary>Survive a deadly blow.</summary>
    public static readonly Perk LastStand = Blocking(75, "last_stand", "Last Stand",
        "Once every {0} minutes a deadly blow leaves you with 1 health, and nothing can hurt you for {1} seconds.",
        () => new[] { Number(MasterySettings.LastStandCooldownMinutes.Value), Number(MasterySettings.LastStandGraceSeconds.Value) });

    /// <summary>More block power.</summary>
    public static readonly Perk IronGuard = Blocking(100, "iron_guard", "Iron Guard", "{0}% more block power.",
        () => new[] { Percent(MasterySettings.IronGuardBonus.Value) });

    /// <summary>Crops and seeds can get stars.</summary>
    public static readonly Perk StarredCrops = Farming(25, "starred_crops", "Starred Crops",
        "Crops and seeds you harvest can get a star. Seeds with stars grow crops with more stars.");

    /// <summary>Faster growth.</summary>
    public static readonly Perk GreenThumb = Farming(50, "green_thumb", "Green Thumb", "What you plant grows {0}% faster. Up to two stars.",
        () => new[] { Percent(MasterySettings.GreenThumbBonus.Value) });

    /// <summary>Some crops grow giant.</summary>
    public static readonly Perk GiantCrops = Farming(75, "giant_crops", "Giant Crops",
        "{0}% of the crops you plant grow into giants that give {1} times as much.",
        () => new[] { Percent(MasterySettings.GiantCropChance.Value), MasterySettings.GiantCropYield.Value.ToString() });

    /// <summary>Crops up to three stars.</summary>
    public static readonly Perk MasterFarmer = Farming(100, "master_farmer", "Master Farmer", "Up to three stars.");

    /// <summary>The hook brings up more.</summary>
    public static readonly Perk Snags = Fishing(25, "snags", "Snags", "Now and then the hook brings up something along with the fish.");

    /// <summary>Line tension builds slower.</summary>
    public static readonly Perk SteadyHands = Fishing(50, "steady_hands", "Steady Hands", "The line strains {0}% slower while a fish runs.",
        () => new[] { Percent(MasterySettings.SteadyHandsReduction.Value) });

    /// <summary>Rare legendary fish.</summary>
    public static readonly Perk LegendaryFish = Fishing(75, "legendary", "Legendary Fish", "Now and then a catch is legendary: the biggest of its kind.");

    /// <summary>Some catches bring two fish.</summary>
    public static readonly Perk DoubleCatch = Fishing(100, "double_catch", "Double Catch", "{0}% of your catches bring two fish.",
        () => new[] { Percent(MasterySettings.DoubleCatchChance.Value) });

    /// <summary>Animals near the player can have twins.</summary>
    public static readonly Perk Twins = new(() => HusbandrySkill.Type, "Husbandry", 50, "twins", "Twins", "Animals near you sometimes have twins.",
        () => MasterySettings.Husbandry.Value);

    /// <summary>Young can be one star stronger.</summary>
    public static readonly Perk StrongYoung = new(() => HusbandrySkill.Type, "Husbandry", 75, "strong_young", "Strong Young",
        "Young born near you are sometimes one star stronger than their parents.", () => MasterySettings.Husbandry.Value);

    /// <summary>Look out over the land and sea.</summary>
    public static readonly Perk Lookout = new(() => ExplorationSkill.Type, "Exploration", 25, "lookout", "Lookout",
        "Press the Lookout key (O): the map opens up around you, wider with skill, and sea monsters and ships within reach show on it. Once every {0} minutes.",
        () => MasterySettings.Lookout.Value, () => new[] { Number(MasterySettings.LookoutCooldownMinutes.Value) });

    /// <summary>The Pathfinder's Ruby Amulet can be made.</summary>
    public static readonly Perk RubyPathfinder = new(() => ExplorationSkill.Type, "Exploration", 50, "ruby_pathfinder", "Ruby Pathfinder",
        "Set a ruby in the Pathfinder's Amulet at the Cartographer's Desk. Wearing it, Shift + click the large map to set a target, and an arrow leads you there.",
        () => WhiteHiltConfig.IsEnabled(RubyPathfinderAmulet.RubyPrefabName));

    private static readonly List<Perk> all = new()
    {
        KeenEye, SeasonSense, SweepPicking, ForagersBounty,
        FineCooking, WatchfulCook, ChefsTouch, MasterChef,
        AimedFall, Replanting, DominoFelling, OldGrowth,
        CleanStrike, OreEcho, RichVeins, Prospector,
        Riposte, ShieldWall, LastStand, IronGuard,
        StarredCrops, GreenThumb, GiantCrops, MasterFarmer,
        Snags, SteadyHands, LegendaryFish, DoubleCatch,
        Twins, StrongYoung, Lookout, RubyPathfinder
    };

    /// <summary>
    /// The milestones of a skill, lowest first.
    /// </summary>
    /// <param name="skill">The skill.</param>
    /// <returns>Its milestones.</returns>
    public static IEnumerable<Perk> For(Skills.SkillType skill)
    {
        return all.Where(perk => perk.Skill == skill).OrderBy(perk => perk.Level);
    }

    /// <summary>
    /// Registers the English names and descriptions. Call from the plugin's Awake.
    /// </summary>
    public static void RegisterTranslations()
    {
        foreach (Perk perk in all)
        {
            Translations.AddEnglishNameAndDescription(perk.Key, perk.Name, perk.Description);
        }
    }

    /// <summary>
    /// Binds the level of every milestone. Called from <see cref="MasterySettings.Initialize"/>.
    /// </summary>
    /// <param name="section">Parent section.</param>
    public static void BindLevels(string section)
    {
        foreach (Perk perk in all)
        {
            perk.BindLevel(section);
        }
    }

    /// <summary>Chance of one more item from a wild pick.</summary>
    /// <param name="level">Foraging level.</param>
    /// <returns>The chance, 0 to 1.</returns>
    public static float ForagingExtraYield(float level) => MasterySettings.Foraging.Value ? MasterySettings.ExtraYieldChance.Value * Factor(level) : 0f;

    /// <summary>How much faster cooking stations work with a cook nearby.</summary>
    /// <param name="level">Cooking level.</param>
    /// <returns>The speed multiplier.</returns>
    public static float KitchenSpeed(float level) => MasterySettings.Cooking.Value ? 1f + MasterySettings.KitchenSpeedBonus.Value * Factor(level) : 1f;

    /// <summary>Chance of one more piece of wood from a log.</summary>
    /// <param name="level">Woodcutting level.</param>
    /// <returns>The chance, 0 to 1.</returns>
    public static float CleanSplit(float level) => MasterySettings.Woodcutting.Value ? MasterySettings.CleanSplitChance.Value * Factor(level) : 0f;

    /// <summary>Chance of a bird nest from a felled tree.</summary>
    /// <param name="level">Woodcutting level.</param>
    /// <returns>The chance, 0 to 1.</returns>
    public static float BirdNest(float level) => MasterySettings.Woodcutting.Value
        ? MasterySettings.BirdNestBaseChance.Value + MasterySettings.BirdNestSkillChance.Value * Factor(level)
        : 0f;

    /// <summary>Chance of one more ore per ore from a broken rock.</summary>
    /// <param name="level">Pickaxes level.</param>
    /// <returns>The chance, 0 to 1.</returns>
    public static float ExtraOre(float level) => MasterySettings.Mining.Value ? MasterySettings.ExtraOreChance.Value * Factor(level) : 0f;

    /// <summary>Chance of a find from a broken piece of rock.</summary>
    /// <param name="level">Pickaxes level.</param>
    /// <returns>The chance, 0 to 1.</returns>
    public static float MiningFind(float level)
    {
        if (!MasterySettings.Mining.Value)
        {
            return 0f;
        }

        float chance = MasterySettings.FindBaseChance.Value + MasterySettings.FindSkillChance.Value * Factor(level);
        return Prospector.ReachedAt(level) ? chance * MasterySettings.ProspectorFindMultiplier.Value : chance;
    }

    /// <summary>Extra maximum health.</summary>
    /// <param name="level">Blocking level.</param>
    /// <returns>Health points.</returns>
    public static float ExtraHealth(float level) => MasterySettings.Blocking.Value ? Mathf.Floor(MasterySettings.ExtraHealth.Value * Factor(level)) : 0f;

    /// <summary>Share of damage taken away.</summary>
    /// <param name="level">Blocking level.</param>
    /// <returns>The share, 0 to 1.</returns>
    public static float DamageReduction(float level) => MasterySettings.Blocking.Value ? MasterySettings.DamageReduction.Value * Factor(level) : 0f;

    /// <summary>Share of block stamina saved.</summary>
    /// <param name="level">Blocking level.</param>
    /// <returns>The share, 0 to 1.</returns>
    public static float BlockStaminaSaving(float level) => MasterySettings.Blocking.Value ? MasterySettings.BlockStaminaSaving.Value * Factor(level) : 0f;

    /// <summary>Chance that a catch brings up something more.</summary>
    /// <param name="level">Fishing level.</param>
    /// <returns>The chance, 0 to 1.</returns>
    public static float SnagChance(float level) => Snags.ReachedAt(level)
        ? MasterySettings.SnagBaseChance.Value + MasterySettings.SnagSkillChance.Value * Factor(level)
        : 0f;

    /// <summary>Chance that a catch is legendary.</summary>
    /// <param name="level">Fishing level.</param>
    /// <returns>The chance, 0 to 1.</returns>
    public static float LegendaryChance(float level) => LegendaryFish.ReachedAt(level)
        ? MasterySettings.LegendaryBaseChance.Value + MasterySettings.LegendarySkillChance.Value * Factor(level)
        : 0f;

    /// <summary>Chance of twins.</summary>
    /// <param name="level">Animal Husbandry level.</param>
    /// <returns>The chance, 0 to 1.</returns>
    public static float TwinChance(float level) => Twins.ReachedAt(level) ? MasterySettings.TwinChance.Value * Factor(level) : 0f;

    /// <summary>Chance that a young animal gets one more star.</summary>
    /// <param name="level">Animal Husbandry level.</param>
    /// <returns>The chance, 0 to 1.</returns>
    public static float StrongYoungChance(float level) => StrongYoung.ReachedAt(level) ? MasterySettings.StrongYoungChance.Value * Factor(level) : 0f;

    /// <summary>Skill level as a factor from 0 to 1.</summary>
    /// <param name="level">Skill level.</param>
    /// <returns>The factor.</returns>
    public static float Factor(float level) => Mathf.Clamp01(level / 100f);

    /// <summary>A share as a whole percentage, e.g. 0.25 as "25".</summary>
    /// <param name="share">The share.</param>
    /// <returns>The text.</returns>
    public static string Percent(float share) => Mathf.RoundToInt(share * 100f).ToString();

    /// <summary>
    /// Localizes a text and fills its <c>{0}</c>, <c>{1}</c> placeholders; vanilla only fills <c>$1</c>, <c>$2</c>.
    /// </summary>
    /// <param name="token">Text with <c>$</c> tokens.</param>
    /// <param name="words">The words to fill in.</param>
    /// <returns>The text.</returns>
    public static string Text(string token, params string[] words)
    {
        return string.Format(Localization.instance.Localize(token), words);
    }

    private static string Number(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static Perk Foraging(int level, string key, string name, string description, Func<string[]> words = null)
    {
        return new Perk(() => ForagingSkill.Type, "Foraging", level, key, name, description, () => MasterySettings.Foraging.Value, words);
    }

    private static Perk Cooking(int level, string key, string name, string description, Func<string[]> words = null)
    {
        return new Perk(() => Skills.SkillType.Cooking, "Cooking", level, key, name, description, () => MasterySettings.Cooking.Value, words);
    }

    private static Perk Woodcutting(int level, string key, string name, string description, Func<string[]> words = null)
    {
        return new Perk(() => Skills.SkillType.WoodCutting, "Woodcutting", level, key, name, description, () => MasterySettings.Woodcutting.Value, words);
    }

    private static Perk Mining(int level, string key, string name, string description, Func<string[]> words = null)
    {
        return new Perk(() => Skills.SkillType.Pickaxes, "Mining", level, key, name, description, () => MasterySettings.Mining.Value, words);
    }

    private static Perk Blocking(int level, string key, string name, string description, Func<string[]> words = null)
    {
        return new Perk(() => Skills.SkillType.Blocking, "Blocking", level, key, name, description, () => MasterySettings.Blocking.Value, words);
    }

    private static Perk Farming(int level, string key, string name, string description, Func<string[]> words = null)
    {
        return new Perk(() => Skills.SkillType.Farming, "Farming", level, key, name, description, () => MasterySettings.Farming.Value, words);
    }

    private static Perk Fishing(int level, string key, string name, string description, Func<string[]> words = null)
    {
        return new Perk(() => Skills.SkillType.Fishing, "Fishing", level, key, name, description, () => MasterySettings.Fishing.Value, words);
    }
}
