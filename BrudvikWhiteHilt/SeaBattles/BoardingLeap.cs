using BrudvikWhiteHilt.Backpack;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles;

/// <summary>
/// The local player's leap from one ship's deck to another: standing on a ship, look at another ship within reach and
/// press Use. The prompt is shown under the crosshair, the player lands on the nearest deck point of the other ship
/// carrying its speed, and a short cooldown follows. It works between any two ships, so it also helps a friend's ship. It
/// runs only for the local player, on its own machine, which owns the player's body.
/// </summary>
public static class BoardingLeap
{
    // Seconds between two leaps.
    private const float Cooldown = 1f;

    // The target is looked for this often, not every frame; a leap looks again at once.
    private const float AimInterval = 0.1f;

    // A velocity sample older than this is forgotten.
    private const float VelocityMemory = 0.5f;
    private const float VelocityBlend = 0.5f;

    // The player lands this far above the deck point.
    private const float LandingHeight = 0.1f;

    private static readonly RaycastHit[] hits = new RaycastHit[16];
    private static int mask;
    private static Ship target;
    private static Vector3 aim;
    private static float nextAim;
    private static float nextLeap;
    private static Ship trackedShip;
    private static Vector3 trackedPosition;
    private static float trackedTime;
    private static Vector3 trackedVelocity;

    /// <summary>
    /// Looks for a ship to leap to and leaps when Use is pressed. Called every frame for the local player.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Tick(Player player)
    {
        Ship own = SeaBattleSettings.Enabled.Value && !player.IsDead() && !player.IsSwimming() && !player.InPlaceMode()
            ? player.GetStandingOnShip() : null;
        if (own == null || !player.TakeInput())
        {
            target = null;
            return;
        }

        if (Time.time >= nextAim)
        {
            nextAim = Time.time + AimInterval;
            Aim(player, own);
        }

        if (target != null && HoverAllows(player) && UsePressed())
        {
            Leap(player, own);
        }
    }

    /// <summary>
    /// Shows the leap prompt under the crosshair while a ship is within reach. Called after the game's crosshair update.
    /// </summary>
    /// <param name="hud">The HUD.</param>
    /// <param name="player">The local player.</param>
    public static void UpdateCrosshair(Hud hud, Player player)
    {
        if (target == null || player == null || Time.time < nextLeap || !HoverAllows(player))
        {
            return;
        }

        string prompt = Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] $whitehilt_boarding_leap");
        string shown = hud.m_hoverName.text;
        hud.m_hoverName.text = string.IsNullOrEmpty(shown) ? prompt : shown + "\n" + prompt;
        hud.m_crosshair.color = Color.yellow;
    }

    // Casts a ray from the camera and keeps the nearest thing it hits that is not the player's own ship. It is a target
    // when it is another ship and the hit lies within the leap range of the player.
    private static void Aim(Player player, Ship own)
    {
        target = null;
        Camera camera = GameCamera.instance != null ? GameCamera.instance.m_camera : null;
        if (camera == null)
        {
            return;
        }

        if (mask == 0)
        {
            mask = LayerMask.GetMask("vehicle", "piece", "static_solid", "Default", "Default_small", "terrain");
        }

        float range = SeaBattleSettings.BoardingLeapRange.Value;
        Vector3 origin = camera.transform.position;
        float length = range + Vector3.Distance(origin, player.transform.position);
        int count = Physics.RaycastNonAlloc(origin, camera.transform.forward, hits, length, mask, QueryTriggerInteraction.Ignore);
        float nearest = float.MaxValue;
        Ship found = null;
        Vector3 point = Vector3.zero;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hits[i];
            Rigidbody body = hit.collider.attachedRigidbody;
            if (body == own.m_body || hit.distance >= nearest)
            {
                continue;
            }

            nearest = hit.distance;
            found = body != null ? body.GetComponent<Ship>() : null;
            point = hit.point;
        }

        if (found != null && found != own && Vector3.Distance(player.transform.position, point) <= range)
        {
            target = found;
            aim = point;
            Track(found);
        }
    }

    // Keeps a running estimate of the target's velocity, restarted when the target changes or has not been seen for a while.
    private static void Track(Ship ship)
    {
        float now = Time.time;
        Vector3 position = ship.transform.position;
        if (ship != trackedShip || now - trackedTime > VelocityMemory)
        {
            trackedShip = ship;
            trackedVelocity = Vector3.zero;
        }
        else
        {
            trackedVelocity = EncounterRules.SmoothedVelocity(trackedPosition, position, now - trackedTime, trackedVelocity, VelocityBlend);
        }

        trackedPosition = position;
        trackedTime = now;
    }

    // Moves the player onto the other ship's deck, at the deck point nearest to where they looked, and gives them the
    // ship's velocity there so they do not slide off it.
    private static void Leap(Player player, Ship own)
    {
        if (Time.time < nextLeap)
        {
            return;
        }

        Ship other = target;
        Aim(player, own);
        if (other == null || target != other || !RaiderDeck.TryDeckPoint(other, aim, out Vector3 deck))
        {
            return;
        }

        nextLeap = Time.time + Cooldown;
        Vector3 landing = deck + Vector3.up * LandingHeight;
        player.m_jumpEffects.Create(player.transform.position, Quaternion.identity);
        player.transform.position = landing;
        player.m_body.position = landing;
        player.m_body.linearVelocity = VelocityAt(other, landing);
        player.m_zanim.SetTrigger("jump");
        target = null;
    }

    // The other ship's velocity at a point: its own physics where this machine owns it, else what its movement shows.
    private static Vector3 VelocityAt(Ship ship, Vector3 point)
    {
        Rigidbody body = ship.m_body;
        if (body != null && !body.isKinematic && ship.m_nview != null && ship.m_nview.IsValid() && ship.m_nview.IsOwner())
        {
            return body.GetPointVelocity(point);
        }

        return trackedShip == ship && Time.time - trackedTime <= VelocityMemory ? trackedVelocity : Vector3.zero;
    }

    // Use may be pressed with nothing under the crosshair or with the other ship's hull; anything else (its helm, its
    // hold, a door) takes the key instead.
    private static bool HoverAllows(Player player)
    {
        GameObject hover = player.GetHoverObject();
        return hover == null || (target != null && hover == target.gameObject);
    }

    private static bool UsePressed()
    {
        // Patching a hole and bailing take Use first, and bailing holds the alternative key.
        return !BackpackInput.Typing() && (ZInput.GetButtonDown("Use") || ZInput.GetButtonDown("JoyUse"))
            && Damage.ShipDamageInput.HandledFrame != Time.frameCount && !Damage.ShipDamageInput.AltHeld();
    }
}
