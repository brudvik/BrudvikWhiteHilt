using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Mastery;

/// <summary>
/// Config for the skill milestones, stars and skill loss, section "Skills". Server-synced, except the keys.
/// </summary>
public static class MasterySettings
{
    private const string Section = "Skills";

    /// <summary>Share of the vanilla skill loss on death: 0 keeps every skill, 1 is vanilla, 2 doubles the loss.</summary>
    public static ConfigEntry<float> DeathLossMultiplier { get; private set; }

    /// <summary>Whether food, wild picks and crops can have stars.</summary>
    public static ConfigEntry<bool> Stars { get; private set; }

    /// <summary>Extra health, stamina, eitr and time per star of a dish.</summary>
    public static ConfigEntry<float> StarFoodBonus { get; private set; }

    /// <summary>Whether the Foraging skill and its milestones are on.</summary>
    public static ConfigEntry<bool> Foraging { get; private set; }

    /// <summary>Whether the Cooking milestones are on.</summary>
    public static ConfigEntry<bool> Cooking { get; private set; }

    /// <summary>Whether the Woodcutting milestones are on.</summary>
    public static ConfigEntry<bool> Woodcutting { get; private set; }

    /// <summary>Whether the Pickaxes milestones are on.</summary>
    public static ConfigEntry<bool> Mining { get; private set; }

    /// <summary>Whether the Blocking milestones are on.</summary>
    public static ConfigEntry<bool> Blocking { get; private set; }

    /// <summary>Whether the Farming milestones are on.</summary>
    public static ConfigEntry<bool> Farming { get; private set; }

    /// <summary>Whether the Fishing milestones and the fight on the line are on.</summary>
    public static ConfigEntry<bool> Fishing { get; private set; }

    /// <summary>Whether the Animal Husbandry milestones are on.</summary>
    public static ConfigEntry<bool> Husbandry { get; private set; }

    /// <summary>Whether the Exploration Lookout is on.</summary>
    public static ConfigEntry<bool> Lookout { get; private set; }

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
        Foraging = Bind("Foraging", "the Foraging skill: stars on wild picks, best picking times, extra yield and sweep picking");
        Cooking = Bind("Cooking", "the Cooking milestones: dishes with stars, faster kitchens and food that does not burn");
        Woodcutting = Bind("Woodcutting", "the Woodcutting milestones: aimed falls, replanting, domino felling, old growth, clean splits and bird nests");
        Mining = Bind("Mining", "the Pickaxes milestones: clean strikes, ore echo, rich veins, extra ore and finds");
        Blocking = Bind("Blocking", "the Blocking milestones: more health, less damage, cheaper blocks, Riposte, Shield Wall, Last Stand and Iron Guard");
        Farming = Bind("Farming", "the Farming milestones: crops and seeds with stars, faster growth and giant crops");
        Fishing = Bind("Fishing", "the fight on the line and the Fishing milestones: snags, steady hands, legendary fish, double catches and the catch log");
        Husbandry = Bind("Husbandry", "the Animal Husbandry milestones: twins and stronger young");
        Lookout = Bind("Lookout", "the Exploration Lookout");
        KeyLookout = WhiteHiltConfig.BindLocal(Section + ".Keys", "Lookout", new KeyboardShortcut(UnityEngine.KeyCode.O),
            "Look out from where you stand (Exploration 25): the map opens up around you and sea monsters show on it.");
    }

    private static ConfigEntry<bool> Bind(string key, string what)
    {
        return WhiteHiltConfig.BindAdminOnly(Section, key, true, $"Switch {what}.");
    }
}
