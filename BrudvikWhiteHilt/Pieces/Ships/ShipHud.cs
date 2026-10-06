using BrudvikWhiteHilt.Pieces.Navigation;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Pieces.Ships;

/// <summary>
/// Speed, heading, depth and wind under the ship's wind indicator while steering, and the held course when it is on. While
/// the ship sails a route on its own, the same for everyone aboard, with the time left to arrival.
/// </summary>
public static class ShipHud
{
    private const float KnotsPerMetrePerSecond = 1.94384f;
    private const float RefreshSeconds = 0.2f;
    private const float Gap = 6f;
    private const float MinEtaSpeed = 0.5f;
    private const float SpeedSmoothing = 0.3f;

    private static Text label;
    private static float nextRefresh;
    private static Ship measuredShip;
    private static Vector3 lastPosition;
    private static float lastTime;
    private static float measuredSpeed;

    /// <summary>
    /// Updates the read-out. Called after the game's ship HUD update.
    /// </summary>
    /// <param name="hud">The HUD.</param>
    /// <param name="player">The local player.</param>
    public static void Update(Hud hud, Player player)
    {
        Ship ship = player != null ? player.GetControlledShip() : null;
        ShipRoute route = null;
        if (ship == null && player != null)
        {
            route = ShipRoute.Aboard(player);
            ship = route != null && route.Sailing ? route.Ship : null;
        }

        bool show = ship != null && ShipSettings.ShowShipHud.Value && hud.m_shipWindIndicatorRoot != null;
        if (!show)
        {
            if (label != null && label.gameObject.activeSelf)
            {
                label.gameObject.SetActive(false);
            }

            // The shoal warning works without the read-out.
            if (ship != null && Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + RefreshSeconds;
                ShipSounding.Update(ship, route != null ? MeasureSpeed(ship) : Mathf.Abs(ship.GetSpeed()));
            }

            return;
        }

        Ensure(hud);
        if (!label.gameObject.activeSelf)
        {
            label.gameObject.SetActive(true);
        }

        if (Time.unscaledTime < nextRefresh)
        {
            return;
        }

        nextRefresh = Time.unscaledTime + RefreshSeconds;
        Place(hud);
        bool sailingRoute = route != null;

        // Only the ship's owner has its real velocity; passengers measure how far it moved.
        float speed = sailingRoute ? MeasureSpeed(ship) : Mathf.Abs(ship.GetSpeed());
        float heading = ShipAssist.Heading(ship.transform);
        string text = $"{speed * KnotsPerMetrePerSecond:0.0} kn   {Mathf.RoundToInt(heading) % 360:000}° {Compass(heading)}";
        string depth = ShipSounding.Update(ship, speed);
        if (depth != null)
        {
            text += "\n" + depth;
        }

        if (EnvMan.instance != null)
        {
            Vector3 wind = -EnvMan.instance.GetWindDir();
            float from = Mathf.Repeat(Mathf.Atan2(wind.x, wind.z) * Mathf.Rad2Deg, 360f);
            text += "\n" + string.Format(Localization.instance.Localize("$whitehilt_shiphud_wind"), Compass(from));
        }

        ShipAssist assist = ship.GetComponent<ShipAssist>();
        if (!sailingRoute && assist != null && assist.HoldingCourse)
        {
            text += "\n" + string.Format(Localization.instance.Localize("$whitehilt_shiphud_course"), Mathf.RoundToInt(assist.Course) % 360);
        }

        if (sailingRoute)
        {
            text += "\n" + string.Format(Localization.instance.Localize("$whitehilt_shiphud_eta"), Eta(route.Remaining(), speed));
        }

        if (label.text != text)
        {
            label.text = text;
        }
    }

    /// <summary>
    /// The compass point of a bearing, e.g. "NE".
    /// </summary>
    /// <param name="bearing">Degrees from north.</param>
    /// <returns>The localized compass point.</returns>
    public static string Compass(float bearing)
    {
        string[] points = Localization.instance.Localize("$whitehilt_compass").Split(',');
        int index = Mathf.RoundToInt(Mathf.Repeat(bearing, 360f) / 45f) % 8;
        return points.Length == 8 ? points[index] : string.Empty;
    }

    private static string Eta(float metres, float speed)
    {
        if (speed < MinEtaSpeed)
        {
            return "--:--";
        }

        int seconds = Mathf.RoundToInt(metres / speed);
        return $"{seconds / 60}:{seconds % 60:00}";
    }

    // The ship's speed over the water, measured from its movement and smoothed, as a passenger's copy of the ship has
    // no real velocity.
    private static float MeasureSpeed(Ship ship)
    {
        Vector3 position = ship.transform.position;
        float now = Time.time;
        if (ship != measuredShip || now - lastTime > 2f)
        {
            measuredShip = ship;
            lastPosition = position;
            lastTime = now;
            measuredSpeed = 0f;
            return 0f;
        }

        float elapsed = now - lastTime;
        if (elapsed < 0.1f)
        {
            return measuredSpeed;
        }

        float speed = Utils.DistanceXZ(position, lastPosition) / elapsed;
        measuredSpeed = measuredSpeed <= 0f ? speed : Mathf.Lerp(measuredSpeed, speed, SpeedSmoothing);
        lastPosition = position;
        lastTime = now;
        return measuredSpeed;
    }

    // Lives on the HUD root, not the ship HUD, which the game hides when nobody on it is at the helm.
    private static void Ensure(Hud hud)
    {
        if (label != null)
        {
            return;
        }

        GameObject go = GUIManager.Instance.CreateText(string.Empty, hud.m_rootObject.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, GUIManager.Instance.AveriaSerifBold, 15, Color.white, true, Color.black, 240f, 88f, false);
        go.name = "WhiteHiltShipReadout";
        label = go.GetComponent<Text>();
        label.alignment = TextAnchor.UpperCenter;
        label.raycastTarget = false;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.rectTransform.pivot = new Vector2(0.5f, 1f);
    }

    private static void Place(Hud hud)
    {
        RectTransform indicator = hud.m_shipWindIndicatorRoot;
        Rect rect = indicator.rect;
        Vector3 below = indicator.localPosition
            + new Vector3(rect.width * (0.5f - indicator.pivot.x), -(rect.height * indicator.pivot.y + Gap), 0f);
        label.rectTransform.position = indicator.parent.TransformPoint(below);
    }
}
