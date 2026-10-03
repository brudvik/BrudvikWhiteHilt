using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Navigation.Compass;

/// <summary>Reserves a top band for the compass without altering ordinary enemy health bars.</summary>
public static class HudCompassLayout
{
    private static readonly Dictionary<RectTransform, Vector2> bossPositions = new();
    private static readonly List<RectTransform> expired = new();

    /// <summary>Moves boss HUD instances below the compass and refreshes the clock and waypoint layout.</summary>
    public static void Update()
    {
        float reserved = HudCompass.ReservedHeight;
        EnemyHud hud = EnemyHud.instance;
        if (hud != null)
        {
            foreach (var entry in hud.m_huds)
            {
                if (entry.Key == null || !entry.Key.IsBoss() || entry.Value.m_isMount || entry.Value.m_gui == null) continue;
                RectTransform rect = entry.Value.m_gui.transform as RectTransform;
                if (rect == null) continue;
                if (!bossPositions.TryGetValue(rect, out Vector2 original))
                {
                    original = rect.anchoredPosition;
                    bossPositions.Add(rect, original);
                }
                rect.anchoredPosition = original + new Vector2(0f, -reserved);
            }
        }
        expired.Clear();
        foreach (var entry in bossPositions)
        {
            if (entry.Key == null) expired.Add(entry.Key);
            else if (reserved <= 0f) entry.Key.anchoredPosition = entry.Value;
        }
        foreach (RectTransform rect in expired) bossPositions.Remove(rect);
        Clock.GameClock.UpdateLayout();
        Waypoints.WaypointHud.UpdateLayout();
    }
}