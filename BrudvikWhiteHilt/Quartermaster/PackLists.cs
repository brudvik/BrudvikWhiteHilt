using BepInEx;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace BrudvikWhiteHilt.Quartermaster;

/// <summary>
/// A named list of items and amounts to pack at a Quartermaster's Table, e.g. an outpost kit.
/// </summary>
public sealed class PackList
{
    /// <summary>Display name.</summary>
    public string Name = string.Empty;

    /// <summary>Amount per item prefab name, in the order they were added.</summary>
    public List<KeyValuePair<string, int>> Items = new();

    /// <summary>
    /// Adds to an item's amount, or adds the item.
    /// </summary>
    /// <param name="prefab">The item prefab name.</param>
    /// <param name="amount">The amount to add; negative takes away, and the item goes at 0.</param>
    public void Add(string prefab, int amount)
    {
        int index = Items.FindIndex(item => item.Key == prefab);
        int total = (index >= 0 ? Items[index].Value : 0) + amount;
        if (index >= 0 && total <= 0)
        {
            Items.RemoveAt(index);
        }
        else if (index >= 0)
        {
            Items[index] = new KeyValuePair<string, int>(prefab, total);
        }
        else if (total > 0)
        {
            Items.Add(new KeyValuePair<string, int>(prefab, total));
        }
    }
}

/// <summary>
/// Reads and writes the player's pack lists in BepInEx/config/BrudvikWhiteHilt/packlists.txt. The lists belong to the
/// player, not to a world, so the same outpost kit can be packed anywhere. The file is plain text: a line
/// <c># Name</c> starts a list, and each <c>Prefab=Amount</c> line below it is an item.
/// </summary>
public static class PackListStore
{
    /// <summary>The file.</summary>
    public static string FilePath => Path.Combine(Paths.ConfigPath, "BrudvikWhiteHilt", "packlists.txt");

    /// <summary>
    /// Reads every list, in file order.
    /// </summary>
    /// <returns>The lists.</returns>
    public static List<PackList> LoadAll()
    {
        if (!File.Exists(FilePath))
        {
            return new List<PackList>();
        }

        try
        {
            return Parse(File.ReadAllLines(FilePath, Encoding.UTF8));
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Could not read pack lists from {FilePath}: {ex.Message}");
            return new List<PackList>();
        }
    }

    /// <summary>
    /// Reads the lists from the file's lines. Items before the first list and broken lines are skipped.
    /// </summary>
    /// <param name="lines">The lines.</param>
    /// <returns>The lists.</returns>
    internal static List<PackList> Parse(IEnumerable<string> lines)
    {
        List<PackList> lists = new();
        PackList current = null;
        foreach (string raw in lines)
        {
            string line = raw.Trim();
            if (line.StartsWith("#", StringComparison.Ordinal))
            {
                current = new PackList { Name = line.Substring(1).Trim() };
                lists.Add(current);
                continue;
            }

            int separator = line.LastIndexOf('=');
            if (current == null || separator <= 0
                || !int.TryParse(line.Substring(separator + 1).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount))
            {
                continue;
            }

            current.Add(line.Substring(0, separator).Trim(), amount);
        }

        return lists;
    }

    /// <summary>
    /// Writes every list, replacing the file.
    /// </summary>
    /// <param name="lists">The lists.</param>
    public static void SaveAll(IEnumerable<PackList> lists)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            File.WriteAllText(FilePath, Format(lists), Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Could not write pack lists to {FilePath}: {ex.Message}");
        }
    }

    /// <summary>
    /// Writes the lists as the file's text.
    /// </summary>
    /// <param name="lists">The lists.</param>
    /// <returns>The text.</returns>
    internal static string Format(IEnumerable<PackList> lists)
    {
        StringBuilder text = new();
        foreach (PackList list in lists)
        {
            text.Append("# ").AppendLine(list.Name.Replace('\n', ' ').Replace('\r', ' '));
            foreach (KeyValuePair<string, int> item in list.Items.Where(item => item.Value > 0))
            {
                text.Append(item.Key).Append('=').AppendLine(item.Value.ToString(CultureInfo.InvariantCulture));
            }

            text.AppendLine();
        }

        return text.ToString();
    }
}
