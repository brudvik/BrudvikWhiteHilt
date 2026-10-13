using BrudvikWhiteHilt.Backpack;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles.Damage;

/// <summary>
/// The local player's hands on a damaged ship: look at a hole and press Use to patch it, hold the alternative key and press
/// Use anywhere aboard a flooded ship to bail. There are no objects to hover: the holes are points on the hull, so the
/// player's line of sight is tested against them (see <see cref="HoleRules.Aimed"/>). The prompts are written under the
/// crosshair after the game's, and plain Use is only taken when a hole is aimed at and nothing else is under the crosshair.
/// Runs only for the local player, on its own machine.
/// </summary>
public static class ShipDamageInput
{
    // A hole is aimed at when the line of sight passes this close to it.
    private const float AimRadius = 0.9f;

    // Ships farther than this from the camera are not looked at.
    private const float SearchRange = 60f;

    // Seconds between two patches.
    private const float PatchInterval = 0.5f;

    private static readonly List<Vector3> world = new();
    private static ShipDamage aimedShip;
    private static int aimedIndex = -1;
    private static float nextBail;
    private static float nextPatch;
    private static bool failed;

    /// <summary>The frame in which a Use press was taken by the patch, bail or stamp handlers, so the boarding leap does not also act on it.</summary>
    public static int HandledFrame { get; internal set; } = -1;

    /// <summary>
    /// Whether the alternative-use key is held, as the game reads it for the alternative use of what is under the crosshair.
    /// </summary>
    /// <returns>True while it is held.</returns>
    public static bool AltHeld()
    {
        return ZInput.IsNonClassicFunctionality() && ZInput.IsGamepadActive()
            ? ZInput.GetButton("JoyAltKeys")
            : ZInput.GetButton("AltPlace") || ZInput.GetButton("JoyAltPlace");
    }

    /// <summary>
    /// Whether the player looks at a hole in a ship, so that Use is for the hole.
    /// </summary>
    /// <param name="ship">The ship.</param>
    /// <returns>True if one of its holes is aimed at.</returns>
    public static bool IsAimedAt(Ship ship)
    {
        return aimedShip != null && aimedShip.Ship == ship;
    }

    /// <summary>
    /// Finds the hole under the crosshair and patches or bails when the keys are pressed. Called every frame for the local player.
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

    private static void TickCore(Player player)
    {
        aimedShip = null;
        aimedIndex = -1;
        if (!ShipDamage.Any || !DamageSettings.Enabled.Value || player == null || player.IsDead() || !player.TakeInput()
            || BackpackInput.Typing())
        {
            return;
        }

        FindAimed(player);
        if (!ZInput.GetButtonDown("Use") && !ZInput.GetButtonDown("JoyUse"))
        {
            return;
        }

        bool alt = AltHeld();
        ShipDamage aboard = Aboard(player);
        bool bailable = aboard != null && aboard.Flood > 0f && HoverAllows(player, aboard.Ship);
        if (aimedShip != null && HoverAllows(player, aimedShip.Ship))
        {
            HandledFrame = Time.frameCount;
            if (!alt)
            {
                Patch(player, aimedShip, aimedIndex);
            }
            else if (bailable)
            {
                Bail(player, aboard);
            }
        }
        else if (alt && bailable)
        {
            HandledFrame = Time.frameCount;
            Bail(player, aboard);
        }
    }

    /// <summary>
    /// Writes the patch prompt under the crosshair while a hole is aimed at, or the bail prompt and the water in the hull on
    /// a flooded ship. Called after the game's crosshair update.
    /// </summary>
    /// <param name="hud">The HUD.</param>
    /// <param name="player">The local player.</param>
    public static void UpdateCrosshair(Hud hud, Player player)
    {
        try
        {
            UpdateCrosshairCore(hud, player);
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
            Jotunn.Logger.LogWarning($"Ship damage: the patch and bail keys failed: {ex}");
        }
    }

    private static void UpdateCrosshairCore(Hud hud, Player player)
    {
        if (!ShipDamage.Any || !DamageSettings.Enabled.Value || player == null)
        {
            return;
        }

        Localization text = Localization.instance;
        ShipDamage aboard = Aboard(player);
        string bail = text.Localize("[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $whitehilt_ship_bail");
        if (aimedShip != null)
        {
            string patch = string.Format(text.Localize("[<color=yellow><b>$KEY_Use</b></color>] $whitehilt_ship_patch"),
                DamageSettings.PatchWood.Value, DamageSettings.PatchResin.Value);
            Append(hud, text.Localize("$whitehilt_ship_hole") + "\n" + patch + (aboard != null && aboard.Flood > 0f ? "\n" + bail : string.Empty));
        }
        else if (aboard != null && aboard.Flood > 0f && HoverAllows(player, aboard.Ship))
        {
            string water = ShipDamage.FloodLine(aboard.Ship);
            Append(hud, water != null ? bail + "\n" + water : bail);
        }
    }

    // The nearest hole to the player that the line of sight passes, over every ship that has one; the camera's line, as
    // the game's own hover uses, but the reach is measured from the player's eyes.
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
        IReadOnlyList<ShipDamage> all = ShipDamage.All;
        for (int i = 0; i < all.Count; i++)
        {
            ShipDamage damage = all[i];
            if (damage == null || damage.Holes.Count == 0 || (damage.transform.position - origin).sqrMagnitude > SearchRange * SearchRange)
            {
                continue;
            }

            world.Clear();
            IReadOnlyList<Vector3> holes = damage.Holes;
            for (int h = 0; h < holes.Count; h++)
            {
                world.Add(damage.transform.TransformPoint(holes[h]));
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
                aimedShip = damage;
                aimedIndex = index;
            }
        }
    }

    // The damage of the ship the player is aboard, or null.
    private static ShipDamage Aboard(Player player)
    {
        Ship ship = Ship.GetLocalShip();
        return ship != null && ship.IsPlayerInBoat(player) ? ShipDamage.Of(ship) : null;
    }

    // Use may be pressed with nothing under the crosshair or with the ship itself; anything else (the helm, the hold, a
    // door) takes the key instead.
    internal static bool HoverAllows(Player player, Ship ship)
    {
        GameObject hover = player.GetHoverObject();
        return hover == null || hover == ship.gameObject;
    }

    // Takes the wood and resin from the player's pack and asks the ship's owner to close the hole.
    private static void Patch(Player player, ShipDamage damage, int index)
    {
        if (Time.time < nextPatch || !HoleRules.ValidIndex(index, damage.Holes.Count))
        {
            return;
        }

        int wood = DamageSettings.PatchWood.Value;
        int resin = DamageSettings.PatchResin.Value;
        Inventory inventory = player.GetInventory();
        if (inventory.CountItems("$item_wood") < wood || inventory.CountItems("$item_resin") < resin)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_ship_nopatch");
            return;
        }

        nextPatch = Time.time + PatchInterval;
        if (wood > 0)
        {
            inventory.RemoveItem("$item_wood", wood);
        }

        if (resin > 0)
        {
            inventory.RemoveItem("$item_resin", resin);
        }

        damage.RequestPatch(index);
    }

    // Costs stamina and asks the owner to take water out; no more often than the bail delay.
    private static void Bail(Player player, ShipDamage damage)
    {
        if (Time.time < nextBail)
        {
            return;
        }

        float stamina = DamageSettings.BailStamina.Value;
        if (!player.HaveStamina(stamina))
        {
            return;
        }

        nextBail = Time.time + DamageSettings.BailSeconds.Value;
        player.UseStamina(stamina);
        damage.RequestBail();
    }

    internal static void Append(Hud hud, string text)
    {
        string shown = hud.m_hoverName.text;
        hud.m_hoverName.text = string.IsNullOrEmpty(shown) ? text : shown + "\n" + text;
        hud.m_crosshair.color = Color.yellow;
    }
}
