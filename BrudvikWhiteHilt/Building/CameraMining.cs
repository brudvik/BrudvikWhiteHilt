using UnityEngine;

namespace BrudvikWhiteHilt.Building;

/// <summary>
/// Swings a pickaxe from the build camera: the player stands where they are and swings as usual, but the strike goes
/// along the camera's line of sight, up to <see cref="BuildToolSettings.CameraMiningReach"/> from the camera. So a rock
/// in an awkward spot can be broken while building, or the ground dug under water from the shore. Everything else is
/// the game's own strike: damage, digging, stamina, tool wear, skill and wards.
/// </summary>
public static class CameraMining
{
    private static Transform origin;

    /// <summary>
    /// Whether the player holds a pickaxe that may be used with the build camera.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>True for any pickaxe, while the server allows it.</returns>
    public static bool Holding(Player player)
    {
        return player != null && BuildToolSettings.CameraMining.Value && IsPickaxe(player.GetCurrentWeapon());
    }

    /// <summary>
    /// The camera keys and the strike for the local player holding a pickaxe. Called every frame from
    /// <see cref="BuildTools.Tick"/>.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Tick(Player player)
    {
        if (!player.TakeInput() || Hud.InRadial())
        {
            return;
        }

        if (BuildTools.Pressed(BuildToolSettings.KeyCamera))
        {
            BuildCamera.Toggle(player);
            if (BuildCamera.Active)
            {
                player.Message(MessageHud.MessageType.TopLeft,
                    string.Format(Localization.instance.Localize("$msg_whitehilt_camera_mining"), BuildToolSettings.CameraMiningReach.Value.ToString("0.#")));
            }
        }

        if (BuildTools.Pressed(BuildToolSettings.KeyFlyTo)) BuildTools.FlyTo(player);
        if (BuildTools.Pressed(BuildToolSettings.KeySpeedUp)) BuildTools.ChangeSpeed(player, 1);
        if (BuildTools.Pressed(BuildToolSettings.KeySpeedDown)) BuildTools.ChangeSpeed(player, -1);
        if (BuildTools.Pressed(BuildToolSettings.KeyLight)) BuildTools.ToggleLight(player);

        // The camera keeps the player's own controls from the game, so the swing is started here; holding the button
        // swings again, as it does without the camera.
        if (BuildCamera.Active && !BuildToolbar.CursorMode && (ZInput.GetButton("Attack") || ZInput.GetButton("JoyAttack")))
        {
            player.StartAttack(null, false);
        }
    }

    /// <summary>
    /// Sends a pickaxe strike of the local player along the camera's line of sight while the camera is on.
    /// </summary>
    /// <param name="attack">The strike.</param>
    /// <param name="originJoint">Where the strike starts from.</param>
    /// <param name="attackDir">Which way it goes.</param>
    public static void Redirect(Attack attack, ref Transform originJoint, ref Vector3 attackDir)
    {
        if (!BuildCamera.Active || attack.m_character == null || attack.m_character != Player.m_localPlayer || !IsPickaxe(attack.m_weapon))
        {
            return;
        }

        Vector3 direction = BuildCamera.Rotation * Vector3.forward;

        // The strike reaches the weapon's own range from where it starts; it starts that much short of the reach, but
        // never beyond something in the way.
        float shift = Mathf.Max(0f, BuildToolSettings.CameraMiningReach.Value - attack.m_attackRange);
        int mask = attack.m_hitTerrain ? Attack.m_attackMaskTerrain : Attack.m_attackMask;
        if (shift > 0f && Physics.Raycast(BuildCamera.Position, direction, out RaycastHit hit, shift, mask, QueryTriggerInteraction.Ignore))
        {
            shift = Mathf.Max(0f, hit.distance - attack.m_attackRayWidth - 0.05f);
        }

        if (origin == null)
        {
            origin = new GameObject("WhiteHiltCameraStrike").transform;
            Object.DontDestroyOnLoad(origin.gameObject);
        }

        // The game adds the attack's height and side offset to the start; take them off, so it starts on the line.
        origin.position = BuildCamera.Position + direction * shift - Vector3.up * attack.m_attackHeight
            - attack.m_character.transform.right * attack.m_attackOffset;
        origin.rotation = BuildCamera.Rotation;
        originJoint = origin;
        attackDir = direction;
    }

    private static bool IsPickaxe(ItemDrop.ItemData item)
    {
        return item != null && item.m_shared.m_skillType == Skills.SkillType.Pickaxes;
    }
}
