using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Companions;

/// <summary>
/// Config entries for the dog. Everything but the owner warnings is admin-only and synced from the server.
/// </summary>
public static class DogSettings
{
    private const string Section = "Dog";

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

    /// <summary>
    /// Binds the entries. Call from the plugin's Awake, after the config is set up.
    /// </summary>
    public static void Initialize()
    {
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
    }
}
