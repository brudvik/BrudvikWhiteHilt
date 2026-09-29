using BrudvikWhiteHilt.Backpack;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships;

/// <summary>
/// Sailing help on every ship: holding the course while nobody is at the helm. Added to each ship when it wakes.
/// The ship's owner steers, so the course is kept in the ship's ZDO for whoever owns it next.
/// </summary>
public class ShipAssist : MonoBehaviour
{
    private const string AutopilotRpc = "WhiteHiltAutopilot";
    private const string ShallowRpc = "WhiteHiltAutopilotShallow";
    private const float CheckInterval = 0.5f;

    // Degrees off course that give full rudder, and how much the turn rate damps it.
    private const float FullRudderDegrees = 25f;
    private const float TurnDamping = 0.8f;

    // The keel needs this much water; the lookahead grows with speed.
    private const float Draft = 2f;
    private const float LookaheadSeconds = 8f;
    private const float MinLookahead = 12f;
    private const float MaxLookahead = 60f;

    private static readonly int AutopilotKey = "whitehilt_autopilot".GetStableHashCode();
    private static readonly int CourseKey = "whitehilt_autopilot_course".GetStableHashCode();

    private ZNetView nview;
    private Ship ship;
    private Rigidbody body;
    private bool wasControlled;
    private float nextCheck;

    /// <summary>
    /// True while the ship holds its course when nobody is at the helm.
    /// </summary>
    public bool HoldingCourse => nview != null && nview.IsValid() && nview.GetZDO().GetBool(AutopilotKey);

    /// <summary>
    /// The course held, in degrees from north.
    /// </summary>
    public float Course => nview != null && nview.IsValid() ? nview.GetZDO().GetFloat(CourseKey) : 0f;

    /// <summary>
    /// Adds the help to a ship. Safe to call more than once.
    /// </summary>
    /// <param name="ship">The ship.</param>
    public static void Attach(Ship ship)
    {
        if (ship.GetComponent<ShipAssist>() == null)
        {
            ship.gameObject.AddComponent<ShipAssist>();
        }
    }

    /// <summary>
    /// Handles the hold-course key for the local player at a helm. Called every frame.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Tick(Player player)
    {
        if (BackpackInput.Typing() || !BackpackInput.Pressed(ShipSettings.KeyHoldCourse))
        {
            return;
        }

        Ship ship = player.GetControlledShip();
        ShipAssist assist = ship != null ? ship.GetComponent<ShipAssist>() : null;
        if (assist != null && assist.nview.IsValid())
        {
            assist.ToggleCourse(player);
        }
    }

    /// <summary>
    /// The ship's heading, in degrees from north.
    /// </summary>
    /// <param name="transform">The ship's transform.</param>
    /// <returns>The heading, 0 to 360.</returns>
    public static float Heading(Transform transform)
    {
        return Mathf.Repeat(transform.eulerAngles.y, 360f);
    }

    private void ToggleCourse(Player player)
    {
        bool on = !HoldingCourse;
        float course = Heading(transform);
        nview.InvokeRPC(AutopilotRpc, on, course);
        player.Message(MessageHud.MessageType.TopLeft, on
            ? string.Format(Localization.instance.Localize("$whitehilt_autopilot_on"), Mathf.RoundToInt(course))
            : "$whitehilt_autopilot_off");
    }

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        ship = GetComponent<Ship>();
        body = GetComponent<Rigidbody>();
        if (nview == null || nview.GetZDO() == null)
        {
            return;
        }

        nview.Register<bool, float>(AutopilotRpc, RPC_Autopilot);
        nview.Register(ShallowRpc, RPC_Shallow);
    }

    private void FixedUpdate()
    {
        if (ship == null || nview == null || !nview.IsValid() || !nview.IsOwner() || !HoldingCourse)
        {
            return;
        }

        if (ship.m_players.Count == 0)
        {
            nview.GetZDO().Set(AutopilotKey, false);
            return;
        }

        // At the helm the player steers; when they let go, the ship keeps the heading it has then.
        if (ship.HaveControllingPlayer())
        {
            wasControlled = true;
            return;
        }

        if (wasControlled)
        {
            wasControlled = false;
            nview.GetZDO().Set(CourseKey, Heading(transform));
        }

        float error = Mathf.DeltaAngle(Heading(transform), Course);
        float turnRate = body != null ? body.angularVelocity.y * Mathf.Rad2Deg : 0f;
        ship.m_rudderValue = Mathf.Clamp((error - turnRate * TurnDamping) / FullRudderDegrees, -1f, 1f);

        if (Time.time >= nextCheck)
        {
            nextCheck = Time.time + CheckInterval;
            if (ShallowAhead())
            {
                ship.m_speed = Ship.Speed.Stop;
                nview.GetZDO().Set(AutopilotKey, false);
                nview.InvokeRPC(ZNetView.Everybody, ShallowRpc);
            }
        }
    }

    private bool ShallowAhead()
    {
        float speed = ship.GetSpeed();
        if (speed < 0.5f || ZoneSystem.instance == null)
        {
            return false;
        }

        float distance = Mathf.Clamp(speed * LookaheadSeconds, MinLookahead, MaxLookahead);
        float water = ZoneSystem.instance.m_waterLevel;
        foreach (float part in new[] { 0.5f, 1f })
        {
            Vector3 point = transform.position + transform.forward * (distance * part);
            float ground = ZoneSystem.instance.GetGroundHeight(point, out float height)
                ? height
                : WorldGenerator.instance.GetHeight(point.x, point.z);
            if (ground > water - Draft)
            {
                return true;
            }
        }

        return false;
    }

    private void RPC_Autopilot(long sender, bool on, float course)
    {
        if (!nview.IsOwner())
        {
            return;
        }

        nview.GetZDO().Set(AutopilotKey, on);
        nview.GetZDO().Set(CourseKey, course);
        wasControlled = ship != null && ship.HaveControllingPlayer();
    }

    private void RPC_Shallow(long sender)
    {
        Player player = Player.m_localPlayer;
        if (player != null && ship != null && ship.IsPlayerInBoat(player))
        {
            player.Message(MessageHud.MessageType.Center, "$whitehilt_autopilot_shallow");
        }
    }
}
