using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// Config of what the wild monsters' loot makes: the roast and the hákarl, the Brood Scale Charm and the Garm Fang
/// Arrow. The Heartwood Draught keeps its settings with the other meads. Admin only, synced from the server.
/// </summary>
public static class WildLootSettings
{
    private const string Section = "Monsters.Loot";

    /// <summary>Seconds Mist Stag Meat takes on the iron cooking station.</summary>
    public static ConfigEntry<float> RoastSeconds { get; private set; }

    /// <summary>Hákarl from one Shark Meat in the fermenter.</summary>
    public static ConfigEntry<int> HakarlYield { get; private set; }

    /// <summary>Armor the Brood Scale Charm adds.</summary>
    public static ConfigEntry<float> CharmArmor { get; private set; }

    /// <summary>How the Brood Scale Charm cuts the poison damage taken.</summary>
    public static ConfigEntry<HitData.DamageModifier> CharmPoison { get; private set; }

    /// <summary>Pierce damage of a Garm Fang Arrow.</summary>
    public static ConfigEntry<float> ArrowPierce { get; private set; }

    /// <summary>Fire damage of a Garm Fang Arrow.</summary>
    public static ConfigEntry<float> ArrowFire { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        RoastSeconds = WhiteHiltConfig.BindAdminOnly(Section, "RoastSeconds", 30f, "Seconds Mist Stag Meat takes to roast on the iron cooking station. Takes effect after a restart.",
            new AcceptableValueRange<float>(5f, 300f));
        HakarlYield = WhiteHiltConfig.BindAdminOnly(Section, "HakarlYield", 2, "Hákarl from one Shark Meat in the fermenter. Takes effect after a restart.",
            new AcceptableValueRange<int>(1, 12));
        CharmArmor = WhiteHiltConfig.BindAdminOnly(Section, "CharmArmor", 8f, "Armor the Brood Scale Charm adds while it is worn.",
            new AcceptableValueRange<float>(0f, 100f));
        CharmPoison = WhiteHiltConfig.BindAdminOnly(Section, "CharmPoison", HitData.DamageModifier.Resistant,
            "How the Brood Scale Charm changes the poison damage you take while it is worn.");
        ArrowPierce = WhiteHiltConfig.BindAdminOnly(Section, "ArrowPierce", 82f, "Pierce damage of a Garm Fang Arrow, as much as the vanilla Charred Arrow.",
            new AcceptableValueRange<float>(0f, 500f));
        ArrowFire = WhiteHiltConfig.BindAdminOnly(Section, "ArrowFire", 25f, "Fire damage a Garm Fang Arrow adds to its pierce damage.",
            new AcceptableValueRange<float>(0f, 500f));
    }
}
