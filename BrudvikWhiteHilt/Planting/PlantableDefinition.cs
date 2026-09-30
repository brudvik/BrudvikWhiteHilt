using BepInEx.Configuration;

namespace BrudvikWhiteHilt.Planting;

/// <summary>
/// What kind of thing a plantable is; decides whether it may be removed and whether it needs cultivated ground.
/// </summary>
public enum PlantableKind
{
    /// <summary>Berry bushes, mushrooms and flowers that grow back after picking.</summary>
    Pickable,

    /// <summary>Branches, stones and flint lying on the ground.</summary>
    Debris,

    /// <summary>Decorative small trees, bushes, shrubs, vines and ferns.</summary>
    Flora,

    /// <summary>Saplings that grow into trees.</summary>
    Sapling
}

/// <summary>
/// A vanilla prefab the cultivator can plant, with its cost.
/// </summary>
public class PlantableDefinition
{
    /// <summary>
    /// Creates a definition.
    /// </summary>
    /// <param name="prefab">Vanilla prefab name.</param>
    /// <param name="kind">What kind of plantable it is.</param>
    /// <param name="name">English name in the cultivator menu.</param>
    /// <param name="defaultCost">Default amount of the resource it costs.</param>
    /// <param name="resource">Item it costs, or null for the item the pickable gives.</param>
    /// <param name="grounded">True if it must stand on the ground.</param>
    /// <param name="extraResource">A second item it always costs, or null.</param>
    /// <param name="extraAmount">Amount of the second item.</param>
    public PlantableDefinition(string prefab, PlantableKind kind, string name, int defaultCost, string resource = null, bool grounded = true,
        string extraResource = null, int extraAmount = 0)
    {
        Prefab = prefab;
        Kind = kind;
        Name = name;
        DefaultCost = defaultCost;
        Resource = resource;
        Grounded = grounded;
        ExtraResource = extraResource;
        ExtraAmount = extraAmount;
    }

    /// <summary>Vanilla prefab name.</summary>
    public string Prefab { get; }

    /// <summary>What kind of plantable it is.</summary>
    public PlantableKind Kind { get; }

    /// <summary>English name in the cultivator menu.</summary>
    public string Name { get; }

    /// <summary>Default amount of the resource it costs.</summary>
    public int DefaultCost { get; }

    /// <summary>Item it costs, or null for the item the pickable gives.</summary>
    public string Resource { get; }

    /// <summary>True if it must stand on the ground.</summary>
    public bool Grounded { get; }

    /// <summary>A second item it always costs, or null.</summary>
    public string ExtraResource { get; }

    /// <summary>Amount of the second item.</summary>
    public int ExtraAmount { get; }

    /// <summary>Configured cost; 0 leaves it out of the cultivator.</summary>
    public ConfigEntry<int> Cost { get; set; }

    /// <summary>Translation key of the name.</summary>
    public string NameKey => $"piece_whitehilt_plant_{Prefab.ToLowerInvariant()}";
}
