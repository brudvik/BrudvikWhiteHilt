using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Navigation.Compass;

/// <summary>Reserves a top band for the compass without altering ordinary enemy health bars.</summary>
public static class HudCompassLayout
{
    private static readonly Dictionary<RectTransform, Vector2> bossPositions = new();
    private static readonly List<RectTransform> expired = new();

    private static int laidOutFrame = -1;

    /// <summary>
    /// Moves boss HUD instances below the compass and refreshes the clock and waypoint layout. Called after the camera
    /// and after the enemy HUD each frame: the boss bars are placed both times, since the enemy HUD may make one after
    /// the camera, but a bar is only written when it moves, and the clock and waypoint are laid out once a frame.
    /// </summary>
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
                Vector2 lowered = original + new Vector2(0f, -reserved);
                if (rect.anchoredPosition != lowered) rect.anchoredPosition = lowered;
            }
        }
        expired.Clear();
        foreach (var entry in bossPositions)
        {
            if (entry.Key == null) expired.Add(entry.Key);
            else if (reserved <= 0f && entry.Key.anchoredPosition != entry.Value) entry.Key.anchoredPosition = entry.Value;
        }
        foreach (RectTransform rect in expired) bossPositions.Remove(rect);
        if (laidOutFrame == Time.frameCount) return;
        laidOutFrame = Time.frameCount;
        Clock.GameClock.UpdateLayout();
        Waypoints.WaypointHud.UpdateLayout();
    }
}