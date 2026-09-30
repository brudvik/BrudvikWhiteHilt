using BepInEx.Configuration;

namespace BrudvikWhiteHilt.Planting;

/// <summary>
/// How a sapling borrows its look from the vanilla tree it grows into.
/// </summary>
public enum SaplingLook
{
    /// <summary>Birch sapling trunk with the swamp tree's bark, no leaves.</summary>
    AncientBark,

    /// <summary>Birch sapling with the autumn birch's leaves.</summary>
    AutumnLeaves,

    /// <summary>Birch sapling with the Yggdrasil shoot's bark and leaves.</summary>
    YggaShoot,

    /// <summary>A small copy of the Ashlands tree, glowing.</summary>
    Ashwood
}

/// <summary>
/// A sapling cloned from the vanilla birch sapling that grows into other vanilla trees. The prefab names match
/// PlantEverything's, so saplings planted with that mod keep growing.
/// </summary>
public class SaplingDefinition : PlantableDefinition
{
    /// <summary>
    /// Creates a sapling definition.
    /// </summary>
    /// <param name="prefab">Prefab name of the sapling.</param>
    /// <param name="name">English name in the cultivator menu.</param>
    /// <param name="resource">Item it costs.</param>
    /// <param name="look">How it borrows its look.</param>
    /// <param name="grownPrefabs">Vanilla trees it grows into.</param>
    /// <param name="biomes">Biomes it grows in when saplings keep to their biomes.</param>
    /// <param name="defaultEnabled">Whether it is in the cultivator by default.</param>
    /// <param name="maxScale">Default largest tree scale.</param>
    /// <param name="extraResource">A second item it always costs, or null.</param>
    /// <param name="tolerateHeat">True if it grows in the Ashlands heat without shelter.</param>
    public SaplingDefinition(string prefab, string name, string resource, SaplingLook look, string[] grownPrefabs, Heightmap.Biome biomes,
        bool defaultEnabled, float maxScale, string extraResource = null, bool tolerateHeat = false)
        : base(prefab, PlantableKind.Sapling, name, 1, resource, extraResource: extraResource, extraAmount: extraResource != null ? 1 : 0)
    {
        Look = look;
        GrownPrefabs = grownPrefabs;
        Biomes = biomes;
        DefaultEnabled = defaultEnabled;
        DefaultMaxScale = maxScale;
        TolerateHeat = tolerateHeat;
    }

    /// <summary>How it borrows its look.</summary>
    public SaplingLook Look { get; }

    /// <summary>Vanilla trees it grows into.</summary>
    public string[] GrownPrefabs { get; }

    /// <summary>Biomes it grows in when saplings keep to their biomes.</summary>
    public Heightmap.Biome Biomes { get; }

    /// <summary>Whether it is in the cultivator by default.</summary>
    public bool DefaultEnabled { get; }

    /// <summary>Default largest tree scale.</summary>
    public float DefaultMaxScale { get; }

    /// <summary>True if it grows in the Ashlands heat without shelter.</summary>
    public bool TolerateHeat { get; }

    /// <summary>Whether it is in the cultivator.</summary>
    public ConfigEntry<bool> Enabled { get; set; }

    /// <summary>Seconds it takes to grow.</summary>
    public ConfigEntry<float> GrowthTime { get; set; }

    /// <summary>Smallest scale of the grown tree.</summary>
    public ConfigEntry<float> MinScale { get; set; }

    /// <summary>Largest scale of the grown tree.</summary>
    public ConfigEntry<float> MaxScale { get; set; }

    /// <summary>Free space in metres it needs around it to grow.</summary>
    public ConfigEntry<float> GrowRadius { get; set; }
}
