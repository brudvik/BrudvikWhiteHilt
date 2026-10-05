using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BrudvikWhiteHilt.Quartermaster;

/// <summary>
/// The items a Quartermaster's Table keeps an eye on, each with the amount below which it counts as low. The list is
/// kept on the table, so everyone who uses it sees the same warnings.
/// </summary>
public static class StockWatch
{
    /// <summary>
    /// One watched item that is running low.
    /// </summary>
    public readonly struct Low
    {
        /// <summary>
        /// Creates a low-stock line.
        /// </summary>
        /// <param name="prefab">The item prefab name.</param>
        /// <param name="have">How many lie in the chests.</param>
        /// <param name="threshold">The amount below which it is low.</param>
        public Low(string prefab, int have, int threshold)
        {
            Prefab = prefab;
            Have = have;
            Threshold = threshold;
        }

        /// <summary>The item prefab name.</summary>
        public string Prefab { get; }

        /// <summary>How many lie in the chests.</summary>
        public int Have { get; }

        /// <summary>The amount below which it is low.</summary>
        public int Threshold { get; }
    }

    /// <summary>
    /// Reads a stored watch list: <c>Prefab=Threshold</c> entries separated by <c>;</c>.
    /// </summary>
    /// <param name="text">The stored text.</param>
    /// <returns>Threshold per item prefab name, in stored order.</returns>
    public static List<KeyValuePair<string, int>> Parse(string text)
    {
        List<KeyValuePair<string, int>> watches = new();
        foreach (string part in (text ?? string.Empty).Split(';'))
        {
            int separator = part.LastIndexOf('=');
            if (separator > 0 && int.TryParse(part.Substring(separator + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int threshold)
                && threshold > 0 && watches.All(watch => watch.Key != part.Substring(0, separator)))
            {
                watches.Add(new KeyValuePair<string, int>(part.Substring(0, separator), threshold));
            }
        }

        return watches;
    }

    /// <summary>
    /// Writes a watch list for storing.
    /// </summary>
    /// <param name="watches">Threshold per item prefab name.</param>
    /// <returns>The stored text.</returns>
    public static string Format(IEnumerable<KeyValuePair<string, int>> watches)
    {
        return string.Join(";", watches.Where(watch => watch.Value > 0)
            .Select(watch => watch.Key + "=" + watch.Value.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>
    /// The watched items below their threshold in the containers. An item a chest keeps without limit is never low.
    /// </summary>
    /// <param name="containers">The containers around the table.</param>
    /// <param name="watches">Threshold per item prefab name.</param>
    /// <returns>The low items, in watch order.</returns>
    public static List<Low> FindLow(List<Container> containers, List<KeyValuePair<string, int>> watches)
    {
        List<Low> low = new();
        foreach (KeyValuePair<string, int> watch in watches)
        {
            QuartermasterStore.CountStock(containers, watch.Key, out int have, out bool unlimited);
            if (!unlimited && have < watch.Value)
            {
                low.Add(new Low(watch.Key, have, watch.Value));
            }
        }

        return low;
    }
}
