using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// Supply and demand at a trading post: what a post has been sold lately lowers what it pays for more of the same, and
/// the price comes back over the days. Kept per post and per good as a glut, in stacks sold, with the time it was last
/// written; the glut runs off as the post sells the goods on.
/// </summary>
public static class TradingMarket
{
    private const char EntrySeparator = ';';
    private const char FieldSeparator = '|';

    /// <summary>
    /// A post's glut of one good.
    /// </summary>
    public readonly struct Entry
    {
        /// <summary>
        /// Creates an entry.
        /// </summary>
        /// <param name="glut">Stacks sold and not yet sold on.</param>
        /// <param name="ticks">World ticks when it was written.</param>
        public Entry(float glut, long ticks)
        {
            Glut = glut;
            Ticks = ticks;
        }

        /// <summary>Stacks sold and not yet sold on, when it was written.</summary>
        public float Glut { get; }

        /// <summary>World ticks when it was written.</summary>
        public long Ticks { get; }
    }

    /// <summary>
    /// The glut after some days: it runs off at a steady pace.
    /// </summary>
    /// <param name="glut">The glut when written.</param>
    /// <param name="days">Game days since.</param>
    /// <param name="recoverPerDay">Stacks the post sells on in a day.</param>
    /// <returns>The glut now, never below 0.</returns>
    public static float Glut(float glut, double days, float recoverPerDay)
    {
        return Mathf.Max(0f, glut - (float)(Mathf.Max(0f, (float)days) * Mathf.Max(0f, recoverPerDay)));
    }

    /// <summary>
    /// The share of the full price a post pays for a good it has a glut of.
    /// </summary>
    /// <param name="glut">Stacks sold and not yet sold on.</param>
    /// <param name="dropPerStack">How much less it pays for each stack.</param>
    /// <param name="least">The least share it pays.</param>
    /// <returns>The share, from <paramref name="least"/> to 1.</returns>
    public static float Share(float glut, float dropPerStack, float least)
    {
        return Mathf.Clamp(1f - Mathf.Max(0f, glut) * Mathf.Max(0f, dropPerStack), Mathf.Clamp01(least), 1f);
    }

    /// <summary>
    /// The glut after a sale.
    /// </summary>
    /// <param name="glut">The glut now.</param>
    /// <param name="units">Pieces sold.</param>
    /// <param name="stack">Pieces in the stack the post sells the good in.</param>
    /// <returns>The new glut.</returns>
    public static float AfterSale(float glut, int units, int stack)
    {
        return Mathf.Max(0f, glut) + Mathf.Max(0, units) / (float)Mathf.Max(1, stack);
    }

    /// <summary>
    /// Reads a post's market from its network data.
    /// </summary>
    /// <param name="text">The text, as <see cref="Write"/> makes it.</param>
    /// <returns>The entries by prefab name; bad entries are left out.</returns>
    public static Dictionary<string, Entry> Read(string text)
    {
        Dictionary<string, Entry> market = new();
        if (string.IsNullOrEmpty(text))
        {
            return market;
        }

        foreach (string part in text.Split(EntrySeparator))
        {
            string[] fields = part.Split(FieldSeparator);
            if (fields.Length == 3 && fields[0].Length > 0
                && float.TryParse(fields[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float glut)
                && long.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out long ticks))
            {
                market[fields[0]] = new Entry(glut, ticks);
            }
        }

        return market;
    }

    /// <summary>
    /// Writes a post's market for its network data, leaving out the goods it no longer has a glut of.
    /// </summary>
    /// <param name="market">The entries by prefab name.</param>
    /// <returns>The text.</returns>
    public static string Write(IReadOnlyDictionary<string, Entry> market)
    {
        StringBuilder text = new();
        foreach (KeyValuePair<string, Entry> pair in market)
        {
            if (pair.Value.Glut <= 0.001f || pair.Key.IndexOf(EntrySeparator) >= 0 || pair.Key.IndexOf(FieldSeparator) >= 0)
            {
                continue;
            }

            if (text.Length > 0)
            {
                text.Append(EntrySeparator);
            }

            text.Append(pair.Key).Append(FieldSeparator)
                .Append(pair.Value.Glut.ToString("0.###", CultureInfo.InvariantCulture)).Append(FieldSeparator)
                .Append(pair.Value.Ticks.ToString(CultureInfo.InvariantCulture));
        }

        return text.ToString();
    }

    /// <summary>
    /// Whether the posts pay more after a storm: while it rages and for a day after it passed.
    /// </summary>
    /// <param name="raging">Whether a storm rages now.</param>
    /// <param name="secondsSinceStorm">Seconds since the last storm passed; negative if none has.</param>
    /// <param name="dayLength">Seconds in a game day.</param>
    /// <returns>True while the demand lasts.</returns>
    public static bool StormDemand(bool raging, double secondsSinceStorm, float dayLength)
    {
        return raging || (secondsSinceStorm >= 0.0 && secondsSinceStorm < dayLength);
    }
}
