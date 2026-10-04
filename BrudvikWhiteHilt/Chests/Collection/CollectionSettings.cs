using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Chests.Collection;

/// <summary>Server-controlled settings for collection posts.</summary>
internal static class CollectionSettings
{
    internal static ConfigEntry<bool> Enabled;
    internal static ConfigEntry<float> Radius;
    internal static ConfigEntry<float> ChestRadius;
    internal static ConfigEntry<float> Interval;
    internal static ConfigEntry<int> BatchSize;
    internal static ConfigEntry<int> ScanLimit;
    internal static ConfigEntry<bool> PlayerDrops;
    internal static ConfigEntry<float> OwnershipRetry;

    internal static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindAdminOnly("Chests.Collection", "Enabled", true, "Collection posts gather loose items into White Hilt chests.");
        Radius = WhiteHiltConfig.BindAdminOnly("Chests.Collection", "Radius", 80f, "Collection radius around each post, in metres.", new AcceptableValueRange<float>(10f, 200f));
        ChestRadius = WhiteHiltConfig.BindAdminOnly("Chests.Collection", "ChestRadius", 80f, "Maximum distance from the post to receiving chests, in metres.", new AcceptableValueRange<float>(10f, 200f));
        Interval = WhiteHiltConfig.BindAdminOnly("Chests.Collection", "IntervalSeconds", 2f, "Seconds between collection rounds.", new AcceptableValueRange<float>(0.5f, 60f));
        BatchSize = WhiteHiltConfig.BindAdminOnly("Chests.Collection", "StacksPerRound", 20, "Maximum loose stacks processed per post each round.", new AcceptableValueRange<int>(1, 200));
        ScanLimit = WhiteHiltConfig.BindAdminOnly("Chests.Collection", "ScanLimit", 200, "Maximum loaded drops examined per round; larger sets are scanned over several rounds.", new AcceptableValueRange<int>(20, 2000));
        PlayerDrops = WhiteHiltConfig.BindAdminOnly("Chests.Collection", "CollectPlayerDrops", false, "Also collect items deliberately dropped by players.");
        OwnershipRetry = WhiteHiltConfig.BindAdminOnly("Chests.Collection", "OwnershipRetrySeconds", 5f, "Seconds before retrying a chest ownership request that received no response.", new AcceptableValueRange<float>(1f, 60f));
        Translations.AddEnglish("whitehilt_collection_active", "Collecting into {0} chests nearby");
        Translations.AddEnglish("whitehilt_collection_stuck", "{0} stacks in the basket have no room in any chest");
        Translations.AddEnglish("whitehilt_collection_last", "Last sorted: {0} items, {1} s ago");
        Translations.AddEnglish("whitehilt_collection_paused", "Collection paused");
        Translations.AddEnglish("whitehilt_collection_nochests", "No accessible White Hilt chests in range");
        Translations.AddEnglish("whitehilt_collection_toggle", "Pause / resume collection");
        Translations.AddEnglish("whitehilt_collection_ranges", "Collection: {0} m; chests: {1} m");
        Translations.AddDynamic("whitehilt_collection_ranges", () => new object[] { Translations.Number(Radius.Value), Translations.Number(ChestRadius.Value) });
    }
}