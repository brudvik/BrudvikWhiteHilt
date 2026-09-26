namespace BrudvikWhiteHilt.Progression;

/// <summary>
/// Controls how White Hilt recipes become available.
/// </summary>
public enum ProgressionMode
{
    /// <summary>
    /// Every recipe is available as soon as its materials are known.
    /// </summary>
    Full,

    /// <summary>
    /// Recipes unlock tier by tier as the player discovers each biome's key material.
    /// </summary>
    Linear
}
