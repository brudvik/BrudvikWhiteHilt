using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Pieces.Navigation;

/// <summary>
/// An arrow on the edge of the minimap pointing to the ship's next route marker, with the distance. Shown only aboard a
/// ship with a Navigator's Table and markers. The minimap is north-up, so the arrow turns by the bearing alone.
/// </summary>
public static class RouteArrow
{
    private const float ArrowSize = 30f;
    private const float EdgeInset = 16f;
    private const float LabelInset = 40f;

    private static readonly Color arrowColor = new(1f, 0.6f, 0.1f, 1f);

    private static RectTransform arrow;
    private static Text label;

    /// <summary>
    /// Places or hides the arrow. Called every frame for the local player.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="route">The route of the ship the player is aboard, or null.</param>
    public static void Update(Player player, ShipRoute route)
    {
        Minimap map = Minimap.instance;
        bool show = route != null && route.Markers.Count > 0 && map != null && map.m_mode == Minimap.MapMode.Small
            && map.m_smallRoot != null && map.m_smallRoot.activeInHierarchy;
        if (!show)
        {
            SetVisible(false);
            return;
        }

        Ensure(map);
        SetVisible(true);
        Vector3 here = player.transform.position;
        Vector3 target = route.Markers[0];
        float bearing = Mathf.Atan2(target.x - here.x, target.z - here.z);
        Vector2 direction = new(Mathf.Sin(bearing), Mathf.Cos(bearing));
        Rect rect = ((RectTransform)map.m_mapImageSmall.transform).rect;
        float radius = Mathf.Min(rect.width, rect.height) / 2f;

        arrow.anchoredPosition = direction * (radius - EdgeInset);
        arrow.localRotation = Quaternion.Euler(0f, 0f, -bearing * Mathf.Rad2Deg);
        label.rectTransform.anchoredPosition = direction * (radius - LabelInset);
        float distance = Utils.DistanceXZ(here, target);
        string text = distance >= 1000f ? $"{distance / 1000f:0.0} km" : $"{Mathf.RoundToInt(distance)} m";
        if (label.text != text)
        {
            label.text = text;
        }
    }

    private static void SetVisible(bool visible)
    {
        if (arrow != null && arrow.gameObject.activeSelf != visible)
        {
            arrow.gameObject.SetActive(visible);
            label.gameObject.SetActive(visible);
        }
    }

    private static void Ensure(Minimap map)
    {
        Transform parent = map.m_mapImageSmall.transform;
        if (arrow != null && arrow.parent == parent)
        {
            return;
        }

        Vector2 center = new(0.5f, 0.5f);
        GameObject go = new("WhiteHiltRouteArrow", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        arrow = (RectTransform)go.transform;
        arrow.anchorMin = center;
        arrow.anchorMax = center;
        arrow.sizeDelta = new Vector2(ArrowSize, ArrowSize);
        Image image = go.GetComponent<Image>();
        image.sprite = map.m_smallMarker != null ? map.m_smallMarker.GetComponent<Image>()?.sprite : null;
        image.color = arrowColor;
        image.preserveAspect = true;
        image.raycastTarget = false;

        label = GUIManager.Instance.CreateText(string.Empty, parent, center, center, Vector2.zero, GUIManager.Instance.AveriaSerifBold, 14,
            Color.white, true, Color.black, 80f, 20f, false).GetComponent<Text>();
        label.name = "WhiteHiltRouteDistance";
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
    }
}
