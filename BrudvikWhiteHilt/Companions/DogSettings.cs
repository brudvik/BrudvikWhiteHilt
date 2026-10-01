using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Companions;

/// <summary>
/// Config entries for the dog. Everything but the owner warnings is admin-only and synced from the server.
/// </summary>
public static class DogSettings
{
    private const string Section = "Dog";

    /// <summary>Whether the Bog Witch sells puppies. Existing dogs stay either way.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>Coins a puppy costs at the Bog Witch.</summary>
    public static ConfigEntry<int> Price { get; private set; }

    /// <summary>Game days of care before a puppy is grown.</summary>
    public static ConfigEntry<float> GrowDays { get; private set; }

    /// <summary>Game days without food before the dog starves.</summary>
    public static ConfigEntry<float> StarveDays { get; private set; }

    /// <summary>Game days without a house or bed before the dog runs away.</summary>
    public static ConfigEntry<float> RunAwayDays { get; private set; }

    /// <summary>Game days a dog lives before it dies of old age, 0 for never.</summary>
    public static ConfigEntry<float> LifeDays { get; private set; }

    /// <summary>Extra health per bond level, as a fraction.</summary>
    public static ConfigEntry<float> HealthPerBondLevel { get; private set; }

    /// <summary>Extra damage per bond level, as a fraction.</summary>
    public static ConfigEntry<float> DamagePerBondLevel { get; private set; }

    /// <summary>Minutes the cuddle buff lasts.</summary>
    public static ConfigEntry<float> CuddleMinutes { get; private set; }

    /// <summary>Whether the owner gets messages when the dog is hungry, homeless or about to run away.</summary>
    public static ConfigEntry<bool> OwnerWarnings { get; private set; }

    /// <summary>Bond experience for level n is n squared times this.</summary>
    public static ConfigEntry<float> BondXpPerLevelSquared { get; private set; }

    /// <summary>Bond experience for fetching a stick.</summary>
    public static ConfigEntry<float> FetchXp { get; private set; }

    /// <summary>Bond experience for doing a known trick.</summary>
    public static ConfigEntry<float> TrickXp { get; private set; }

    /// <summary>Lessons, each with a Dog Treat, before a trick is learnt.</summary>
    public static ConfigEntry<int> LessonsToLearn { get; private set; }

    /// <summary>Whether two dogs may have a litter of puppies.</summary>
    public static ConfigEntry<bool> Litters { get; private set; }

    /// <summary>Chance per night that a suitable pair of dogs has a litter.</summary>
    public static ConfigEntry<float> LitterChance { get; private set; }

    /// <summary>Game days between a dog's litters.</summary>
    public static ConfigEntry<int> LitterCooldownDays { get; private set; }

    /// <summary>Bond level both dogs need for a litter.</summary>
    public static ConfigEntry<int> LitterMinBond { get; private set; }

    /// <summary>Chance that the dog digs something up when it gets the urge at home.</summary>
    public static ConfigEntry<float> DigChance { get; private set; }

    /// <summary>Chance of swamp poison per care tick while the dog wades in swamp water.</summary>
    public static ConfigEntry<float> SwampPoisonChance { get; private set; }

    /// <summary>Share of its health a bandage heals.</summary>
    public static ConfigEntry<float> BandageHeal { get; private set; }

    /// <summary>Share of its health a resting dog heals per care tick.</summary>
    public static ConfigEntry<float> RestHealPerTick { get; private set; }

    /// <summary>Metres around home within which the dog barks at enemies and warns its owner.</summary>
    public static ConfigEntry<float> GuardRadius { get; private set; }

    /// <summary>Metres from the following dog's nose within which it sniffs out forage for its owner.</summary>
    public static ConfigEntry<float> SniffRange { get; private set; }

    /// <summary>
    /// Binds the entries. Call from the plugin's Awake, after the config is set up.
    /// </summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true,
            "The Bog Witch sells puppies. Off: she no longer does; dogs already bought or born stay.");
        Price = WhiteHiltConfig.BindAdminOnly(Section, "Price", 1000, "Coins a puppy costs at the Bog Witch.", new AcceptableValueRange<int>(0, 10000));
        GrowDays = WhiteHiltConfig.BindAdminOnly(Section, "GrowDays", 10f, "Game days of care before a puppy is grown.", new AcceptableValueRange<float>(1f, 60f));
        StarveDays = WhiteHiltConfig.BindAdminOnly(Section, "StarveDays", 15f, "Game days without food before the dog starves.", new AcceptableValueRange<float>(2f, 100f));
        RunAwayDays = WhiteHiltConfig.BindAdminOnly(Section, "RunAwayDays", 3f, "Game days without a dog house or a bed under a roof before the dog runs away.",
            new AcceptableValueRange<float>(1f, 30f));
        LifeDays = WhiteHiltConfig.BindAdminOnly(Section, "LifeDays", 160f,
            "Game days a dog lives, give or take 10%, before it dies of old age. It greys from 40% of it. 0 = it never dies of old age.",
            new AcceptableValueRange<float>(0f, 2000f));
        HealthPerBondLevel = WhiteHiltConfig.BindAdminOnly(Section, "HealthPerBondLevel", 0.1f, "Extra health per bond level (0.1 = +10%).",
            new AcceptableValueRange<float>(0f, 0.5f));
        DamagePerBondLevel = WhiteHiltConfig.BindAdminOnly(Section, "DamagePerBondLevel", 0.05f, "Extra damage per bond level (0.05 = +5%).",
            new AcceptableValueRange<float>(0f, 0.3f));
        CuddleMinutes = WhiteHiltConfig.BindAdminOnly(Section, "CuddleMinutes", 10f, "Minutes the cuddle buff lasts.", new AcceptableValueRange<float>(1f, 60f));
        OwnerWarnings = WhiteHiltConfig.BindLocal(Section, "OwnerWarnings", true, "Tell me when my dog is hungry, has no proper home or is about to run away.");
        BondXpPerLevelSquared = WhiteHiltConfig.BindAdminOnly(Section, "BondXpPerLevelSquared", 250f,
            "Bond experience needed for a level is the level squared times this. Changing it changes the bond level of existing dogs.",
            new AcceptableValueRange<float>(10f, 5000f));
        FetchXp = WhiteHiltConfig.BindAdminOnly(Section, "FetchXp", 10f, "Bond experience for fetching a thrown stick.", new AcceptableValueRange<float>(0f, 200f));
        TrickXp = WhiteHiltConfig.BindAdminOnly(Section, "TrickXp", 2f, "Bond experience for doing a trick it knows.", new AcceptableValueRange<float>(0f, 100f));
        LessonsToLearn = WhiteHiltConfig.BindAdminOnly(Section, "LessonsToLearn", 3,
            "Lessons, each costing a Dog Treat, before the dog learns sit, lie down, give paw or roll over.", new AcceptableValueRange<int>(1, 10));
        Litters = WhiteHiltConfig.BindAdminOnly(Section, "Litters", true,
            "Two grown, fed, happy dogs of different players with a strong bond, together at night, may have a puppy.");
        LitterChance = WhiteHiltConfig.BindAdminOnly(Section, "LitterChance", 0.25f, "Chance per night that such a pair has a puppy.",
            new AcceptableValueRange<float>(0f, 1f));
        LitterCooldownDays = WhiteHiltConfig.BindAdminOnly(Section, "LitterCooldownDays", 20, "Game days between a dog's litters.",
            new AcceptableValueRange<int>(1, 200));
        LitterMinBond = WhiteHiltConfig.BindAdminOnly(Section, "LitterMinBond", 3, "Bond level both dogs need for a litter.",
            new AcceptableValueRange<int>(0, 10));
        DigChance = WhiteHiltConfig.BindAdminOnly(Section, "DigChance", 0.5f, "Chance that the dog digs something up when it gets the urge at home.",
            new AcceptableValueRange<float>(0f, 1f));
        SwampPoisonChance = WhiteHiltConfig.BindAdminOnly(Section, "SwampPoisonChance", 0.3f,
            "Chance of swamp poison per care tick while the dog wades in swamp water. 0 = never.", new AcceptableValueRange<float>(0f, 1f));
        BandageHeal = WhiteHiltConfig.BindAdminOnly(Section, "BandageHeal", 0.5f, "Share of its health a bandage heals (0.5 = half).",
            new AcceptableValueRange<float>(0f, 1f));
        RestHealPerTick = WhiteHiltConfig.BindAdminOnly(Section, "RestHealPerTick", 0.01f, "Share of its health the dog heals per care tick while it rests at home.",
            new AcceptableValueRange<float>(0f, 0.1f));
        GuardRadius = WhiteHiltConfig.BindAdminOnly(Section, "GuardRadius", 30f, "Metres around its home within which the dog barks at enemies and warns its owner. 0 = never.",
            new AcceptableValueRange<float>(0f, 100f));
        SniffRange = WhiteHiltConfig.BindAdminOnly(Section, "SniffRange", 25f,
            "Metres from a following dog within which it sniffs out forage and pins it on its owner's map. 0 = off.", new AcceptableValueRange<float>(0f, 100f));
    }
}
