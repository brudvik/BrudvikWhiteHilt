using BrudvikWhiteHilt.Backpack;
using BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships;

/// <summary>
/// Sailing help on every ship: holding the course while nobody is at the helm, and pushing a stranded ship from the
/// shore. Added to each ship when it wakes. The ship's owner steers and moves it, so the course is kept in the ship's
/// ZDO for whoever owns it next.
/// </summary>
public class ShipAssist : MonoBehaviour, Hoverable, Interactable
{
    private const string AutopilotRpc = "WhiteHiltAutopilot";
    private const string ShallowRpc = "WhiteHiltAutopilotShallow";
    private const string PushRpc = "WhiteHiltShipPush";
    private const float CheckInterval = 0.5f;

    // A push gives the ship this speed away from the pusher, and a little lift off the ground.
    private const float PushSpeed = 2.5f;
    private const float PushLift = 0.6f;
    private const float PushInterval = 0.5f;
    private const float MaxPushableSpeed = 1.5f;

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
    private WhiteHiltShipUpgrades upgrades;
    private bool wasControlled;
    private float nextCheck;
    private float nextPush;

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

    /// <summary>
    /// Offers to push the ship to a player standing next to it on shore while it lies still.
    /// </summary>
    /// <returns>The hover text, empty when the ship can not be pushed.</returns>
    public string GetHoverText()
    {
        return CanPush(Player.m_localPlayer) ? Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] $whitehilt_ship_push") : string.Empty;
    }

    /// <inheritdoc/>
    public string GetHoverName()
    {
        return string.Empty;
    }

    /// <inheritdoc/>
    public float GetHoverOffset()
    {
        return 0f;
    }

    /// <summary>
    /// Pushes the ship away from the player; holding the key keeps pushing.
    /// </summary>
    /// <param name="user">The player.</param>
    /// <param name="hold">True while the key is held.</param>
    /// <param name="alt">True with the alternative key.</param>
    /// <returns>True if the ship was pushed.</returns>
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (user is not Player player || !CanPush(player) || Time.time < nextPush)
        {
            return false;
        }

        nextPush = Time.time + PushInterval;
        Vector3 away = transform.position - player.transform.position;
        away.y = 0f;
        nview.InvokeRPC(PushRpc, away.normalized);
        return true;
    }

    /// <inheritdoc/>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        return false;
    }

    private bool CanPush(Player player)
    {
        return player != null && ship != null && nview != null && nview.IsValid() && !ship.IsPlayerInBoat(player)
            && Mathf.Abs(ship.GetSpeed()) < MaxPushableSpeed && (upgrades == null || !upgrades.IsAnchored);
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
        upgrades = GetComponent<WhiteHiltShipUpgrades>();
        if (nview == null || nview.GetZDO() == null)
        {
            return;
        }

        nview.Register<bool, float>(AutopilotRpc, RPC_Autopilot);
        nview.Register(ShallowRpc, RPC_Shallow);
        nview.Register<Vector3>(PushRpc, RPC_Push);
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

        SteerTowards(Course);

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

    /// <summary>
    /// Turns the rudder toward a heading, damped by how fast the ship already turns. Only on the ship's owner.
    /// </summary>
    /// <param name="course">The heading, in degrees from north.</param>
    public void SteerTowards(float course)
    {
        float error = Mathf.DeltaAngle(Heading(transform), course);
        float turnRate = body != null ? body.angularVelocity.y * Mathf.Rad2Deg : 0f;
        ship.m_rudderValue = Mathf.Clamp((error - turnRate * TurnDamping) / FullRudderDegrees, -1f, 1f);
    }

    /// <summary>
    /// Stops holding the course. Only on the ship's owner.
    /// </summary>
    public void StopHolding()
    {
        if (nview != null && nview.IsValid() && nview.IsOwner())
        {
            nview.GetZDO().Set(AutopilotKey, false);
        }
    }

    /// <summary>
    /// True if the water ahead is too shallow for the keel, looking further the faster the ship sails.
    /// </summary>
    /// <param name="maxDistance">Looks no further than this.</param>
    /// <returns>True before shallow water.</returns>
    public bool ShallowAhead(float maxDistance = float.MaxValue)
    {
        float speed = ship.GetSpeed();
        if (speed < 0.5f || ZoneSystem.instance == null)
        {
            return false;
        }

        float distance = Mathf.Min(Mathf.Clamp(speed * LookaheadSeconds, MinLookahead, MaxLookahead), maxDistance);
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

    private void RPC_Push(long sender, Vector3 direction)
    {
        if (!nview.IsOwner() || body == null)
        {
            return;
        }

        body.WakeUp();
        body.AddForce(direction.normalized * PushSpeed + Vector3.up * PushLift, ForceMode.VelocityChange);
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
