using UnityEngine;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// Lost at sea in the storm (<see cref="StormClient.LostAtSea"/>): the minimap is gone, the map shows neither you nor
/// your ship, and it opens somewhere else than where you are. The compass hides itself too
/// (<see cref="Navigation.Compass.HudCompass.ShouldShow"/>).
/// </summary>
public static class StormBearings
{
    private const float NearestOffset = 400f;
    private const float FarthestOffset = 900f;

    private static bool hidSmall;

    /// <summary>
    /// Hides the markers and the minimap while lost, and brings the minimap back after. Called after the game has
    /// placed the player's marker, every frame.
    /// </summary>
    /// <param name="map">The map.</param>
    public static void Apply(Minimap map)
    {
        bool lost = StormClient.LostAtSea;
        if (lost)
        {
            map.m_largeMarker.gameObject.SetActive(false);
            map.m_largeShipMarker.gameObject.SetActive(false);
            map.m_smallShipMarker.gameObject.SetActive(false);
        }

        bool small = map.m_mode == Minimap.MapMode.Small;
        if (lost && small && map.m_smallRoot.activeSelf)
        {
            map.m_smallRoot.SetActive(false);
            hidSmall = true;
        }
        else if (hidSmall && (!lost || !small))
        {
            if (small)
            {
                map.m_smallRoot.SetActive(true);
            }

            hidSmall = false;
        }
    }

    /// <summary>
    /// The map opened while lost is centred somewhere else, so where it opens does not give you away.
    /// </summary>
    /// <param name="map">The map.</param>
    public static void Opened(Minimap map)
    {
        if (StormClient.LostAtSea && map.m_mode == Minimap.MapMode.Large)
        {
            Vector2 away = Random.insideUnitCircle.normalized * Random.Range(NearestOffset, FarthestOffset);
            map.m_mapOffset = new Vector3(away.x, 0f, away.y);
        }
    }
}
