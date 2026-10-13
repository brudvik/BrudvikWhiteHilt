using BrudvikWhiteHilt.Backpack;
using BrudvikWhiteHilt.SeaBattles.Damage;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles.Weapons;

/// <summary>
/// The local player's hands on a grapnel rope: look at the end of the rope on the rail of the ship it was shot from and
/// press Use to cut it. The end is a point, not an object to hover (a collider on a ship's child would be counted as aboard
/// by the ship), so the player's line of sight is tested against the point, as <see cref="DeckFireInput"/> does for fires,
/// and it takes Use the same way: only when nothing else is under the crosshair and no hole or fire is aimed at. Runs only
/// for the local player, on its own machine.
/// </summary>
public static class GrappleInput
{
    // The end of the rope is aimed at when the line of sight passes this close to it.
    private const float AimRadius = 1f;

    // Ships farther than this from the camera are not looked at.
    private const float SearchRange = 60f;

    // Seconds between two cuts.
    private const float CutInterval = 0.5f;

    private static readonly List<Vector3> one = new() { Vector3.zero };
    private static GrappleLink aimed;
    private static float nextCut;
    private static bool failed;

    /// <summary>
    /// Whether the player looks at the end of a rope on a ship, so that Use is for the rope.
    /// </summary>
    /// <param name="ship">The ship.</param>
    /// <returns>True if the end of a rope on that ship is aimed at.</returns>
    public static bool IsAimedAt(Ship ship)
    {
        return aimed != null && aimed.Ship == ship;
    }

    /// <summary>
    /// Finds the end of a rope under the crosshair and cuts the rope when Use is pressed. Called every frame for the local
    /// player, after <see cref="DeckFireInput.Tick"/>.
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
    /// Writes the cut prompt under the crosshair while the end of a rope is aimed at, and no hole or fire is. Called after the
    /// game's crosshair update.
    /// </summary>
    /// <param name="hud">The HUD.</param>
    /// <param name="player">The local player.</param>
    public static void UpdateCrosshair(Hud hud, Player player)
    {
        try
        {
            if (aimed == null || player == null || ShipDamageInput.IsAimedAt(aimed.Ship) || DeckFireInput.IsAimedAt(aimed.Ship))
            {
                return;
            }

            Localization text = Localization.instance;
            ShipDamageInput.Append(hud, text.Localize("$whitehilt_ship_grapple") + "\n[<color=yellow><b>$KEY_Use</b></color>] "
                + text.Localize("$whitehilt_ship_cut"));
        }
        catch (System.Exception ex)
        {
            Fail(ex);
        }
    }

    // An exception in a postfix would stop the postfixes after it, so this class never lets one out; it logs the first.
    private static void Fail(System.Exception ex)
    {
        aimed = null;
        if (!failed)
        {
            failed = true;
            Jotunn.Logger.LogWarning($"Grapnel rope: cutting the rope failed: {ex}");
        }
    }

    private static void TickCore(Player player)
    {
        aimed = null;
        if (!GrappleLink.Any || player == null || player.IsDead() || !player.TakeInput() || BackpackInput.Typing())
        {
            return;
        }

        FindAimed(player);
        if (aimed == null || !(ZInput.GetButtonDown("Use") || ZInput.GetButtonDown("JoyUse")) || ShipDamageInput.AltHeld()
            || ShipDamageInput.HandledFrame == Time.frameCount || !ShipDamageInput.HoverAllows(player, aimed.Ship))
        {
            return;
        }

        ShipDamageInput.HandledFrame = Time.frameCount;
        if (Time.time >= nextCut)
        {
            nextCut = Time.time + CutInterval;
            aimed.RequestCut();
        }
    }

    // The nearest rope end to the player that the line of sight passes, over every ship that holds a rope; the camera's
    // line, as the game's own hover uses, but the reach is measured from the player's eyes.
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
        IReadOnlyList<GrappleLink> all = GrappleLink.All;
        for (int i = 0; i < all.Count; i++)
        {
            GrappleLink link = all[i];
            if (link == null || !link.IsHolding || (link.transform.position - origin).sqrMagnitude > SearchRange * SearchRange)
            {
                continue;
            }

            Vector3 end = link.HolderPoint;
            one[0] = end;
            if (HoleRules.Aimed(one, origin, camera.transform.forward, AimRadius, reach + Vector3.Distance(origin, eye)) < 0)
            {
                continue;
            }

            float distance = Vector3.Distance(eye, end);
            if (distance <= reach && distance < nearest)
            {
                nearest = distance;
                aimed = link;
            }
        }
    }
}
