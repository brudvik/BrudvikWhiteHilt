namespace BrudvikWhiteHilt.Progression;

/// <summary>
/// Progression tiers in linear mode, ordered by biome.
/// </summary>
public enum ProgressionTier
{
    /// <summary>
    /// Available from the start.
    /// </summary>
    Start,

    /// <summary>
    /// Unlocked by Bronze.
    /// </summary>
    BlackForest,

    /// <summary>
    /// Unlocked by Iron.
    /// </summary>
    Swamp,

    /// <summary>
    /// Unlocked by Silver.
    /// </summary>
    Mountain,

    /// <summary>
    /// Unlocked by Black Metal.
    /// </summary>
    Plains,

    /// <summary>
    /// Unlocked by Eitr.
    /// </summary>
    Mistlands,

    /// <summary>
    /// Unlocked by Flametal.
    /// </summary>
    Ashlands
}
