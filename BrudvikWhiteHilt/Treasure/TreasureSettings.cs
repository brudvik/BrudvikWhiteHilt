using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Treasure;

/// <summary>
/// How much help a treasure map gives.
/// </summary>
public enum TreasureHintLevel
{
    /// <summary>Named landmarks, the note at the bottom, a dotted path to the cross, the cairn and the warnings when close.</summary>
    Easy,

    /// <summary>Landmarks as symbols, the note at the bottom, the cairn and the warnings; the Exploration skill adds more.</summary>
    Normal,

    /// <summary>Only the land, the cross and the cairn.</summary>
    Hard
}

/// <summary>
/// Config entries for the treasure maps Hildir sells. All are admin-only and synced from the server.
/// </summary>
public static class TreasureSettings
{
    private const string Section = "Treasure";

    /// <summary>Whether Hildir sells treasure maps. Maps and chests already in the world stay either way.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>Coins a map costs.</summary>
    public static ConfigEntry<int> Price { get; private set; }

    /// <summary>Global key that must be set before Hildir sells maps; empty for always.</summary>
    public static ConfigEntry<string> RequiredGlobalKey { get; private set; }

    /// <summary>Treasures a player may have waiting in the ground at once.</summary>
    public static ConfigEntry<int> MaxActiveMaps { get; private set; }

    /// <summary>How much help the maps give.</summary>
    public static ConfigEntry<TreasureHintLevel> HintLevel { get; private set; }

    /// <summary>Width of the land a map shows, in metres.</summary>
    public static ConfigEntry<float> FragmentSize { get; private set; }

    /// <summary>Share of the map that is faded away.</summary>
    public static ConfigEntry<float> MissingShare { get; private set; }

    /// <summary>Whether a map may be drawn turned a quarter, half or three quarters, with the north arrow turned with it.</summary>
    public static ConfigEntry<bool> RotateFragment { get; private set; }

    /// <summary>Share of the map's land the buyer must have explored.</summary>
    public static ConfigEntry<float> MinExploredShare { get; private set; }

    /// <summary>Nearest a treasure is buried to the buyer, in metres.</summary>
    public static ConfigEntry<float> MinDistance { get; private set; }

    /// <summary>Farthest a treasure is buried from the buyer, in metres.</summary>
    public static ConfigEntry<float> MaxDistance { get; private set; }

    /// <summary>Biomes a treasure may be buried in, comma separated.</summary>
    public static ConfigEntry<string> AllowedBiomes { get; private set; }

    /// <summary>Nearest a treasure is buried to anything a player has built, in metres.</summary>
    public static ConfigEntry<float> MinBaseDistance { get; private set; }

    /// <summary>Nearest two treasures are buried to each other, in metres.</summary>
    public static ConfigEntry<float> MinTreasureDistance { get; private set; }

    /// <summary>Farthest a treasure is buried from water or a landmark it can be found by, in metres.</summary>
    public static ConfigEntry<float> FeatureDistance { get; private set; }

    /// <summary>Pickaxe blows it takes to dig the chest up.</summary>
    public static ConfigEntry<int> DigHits { get; private set; }

    /// <summary>Black beast trophies in a chest.</summary>
    public static ConfigEntry<int> TrophyCount { get; private set; }

    /// <summary>Draws from the loot list besides the trophies.</summary>
    public static ConfigEntry<int> LootRolls { get; private set; }

    /// <summary>The loot list: Prefab:min-max:weight, comma separated.</summary>
    public static ConfigEntry<string> Loot { get; private set; }

    /// <summary>Metres from the treasure at which a player carrying its map is told the ground looks dug.</summary>
    public static ConfigEntry<float> WarmDistance { get; private set; }

    /// <summary>Metres from the treasure at which dust rises from it for a player carrying its map.</summary>
    public static ConfigEntry<float> DustDistance { get; private set; }

    /// <summary>Exploration level from which the landmarks on a Normal map are named.</summary>
    public static ConfigEntry<int> SkillNamesLevel { get; private set; }

    /// <summary>Exploration level from which a Normal map shows a dotted path from a landmark to the cross.</summary>
    public static ConfigEntry<int> SkillPathLevel { get; private set; }

    /// <summary>Share of the faded parts restored at Exploration 100; less at lower levels.</summary>
    public static ConfigEntry<float> SkillFadeReduction { get; private set; }

    /// <summary>
    /// Binds the entries. Call from the plugin's Awake, after the config is set up.
    /// </summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true,
            "Hildir sells treasure maps. Off: she no longer does; maps and buried chests already in the world stay.");
        Price = WhiteHiltConfig.BindAdminOnly(Section, "Price", 750, "Coins a treasure map costs at Hildir.", new AcceptableValueRange<int>(0, 10000));
        RequiredGlobalKey = WhiteHiltConfig.BindAdminOnly(Section, "RequiredGlobalKey", "defeated_gdking",
            "World key that must be set before Hildir sells maps, e.g. defeated_gdking for the Elder. Empty: always.");
        MaxActiveMaps = WhiteHiltConfig.BindAdminOnly(Section, "MaxActiveMaps", 3,
            "Treasures a player may have waiting in the ground at once. Buying one more gives the coins back.", new AcceptableValueRange<int>(1, 20));
        HintLevel = WhiteHiltConfig.BindAdminOnly(Section, "HintLevel", TreasureHintLevel.Normal,
            "Easy: named landmarks, a note, a dotted path to the cross. Normal: landmark symbols and a note; the Exploration skill adds names and the path. Hard: only the land and the cross. The cairn by the treasure is always there.");
        FragmentSize = WhiteHiltConfig.BindAdminOnly(Section, "FragmentSize", 480f, "Width of the land a map shows, in metres.",
            new AcceptableValueRange<float>(200f, 1200f));
        MissingShare = WhiteHiltConfig.BindAdminOnly(Section, "MissingShare", 0.25f, "Share of a map that is faded away (0.25 = a quarter).",
            new AcceptableValueRange<float>(0f, 0.6f));
        RotateFragment = WhiteHiltConfig.BindAdminOnly(Section, "RotateFragment", false,
            "A map may be drawn turned a quarter, half or three quarters round; its north arrow turns with it.");
        MinExploredShare = WhiteHiltConfig.BindAdminOnly(Section, "MinExploredShare", 0.6f,
            "Share of the land on a map the buyer must have explored, so it can be recognised.", new AcceptableValueRange<float>(0f, 1f));
        MinDistance = WhiteHiltConfig.BindAdminOnly(Section, "MinDistance", 200f, "Nearest a treasure is buried to the buyer, in metres.",
            new AcceptableValueRange<float>(0f, 5000f));
        MaxDistance = WhiteHiltConfig.BindAdminOnly(Section, "MaxDistance", 3000f, "Farthest a treasure is buried from the buyer, in metres.",
            new AcceptableValueRange<float>(300f, 10000f));
        AllowedBiomes = WhiteHiltConfig.BindAdminOnly(Section, "AllowedBiomes", "Meadows, BlackForest, Swamp, Mountain, Plains",
            "Biomes a treasure may be buried in, comma separated: Meadows, BlackForest, Swamp, Mountain, Plains, Mistlands, AshLands, DeepNorth.");
        MinBaseDistance = WhiteHiltConfig.BindAdminOnly(Section, "MinBaseDistance", 60f, "Nearest a treasure is buried to anything a player has built, in metres.",
            new AcceptableValueRange<float>(0f, 300f));
        MinTreasureDistance = WhiteHiltConfig.BindAdminOnly(Section, "MinTreasureDistance", 150f, "Nearest two treasures are buried to each other, in metres.",
            new AcceptableValueRange<float>(0f, 1000f));
        FeatureDistance = WhiteHiltConfig.BindAdminOnly(Section, "FeatureDistance", 80f,
            "A treasure is buried at most this many metres from water or a landmark, so the cross points at something on the map.",
            new AcceptableValueRange<float>(20f, 300f));
        DigHits = WhiteHiltConfig.BindAdminOnly(Section, "DigHits", 3, "Pickaxe blows it takes to dig the chest up.", new AcceptableValueRange<int>(1, 20));
        TrophyCount = WhiteHiltConfig.BindAdminOnly(Section, "TrophyCount", 1,
            "Black beast trophies in a chest, of beasts whose boss has been defeated.", new AcceptableValueRange<int>(0, 5));
        LootRolls = WhiteHiltConfig.BindAdminOnly(Section, "LootRolls", 3, "Draws from the loot list besides the trophies.", new AcceptableValueRange<int>(0, 10));
        Loot = WhiteHiltConfig.BindAdminOnly(Section, "Loot", TreasureLoot.DefaultLoot,
            "What a chest may hold besides the trophies: Prefab:min-max:weight, comma separated. Items that do not exist are skipped.");
        WarmDistance = WhiteHiltConfig.BindAdminOnly(Section, "WarmDistance", 25f,
            "Metres from the treasure at which a player carrying its map is told the ground looks dug up. 0 = never.", new AcceptableValueRange<float>(0f, 100f));
        DustDistance = WhiteHiltConfig.BindAdminOnly(Section, "DustDistance", 10f,
            "Metres from the treasure at which dust rises from it for a player carrying its map. 0 = never.", new AcceptableValueRange<float>(0f, 50f));
        SkillNamesLevel = WhiteHiltConfig.BindAdminOnly(Section, "SkillNamesLevel", 40,
            "Exploration level from which the landmarks on a Normal map are named.", new AcceptableValueRange<int>(0, 100));
        SkillPathLevel = WhiteHiltConfig.BindAdminOnly(Section, "SkillPathLevel", 70,
            "Exploration level from which a Normal map shows a dotted path from a landmark to the cross.", new AcceptableValueRange<int>(0, 100));
        SkillFadeReduction = WhiteHiltConfig.BindAdminOnly(Section, "SkillFadeReduction", 0.6f,
            "Share of the faded parts that comes back at Exploration 100, less at lower levels (0.6 = 60%).", new AcceptableValueRange<float>(0f, 1f));
    }
}
