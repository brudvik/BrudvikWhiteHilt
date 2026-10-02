using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Navigation;
using BrudvikWhiteHilt.Navigation.Dowsing;
using BrudvikWhiteHilt.Pieces.Navigation;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Navigation.Waypoints;

/// <summary>
/// The ruby amulet's target: Shift + click on the large map sets or removes it, and while the amulet is worn an arrow
/// on the screen and on the minimap leads there. Reaching it removes it; moving away from it for a while brings a
/// warning. Everything runs on the local client only.
/// </summary>
public static class WaypointGuide
{
    // How often the player's position is sampled to tell the way they move.
    private const float SampleInterval = 0.25f;

    private static readonly List<(float Time, Vector3 Position)> samples = new();
    private static float nextSample;
    private static float nextWarning;

    /// <summary>Whether the player has been moving away from the target.</summary>
    public static bool WrongWay { get; private set; }

    /// <summary>
    /// True when a click on the large map should set the target instead of what vanilla does: Shift is held, the ruby
    /// amulet is worn and no ship route is being planned.
    /// </summary>
    /// <returns>True for a target click.</returns>
    public static bool IsTargetClick()
    {
        return (ZInput.GetKey(KeyCode.LeftShift) || ZInput.GetKey(KeyCode.RightShift))
            && !ShipRoutePlanner.Planning && RubyPathfinderAmulet.WearsRuby(Player.m_localPlayer);
    }

    /// <summary>
    /// Sets the target where the large map was clicked, on the pin there if any, or removes it when the click is on it.
    /// </summary>
    /// <param name="map">The map.</param>
    /// <param name="world">World position under the cursor.</param>
    public static void OnMapClick(Minimap map, Vector3 world)
    {
        Player player = Player.m_localPlayer;
        if (player == null)
        {
            return;
        }

        float radius = map.PinInteractRadius;
        if (WaypointTarget.Refresh(player) && Utils.DistanceXZ(world, WaypointTarget.Position) < radius)
        {
            WaypointTarget.Clear(player);
            player.Message(MessageHud.MessageType.TopLeft, Translations.Word("msg_whitehilt_waypoint_cleared"));
            return;
        }

        Minimap.PinData pin = map.GetClosestPin(world, radius);
        string name = pin != null && !string.IsNullOrEmpty(pin.m_name) ? Localization.instance.Localize(pin.m_name) : Translations.Word("whitehilt_waypoint_default");
        WaypointTarget.Set(player, pin?.m_pos ?? world, name);
        samples.Clear();
        player.Message(MessageHud.MessageType.TopLeft, string.Format(Translations.Word("msg_whitehilt_waypoint_set"), name));
    }

    /// <summary>
    /// Updates the arrows, the marker, the arrival and the wrong-way warning. Called after the map's own update.
    /// </summary>
    /// <param name="map">The map.</param>
    public static void Update(Minimap map)
    {
        Player player = Player.m_localPlayer;
        if (player == null || player.IsDead() || !RubyPathfinderAmulet.WearsRuby(player) || !WaypointTarget.Refresh(player))
        {
            Hide();
            return;
        }

        Vector3 here = player.transform.position;
        Vector3 target = WaypointTarget.Position;
        float distance = Utils.DistanceXZ(here, target);
        if (distance <= WaypointSettings.ArrivalRadius.Value)
        {
            player.Message(MessageHud.MessageType.Center, string.Format(Translations.Word("msg_whitehilt_waypoint_arrived"), WaypointTarget.Name));
            WaypointTarget.Clear(player);
            Hide();
            return;
        }

        UpdateWrongWay(player, here, target, distance);
        WaypointHud.Update(map, here, target, distance, WrongWay);
        WaypointMap.Update(map, here, target);
    }

    private static void Hide()
    {
        WrongWay = false;
        samples.Clear();
        WaypointHud.Hide();
        WaypointMap.Hide();
    }

    // The way the player moves is measured over the last WrongWaySeconds, so looking around or a short detour is not enough.
    private static void UpdateWrongWay(Player player, Vector3 here, Vector3 target, float distance)
    {
        if (player.IsTeleporting())
        {
            samples.Clear();
            WrongWay = false;
            return;
        }

        float now = Time.time;
        if (now >= nextSample || samples.Count == 0)
        {
            samples.Add((now, here));
            nextSample = now + SampleInterval;
        }

        float window = WaypointSettings.WrongWaySeconds.Value;
        while (samples.Count > 1 && now - samples[1].Time >= window)
        {
            samples.RemoveAt(0);
        }

        float age = now - samples[0].Time;
        Vector3 moved = here - samples[0].Position;
        moved.y = 0f;
        Vector3 toward = target - here;
        toward.y = 0f;
        WrongWay = age >= window && distance > WaypointSettings.WrongWayMinDistance.Value
            && moved.magnitude >= WaypointSettings.WrongWayMinSpeed.Value * age
            && Vector3.Angle(moved, toward) >= WaypointSettings.WrongWayAngle.Value;
        if (WrongWay && now >= nextWarning)
        {
            nextWarning = now + WaypointSettings.WrongWayCooldown.Value;
            player.Message(MessageHud.MessageType.Center,
                string.Format(Translations.Word("msg_whitehilt_waypoint_wrongway"), WaypointTarget.Name, StoneDowsingService.Direction(toward)));
        }
    }
}
