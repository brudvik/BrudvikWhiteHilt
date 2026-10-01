using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using Chanterelle = BrudvikWhiteHilt.Items.Foraging.Chanterelle.Chanterelle;
using Crowberries = BrudvikWhiteHilt.Items.Foraging.Crowberries.Crowberries;
using Lingonberries = BrudvikWhiteHilt.Items.Foraging.Lingonberries.Lingonberries;
using Porcini = BrudvikWhiteHilt.Items.Foraging.Porcini.Porcini;

namespace BrudvikWhiteHilt.Ranching;

/// <summary>
/// Config for animal husbandry, favourite foods, grooming, production, troughs and tether posts, section "Ranching". All admin-only.
/// </summary>
public static class RanchingSettings
{
    private const string Section = "Ranching";

    /// <summary>Whether the Animal Husbandry skill gives its bonuses. Off: it still gains experience.</summary>
    public static ConfigEntry<bool> HusbandryEffects { get; private set; }

    /// <summary>Whether favourite foods give their bonuses, are picked first from a trough and are shown on hover.</summary>
    public static ConfigEntry<bool> FavoriteFoodsEnabled { get; private set; }

    /// <summary>Whether groomed, fed tame animals put products in nearby troughs.</summary>
    public static ConfigEntry<bool> AnimalProduction { get; private set; }

    /// <summary>Whether groomed (content) animals breed faster and drop more when slaughtered.</summary>
    public static ConfigEntry<bool> GroomingBonus { get; private set; }

    /// <summary>Share taming time is shortened at skill level 100.</summary>
    public static ConfigEntry<float> TamingTimeReduction { get; private set; }

    /// <summary>Share food lasts longer at skill level 100.</summary>
    public static ConfigEntry<float> FedDurationBonus { get; private set; }

    /// <summary>Share pregnancy is shortened at skill level 100.</summary>
    public static ConfigEntry<float> PregnancyReduction { get; private set; }

    /// <summary>Extra animals a herd may hold at skill level 100.</summary>
    public static ConfigEntry<int> ExtraHerdSize { get; private set; }

    /// <summary>Metres within which players get experience and lend their skill to an animal.</summary>
    public static ConfigEntry<float> HusbandryRange { get; private set; }

    /// <summary>Experience per taming tick.</summary>
    public static ConfigEntry<float> TamingTickExperience { get; private set; }

    /// <summary>Experience when an animal becomes tame.</summary>
    public static ConfigEntry<float> TamedExperience { get; private set; }

    /// <summary>Experience when an animal eats.</summary>
    public static ConfigEntry<float> FeedingExperience { get; private set; }

    /// <summary>Experience when an animal is born or an egg is laid.</summary>
    public static ConfigEntry<float> BirthExperience { get; private set; }

    /// <summary>Experience for grooming an animal.</summary>
    public static ConfigEntry<float> GroomExperience { get; private set; }

    /// <summary>Experience when an animal puts something in a trough.</summary>
    public static ConfigEntry<float> ProduceExperience { get; private set; }

    /// <summary>Taming speed multiplier after a favourite meal.</summary>
    public static ConfigEntry<float> FavoriteTamingSpeed { get; private set; }

    /// <summary>Fed duration multiplier after a favourite meal.</summary>
    public static ConfigEntry<float> FavoriteFedDuration { get; private set; }

    /// <summary>Favourite foods per creature, "Creature:Item|Item, ...".</summary>
    public static ConfigEntry<string> FavoriteFoodsList { get; private set; }

    /// <summary>Products per creature, "Creature:Item:Days, ...".</summary>
    public static ConfigEntry<string> Products { get; private set; }

    /// <summary>Multiplier on pregnancy time and on the chance to skip a breeding check for a groomed animal.</summary>
    public static ConfigEntry<float> ContentBreedingFactor { get; private set; }

    /// <summary>Extra of each drop, except the trophy, from a groomed animal when slaughtered.</summary>
    public static ConfigEntry<int> ContentDropBonus { get; private set; }

    /// <summary>Metres within which a tether post reaches tame animals.</summary>
    public static ConfigEntry<float> TetherRange { get; private set; }

    /// <summary>Metres within which an animal notices a feeding trough.</summary>
    public static ConfigEntry<float> TroughRange { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake, before anything reads them.
    /// </summary>
    public static void Initialize()
    {
        HusbandryEffects = WhiteHiltConfig.BindAdminOnly(Section, "HusbandryEffects", true,
            "The Animal Husbandry skill makes nearby animals tame faster, stay fed longer and breed faster and in larger herds, and gives Strong Young and Twins. " +
            "Off: no bonuses; the skill still gains experience.");
        FavoriteFoodsEnabled = WhiteHiltConfig.BindAdminOnly(Section, "FavoriteFoods", true,
            "Favourite foods tame faster and keep animals fed longer, are picked first from a trough and are shown on hover. " +
            "Off: none of that. The White Hilt forageables stay in the animals' diets.");
        AnimalProduction = WhiteHiltConfig.BindAdminOnly(Section, "AnimalProduction", true,
            "Groomed, fed tame animals put their product (see Products) in a nearby feeding trough.");
        GroomingBonus = WhiteHiltConfig.BindAdminOnly(Section, "GroomingBonus", true,
            "Groomed (content) animals breed faster and drop more when slaughtered. Off: grooming only matters for production.");

        TamingTimeReduction = WhiteHiltConfig.BindAdminOnly(Section, "TamingTimeReduction", 0.4f,
            "Share of the taming time taken off at Animal Husbandry 100 (0.4 = 40% faster).", new AcceptableValueRange<float>(0f, 0.9f));
        FedDurationBonus = WhiteHiltConfig.BindAdminOnly(Section, "FedDurationBonus", 0.5f,
            "How much longer food lasts at Animal Husbandry 100 (0.5 = +50%).", new AcceptableValueRange<float>(0f, 5f));
        PregnancyReduction = WhiteHiltConfig.BindAdminOnly(Section, "PregnancyReduction", 0.3f,
            "Share of the pregnancy taken off at Animal Husbandry 100 (0.3 = 30% shorter).", new AcceptableValueRange<float>(0f, 0.9f));
        ExtraHerdSize = WhiteHiltConfig.BindAdminOnly(Section, "ExtraHerdSize", 2,
            "How many more animals a herd may hold at Animal Husbandry 100.", new AcceptableValueRange<int>(0, 20));
        HusbandryRange = WhiteHiltConfig.BindAdminOnly(Section, "HusbandryRange", 30f,
            "Metres within which players get Animal Husbandry experience and lend their skill to an animal.", new AcceptableValueRange<float>(5f, 100f));

        TamingTickExperience = WhiteHiltConfig.BindAdminOnly(Section, "TamingTickExperience", 0.2f,
            "Animal Husbandry experience for every taming tick (every 3 seconds).", new AcceptableValueRange<float>(0f, 10f));
        TamedExperience = WhiteHiltConfig.BindAdminOnly(Section, "TamedExperience", 50f,
            "Animal Husbandry experience when an animal becomes tame.", new AcceptableValueRange<float>(0f, 500f));
        FeedingExperience = WhiteHiltConfig.BindAdminOnly(Section, "FeedingExperience", 3f,
            "Animal Husbandry experience when an animal eats.", new AcceptableValueRange<float>(0f, 100f));
        BirthExperience = WhiteHiltConfig.BindAdminOnly(Section, "BirthExperience", 15f,
            "Animal Husbandry experience when an animal is born or an egg is laid.", new AcceptableValueRange<float>(0f, 200f));
        GroomExperience = WhiteHiltConfig.BindAdminOnly(Section, "GroomExperience", 10f,
            "Animal Husbandry experience for grooming an animal.", new AcceptableValueRange<float>(0f, 200f));
        ProduceExperience = WhiteHiltConfig.BindAdminOnly(Section, "ProduceExperience", 5f,
            "Animal Husbandry experience when an animal puts something in a trough.", new AcceptableValueRange<float>(0f, 200f));

        FavoriteTamingSpeed = WhiteHiltConfig.BindAdminOnly(Section, "FavoriteTamingSpeed", 1.5f,
            "How much faster an animal tames after eating a favourite food.", new AcceptableValueRange<float>(1f, 10f));
        FavoriteFedDuration = WhiteHiltConfig.BindAdminOnly(Section, "FavoriteFedDuration", 2f,
            "How much longer an animal stays fed after eating a favourite food.", new AcceptableValueRange<float>(1f, 10f));
        FavoriteFoodsList = WhiteHiltConfig.BindAdminOnly(Section, "FavoriteFoodsList",
            $"Boar:{Chanterelle.PrefabName}|{Porcini.PrefabName}, Wolf:Sausages, Lox:{Crowberries.PrefabName}, Hen:{Lingonberries.PrefabName}, Asksvin:MushroomSmokePuff",
            "Favourite foods per creature prefab: Creature:Item|Item, separated by commas. Foods are added to the creature's diet. " +
            "Applies on the next world load; foods removed from the list stay in the diet until the game restarts.");
        Products = WhiteHiltConfig.BindAdminOnly(Section, "Products", "Boar:LeatherScraps:1, Wolf:WolfHairBundle:1, Lox:LoxPelt:3",
            "What a groomed, fed tame animal puts in a nearby trough, and how many game days apart: Creature:Item:Days, separated by commas.");

        ContentBreedingFactor = WhiteHiltConfig.BindAdminOnly(Section, "ContentBreedingFactor", 0.5f,
            "A groomed animal's pregnancy time and its chance to skip a breeding check are multiplied by this (0.5 = halved).",
            new AcceptableValueRange<float>(0.1f, 1f));
        ContentDropBonus = WhiteHiltConfig.BindAdminOnly(Section, "ContentDropBonus", 1,
            "Extra of each drop, except the trophy, a groomed animal gives when slaughtered.", new AcceptableValueRange<int>(0, 5));
        TetherRange = WhiteHiltConfig.BindAdminOnly(Section, "TetherRange", 10f,
            "Metres within which a tether post tethers or frees tame animals.", new AcceptableValueRange<float>(2f, 50f));
        TroughRange = WhiteHiltConfig.BindAdminOnly(Section, "TroughRange", 15f,
            "Metres within which a hungry animal notices a feeding trough and puts its products in one.", new AcceptableValueRange<float>(3f, 50f));
    }
}
