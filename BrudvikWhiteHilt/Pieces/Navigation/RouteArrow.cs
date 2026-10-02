using BrudvikWhiteHilt.Navigation;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Navigation;

/// <summary>
/// An arrow on the edge of the minimap pointing to the ship's next route marker, with the distance. Shown only aboard a
/// ship with a Navigator's Table and markers.
/// </summary>
public static class RouteArrow
{
    private static readonly MinimapEdgeArrow arrow = new("WhiteHiltRouteArrow", new Color(1f, 0.6f, 0.1f, 1f));

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
        if (show)
        {
            arrow.Show(map, player.transform.position, route.Markers[0]);
        }
        else
        {
            arrow.Hide();
        }
    }
}
