using System;
using System.Globalization;

namespace BrudvikWhiteHilt.SeaBattles;

/// <summary>
/// Reads the settings that give a value per global key, <c>key=value; key=value</c>, and picks the value of the last key
/// in the text that the world has set. The Nail Ships' hull, hold, crew level and leapers grow with the world this way.
/// Pure, so it is tested without the game; it never throws on a broken text.
/// </summary>
public static class TierValues
{
    /// <summary>
    /// The text of the last key that is set, as written after its <c>=</c>, trimmed. A part without a key or with an empty
    /// value is skipped. The value may hold commas and colons, as a loot list does, but no semicolon.
    /// </summary>
    /// <param name="text">The config text.</param>
    /// <param name="hasKey">Tells whether a global key is set.</param>
    /// <returns>The value, or null when no key in the text is set.</returns>
    public static string PickText(string text, Func<string, bool> hasKey)
    {
        string picked = null;
        if (hasKey == null)
        {
            return null;
        }

        foreach (string part in (text ?? string.Empty).Split(';'))
        {
            int equals = part.IndexOf('=');
            if (equals <= 0)
            {
                continue;
            }

            string key = part.Substring(0, equals).Trim();
            string value = part.Substring(equals + 1).Trim();
            if (key.Length > 0 && value.Length > 0 && hasKey(key))
            {
                picked = value;
            }
        }

        return picked;
    }

    /// <summary>
    /// The number of the last key that is set. Parts whose value is not a number are skipped, so an earlier key that reads
    /// wins over a later one that does not.
    /// </summary>
    /// <param name="text">The config text.</param>
    /// <param name="hasKey">Tells whether a global key is set.</param>
    /// <param name="fallback">The value when no key in the text is set.</param>
    /// <returns>The value.</returns>
    public static float PickNumber(string text, Func<string, bool> hasKey, float fallback)
    {
        float picked = fallback;
        if (hasKey == null)
        {
            return fallback;
        }

        foreach (string part in (text ?? string.Empty).Split(';'))
        {
            int equals = part.IndexOf('=');
            if (equals <= 0)
            {
                continue;
            }

            string key = part.Substring(0, equals).Trim();
            if (key.Length > 0 && hasKey(key)
                && float.TryParse(part.Substring(equals + 1).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
                && !float.IsNaN(value) && !float.IsInfinity(value))
            {
                picked = value;
            }
        }

        return picked;
    }
}
