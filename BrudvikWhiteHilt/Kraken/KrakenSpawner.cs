using UnityEngine;

namespace BrudvikWhiteHilt.Kraken;

/// <summary>
/// Places the Kraken beside a ship and its tentacles in a ring around it.
/// </summary>
public static class KrakenSpawner
{
    // From the ship's centre to the Kraken's origin; its arms reach under the hull.
    private const float BodyDistance = 12f;
    private const float TentacleRadiusMin = 6.5f;
    private const float TentacleRadiusMax = 9f;
    private const float TentacleSink = 0.6f;
    private const float MinWaterUnder = 6f;

    /// <summary>
    /// Raises the Kraken beside a ship.
    /// </summary>
    /// <param name="ship">The ship.</param>
    /// <param name="summoned">Whether a horn called this Kraken, allowing it to stay by day.</param>
    /// <returns>True if it rose.</returns>
    public static bool Spawn(Ship ship, bool summoned = false)
    {
        GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(KrakenRegistry.BodyName) : null;
        if (prefab == null || ship == null)
        {
            Jotunn.Logger.LogWarning($"Kraken: prefab {KrakenRegistry.BodyName} or ship not found");
            return false;
        }

        Vector3 centre = ship.transform.position;
        float side = Random.value < 0.5f ? -1f : 1f;
        for (int attempt = 0; attempt < 2; attempt++, side = -side)
        {
            Vector3 point = centre + ship.transform.right * (side * BodyDistance * Mathf.Max(1f, KrakenSettings.Scale.Value / 1.5f))
                + ship.transform.forward * Random.Range(-3f, 3f);
            if (!DeepEnough(point))
            {
                continue;
            }

            point.y = WaterLevel();
            Vector3 toShip = centre - point;
            toShip.y = 0f;
            GameObject kraken = Object.Instantiate(prefab, point, Quaternion.LookRotation(toShip));
            ZNetView nview = kraken.GetComponent<ZNetView>();
            nview.GetZDO().Set(KrakenBody.ShipKey, ship.m_nview.GetZDO().m_uid);
            nview.GetZDO().Set(KrakenBody.CrewKey, Mathf.Max(1, ship.m_players.Count));
            nview.GetZDO().Set(Items.Summoning.SummoningHornService.SummonedKey, summoned);
            Jotunn.Logger.LogInfo($"The Kraken rises at {point} beside the ship at {centre}");
            return true;
        }

        Jotunn.Logger.LogInfo("Kraken: the water beside the ship is too shallow");
        return false;
    }

    /// <summary>
    /// Raises the tentacles in a ring around the ship, leaving room on the Kraken's side.
    /// </summary>
    /// <param name="kraken">The Kraken.</param>
    /// <param name="ship">Its ship, or null to ring the Kraken itself.</param>
    public static void SpawnTentacles(KrakenBody kraken, Ship ship)
    {
        int count = KrakenSettings.Tentacles.Value;
        GameObject prefab = ZNetScene.instance.GetPrefab(KrakenRegistry.TentacleName);
        if (count <= 0 || prefab == null)
        {
            return;
        }

        Vector3 centre = ship != null ? ship.transform.position : kraken.transform.position;
        Vector3 away = centre - kraken.transform.position;
        away.y = 0f;
        float start = Mathf.Atan2(away.x, away.z) * Mathf.Rad2Deg - 120f;
        ZDOID body = kraken.GetComponent<ZNetView>().GetZDO().m_uid;
        for (int i = 0; i < count; i++)
        {
            // Spread over the 240 degrees away from the Kraken.
            float angle = start + 240f * (i + 0.5f) / count + Random.Range(-12f, 12f);
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            Vector3 point = centre + direction * Random.Range(TentacleRadiusMin, TentacleRadiusMax);
            if (!DeepEnough(point))
            {
                continue;
            }

            point.y = WaterLevel() - TentacleSink;
            GameObject tentacle = Object.Instantiate(prefab, point, Quaternion.LookRotation(-direction));
            tentacle.GetComponent<ZNetView>().GetZDO().Set(KrakenTentacle.BodyKey, body);
            tentacle.GetComponent<ZNetView>().GetZDO().Set(KrakenBody.CrewKey, kraken.GetComponent<ZNetView>().GetZDO().GetInt(KrakenBody.CrewKey, 1));
        }
    }

    /// <summary>
    /// Whether the sea is deep enough here for the Kraken.
    /// </summary>
    /// <param name="point">A point.</param>
    /// <param name="depth">Least depth.</param>
    /// <returns>True if the seabed is deep enough.</returns>
    public static bool DeepEnough(Vector3 point, float depth = MinWaterUnder)
    {
        return ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(point) < WaterLevel() - depth;
    }

    private static float WaterLevel()
    {
        return ZoneSystem.instance != null ? ZoneSystem.instance.m_waterLevel : 30f;
    }
}
