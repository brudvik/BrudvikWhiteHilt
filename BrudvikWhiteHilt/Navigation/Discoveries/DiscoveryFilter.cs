using System;
using System.Collections.Generic;
using System.Linq;

namespace BrudvikWhiteHilt.Navigation.Discoveries;

/// <summary>
/// Which kinds of discoveries the local player has switched on. Nothing is shown until the player picks it; the choice
/// is saved with the character.
/// </summary>
public static class DiscoveryFilter
{
    private const string CustomDataKey = "whitehilt_discoveries_shown";

    private static readonly HashSet<string> shown = new();
    private static Player loadedFor;
    private static int version;

    /// <summary>
    /// Goes up every time the choice changes or another character plays, so drawings know to update.
    /// </summary>
    public static int Version
    {
        get
        {
            Load();
            return version;
        }
    }

    /// <summary>
    /// True if the kind is switched on.
    /// </summary>
    /// <param name="key">Kind key.</param>
    /// <returns>True if shown.</returns>
    public static bool IsShown(string key)
    {
        Load();
        return shown.Contains(key);
    }

    /// <summary>
    /// Switches one kind on or off.
    /// </summary>
    /// <param name="key">Kind key.</param>
    public static void Toggle(string key)
    {
        Load();
        if (!shown.Remove(key))
        {
            shown.Add(key);
        }

        Save();
    }

    /// <summary>
    /// Switches several kinds on or off together.
    /// </summary>
    /// <param name="keys">Kind keys.</param>
    /// <param name="on">True to show them.</param>
    public static void Set(IEnumerable<string> keys, bool on)
    {
        Load();
        foreach (string key in keys)
        {
            if (on)
            {
                shown.Add(key);
            }
            else
            {
                shown.Remove(key);
            }
        }

        Save();
    }

    private static void Load()
    {
        Player player = Player.m_localPlayer;
        if (player == loadedFor)
        {
            return;
        }

        loadedFor = player;
        shown.Clear();
        if (player != null && player.m_customData.TryGetValue(CustomDataKey, out string saved))
        {
            shown.UnionWith(saved.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
        }

        version++;
    }

    private static void Save()
    {
        if (loadedFor != null)
        {
            loadedFor.m_customData[CustomDataKey] = string.Join(",", shown.OrderBy(key => key, StringComparer.Ordinal));
        }

        version++;
    }
}
