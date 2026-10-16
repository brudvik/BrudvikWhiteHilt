using UnityEngine;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// The kinds of storm, one per biome or group of biomes.
/// </summary>
public enum StormKind
{
    /// <summary>No storm here: the Meadows.</summary>
    None,

    /// <summary>Thunder, rain and wind: the Black Forest and the Swamp.</summary>
    Tempest,

    /// <summary>Rain, fog and cold at sea.</summary>
    Sea,

    /// <summary>Sand that blinds but does not freeze: the Plains.</summary>
    Sand,

    /// <summary>A snowstorm so thick you can hardly move: the Mountain.</summary>
    Blizzard,

    /// <summary>Dense fog and bitter cold: the Mistlands.</summary>
    Mist,

    /// <summary>A storm of fire that sets you alight: the Ashlands.</summary>
    Fire,

    /// <summary>Snow, snowstorm and howling wind: the Deep North.</summary>
    Arctic
}

/// <summary>
/// How a storm treats someone caught out in it, by biome.
/// </summary>
public sealed class StormProfile
{
    private static readonly StormProfile none = new(StormKind.None, 0f, false, 100f, 1f, 0f, Color.clear, null);

    private static readonly StormProfile tempest = new(StormKind.Tempest, 1f, true, 80f, 1f, 0f, new Color(0.05f, 0.07f, 0.1f), "amb_storm_rain");
    private static readonly StormProfile sea = new(StormKind.Sea, 1.25f, true, 80f, 1f, 0.1f, new Color(0.05f, 0.08f, 0.12f), "amb_storm_sea");
    private static readonly StormProfile sand = new(StormKind.Sand, 1f, false, 100f, 0.85f, 0.45f, new Color(0.45f, 0.32f, 0.16f), "amb_storm_sand");
    private static readonly StormProfile blizzard = new(StormKind.Blizzard, 1.25f, true, 80f, 0.6f, 0.2f, new Color(0.75f, 0.82f, 0.9f), "amb_storm_blizzard");
    private static readonly StormProfile mist = new(StormKind.Mist, 1.5f, true, 80f, 0.9f, 0.25f, new Color(0.35f, 0.38f, 0.45f), "amb_storm_mist");
    private static readonly StormProfile fire = new(StormKind.Fire, 1.5f, true, 50f, 1f, 0.15f, new Color(0.45f, 0.08f, 0.02f), "amb_storm_fire");
    private static readonly StormProfile arctic = new(StormKind.Arctic, 2f, true, 80f, 0.5f, 0.3f, new Color(0.8f, 0.86f, 0.95f), "amb_storm_blizzard");

    private StormProfile(StormKind kind, float rate, bool hurts, float hurtFrom, float speed, float baseVision, Color tint, string ambience)
    {
        Kind = kind;
        Rate = rate;
        Hurts = hurts;
        HurtFrom = hurtFrom;
        Speed = speed;
        BaseVision = baseVision;
        Tint = tint;
        Ambience = ambience;
    }

    /// <summary>The kind of storm.</summary>
    public StormKind Kind { get; }

    /// <summary>How fast exposure climbs out in the open, 1 being the configured pace.</summary>
    public float Rate { get; }

    /// <summary>Whether it costs health in the end; the sandstorm only blinds and tires.</summary>
    public bool Hurts { get; }

    /// <summary>Exposure from which health is lost.</summary>
    public float HurtFrom { get; }

    /// <summary>Speed out in the open while it rages.</summary>
    public float Speed { get; }

    /// <summary>How much the view closes in while it rages, before any exposure.</summary>
    public float BaseVision { get; }

    /// <summary>Colour of the closing view.</summary>
    public Color Tint { get; }

    /// <summary>The looping sound, or null.</summary>
    public string Ambience { get; }

    /// <summary>Whether a fire sets you alight instead of the cold freezing you.</summary>
    public bool Burns => Kind == StormKind.Fire;

    /// <summary>Whether only a roof of stone shelters, as wood burns.</summary>
    public bool NeedsStoneRoof => Kind == StormKind.Fire;

    /// <summary>Whether trees close around give some shelter.</summary>
    public bool ForestShelters => Kind == StormKind.Tempest;

    /// <summary>Whether thunder cracks overhead.</summary>
    public bool Thunders => Kind is StormKind.Tempest or StormKind.Sea or StormKind.Mist;

    /// <summary>
    /// The storm of a biome.
    /// </summary>
    /// <param name="biome">The biome.</param>
    /// <returns>Its profile; Meadows and unknown biomes get none.</returns>
    public static StormProfile For(Heightmap.Biome biome)
    {
        return For(biome, false);
    }

    /// <summary>
    /// The storm of a biome now: in Ragnarok the Meadows have the forest's storm.
    /// </summary>
    /// <param name="biome">The biome.</param>
    /// <returns>Its profile.</returns>
    public static StormProfile Here(Heightmap.Biome biome)
    {
        return For(biome, StormState.Ragnarok && StormState.Phase != StormPhase.Calm);
    }

    /// <summary>
    /// The storm of a biome.
    /// </summary>
    /// <param name="biome">The biome.</param>
    /// <param name="ragnarok">Whether Ragnarok rages, which reaches the Meadows.</param>
    /// <returns>Its profile; Meadows outside Ragnarok and unknown biomes get none.</returns>
    public static StormProfile For(Heightmap.Biome biome, bool ragnarok)
    {
        if (ragnarok && biome == Heightmap.Biome.Meadows)
        {
            return tempest;
        }

        return biome switch
        {
            Heightmap.Biome.BlackForest or Heightmap.Biome.Swamp => tempest,
            Heightmap.Biome.Ocean => sea,
            Heightmap.Biome.Plains => sand,
            Heightmap.Biome.Mountain => blizzard,
            Heightmap.Biome.Mistlands => mist,
            Heightmap.Biome.AshLands => fire,
            Heightmap.Biome.DeepNorth => arctic,
            _ => none
        };
    }
}
