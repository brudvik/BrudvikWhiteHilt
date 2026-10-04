using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Navigation.Discoveries;

/// <summary>
/// Which kinds of discoveries the local player has switched on. Nothing is shown until the player picks it; the choice
/// is saved with the character. While unlimited items are hidden, kinds the player has unlimited in chests stay off the
/// map, including kinds that become unlimited later, without losing their own choice.
/// </summary>
public static class DiscoveryFilter
{
    private const string CustomDataKey = "whitehilt_discoveries_shown";
    private const string HideUnlimitedKey = "whitehilt_discoveries_hide_unlimited";

    // Items become unlimited as chests fill; the map follows within this time.
    private const float UnlimitedRefreshSeconds = 2f;

    private static readonly HashSet<string> shown = new();
    private static readonly HashSet<string> unlimited = new();
    private static Player loadedFor;
    private static int version;
    private static bool hideUnlimited;
    private static float nextRefresh;

    /// <summary>
    /// Goes up every time the choice changes, another character plays or a shown kind becomes unlimited while unlimited
    /// items are hidden, so drawings know to update.
    /// </summary>
    public static int Version
    {
        get
        {
            Load();
            if (hideUnlimited && Time.time >= nextRefresh)
            {
                RefreshUnlimited();
            }

            return version;
        }
    }

    /// <summary>
    /// True while kinds the player has unlimited in chests are kept off the map.
    /// </summary>
    public static bool HideUnlimited
    {
        get
        {
            Load();
            return hideUnlimited;
        }
    }

    /// <summary>
    /// True if the kind shows on the map: switched on, and not hidden as unlimited.
    /// </summary>
    /// <param name="key">Kind key.</param>
    /// <returns>True if shown.</returns>
    public static bool IsShown(string key)
    {
        Load();
        return shown.Contains(key) && !IsHiddenAsUnlimited(key);
    }

    /// <summary>
    /// True if the kind is switched on but kept off the map because it is unlimited.
    /// </summary>
    /// <param name="key">Kind key.</param>
    /// <returns>True if hidden as unlimited.</returns>
    public static bool IsHiddenAsUnlimited(string key)
    {
        Load();
        return hideUnlimited && shown.Contains(key) && unlimited.Contains(key);
    }

    /// <summary>
    /// Starts or stops keeping unlimited kinds off the map.
    /// </summary>
    public static void ToggleHideUnlimited()
    {
        Load();
        hideUnlimited = !hideUnlimited;
        RefreshUnlimited();
        Save();
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

        RefreshUnlimited();
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

        RefreshUnlimited();
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
        hideUnlimited = false;
        if (player != null && player.m_customData.TryGetValue(CustomDataKey, out string saved))
        {
            shown.UnionWith(saved.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
        }

        if (player != null && player.m_customData.TryGetValue(HideUnlimitedKey, out string hide))
        {
            hideUnlimited = hide == "1";
        }

        RefreshUnlimited();
        version++;
    }

    // Only kinds switched on matter, so only they are looked up.
    private static void RefreshUnlimited()
    {
        nextRefresh = Time.time + UnlimitedRefreshSeconds;
        if (!hideUnlimited)
        {
            unlimited.Clear();
            return;
        }

        HashSet<string> now = new(shown.Where(DiscoveryPanel.IsUnlimited));
        if (!now.SetEquals(unlimited))
        {
            unlimited.Clear();
            unlimited.UnionWith(now);
            version++;
        }
    }

    private static void Save()
    {
        if (loadedFor != null)
        {
            loadedFor.m_customData[CustomDataKey] = string.Join(",", shown.OrderBy(key => key, StringComparer.Ordinal));
            loadedFor.m_customData[HideUnlimitedKey] = hideUnlimited ? "1" : "0";
        }

        version++;
    }
}
