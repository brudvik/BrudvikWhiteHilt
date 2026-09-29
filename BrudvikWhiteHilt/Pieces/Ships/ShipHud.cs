using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Pieces.Ships;

/// <summary>
/// Speed, heading and wind under the ship's wind indicator while steering, and the held course when it is on.
/// </summary>
public static class ShipHud
{
    private const float KnotsPerMetrePerSecond = 1.94384f;
    private const float RefreshSeconds = 0.2f;
    private const float Gap = 6f;

    private static Text label;
    private static float nextRefresh;

    /// <summary>
    /// Updates the read-out. Called after the game's ship HUD update.
    /// </summary>
    /// <param name="hud">The HUD.</param>
    /// <param name="player">The local player.</param>
    public static void Update(Hud hud, Player player)
    {
        Ship ship = player != null ? player.GetControlledShip() : null;
        bool show = ship != null && ShipSettings.ShowShipHud.Value && hud.m_shipWindIndicatorRoot != null;
        if (!show)
        {
            if (label != null && label.gameObject.activeSelf)
            {
                label.gameObject.SetActive(false);
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
        float heading = ShipAssist.Heading(ship.transform);
        string text = $"{Mathf.Abs(ship.GetSpeed()) * KnotsPerMetrePerSecond:0.0} kn   {Mathf.RoundToInt(heading) % 360:000}° {Compass(heading)}";
        if (EnvMan.instance != null)
        {
            Vector3 wind = -EnvMan.instance.GetWindDir();
            float from = Mathf.Repeat(Mathf.Atan2(wind.x, wind.z) * Mathf.Rad2Deg, 360f);
            text += "\n" + string.Format(Localization.instance.Localize("$whitehilt_shiphud_wind"), Compass(from));
        }

        ShipAssist assist = ship.GetComponent<ShipAssist>();
        if (assist != null && assist.HoldingCourse)
        {
            text += "\n" + string.Format(Localization.instance.Localize("$whitehilt_shiphud_course"), Mathf.RoundToInt(assist.Course) % 360);
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

    private static void Ensure(Hud hud)
    {
        RectTransform indicator = hud.m_shipWindIndicatorRoot;
        if (label != null && label.transform.parent == indicator.parent)
        {
            return;
        }

        GameObject go = GUIManager.Instance.CreateText(string.Empty, indicator.parent, indicator.anchorMin, indicator.anchorMax, Vector2.zero,
            GUIManager.Instance.AveriaSerifBold, 15, Color.white, true, Color.black, 240f, 66f, false);
        go.name = "WhiteHiltShipReadout";
        label = go.GetComponent<Text>();
        label.alignment = TextAnchor.UpperCenter;
        label.raycastTarget = false;
        RectTransform rect = label.rectTransform;
        rect.pivot = new Vector2(0.5f, 1f);
        float below = indicator.rect.height * indicator.pivot.y + Gap;
        rect.anchoredPosition = indicator.anchoredPosition + new Vector2(indicator.rect.width * (0.5f - indicator.pivot.x), -below);
    }
}
