using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Navigation;

/// <summary>
/// An arrow on the edge of the minimap pointing toward a world position, with the distance. The minimap is north-up,
/// so the arrow turns by the bearing alone. Used by the ship route and the ruby amulet's target.
/// </summary>
public sealed class MinimapEdgeArrow
{
    private const float ArrowSize = 30f;
    private const float EdgeInset = 16f;
    private const float LabelInset = 40f;

    private readonly string name;
    private readonly Color color;
    private RectTransform arrow;
    private Text label;

    /// <summary>
    /// Creates an arrow; its UI is built the first time it is shown.
    /// </summary>
    /// <param name="name">Name of the arrow's GameObject.</param>
    /// <param name="color">Colour of the arrow.</param>
    public MinimapEdgeArrow(string name, Color color)
    {
        this.name = name;
        this.color = color;
    }

    /// <summary>
    /// Formats a distance as metres, or kilometres from 1 km.
    /// </summary>
    /// <param name="metres">The distance in metres.</param>
    /// <returns>The text.</returns>
    public static string FormatDistance(float metres)
    {
        return metres >= 1000f ? $"{metres / 1000f:0.0} km" : $"{Mathf.RoundToInt(metres)} m";
    }

    /// <summary>
    /// Shows the arrow on the minimap's edge toward the target.
    /// </summary>
    /// <param name="map">The map, in small mode.</param>
    /// <param name="from">The player's position, the minimap's centre.</param>
    /// <param name="target">Where the arrow points.</param>
    public void Show(Minimap map, Vector3 from, Vector3 target)
    {
        Ensure(map);
        SetVisible(true);
        float bearing = Mathf.Atan2(target.x - from.x, target.z - from.z);
        Vector2 direction = new(Mathf.Sin(bearing), Mathf.Cos(bearing));
        Rect rect = ((RectTransform)map.m_mapImageSmall.transform).rect;
        float radius = Mathf.Min(rect.width, rect.height) / 2f;

        arrow.anchoredPosition = direction * (radius - EdgeInset);
        arrow.localRotation = Quaternion.Euler(0f, 0f, -bearing * Mathf.Rad2Deg);
        label.rectTransform.anchoredPosition = direction * (radius - LabelInset);
        string text = FormatDistance(Utils.DistanceXZ(from, target));
        if (label.text != text)
        {
            label.text = text;
        }
    }

    /// <summary>
    /// Hides the arrow.
    /// </summary>
    public void Hide()
    {
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (arrow != null && arrow.gameObject.activeSelf != visible)
        {
            arrow.gameObject.SetActive(visible);
            label.gameObject.SetActive(visible);
        }
    }

    // Creates the arrow and its distance label on the small map, again when the map is rebuilt for another world.
    private void Ensure(Minimap map)
    {
        Transform parent = map.m_mapImageSmall.transform;
        if (arrow != null && arrow.parent == parent)
        {
            return;
        }

        Vector2 center = new(0.5f, 0.5f);
        GameObject go = new(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        arrow = (RectTransform)go.transform;
        arrow.anchorMin = center;
        arrow.anchorMax = center;
        arrow.sizeDelta = new Vector2(ArrowSize, ArrowSize);
        Image image = go.GetComponent<Image>();
        image.sprite = map.m_smallMarker != null ? map.m_smallMarker.GetComponent<Image>()?.sprite : null;
        image.color = color;
        image.preserveAspect = true;
        image.raycastTarget = false;

        label = GUIManager.Instance.CreateText(string.Empty, parent, center, center, Vector2.zero, GUIManager.Instance.AveriaSerifBold, 14,
            Color.white, true, Color.black, 80f, 20f, false).GetComponent<Text>();
        label.name = $"{name}Distance";
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
    }
}
