namespace BrudvikWhiteHilt.Progression;

/// <summary>
/// Per-item override of the progression tier, set in the config file.
/// </summary>
public enum TierOverride
{
    /// <summary>
    /// Use the item's built-in tier.
    /// </summary>
    Default,

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
    Ashlands,

    /// <summary>
    /// Never craftable, in any mode. Existing copies are kept.
    /// </summary>
    Never
}
