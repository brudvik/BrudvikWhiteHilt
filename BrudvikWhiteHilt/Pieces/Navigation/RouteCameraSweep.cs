using BrudvikWhiteHilt.Pieces.Ships;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Navigation;

/// <summary>
/// Swings the camera around a ship as it sets off on a route: out past the front of the sail and round to behind
/// the player again. Runs on each client for the local player, if they sit aboard; nothing is sent over the network.
/// </summary>
public static class RouteCameraSweep
{
    // Vanilla's look speed for a fully pushed right stick, in degrees per second.
    private const float StickDegreesPerSecond = 110f;

    // Same sphere radius as vanilla's own camera collision.
    private const float CollisionRadius = 0.2f;

    private static Ship ship;
    private static bool running;
    private static float elapsed;
    private static float startBearing;
    private static float holdAngle;
    private static float direction;
    private static float weight;
    private static float cancelElapsed = -1f;
    private static float cancelFrom;
    private static float lookTurned;
    private static bool hudHidden;
    private static bool savedHudHidden;
    private static int blockMask;

    /// <summary>
    /// Starts the sweep around a ship that has just set off, if the local player sits aboard it.
    /// </summary>
    /// <param name="target">The ship.</param>
    public static void Begin(Ship target)
    {
        Player player = Player.m_localPlayer;
        GameCamera camera = GameCamera.instance;
        if (!ShipSettings.RouteCameraSweep.Value || target == null || camera == null || GameCamera.InFreeFly()
            || player == null || player.IsDead() || !target.IsPlayerInBoat(player) || !ShipRoute.Seated(player))
        {
            return;
        }

        End();
        ship = target;
        running = true;
        elapsed = 0f;
        weight = 0f;
        cancelElapsed = -1f;
        lookTurned = 0f;
        startBearing = Bearing(camera.transform.position - Focus(target)) - target.transform.eulerAngles.y;

        // Round the shorter way to the bow, stopping the set angle short of it so the figurehead and mast leave the sail free.
        float toFront = Mathf.DeltaAngle(startBearing, 0f);
        direction = toFront >= 0f ? 1f : -1f;
        holdAngle = Mathf.Max(0f, Mathf.Abs(toFront) - ShipSettings.RouteCameraSweepAngle.Value);

        if (ShipSettings.RouteCameraSweepHideHud.Value && Hud.instance != null)
        {
            savedHudHidden = Hud.instance.m_userHidden;
            Hud.instance.m_userHidden = true;
            hudHidden = true;
        }
    }

    /// <summary>
    /// Moves the camera along the sweep, blended with where vanilla put it this frame. Call after GameCamera.UpdateCamera.
    /// </summary>
    /// <param name="camera">The game camera.</param>
    public static void Apply(GameCamera camera)
    {
        if (!running)
        {
            return;
        }

        Player player = Player.m_localPlayer;
        if (ship == null || camera == null || player == null || GameCamera.InFreeFly() || !ShipSettings.RouteCameraSweep.Value)
        {
            End();
            return;
        }

        float dt = Time.unscaledDeltaTime;
        elapsed += dt;
        if (cancelElapsed < 0f && Interrupted(player, dt))
        {
            cancelElapsed = 0f;
            cancelFrom = weight;
            RestoreHud();
        }

        float total = ShipSettings.RouteCameraSweepSeconds.Value;
        float hold = Mathf.Min(ShipSettings.RouteCameraSweepHoldSeconds.Value, total * 0.5f);
        float move = total - hold;
        float first = move * holdAngle / 360f;
        float second = move - first;

        if (cancelElapsed >= 0f)
        {
            cancelElapsed += dt;
            float fade = ShipSettings.RouteCameraSweepCancelSeconds.Value;
            weight = cancelFrom * (1f - Mathf.SmoothStep(0f, 1f, cancelElapsed / fade));
            if (cancelElapsed >= fade)
            {
                End();
                return;
            }
        }
        else
        {
            if (elapsed >= total)
            {
                End();
                return;
            }

            float easeIn = Mathf.SmoothStep(0f, 1f, elapsed / Mathf.Max(first * 0.5f, move * 0.15f));
            float easeOut = Mathf.SmoothStep(0f, 1f, (total - elapsed) / (second * 0.5f));
            weight = Mathf.Min(easeIn, easeOut);
        }

        if (weight <= 0f)
        {
            return;
        }

        Place(camera, Angle(first, hold, second));
    }

    private static float Angle(float first, float hold, float second)
    {
        if (elapsed < first)
        {
            return holdAngle * Mathf.SmoothStep(0f, 1f, elapsed / first);
        }

        if (elapsed < first + hold)
        {
            return holdAngle;
        }

        return holdAngle + (360f - holdAngle) * Mathf.SmoothStep(0f, 1f, (elapsed - first - hold) / second);
    }

    // Blends vanilla's camera and the orbit in bearing, distance and height around the sail, so it never cuts across the ship.
    private static void Place(GameCamera camera, float angle)
    {
        Transform transform = camera.transform;
        Vector3 focus = Focus(ship);
        float yaw = ship.transform.eulerAngles.y;
        Vector3 offset = transform.position - focus;
        float vanillaBearing = Bearing(offset) - yaw;
        float vanillaRadius = new Vector2(offset.x, offset.z).magnitude;

        float length = ShipLength(ship);
        float radius = Mathf.Max(length * ShipSettings.RouteCameraSweepDistance.Value, camera.m_maxDistanceBoat);
        float height = length * ShipSettings.RouteCameraSweepHeight.Value;

        float path = startBearing + direction * angle;
        float bearing = path + (1f - weight) * Mathf.DeltaAngle(path, vanillaBearing);
        float distance = Mathf.Lerp(vanillaRadius, radius, weight);
        float up = Mathf.Lerp(offset.y, height, weight);

        Vector3 position = focus + Quaternion.Euler(0f, yaw + bearing, 0f) * Vector3.forward * distance + Vector3.up * up;
        position = KeepClear(camera, focus, position);
        Vector3 look = focus - position;
        Quaternion rotation = look.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(look) : transform.rotation;
        transform.SetPositionAndRotation(position, Quaternion.Slerp(transform.rotation, rotation, weight));
    }

    // Pulls the camera in front of land and rocks between it and the sail, and keeps it above the water.
    private static Vector3 KeepClear(GameCamera camera, Vector3 focus, Vector3 position)
    {
        if (blockMask == 0)
        {
            blockMask = LayerMask.GetMask("terrain", "static_solid");
        }

        Vector3 offset = position - focus;
        float distance = offset.magnitude;
        if (distance > CollisionRadius && Physics.SphereCast(focus, CollisionRadius, offset / distance, out RaycastHit hit, distance, blockMask))
        {
            position = focus + offset / distance * hit.distance;
        }

        float water = Floating.GetLiquidLevel(position) + camera.m_minWaterDistance;
        if (position.y < water)
        {
            position.y = water;
        }

        return position;
    }

    private static bool Interrupted(Player player, float dt)
    {
        return player.IsDead() || !ship.IsPlayerInBoat(player) || !ShipRoute.Seated(player) || ship.HaveControllingPlayer()
            || Menu.IsVisible() || InventoryGui.IsVisible() || Minimap.IsOpen() || TurnedQuickly(dt);
    }

    // Degrees the view turned, leaking away at the limit per second: looking around calmly never adds up, a quick swing does.
    private static bool TurnedQuickly(float dt)
    {
        float limit = ShipSettings.RouteCameraSweepCancelLook.Value;
        Vector2 stick = new(ZInput.GetJoyRightStickX(), ZInput.GetJoyRightStickY());
        float turned = ZInput.GetMouseDelta().magnitude * PlayerController.m_mouseSens
            + stick.magnitude * StickDegreesPerSecond * dt * PlayerController.m_gamepadSens;
        lookTurned = Mathf.Max(0f, lookTurned - limit * dt) + turned;
        return lookTurned > limit;
    }

    // The middle of the sail when it is fully set, between where its lower edge hangs furled and unfurled.
    private static Vector3 Focus(Ship target)
    {
        if (target.m_hasSail && target.m_sailFurledPosition != null && target.m_sailUnfurledPosition != null)
        {
            return Vector3.Lerp(target.m_sailFurledPosition.position, target.m_sailUnfurledPosition.position, 0.5f);
        }

        return target.m_mastObject != null ? target.m_mastObject.transform.position : target.transform.position;
    }

    private static float ShipLength(Ship target)
    {
        BoxCollider hull = target.m_floatCollider;
        return hull != null ? hull.size.z * hull.transform.lossyScale.z : 0f;
    }

    private static float Bearing(Vector3 offset)
    {
        return Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
    }

    private static void RestoreHud()
    {
        if (hudHidden && Hud.instance != null && Hud.instance.m_userHidden)
        {
            Hud.instance.m_userHidden = savedHudHidden;
        }

        hudHidden = false;
    }

    private static void End()
    {
        RestoreHud();
        running = false;
        ship = null;
        weight = 0f;
        cancelElapsed = -1f;
    }
}
