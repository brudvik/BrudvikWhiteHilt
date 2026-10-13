using BrudvikWhiteHilt.Backpack;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles.Damage;

/// <summary>
/// The local player's hands on a burning deck: look at a fire and press Use to stamp it out. A fire is a point on the deck,
/// not an object to hover, so the player's line of sight is tested against the points, as <see cref="ShipDamageInput"/> does
/// for holes, and it takes Use the same way: only when nothing else is under the crosshair, and after the patch and bail
/// handlers, so a hole aimed at first keeps the key. Runs only for the local player, on its own machine.
/// </summary>
public static class DeckFireInput
{
    // A fire is aimed at when the line of sight passes this close to it; the flames are as wide as that.
    private const float AimRadius = 1.2f;

    // Ships farther than this from the camera are not looked at.
    private const float SearchRange = 60f;

    // Seconds between two stamps.
    private const float StampInterval = 0.5f;

    private static readonly List<Vector3> world = new();
    private static DeckFires aimedShip;
    private static int aimedIndex = -1;
    private static float nextStamp;
    private static bool failed;

    /// <summary>
    /// Whether the player looks at a fire on a ship, so that Use is for the fire.
    /// </summary>
    /// <param name="ship">The ship.</param>
    /// <returns>True if one of its fires is aimed at.</returns>
    public static bool IsAimedAt(Ship ship)
    {
        return aimedShip != null && aimedShip.Ship == ship;
    }

    /// <summary>
    /// Finds the fire under the crosshair and stamps it out when Use is pressed. Called every frame for the local player,
    /// after <see cref="ShipDamageInput.Tick"/>.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Tick(Player player)
    {
        try
        {
            TickCore(player);
        }
        catch (System.Exception ex)
        {
            Fail(ex);
        }
    }

    /// <summary>
    /// Writes the stamp prompt under the crosshair while a fire is aimed at, and no hole is. Called after the game's
    /// crosshair update.
    /// </summary>
    /// <param name="hud">The HUD.</param>
    /// <param name="player">The local player.</param>
    public static void UpdateCrosshair(Hud hud, Player player)
    {
        try
        {
            if (aimedShip == null || player == null || ShipDamageInput.IsAimedAt(aimedShip.Ship))
            {
                return;
            }

            Localization text = Localization.instance;
            ShipDamageInput.Append(hud, text.Localize("$whitehilt_ship_fire") + "\n[<color=yellow><b>$KEY_Use</b></color>] "
                + text.Localize("$whitehilt_ship_stamp"));
        }
        catch (System.Exception ex)
        {
            Fail(ex);
        }
    }

    // An exception in a postfix would stop the postfixes after it, so this class never lets one out; it logs the first.
    private static void Fail(System.Exception ex)
    {
        aimedShip = null;
        aimedIndex = -1;
        if (!failed)
        {
            failed = true;
            Jotunn.Logger.LogWarning($"Deck fires: stamping out a fire failed: {ex}");
        }
    }

    private static void TickCore(Player player)
    {
        aimedShip = null;
        aimedIndex = -1;
        if (!DeckFires.Any || !DamageSettings.Enabled.Value || player == null || player.IsDead() || !player.TakeInput()
            || BackpackInput.Typing())
        {
            return;
        }

        FindAimed(player);
        if (aimedShip == null || !(ZInput.GetButtonDown("Use") || ZInput.GetButtonDown("JoyUse")) || ShipDamageInput.AltHeld()
            || ShipDamageInput.HandledFrame == Time.frameCount || !ShipDamageInput.HoverAllows(player, aimedShip.Ship))
        {
            return;
        }

        ShipDamageInput.HandledFrame = Time.frameCount;
        Stamp(player, aimedShip, aimedIndex);
    }

    // The nearest fire to the player that the line of sight passes, over every ship that burns; the camera's line, as the
    // game's own hover uses, but the reach is measured from the player's eyes.
    private static void FindAimed(Player player)
    {
        Camera camera = GameCamera.instance != null ? GameCamera.instance.m_camera : null;
        if (camera == null)
        {
            return;
        }

        Vector3 origin = camera.transform.position;
        Vector3 eye = player.m_eye.position;
        float reach = player.m_maxInteractDistance;
        float nearest = float.MaxValue;
        IReadOnlyList<DeckFires> all = DeckFires.All;
        for (int i = 0; i < all.Count; i++)
        {
            DeckFires fires = all[i];
            if (fires == null || fires.Points.Count == 0 || (fires.transform.position - origin).sqrMagnitude > SearchRange * SearchRange)
            {
                continue;
            }

            world.Clear();
            IReadOnlyList<Vector3> points = fires.Points;
            for (int p = 0; p < points.Count; p++)
            {
                world.Add(fires.transform.TransformPoint(points[p]));
            }

            int index = HoleRules.Aimed(world, origin, camera.transform.forward, AimRadius, reach + Vector3.Distance(origin, eye));
            if (index < 0)
            {
                continue;
            }

            float distance = Vector3.Distance(eye, world[index]);
            if (distance <= reach && distance < nearest)
            {
                nearest = distance;
                aimedShip = fires;
                aimedIndex = index;
            }
        }
    }

    // Costs stamina and asks the ship's owner to put the fire out; no more often than the stamp delay.
    private static void Stamp(Player player, DeckFires fires, int index)
    {
        if (Time.time < nextStamp || !HoleRules.ValidIndex(index, fires.Points.Count))
        {
            return;
        }

        float stamina = DamageSettings.StampStamina.Value;
        if (!player.HaveStamina(stamina))
        {
            return;
        }

        nextStamp = Time.time + StampInterval;
        player.UseStamina(stamina);
        fires.RequestStamp(fires.Points[index]);
    }
}
