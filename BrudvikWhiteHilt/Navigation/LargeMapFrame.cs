using UnityEngine;

namespace BrudvikWhiteHilt.Navigation;

/// <summary>
/// Lines panels up under the large map's bottom-left corner. The map and the panels live on different canvases, so
/// the corner goes through screen space.
/// </summary>
public static class LargeMapFrame
{
    private static readonly Vector3[] corners = new Vector3[4];

    /// <summary>
    /// Puts a panel under the large map: its left edge on the map's left edge plus an offset, the top of its collapsed
    /// height on the map's bottom edge. The pivot is the bottom-left corner, so a taller panel opens upwards over the map.
    /// </summary>
    /// <param name="map">The map.</param>
    /// <param name="panel">The panel; its parent must be a screen-space canvas or lie on one.</param>
    /// <param name="left">Pixels to the right of the map's left edge.</param>
    /// <param name="collapsedHeight">The panel's height when closed.</param>
    public static void PlaceBelow(Minimap map, RectTransform panel, float left, float collapsedHeight)
    {
        RectTransform parent = panel.parent as RectTransform;
        if (map.m_mapImageLarge == null || parent == null)
        {
            return;
        }

        map.m_mapImageLarge.rectTransform.GetWorldCorners(corners);
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(CameraOf(map.m_mapImageLarge.canvas), corners[0]);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, CameraOf(parent.GetComponentInParent<Canvas>()), out Vector2 corner))
        {
            return;
        }

        panel.anchorMin = panel.anchorMax = parent.pivot;
        panel.pivot = Vector2.zero;
        panel.anchoredPosition = corner + new Vector2(left, -collapsedHeight);
    }

    private static Camera CameraOf(Canvas canvas)
    {
        Canvas root = canvas != null ? canvas.rootCanvas : null;
        return root == null || root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
    }
}
