using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Navigation.Waypoints;

/// <summary>
/// The arrow toward the target at the top of the screen, turned by where the camera looks, with the target's name and
/// distance under it. It pulses while the player moves the wrong way. Lives under the HUD, so it hides with it.
/// </summary>
public static class WaypointHud
{
    private const string RootName = "WhiteHiltWaypointHud";
    private const float LabelWidth = 300f;
    private const float LabelHeight = 44f;
    private const int FontSize = 16;
    private const float PulseSpeed = 8f;
    private const float PulseGrowth = 0.2f;

    private static readonly Color arrowColor = new(0.9f, 0.12f, 0.16f, 1f);
    private static readonly Color pulseColor = new(1f, 0.85f, 0.85f, 1f);

    private static RectTransform root;
    private static Image arrow;
    private static Text label;

    /// <summary>
    /// Hides the arrow.
    /// </summary>
    public static void Hide()
    {
        if (root != null && root.gameObject.activeSelf)
        {
            root.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Places the arrow, or hides it while the large map is open or the player turned it off.
    /// </summary>
    /// <param name="map">The map.</param>
    /// <param name="here">The player's position.</param>
    /// <param name="target">The target.</param>
    /// <param name="distance">Distance to the target in metres.</param>
    /// <param name="wrongWay">Whether the player moves away from the target.</param>
    public static void Update(Minimap map, Vector3 here, Vector3 target, float distance, bool wrongWay)
    {
        if (!WaypointSettings.HudArrow.Value || Hud.instance == null || GameCamera.instance == null || map.m_mode == Minimap.MapMode.Large)
        {
            Hide();
            return;
        }

        if (!Ensure(map))
        {
            return;
        }

        if (!root.gameObject.activeSelf)
        {
            root.gameObject.SetActive(true);
        }

        float size = WaypointSettings.HudArrowSize.Value;
        UpdateLayout();
        arrow.rectTransform.sizeDelta = new Vector2(size, size);
        arrow.rectTransform.anchoredPosition = new Vector2(0f, -size / 2f);
        label.rectTransform.anchoredPosition = new Vector2(0f, -size);

        float bearing = Mathf.Atan2(target.x - here.x, target.z - here.z) * Mathf.Rad2Deg;
        float facing = GameCamera.instance.transform.eulerAngles.y;
        arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, facing - bearing);

        float pulse = wrongWay ? 0.5f + 0.5f * Mathf.Sin(Time.time * PulseSpeed) : 0f;
        arrow.color = Color.Lerp(arrowColor, pulseColor, pulse);
        arrow.rectTransform.localScale = Vector3.one * (1f + PulseGrowth * pulse);

        string text = WaypointSettings.ShowDistance.Value ? $"{WaypointTarget.Name}\n{MinimapEdgeArrow.FormatDistance(distance)}" : WaypointTarget.Name;
        if (label.text != text)
        {
            label.text = text;
        }
    }

    internal static void UpdateLayout()
    {
        if (root == null) return;
        float reserved = Compass.HudCompass.ReservedHeight;
        float top = reserved > 0f ? Mathf.Max(WaypointSettings.HudArrowTop.Value,
            reserved + Clock.GameClock.BossOffset)
            : WaypointSettings.HudArrowTop.Value;
        root.anchoredPosition = new Vector2(0f, -top);
    }

    // Creates the arrow and label at the top of the HUD, again after the HUD is rebuilt for another world.
    private static bool Ensure(Minimap map)
    {
        Transform parent = Hud.instance.m_rootObject != null ? Hud.instance.m_rootObject.transform : null;
        if (parent == null)
        {
            return false;
        }

        if (root != null && root.parent == parent)
        {
            return true;
        }

        Vector2 top = new(0.5f, 1f);
        GameObject go = new(RootName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        root = (RectTransform)go.transform;
        root.anchorMin = top;
        root.anchorMax = top;
        root.pivot = top;
        root.sizeDelta = Vector2.zero;

        GameObject arrowObject = new("Arrow", typeof(RectTransform), typeof(Image));
        arrowObject.transform.SetParent(root, false);
        arrow = arrowObject.GetComponent<Image>();
        arrow.sprite = map.m_smallMarker != null ? map.m_smallMarker.GetComponent<Image>()?.sprite : null;
        arrow.preserveAspect = true;
        arrow.raycastTarget = false;
        arrow.rectTransform.anchorMin = top;
        arrow.rectTransform.anchorMax = top;
        arrow.rectTransform.pivot = new Vector2(0.5f, 0.5f);

        label = GUIManager.Instance.CreateText(string.Empty, root, top, top, Vector2.zero, GUIManager.Instance.AveriaSerifBold, FontSize,
            Color.white, true, Color.black, LabelWidth, LabelHeight, false).GetComponent<Text>();
        label.name = "Label";
        label.alignment = TextAnchor.UpperCenter;
        label.raycastTarget = false;
        label.rectTransform.pivot = top;
        return true;
    }
}
