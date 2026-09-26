namespace BrudvikWhiteHilt.Progression;

/// <summary>
/// An item or piece whose recipe follows White Hilt progression.
/// </summary>
public interface IWhiteHiltProgressionEntry
{
    /// <summary>
    /// Stable identifier, used as the config key.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Name shown to players.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Prefab whose recipe or piece is gated.
    /// </summary>
    string GatedPrefabName { get; }

    /// <summary>
    /// Tier used in linear mode when the config does not override it.
    /// </summary>
    ProgressionTier DefaultTier { get; }
}
