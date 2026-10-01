using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Mastery;

/// <summary>
/// Config for the skill milestones, stars and skill loss, section "Skills", with the numbers of each skill in
/// "Skills.&lt;Skill&gt;". Server-synced, except the keys. Every number is read when used, so changes apply at once.
/// </summary>
public static class MasterySettings
{
    private const string Section = "Skills";
    private const string ForagingSection = Section + ".Foraging";
    private const string CookingSection = Section + ".Cooking";
    private const string WoodcuttingSection = Section + ".Woodcutting";
    private const string MiningSection = Section + ".Mining";
    private const string BlockingSection = Section + ".Blocking";
    private const string FarmingSection = Section + ".Farming";
    private const string FishingSection = Section + ".Fishing";
    private const string HusbandrySection = Section + ".Husbandry";
    private const string ExplorationSection = Section + ".Exploration";

    /// <summary>Share of the vanilla skill loss on death: 0 keeps every skill, 1 is vanilla, 2 doubles the loss.</summary>
    public static ConfigEntry<float> DeathLossMultiplier { get; private set; }

    /// <summary>Whether food, wild picks and crops can have stars.</summary>
    public static ConfigEntry<bool> Stars { get; private set; }

    /// <summary>Extra health, stamina, eitr and time per star of a dish.</summary>
    public static ConfigEntry<float> StarFoodBonus { get; private set; }

    /// <summary>Chance of the first star at skill level 100; it grows with the level.</summary>
    public static ConfigEntry<float> FirstStarChance { get; private set; }

    /// <summary>Chance of a second star once the first is rolled.</summary>
    public static ConfigEntry<float> SecondStarChance { get; private set; }

    /// <summary>Chance of a third star once the second is rolled.</summary>
    public static ConfigEntry<float> ThirdStarChance { get; private set; }

    /// <summary>Whether the Foraging skill and its milestones are on.</summary>
    public static ConfigEntry<bool> Foraging { get; private set; }

    /// <summary>Chance of one more item from a wild pick at Foraging 100.</summary>
    public static ConfigEntry<float> ExtraYieldChance { get; private set; }

    /// <summary>Multiplier on the first star chance at a plant's best time (Season Sense).</summary>
    public static ConfigEntry<float> BestTimeStarMultiplier { get; private set; }

    /// <summary>How far, in metres, Sweep Picking reaches.</summary>
    public static ConfigEntry<float> SweepRange { get; private set; }

    /// <summary>Whether the Cooking milestones are on.</summary>
    public static ConfigEntry<bool> Cooking { get; private set; }

    /// <summary>How much faster cooking stations work at Cooking 100.</summary>
    public static ConfigEntry<float> KitchenSpeedBonus { get; private set; }

    /// <summary>How near, in metres, a cook must be to speed up a station or keep its food from burning.</summary>
    public static ConfigEntry<float> CookRange { get; private set; }

    /// <summary>Whether the Woodcutting milestones are on.</summary>
    public static ConfigEntry<bool> Woodcutting { get; private set; }

    /// <summary>Chance of one more piece of wood from a log at Woodcutting 100.</summary>
    public static ConfigEntry<float> CleanSplitChance { get; private set; }

    /// <summary>Chance of a bird's nest from a felled tree at Woodcutting 0.</summary>
    public static ConfigEntry<float> BirdNestBaseChance { get; private set; }

    /// <summary>Chance of a bird's nest added at Woodcutting 100.</summary>
    public static ConfigEntry<float> BirdNestSkillChance { get; private set; }

    /// <summary>Chance that a felled tree is old growth (Old Growth).</summary>
    public static ConfigEntry<float> OldGrowthChance { get; private set; }

    /// <summary>Whether the Pickaxes milestones are on.</summary>
    public static ConfigEntry<bool> Mining { get; private set; }

    /// <summary>Chance of one more ore per ore at Pickaxes 100.</summary>
    public static ConfigEntry<float> ExtraOreChance { get; private set; }

    /// <summary>Chance of a find from broken rock at Pickaxes 0.</summary>
    public static ConfigEntry<float> FindBaseChance { get; private set; }

    /// <summary>Chance of a find added at Pickaxes 100.</summary>
    public static ConfigEntry<float> FindSkillChance { get; private set; }

    /// <summary>Multiplier on the find chance with Prospector.</summary>
    public static ConfigEntry<float> ProspectorFindMultiplier { get; private set; }

    /// <summary>Chance that a strike is a Clean Strike.</summary>
    public static ConfigEntry<float> CleanStrikeChance { get; private set; }

    /// <summary>Damage multiplier of a Clean Strike.</summary>
    public static ConfigEntry<float> CleanStrikeMultiplier { get; private set; }

    /// <summary>Chance that a deposit is a rich vein (Rich Veins).</summary>
    public static ConfigEntry<float> RichVeinChance { get; private set; }

    /// <summary>Reach of the Ore Echo, in metres.</summary>
    public static ConfigEntry<float> OreEchoRange { get; private set; }

    /// <summary>Reach of the Ore Echo with Prospector, in metres.</summary>
    public static ConfigEntry<float> ProspectorRange { get; private set; }

    /// <summary>Seconds between two Ore Echoes.</summary>
    public static ConfigEntry<float> OreEchoCooldown { get; private set; }

    /// <summary>Whether the Blocking milestones are on.</summary>
    public static ConfigEntry<bool> Blocking { get; private set; }

    /// <summary>Extra maximum health at Blocking 100.</summary>
    public static ConfigEntry<float> ExtraHealth { get; private set; }

    /// <summary>Share of damage taken away at Blocking 100.</summary>
    public static ConfigEntry<float> DamageReduction { get; private set; }

    /// <summary>Share of block stamina saved at Blocking 100.</summary>
    public static ConfigEntry<float> BlockStaminaSaving { get; private set; }

    /// <summary>Extra damage of the hit after a perfect parry (Riposte).</summary>
    public static ConfigEntry<float> RiposteBonus { get; private set; }

    /// <summary>Seconds the Riposte waits for a hit.</summary>
    public static ConfigEntry<float> RiposteSeconds { get; private set; }

    /// <summary>Share of damage taken away by the Shield Wall.</summary>
    public static ConfigEntry<float> ShieldWallReduction { get; private set; }

    /// <summary>Reach of the Shield Wall, in metres.</summary>
    public static ConfigEntry<float> ShieldWallRange { get; private set; }

    /// <summary>Minutes between two Last Stands.</summary>
    public static ConfigEntry<float> LastStandCooldownMinutes { get; private set; }

    /// <summary>Seconds nothing can hurt after a Last Stand.</summary>
    public static ConfigEntry<float> LastStandGraceSeconds { get; private set; }

    /// <summary>Extra block power with Iron Guard.</summary>
    public static ConfigEntry<float> IronGuardBonus { get; private set; }

    /// <summary>Whether the Farming milestones are on.</summary>
    public static ConfigEntry<bool> Farming { get; private set; }

    /// <summary>Share of the grow time taken away by Green Thumb.</summary>
    public static ConfigEntry<float> GreenThumbBonus { get; private set; }

    /// <summary>Chance that a planted crop grows into a giant.</summary>
    public static ConfigEntry<float> GiantCropChance { get; private set; }

    /// <summary>How many times the normal harvest a giant crop gives.</summary>
    public static ConfigEntry<int> GiantCropYield { get; private set; }

    /// <summary>Added to every star chance per star of the seed.</summary>
    public static ConfigEntry<float> SeedStarBonus { get; private set; }

    /// <summary>Whether the Fishing milestones and the fight on the line are on.</summary>
    public static ConfigEntry<bool> Fishing { get; private set; }

    /// <summary>Share of the line's strength used per second while reeling in a running fish at Fishing 0.</summary>
    public static ConfigEntry<float> LineStrain { get; private set; }

    /// <summary>Share of the line strain taken away by Steady Hands.</summary>
    public static ConfigEntry<float> SteadyHandsReduction { get; private set; }

    /// <summary>Chance of a snag at Fishing 0, once Snags is reached.</summary>
    public static ConfigEntry<float> SnagBaseChance { get; private set; }

    /// <summary>Chance of a snag added at Fishing 100.</summary>
    public static ConfigEntry<float> SnagSkillChance { get; private set; }

    /// <summary>Chance of a legendary fish at Fishing 0, once Legendary Fish is reached.</summary>
    public static ConfigEntry<float> LegendaryBaseChance { get; private set; }

    /// <summary>Chance of a legendary fish added at Fishing 100.</summary>
    public static ConfigEntry<float> LegendarySkillChance { get; private set; }

    /// <summary>Chance that a catch brings two fish (Double Catch).</summary>
    public static ConfigEntry<float> DoubleCatchChance { get; private set; }

    /// <summary>Whether the Animal Husbandry milestones are on.</summary>
    public static ConfigEntry<bool> Husbandry { get; private set; }

    /// <summary>Chance of twins at Animal Husbandry 100.</summary>
    public static ConfigEntry<float> TwinChance { get; private set; }

    /// <summary>Chance of a stronger young at Animal Husbandry 100.</summary>
    public static ConfigEntry<float> StrongYoungChance { get; private set; }

    /// <summary>Whether the Exploration Lookout is on.</summary>
    public static ConfigEntry<bool> Lookout { get; private set; }

    /// <summary>Minutes between two Lookouts.</summary>
    public static ConfigEntry<float> LookoutCooldownMinutes { get; private set; }

    /// <summary>Radius the Lookout reveals at Exploration 0, in metres; twice as far at 100.</summary>
    public static ConfigEntry<float> LookoutRadius { get; private set; }

    /// <summary>Key for the Lookout.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyLookout { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        DeathLossMultiplier = WhiteHiltConfig.BindAdminOnly(Section, "DeathLossMultiplier", 1f,
            "Share of the vanilla skill loss when you die: 0 keeps every skill, 1 is vanilla, 2 loses twice as much.",
            new AcceptableValueRange<float>(0f, 2f));
        Stars = WhiteHiltConfig.BindAdminOnly(Section, "Stars", true,
            "Let food, wild picks, crops and seeds get stars from the Foraging, Farming and Cooking skills.");
        StarFoodBonus = WhiteHiltConfig.BindAdminOnly(Section, "StarFoodBonus", 0.1f,
            "Extra health, stamina, eitr and time per star when you eat something with stars.",
            new AcceptableValueRange<float>(0f, 0.5f));
        FirstStarChance = Chance(Section, "FirstStarChance", 0.6f,
            "Chance of the first star on a wild pick, crop or dish at skill level 100; it grows with the level from 0.");
        SecondStarChance = Chance(Section, "SecondStarChance", 0.35f, "Chance of a second star once the first is rolled.");
        ThirdStarChance = Chance(Section, "ThirdStarChance", 0.25f, "Chance of a third star once the second is rolled.");

        Foraging = Bind("Foraging", "the Foraging skill: stars on wild picks, best picking times, extra yield and sweep picking");
        ExtraYieldChance = Chance(ForagingSection, "ExtraYieldChance", 0.5f, "Chance of one more item from a wild pick at Foraging 100; it grows with the level from 0.");
        BestTimeStarMultiplier = Number(ForagingSection, "BestTimeStarMultiplier", 2f, "Season Sense: the first star chance is multiplied by this at a plant's best time.", 1f, 5f);
        SweepRange = Number(ForagingSection, "SweepRange", 4f, "Sweep Picking: how far, in metres, picking one plant also picks the same plants.", 1f, 20f);

        Cooking = Bind("Cooking", "the Cooking milestones: dishes with stars, faster kitchens and food that does not burn");
        KitchenSpeedBonus = Number(CookingSection, "KitchenSpeedBonus", 0.5f, "How much faster cooking stations near a cook work at Cooking 100 (0.5 = 50%); it grows with the level from 0.", 0f, 3f);
        CookRange = Number(CookingSection, "CookRange", 10f, "How near, in metres, a cook must be to speed up a cooking station and, with Watchful Cook, keep its food from burning.", 2f, 50f);

        Woodcutting = Bind("Woodcutting", "the Woodcutting milestones: aimed falls, replanting, domino felling, old growth, clean splits and bird nests");
        CleanSplitChance = Chance(WoodcuttingSection, "CleanSplitChance", 0.5f, "Chance of one more piece of wood from a log at Woodcutting 100; it grows with the level from 0.");
        BirdNestBaseChance = Chance(WoodcuttingSection, "BirdNestBaseChance", 0.02f, "Chance of a bird's nest from a felled tree at Woodcutting 0.");
        BirdNestSkillChance = Chance(WoodcuttingSection, "BirdNestSkillChance", 0.06f, "Chance of a bird's nest added on top of the base chance at Woodcutting 100.");
        OldGrowthChance = Chance(WoodcuttingSection, "OldGrowthChance", 0.05f, "Old Growth: chance that a tree you fell gives twice the wood.");

        Mining = Bind("Mining", "the Pickaxes milestones: clean strikes, ore echo, rich veins, extra ore and finds");
        ExtraOreChance = Chance(MiningSection, "ExtraOreChance", 0.3f, "Chance of one more ore per ore at Pickaxes 100; it grows with the level from 0.");
        FindBaseChance = Chance(MiningSection, "FindBaseChance", 0.015f, "Chance of a find (amber, flint, coins, pearls) from broken rock at Pickaxes 0.");
        FindSkillChance = Chance(MiningSection, "FindSkillChance", 0.035f, "Chance of a find added on top of the base chance at Pickaxes 100.");
        ProspectorFindMultiplier = Number(MiningSection, "ProspectorFindMultiplier", 2f, "Prospector: the find chance is multiplied by this.", 1f, 5f);
        CleanStrikeChance = Chance(MiningSection, "CleanStrikeChance", 0.2f, "Clean Strike: chance that a pickaxe strike hits the seam.");
        CleanStrikeMultiplier = Number(MiningSection, "CleanStrikeMultiplier", 2f, "Clean Strike: damage multiplier of a strike that hits the seam.", 1f, 5f);
        RichVeinChance = Chance(MiningSection, "RichVeinChance", 0.1f, "Rich Veins: chance that a deposit gives twice the ore.");
        OreEchoRange = Number(MiningSection, "OreEchoRange", 40f, "Ore Echo: how far, in metres, ore deposits show on the map.", 5f, 200f);
        ProspectorRange = Number(MiningSection, "ProspectorRange", 80f, "Prospector: how far, in metres, the Ore Echo reaches.", 5f, 400f);
        OreEchoCooldown = Number(MiningSection, "OreEchoCooldown", 30f, "Ore Echo: seconds before striking rock can ring out again.", 1f, 600f);

        Blocking = Bind("Blocking", "the Blocking milestones: more health, less damage, cheaper blocks, Riposte, Shield Wall, Last Stand and Iron Guard");
        ExtraHealth = Number(BlockingSection, "ExtraHealth", 20f, "Extra maximum health at Blocking 100; it grows with the level from 0.", 0f, 200f);
        DamageReduction = Number(BlockingSection, "DamageReduction", 0.1f, "Share of all damage taken away at Blocking 100; it grows with the level from 0.", 0f, 0.9f);
        BlockStaminaSaving = Number(BlockingSection, "BlockStaminaSaving", 0.3f, "Share of the block stamina cost saved at Blocking 100; it grows with the level from 0.", 0f, 0.9f);
        RiposteBonus = Number(BlockingSection, "RiposteBonus", 0.5f, "Riposte: extra damage of the next melee hit after a perfect parry (0.5 = 50%).", 0f, 3f);
        RiposteSeconds = Number(BlockingSection, "RiposteSeconds", 3f, "Riposte: seconds the stronger hit waits.", 0.5f, 30f);
        ShieldWallReduction = Number(BlockingSection, "ShieldWallReduction", 0.2f, "Shield Wall: share of damage taken away around a blocking tower shield.", 0f, 0.9f);
        ShieldWallRange = Number(BlockingSection, "ShieldWallRange", 4f, "Shield Wall: how far, in metres, players are covered.", 1f, 30f);
        LastStandCooldownMinutes = Number(BlockingSection, "LastStandCooldownMinutes", 10f, "Last Stand: minutes before it can save you again.", 0.5f, 120f);
        LastStandGraceSeconds = Number(BlockingSection, "LastStandGraceSeconds", 3f, "Last Stand: seconds nothing can hurt you after it saves you.", 0f, 30f);
        IronGuardBonus = Number(BlockingSection, "IronGuardBonus", 0.25f, "Iron Guard: extra block power (0.25 = 25%).", 0f, 3f);

        Farming = Bind("Farming", "the Farming milestones: crops and seeds with stars, faster growth and giant crops");
        GreenThumbBonus = Number(FarmingSection, "GreenThumbBonus", 0.25f, "Green Thumb: share of the grow time taken away from what you plant.", 0f, 0.9f);
        GiantCropChance = Chance(FarmingSection, "GiantCropChance", 0.05f, "Giant Crops: chance that a crop you plant grows into a giant.");
        GiantCropYield = WhiteHiltConfig.BindAdminOnly(FarmingSection, "GiantCropYield", 3, "Giant Crops: how many times the normal harvest a giant gives.",
            new AcceptableValueRange<int>(1, 10));
        SeedStarBonus = Number(FarmingSection, "SeedStarBonus", 0.15f, "Added to every star chance of a crop per star of the seed it grew from.", 0f, 0.5f);

        Fishing = Bind("Fishing", "the fight on the line and the Fishing milestones: snags, steady hands, legendary fish, double catches and the catch log");
        LineStrain = Number(FishingSection, "LineStrain", 0.35f, "Share of the line's strength used per second while you reel in a running fish, at Fishing 0 (up to 40% less with skill). The line snaps at 1.", 0.05f, 5f);
        SteadyHandsReduction = Number(FishingSection, "SteadyHandsReduction", 0.5f, "Steady Hands: share of the line strain taken away.", 0f, 0.9f);
        SnagBaseChance = Chance(FishingSection, "SnagBaseChance", 0.05f, "Snags: chance that a catch brings up something more, at Fishing 0.");
        SnagSkillChance = Chance(FishingSection, "SnagSkillChance", 0.15f, "Snags: chance added on top of the base chance at Fishing 100.");
        LegendaryBaseChance = Chance(FishingSection, "LegendaryBaseChance", 0.01f, "Legendary Fish: chance that a catch is legendary, at Fishing 0.");
        LegendarySkillChance = Chance(FishingSection, "LegendarySkillChance", 0.03f, "Legendary Fish: chance added on top of the base chance at Fishing 100.");
        DoubleCatchChance = Chance(FishingSection, "DoubleCatchChance", 0.2f, "Double Catch: chance that a catch brings two fish.");

        Husbandry = Bind("Husbandry", "the Animal Husbandry milestones: twins and stronger young");
        TwinChance = Chance(HusbandrySection, "TwinChance", 0.25f, "Twins: chance of twins at Animal Husbandry 100; it grows with the level from 0.");
        StrongYoungChance = Chance(HusbandrySection, "StrongYoungChance", 0.3f, "Strong Young: chance of one more star at Animal Husbandry 100; it grows with the level from 0.");

        Lookout = Bind("Lookout", "the Exploration Lookout");
        LookoutCooldownMinutes = Number(ExplorationSection, "LookoutCooldownMinutes", 5f, "Lookout: minutes before you can look out again.", 0.1f, 60f);
        LookoutRadius = Number(ExplorationSection, "LookoutRadius", 200f, "Lookout: radius in metres the map opens up at Exploration 0; twice as far at 100.", 20f, 1000f);
        KeyLookout = WhiteHiltConfig.BindLocal(Section + ".Keys", "Lookout", new KeyboardShortcut(UnityEngine.KeyCode.O),
            "Look out from where you stand (Exploration 25): the map opens up around you and sea monsters show on it.");

        Perks.BindLevels(Section);
    }

    private static ConfigEntry<bool> Bind(string key, string what)
    {
        return WhiteHiltConfig.BindAdminOnly(Section, key, true, $"Switch {what}.");
    }

    private static ConfigEntry<float> Chance(string section, string key, float defaultValue, string description)
    {
        return WhiteHiltConfig.BindAdminOnly(section, key, defaultValue, description, new AcceptableValueRange<float>(0f, 1f));
    }

    private static ConfigEntry<float> Number(string section, string key, float defaultValue, string description, float min, float max)
    {
        return WhiteHiltConfig.BindAdminOnly(section, key, defaultValue, description, new AcceptableValueRange<float>(min, max));
    }
}
