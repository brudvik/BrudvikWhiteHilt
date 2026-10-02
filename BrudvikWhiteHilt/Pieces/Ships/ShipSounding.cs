using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships;

/// <summary>
/// The sounding line: the depth of the water under the ship for the ship's read-out, and a warning with the ship's
/// bell when shallow water or rocks lie ahead on the way it sails. Runs on this client only, for the one steering and
/// for those aboard a ship sailing its route.
/// </summary>
public static class ShipSounding
{
    // Metres between the depth samples ahead of the ship.
    private const float SampleStep = 5f;

    // Rays for rocks under water start this far above the water.
    private const float RayStart = 1f;

    private const string DangerColour = "#ff5a4a";

    private static float nextWarning;
    private static int rockMask;

    /// <summary>
    /// Sounds the water under and ahead of the ship and warns when it is dangerous.
    /// </summary>
    /// <param name="ship">The ship the player steers or rides.</param>
    /// <param name="speed">The ship's speed in m/s.</param>
    /// <returns>The depth line for the read-out, or null when the depth is not shown.</returns>
    public static string Update(Ship ship, float speed)
    {
        if (ZoneSystem.instance == null || WorldGenerator.instance == null)
        {
            return null;
        }

        bool showDepth = ShipSettings.ShowDepth.Value;
        bool warn = ShipSettings.ShoalWarning.Value;
        if (!showDepth && !warn)
        {
            return null;
        }

        string danger = warn && speed >= ShipSettings.ShoalMinSpeed.Value ? DangerAhead(ship, speed) : null;
        if (danger != null && Time.time >= nextWarning)
        {
            nextWarning = Time.time + ShipSettings.ShoalCooldown.Value;
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center, danger);
            if (ShipSettings.ShoalBell.Value)
            {
                ShipBell.Ring(ship.transform.position);
            }
        }

        if (!showDepth)
        {
            return null;
        }

        string line = string.Format(Localization.instance.Localize("$whitehilt_shiphud_depth"), Mathf.Max(0f, Depth(ship, ship.transform.position)).ToString("0.0"));
        return danger != null ? $"<color={DangerColour}>{line}</color>" : line;
    }

    /// <summary>
    /// Depth of the water at a point, down to the ground or to a rock under water, whichever is higher.
    /// </summary>
    /// <param name="ship">The ship, whose own hull does not count.</param>
    /// <param name="point">Where to sound; y is not used.</param>
    /// <returns>Metres of water; negative on land.</returns>
    public static float Depth(Ship ship, Vector3 point)
    {
        float water = ZoneSystem.instance.m_waterLevel;
        float ground = ZoneSystem.instance.GetGroundHeight(point, out float height) ? height : WorldGenerator.instance.GetHeight(point.x, point.z);
        if (rockMask == 0)
        {
            rockMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain");
        }

        float top = water + RayStart;
        if (ground < top)
        {
            foreach (RaycastHit hit in Physics.RaycastAll(new Vector3(point.x, top, point.z), Vector3.down, top - ground, rockMask, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.transform.IsChildOf(ship.transform))
                {
                    ground = Mathf.Max(ground, hit.point.y);
                }
            }
        }

        return water - ground;
    }

    // The warning text when shallows or rocks lie on the ship's way within the lookahead, or null.
    private static string DangerAhead(Ship ship, float speed)
    {
        Transform transform = ship.transform;
        Vector3 forward = ship.GetSpeed() < 0f ? -transform.forward : transform.forward;
        forward.y = 0f;
        forward.Normalize();
        float lookahead = Mathf.Clamp(speed * ShipSettings.ShoalLookaheadSeconds.Value, ShipSettings.ShoalMinLookahead.Value, ShipSettings.ShoalMaxLookahead.Value);
        float bow = ship.m_floatCollider != null ? ship.m_floatCollider.size.z * ship.m_floatCollider.transform.lossyScale.z / 2f : 0f;
        for (float distance = bow; distance <= bow + lookahead; distance += SampleStep)
        {
            if (Depth(ship, transform.position + forward * distance) < ShipSettings.ShoalDepth.Value)
            {
                return "$whitehilt_shoal_shallow";
            }
        }

        ShipAssist assist = ship.GetComponent<ShipAssist>();
        float course = Mathf.Repeat(Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg, 360f);
        return assist != null && assist.Blocked(course, lookahead) ? "$whitehilt_shoal_blocked" : null;
    }
}
