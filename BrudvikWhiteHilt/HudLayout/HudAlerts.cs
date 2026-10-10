using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.HudLayout;

/// <summary>
/// Warnings in the White Hilt HUD's context strip instead of a message in the middle of the screen: night falling, a
/// storm coming, and a raid or other event while it lasts. With the game's HUD they stay messages.
/// </summary>
public static class HudAlerts
{
    private const string AlertId = "whitehilt.alerts";
    private const string EventId = "whitehilt.event";
    private const int AlertPriority = 200;
    private const int EventPriority = 150;
    private const float EventDelay = 3f;
    private const string Separator = "   ·   ";

    private static readonly Color eventColor = new(1f, 0.45f, 0.4f);
    private static readonly List<Alert> alerts = new();

    /// <summary>
    /// Puts the warnings and events in the context strip. Call from the plugin's Awake.
    /// </summary>
    public static void Register()
    {
        WhiteHiltHud.RegisterContext(AlertId, AlertPriority, Line);
        WhiteHiltHud.RegisterContext(EventId, EventPriority, EventLine);
    }

    /// <summary>
    /// Warns the player: in the context strip with the White Hilt HUD, else as a message in the middle of the screen.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="text">The warning, localized or with $tokens.</param>
    /// <param name="color">Its colour in the strip.</param>
    /// <param name="seconds">How long it stays in the strip.</param>
    public static void Warn(Player player, string text, Color color, float seconds)
    {
        if (!HudSettings.WhiteHilt)
        {
            player?.Message(MessageHud.MessageType.Center, text);
            return;
        }

        string line = Localization.instance.Localize(text);
        alerts.RemoveAll(alert => alert.Text == line);
        alerts.Insert(0, new Alert(line, color, Time.unscaledTime + seconds));
    }

    // The warnings still showing, newest first, on one line.
    private static string Line()
    {
        float now = Time.unscaledTime;
        alerts.RemoveAll(alert => alert.Until <= now);
        if (alerts.Count == 0)
        {
            return null;
        }

        List<string> parts = new(alerts.Count);
        foreach (Alert alert in alerts)
        {
            parts.Add($"<color=#{ColorUtility.ToHtmlStringRGB(alert.Color)}>{alert.Text}</color>");
        }

        return string.Join(Separator, parts);
    }

    // The game's event bar, as the game shows it: a running raid or event once it has lasted a moment, unless a boss
    // bar shows.
    private static string EventLine()
    {
        RandomEvent active = RandEventSystem.instance != null ? RandEventSystem.instance.GetActiveEvent() : null;
        if (active == null || active.GetTime() <= EventDelay || (EnemyHud.instance != null && EnemyHud.instance.ShowingBossHud()))
        {
            return null;
        }

        return $"<color=#{ColorUtility.ToHtmlStringRGB(eventColor)}>{Localization.instance.Localize(active.GetHudText())}</color>";
    }

    private readonly struct Alert
    {
        internal readonly string Text;
        internal readonly Color Color;
        internal readonly float Until;

        internal Alert(string text, Color color, float until)
        {
            Text = text;
            Color = color;
            Until = until;
        }
    }
}
