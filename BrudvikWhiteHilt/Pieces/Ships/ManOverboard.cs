using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Navigation;
using BrudvikWhiteHilt.Pieces.Navigation;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships;

/// <summary>
/// Man overboard: a player who falls from a moving ship tells everyone aboard, who get a message, the bell, a pin on
/// the map and an arrow on the minimap's edge toward them. A ship sailing its route or holding its course stops. The
/// one in the water gets a pin and an arrow toward the ship. Someone on deck within reach can throw a lifeline that
/// pulls them back aboard. The player who falls watches for it on their own client and sends their position to all.
/// </summary>
public static class ManOverboard
{
    private const string OverboardRpc = "WhiteHiltOverboard";
    private const string LifelineRpc = "WhiteHiltLifeline";

    // How often the one in the water sends their position, and how long a silent alert is kept.
    private const float SendInterval = 2f;
    private const float AlertTimeout = 3f * SendInterval;

    // A swimmer counts as fallen from a ship they were aboard this recently, and is back once out of the water this long.
    private const float FallWindow = 3f;
    private const float BackAfter = 1f;
    private const float AboveDeck = 0.3f;

    private static readonly Color arrowColor = new(1f, 0.25f, 0.2f, 1f);
    private static readonly Dictionary<long, Alert> alerts = new();
    private static readonly MinimapEdgeArrow alertArrow = new("WhiteHiltOverboardArrow", arrowColor);
    private static readonly MinimapEdgeArrow shipArrow = new("WhiteHiltOverboardShipArrow", arrowColor);

    // The local player's own state.
    private static Ship lastShip;
    private static Vector3 lastShipPosition;
    private static float lastAboard;
    private static float shipSpeed;
    private static bool overboard;
    private static ZDOID overboardShip = ZDOID.None;
    private static Vector3 shipPosition;
    private static float overboardSince;
    private static float outOfWaterSince;
    private static float nextSend;
    private static Minimap.PinData shipPin;
    private static float pullAt;
    private static ZDOID puller = ZDOID.None;

    /// <summary>
    /// Registers the routed RPCs. Call once per game session.
    /// </summary>
    public static void RegisterRpcs()
    {
        alerts.Clear();
        overboard = false;
        ZRoutedRpc.instance?.Register<ZDOID, string, Vector3, bool>(OverboardRpc, RPC_Overboard);
        ZRoutedRpc.instance?.Register<ZDOID>(LifelineRpc, RPC_Lifeline);
    }

    /// <summary>
    /// Watches the local player for a fall, keeps the alerts up to date and throws the lifeline. Called every frame.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Tick(Player player)
    {
        if (!ShipSettings.ManOverboard.Value)
        {
            if (overboard)
            {
                End(player);
            }

            ClearAlerts();
            return;
        }

        WatchOwnFall(player);
        UpdateAlerts(player);
        if (TryGetLifelineTarget(player, out long peer, out _) && UsePressed(player))
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(peer, LifelineRpc, player.GetZDOID());
        }
    }

    /// <summary>
    /// Shows the lifeline prompt under the crosshair while it can be thrown. Called after the game's crosshair update.
    /// </summary>
    /// <param name="hud">The HUD.</param>
    /// <param name="player">The local player.</param>
    public static void UpdateCrosshair(Hud hud, Player player)
    {
        if (player != null && ShipSettings.ManOverboard.Value && TryGetLifelineTarget(player, out _, out string name))
        {
            hud.m_hoverName.text = string.Format(Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] $whitehilt_overboard_lifeline"), name);
            hud.m_crosshair.color = Color.yellow;
        }
    }

    // Notices the local player falling off a moving ship: swimming within moments of being aboard, while the ship went
    // fast enough. The ship's speed is measured from its movement, as a passenger's copy of the ship has no real
    // velocity. While overboard, the player's position is sent to everybody now and then.
    private static void WatchOwnFall(Player player)
    {
        float now = Time.time;
        Ship current = Ship.GetLocalShip();
        if (current != null && !player.IsSwimming())
        {
            if (current != lastShip)
            {
                lastShip = current;
                lastShipPosition = current.transform.position;
                shipSpeed = 0f;
            }

            lastAboard = now;
        }

        // Passengers do not have the ship's real velocity, so the speed is measured from its movement.
        if (lastShip != null && Time.deltaTime > 0f)
        {
            Vector3 position = lastShip.transform.position;
            shipSpeed = Mathf.Lerp(shipSpeed, Utils.DistanceXZ(position, lastShipPosition) / Time.deltaTime, 0.1f);
            lastShipPosition = position;
        }

        if (!overboard)
        {
            if (lastShip != null && player.IsSwimming() && now - lastAboard <= FallWindow && shipSpeed >= ShipSettings.OverboardMinSpeed.Value
                && !player.IsTeleporting() && !player.IsDead())
            {
                Begin(player);
            }

            return;
        }

        if (lastShip != null)
        {
            shipPosition = lastShip.transform.position;
        }

        if (player.IsSwimming())
        {
            outOfWaterSince = 0f;
        }
        else if (outOfWaterSince == 0f)
        {
            outOfWaterSince = now;
        }

        if (player.IsDead() || (outOfWaterSince > 0f && now - outOfWaterSince >= BackAfter) || now - overboardSince >= ShipSettings.OverboardTimeout.Value)
        {
            End(player);
            return;
        }

        if (pullAt > 0f && now >= pullAt)
        {
            PullAboard(player);
        }

        if (now >= nextSend)
        {
            nextSend = now + SendInterval;
            ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, OverboardRpc, overboardShip, player.GetPlayerName(), player.transform.position, true);
        }

        UpdateShipPin(player);
    }

    // Starts an overboard: remembers the ship, tells the player, and if the setting says so stops the ship's route and
    // held course so it does not sail away from them.
    private static void Begin(Player player)
    {
        ZNetView view = lastShip.m_nview;
        if (view == null || !view.IsValid())
        {
            return;
        }

        overboard = true;
        overboardShip = view.GetZDO().m_uid;
        shipPosition = lastShip.transform.position;
        overboardSince = Time.time;
        outOfWaterSince = 0f;
        nextSend = 0f;
        pullAt = 0f;
        player.Message(MessageHud.MessageType.Center, "$whitehilt_overboard_self");

        if (ShipSettings.OverboardStopShip.Value)
        {
            ShipRoute route = lastShip.GetComponent<ShipRoute>();
            if (route != null && route.Sailing)
            {
                route.Stop();
            }

            ShipAssist assist = lastShip.GetComponent<ShipAssist>();
            if (assist != null && (assist.HoldingCourse || (route != null && route.Sailing)))
            {
                assist.Halt();
            }
        }
    }

    private static void End(Player player)
    {
        overboard = false;
        pullAt = 0f;
        ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, OverboardRpc, overboardShip, player.GetPlayerName(), player.transform.position, false);
        overboardShip = ZDOID.None;
        RemovePin(ref shipPin);
        shipArrow.Hide();
    }

    private static void UpdateShipPin(Player player)
    {
        Minimap map = Minimap.instance;
        if (map == null)
        {
            return;
        }

        if (shipPin == null)
        {
            shipPin = map.AddPin(shipPosition, Minimap.PinType.Icon3, Translations.Word("whitehilt_overboard_ship_pin"), save: false, isChecked: false);
            shipPin.m_animate = true;
        }

        shipPin.m_pos = shipPosition;
        ShowEdgeArrow(map, shipArrow, player.transform.position, shipPosition);
    }

    // Moves the map pins of those overboard to where they were last heard of, forgets those not heard of for a while,
    // and points an arrow at the edge of the screen towards the nearest.
    private static void UpdateAlerts(Player player)
    {
        if (alerts.Count == 0)
        {
            alertArrow.Hide();
            return;
        }

        float now = Time.time;
        Alert nearest = null;
        float nearestDistance = float.MaxValue;
        List<long> gone = null;
        foreach (KeyValuePair<long, Alert> pair in alerts)
        {
            if (now - pair.Value.Heard > AlertTimeout)
            {
                (gone ??= new List<long>()).Add(pair.Key);
                continue;
            }

            pair.Value.Pin.m_pos = pair.Value.Position;
            float distance = Utils.DistanceXZ(player.transform.position, pair.Value.Position);
            if (distance < nearestDistance)
            {
                nearest = pair.Value;
                nearestDistance = distance;
            }
        }

        gone?.ForEach(RemoveAlert);
        Minimap map = Minimap.instance;
        if (nearest == null || map == null)
        {
            alertArrow.Hide();
            return;
        }

        ShowEdgeArrow(map, alertArrow, player.transform.position, nearest.Position);
    }

    private static void ShowEdgeArrow(Minimap map, MinimapEdgeArrow arrow, Vector3 from, Vector3 target)
    {
        bool small = map.m_mode == Minimap.MapMode.Small && map.m_smallRoot != null && map.m_smallRoot.activeInHierarchy;
        if (small && !map.IsPointVisible(target, map.m_mapImageSmall))
        {
            arrow.Show(map, from, target);
        }
        else
        {
            arrow.Hide();
        }
    }

    // A lifeline can be thrown to the nearest one in the water from the deck of the ship they fell from, within reach.
    private static bool TryGetLifelineTarget(Player player, out long peer, out string name)
    {
        peer = 0L;
        name = null;
        if (!ShipSettings.Lifeline.Value || alerts.Count == 0 || player.IsSwimming() || player.IsDead())
        {
            return false;
        }

        Ship ship = Ship.GetLocalShip();
        ZDOID shipId = ship != null && ship.m_nview != null && ship.m_nview.IsValid() ? ship.m_nview.GetZDO().m_uid : ZDOID.None;
        float best = ShipSettings.LifelineRange.Value;
        foreach (KeyValuePair<long, Alert> pair in alerts)
        {
            float distance = Vector3.Distance(player.transform.position, pair.Value.Position);
            if (pair.Value.Ship == shipId && distance <= best)
            {
                best = distance;
                peer = pair.Key;
                name = pair.Value.Name;
            }
        }

        return peer != 0L;
    }

    // Use with nothing under the crosshair but, at most, a player.
    private static bool UsePressed(Player player)
    {
        if (!ZInput.GetButtonDown("Use") && !ZInput.GetButtonDown("JoyUse"))
        {
            return false;
        }

        GameObject hover = player.GetHoverObject();
        return hover == null || hover.GetComponentInParent<Player>() != null;
    }

    // On every machine: someone fell overboard, moved, or is back aboard. Only those aboard the ship they fell from are
    // told, with a pin on the map, a message and the ship's bell.
    private static void RPC_Overboard(long sender, ZDOID shipId, string name, Vector3 position, bool active)
    {
        if (ZNet.instance == null || sender == ZDOMan.GetSessionID())
        {
            return;
        }

        if (!active)
        {
            RemoveAlert(sender);
            return;
        }

        if (alerts.TryGetValue(sender, out Alert alert))
        {
            alert.Position = position;
            alert.Heard = Time.time;
            return;
        }

        // Only those aboard the ship it fell from are told.
        Player player = Player.m_localPlayer;
        Ship ship = Ship.GetLocalShip();
        if (player == null || ship == null || ship.m_nview == null || !ship.m_nview.IsValid() || ship.m_nview.GetZDO().m_uid != shipId || Minimap.instance == null)
        {
            return;
        }

        Minimap.PinData pin = Minimap.instance.AddPin(position, Minimap.PinType.Icon3, name, save: false, isChecked: false);
        pin.m_animate = true;
        alerts[sender] = new Alert { Ship = shipId, Name = name, Position = position, Heard = Time.time, Pin = pin };
        player.Message(MessageHud.MessageType.Center, string.Format(Translations.Word("whitehilt_overboard_alert"), name));
        ShipBell.Ring(ship.transform.position);
    }

    private static void RPC_Lifeline(long sender, ZDOID rescuer)
    {
        Player player = Player.m_localPlayer;
        if (player == null || !overboard || pullAt > 0f || !ShipSettings.Lifeline.Value)
        {
            return;
        }

        GameObject thrower = ZNetScene.instance != null ? ZNetScene.instance.FindInstance(rescuer) : null;
        string name = thrower != null && thrower.TryGetComponent(out Player other) ? other.GetPlayerName() : string.Empty;
        puller = rescuer;
        pullAt = Time.time + ShipSettings.LifelineDelay.Value;
        player.Message(MessageHud.MessageType.Center, string.Format(Translations.Word("whitehilt_overboard_pulled"), name));
    }

    // Pulls the player who fell overboard up onto the deck beside the shipmate who threw the rope, moving with the
    // ship.
    private static void PullAboard(Player player)
    {
        pullAt = 0f;
        GameObject thrower = ZNetScene.instance != null ? ZNetScene.instance.FindInstance(puller) : null;
        if (thrower == null)
        {
            return;
        }

        Vector3 deck = thrower.transform.position + Vector3.up * AboveDeck;
        player.transform.position = deck;
        Rigidbody body = player.GetComponent<Rigidbody>();
        if (body != null)
        {
            Rigidbody shipBody = lastShip != null ? lastShip.GetComponent<Rigidbody>() : null;
            body.position = deck;
            body.linearVelocity = shipBody != null ? shipBody.linearVelocity : Vector3.zero;
        }
    }

    private static void RemoveAlert(long sender)
    {
        if (alerts.TryGetValue(sender, out Alert alert))
        {
            RemovePin(ref alert.Pin);
            alerts.Remove(sender);
        }
    }

    private static void ClearAlerts()
    {
        if (alerts.Count == 0)
        {
            return;
        }

        foreach (long sender in new List<long>(alerts.Keys))
        {
            RemoveAlert(sender);
        }

        alertArrow.Hide();
    }

    private static void RemovePin(ref Minimap.PinData pin)
    {
        if (pin != null && Minimap.instance != null)
        {
            Minimap.instance.RemovePin(pin);
        }

        pin = null;
    }

    private sealed class Alert
    {
        public ZDOID Ship;
        public string Name;
        public Vector3 Position;
        public float Heard;
        public Minimap.PinData Pin;
    }
}
