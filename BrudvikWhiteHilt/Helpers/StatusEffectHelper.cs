namespace BrudvikWhiteHilt.Helpers;

/// <summary>
/// Helpers for querying active status effects.
/// </summary>
public static class StatusEffectHelper
{
    /// <summary>
    /// Checks whether the character has an active status effect of the given type.
    /// </summary>
    /// <typeparam name="T">The status effect type.</typeparam>
    /// <param name="character">The character to check.</param>
    /// <returns>True if the effect is active.</returns>
    public static bool Has<T>(Character character) where T : StatusEffect
    {
        if (character == null)
        {
            return false;
        }

        foreach (var effect in character.GetSEMan().GetStatusEffects())
        {
            if (effect is T)
            {
                return true;
            }
        }

        return false;
    }
}
