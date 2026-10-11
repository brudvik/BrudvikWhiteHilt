using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.HudLayout;

/// <summary>
/// An icon another mod (or White Hilt itself) shows among the status effects in the White Hilt HUD: a counter, a mode,
/// anything that is not a status effect. Status effects need nothing: every effect the game shows on the HUD is there.
/// </summary>
public sealed class HudIndicator
{
    /// <summary>The icon; without one the indicator is not shown.</summary>
    public Sprite Icon;

    /// <summary>The name, shown beside the icon when the status effects are listed (in the inventory); may be empty.</summary>
    public string Name;

    /// <summary>Short text under the icon, such as a count or a time; may be empty.</summary>
    public string Text;

    /// <summary>Colour the icon is drawn in.</summary>
    public Color Tint = Color.white;

    /// <summary>Whether the icon flashes, as the game's status effects do when they need attention.</summary>
    public bool Flash;
}

/// <summary>
/// The White Hilt HUD for other mods. A mod registers a context line, shown in the strip over the console while it has
/// something to say (as the ship's speed and heading while steering), or an indicator among the status effects. Each
/// is read every frame while the White Hilt HUD shows, so the functions must be cheap; return null to show nothing.
/// Registering again with the same id replaces the earlier one.
/// </summary>
public static class WhiteHiltHud
{
    private static readonly List<Entry<string>> contexts = new();
    private static readonly List<Entry<HudIndicator>> indicators = new();

    /// <summary>Whether the player sees the White Hilt HUD, not the game's own.</summary>
    public static bool Active => HudSettings.WhiteHilt;

    /// <summary>
    /// Registers a context line for the strip over the console. Higher priorities come first; at most three lines show.
    /// </summary>
    /// <param name="id">A unique id, such as "mymod.fishing".</param>
    /// <param name="priority">Order among the lines; White Hilt's ship read-out is 100.</param>
    /// <param name="text">The line, Unity rich text allowed; null or empty when there is nothing to show.</param>
    public static void RegisterContext(string id, int priority, Func<string> text)
    {
        Register(contexts, id, priority, text);
    }

    /// <summary>
    /// Registers an indicator among the status effects, after them. Higher priorities come first.
    /// </summary>
    /// <param name="id">A unique id, such as "mymod.quiver".</param>
    /// <param name="priority">Order among the indicators.</param>
    /// <param name="read">The indicator; null, or one without an icon, when there is nothing to show.</param>
    public static void RegisterIndicator(string id, int priority, Func<HudIndicator> read)
    {
        Register(indicators, id, priority, read);
    }

    /// <summary>
    /// Removes a context line or indicator.
    /// </summary>
    /// <param name="id">The id it was registered with.</param>
    public static void Unregister(string id)
    {
        contexts.RemoveAll(entry => entry.Id == id);
        indicators.RemoveAll(entry => entry.Id == id);
    }

    /// <summary>
    /// The context lines that have something to show, highest priority first.
    /// </summary>
    /// <param name="into">Filled with the lines.</param>
    internal static void ReadContexts(List<string> into)
    {
        into.Clear();
        Read(contexts, into, text => !string.IsNullOrEmpty(text));
    }

    /// <summary>
    /// The indicators that have something to show, highest priority first.
    /// </summary>
    /// <param name="into">Filled with the indicators.</param>
    internal static void ReadIndicators(List<HudIndicator> into)
    {
        into.Clear();
        Read(indicators, into, indicator => indicator != null && indicator.Icon != null);
    }

    private static void Register<T>(List<Entry<T>> entries, string id, int priority, Func<T> read)
    {
        if (string.IsNullOrEmpty(id) || read == null)
        {
            throw new ArgumentException("A HUD entry needs an id and a function.");
        }

        entries.RemoveAll(entry => entry.Id == id);
        entries.Add(new Entry<T>(id, priority, read));
        entries.Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }

    // Reads every entry; one that throws is logged and dropped, so a broken mod cannot break the HUD every frame.
    private static void Read<T>(List<Entry<T>> entries, List<T> into, Predicate<T> shows)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            Entry<T> entry = entries[i];
            try
            {
                T value = entry.Read();
                if (shows(value))
                {
                    into.Add(value);
                }
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogWarning($"HUD: {entry.Id} failed and is removed: {ex.Message}");
                entries.RemoveAt(i);
                i--;
            }
        }
    }

    private sealed class Entry<T>
    {
        internal readonly string Id;
        internal readonly int Priority;
        internal readonly Func<T> Read;

        internal Entry(string id, int priority, Func<T> read)
        {
            Id = id;
            Priority = priority;
            Read = read;
        }
    }
}
