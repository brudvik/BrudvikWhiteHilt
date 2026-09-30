using BepInEx.Configuration;
using BrudvikWhiteHilt.Building.Groups;
using BrudvikWhiteHilt.Building.Media;
using BrudvikWhiteHilt.Building.Terrain;
using UnityEngine;

namespace BrudvikWhiteHilt.Building;

/// <summary>
/// The build tool actions, reached both from hotkeys and from the toolbar buttons.
/// Works with every item that has build pieces: hammer, hoe, cultivator and build tools from other mods.
/// </summary>
public static class BuildTools
{
    private static float wheelAmount;

    /// <summary>
    /// True once the mouse wheel tilted the piece during the current hold of the tilt modifier.
    /// The camera then stops sinking on the same key.
    /// </summary>
    public static bool TiltWheelUsed { get; private set; }

    /// <summary>
    /// True while a wheel modifier is held, so vanilla does not also turn the piece with the wheel.
    /// </summary>
    public static bool WheelModifierHeld =>
        Input.GetKey(BuildToolSettings.TiltWheelModifier.Value) || Input.GetKey(BuildToolSettings.RollWheelModifier.Value);

    /// <summary>
    /// Handles the hotkeys for the local player. Called every frame from <see cref="Player.Update"/>.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Tick(Player player)
    {
        BuildToolbar.Ensure();
        MediaPanel.Ensure();
        TerrainPanel.Ensure();
        BuildCamera.CheckStillAllowed(player);
        MediaMode.Tick();
        TerrainEdit.Tick();
        RoadBuilder.Tick(player);
        GrowthMarkers.Tick(player);
        BuildGizmos.UpdateAxes(player);
        if (!player.InPlaceMode())
        {
            if (GroupTools.Active)
            {
                if (GroupTools.Mode == GroupTools.ToolMode.Select)
                {
                    BuildSelection.Clear();
                }

                GroupTools.Exit();
            }

            ExitTerrainTools(player);
            return;
        }

        BuildRotation.Apply(player);
        if (!player.TakeInput() || Hud.IsPieceSelectionVisible() || FilmPlayer.Playing)
        {
            return;
        }

        GroupTools.Tick(player);
        ExitTerrainTools(player);
        RepairTools.Tick(player);
        if (TerrainSettings.HoldingHoe(player))
        {
            HoeTools.Tick(player);
        }
        else if (TerrainSettings.HoldingCultivator(player))
        {
            FarmTools.Tick(player);
        }
        if (Pressed(MediaSettings.KeyPhoto)) WithCamera(player, () => MediaMode.TakePhoto(MediaPanel.Host));
        if (Pressed(MediaSettings.KeyPhotoView)) WithCamera(player, MediaMode.TogglePhotoView);
        if (Pressed(MediaSettings.KeyPanel)) WithCamera(player, MediaPanel.Toggle);
        if (!MediaMode.PhotoView && !MediaPanel.IsOpen && !GroupTools.Active && !HoeTools.Active && !FarmTools.Active && !HoeTools.WheelSetsBrush(player) && !RepairTools.WheelSetsRadius(player) && !Painting.PaintBrush.WheelSetsRadius(player))
        {
            HandleWheel(player);
        }
        if (Pressed(BuildToolSettings.KeyCamera)) ToggleCamera(player);
        if (Pressed(BuildToolSettings.KeyFlyTo)) FlyTo(player);
        if (Pressed(BuildToolSettings.KeySpeedUp)) ChangeSpeed(player, 1);
        if (Pressed(BuildToolSettings.KeySpeedDown)) ChangeSpeed(player, -1);
        if (Pressed(BuildToolSettings.KeyStep)) CycleStep(player);
        if (Pressed(BuildToolSettings.KeyTiltForward)) Tilt(player, 1);
        if (Pressed(BuildToolSettings.KeyTiltBack)) Tilt(player, -1);
        if (Pressed(BuildToolSettings.KeyRollLeft)) Roll(player, -1);
        if (Pressed(BuildToolSettings.KeyRollRight)) Roll(player, 1);
        if (Pressed(BuildToolSettings.KeyQuickTilt)) QuickTilt(player);
        if (Pressed(BuildToolSettings.KeyQuickRoll)) QuickRoll(player);
        if (Pressed(BuildToolSettings.KeyFlip)) Flip(player);
        if (Pressed(BuildToolSettings.KeyReset)) Reset(player);
        if (Pressed(BuildToolSettings.KeySnap)) ToggleSnap(player);
        if (Pressed(BuildToolSettings.KeyCopy)) Copy(player);
        if (Pressed(BuildToolSettings.KeyGrid)) ToggleGrid(player);
        if (Pressed(BuildToolSettings.KeyStamp)) Stamp(player);
        if (Pressed(BuildToolSettings.KeyUndo)) Undo(player);
        if (Pressed(BuildToolSettings.KeyRedo)) Redo(player);
        if (Pressed(BuildToolSettings.KeyLight)) ToggleLight(player);
        if (Pressed(BuildToolSettings.KeyRepair) && !TerrainSettings.HoldingHoe(player) && !TerrainSettings.HoldingCultivator(player)) ToggleRepair(player);
        if (GroupTools.Active || HoeTools.Active || FarmTools.Active)
        {
            return;
        }

        if (Pressed(BuildToolSettings.KeyNudgeForward)) Nudge(player, 1, 0, 0);
        if (Pressed(BuildToolSettings.KeyNudgeBack)) Nudge(player, -1, 0, 0);
        if (Pressed(BuildToolSettings.KeyNudgeLeft)) Nudge(player, 0, -1, 0);
        if (Pressed(BuildToolSettings.KeyNudgeRight)) Nudge(player, 0, 1, 0);
        if (Pressed(BuildToolSettings.KeyNudgeUp)) Nudge(player, 0, 0, 1);
        if (Pressed(BuildToolSettings.KeyNudgeDown)) Nudge(player, 0, 0, -1);
    }

    /// <summary>Build camera on or off.</summary>
    /// <param name="player">The local player.</param>
    public static void ToggleCamera(Player player)
    {
        BuildCamera.Toggle(player);
    }

    /// <summary>Fly the camera to the aimed point.</summary>
    /// <param name="player">The local player.</param>
    public static void FlyTo(Player player)
    {
        BuildCamera.FlyTo(player);
    }

    /// <summary>Camera faster (1) or slower (-1).</summary>
    /// <param name="player">The local player.</param>
    /// <param name="direction">The direction.</param>
    public static void ChangeSpeed(Player player, int direction)
    {
        BuildCamera.ChangeSpeed(direction);
        player.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("$msg_whitehilt_build_camera_speed") + ": x" + FormatFactor(BuildCamera.SpeedFactor));
    }

    /// <summary>Next rotation step.</summary>
    /// <param name="player">The local player.</param>
    public static void CycleStep(Player player)
    {
        BuildRotation.CycleStep(player);
        player.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("$whitehilt_build_step") + ": " + FormatAngle(BuildRotation.Step));
    }

    /// <summary>Tilt forward (1) or back (-1).</summary>
    /// <param name="player">The local player.</param>
    /// <param name="direction">The direction.</param>
    public static void Tilt(Player player, int direction)
    {
        BuildRotation.Tilt(direction);
    }

    /// <summary>Roll right (1) or left (-1).</summary>
    /// <param name="player">The local player.</param>
    /// <param name="direction">The direction.</param>
    public static void Roll(Player player, int direction)
    {
        BuildRotation.RollBy(direction);
    }

    /// <summary>Tilt to 45, 90 or 0 degrees.</summary>
    /// <param name="player">The local player.</param>
    public static void QuickTilt(Player player)
    {
        BuildRotation.QuickTilt();
    }

    /// <summary>Roll to 45, 90 or 0 degrees.</summary>
    /// <param name="player">The local player.</param>
    public static void QuickRoll(Player player)
    {
        BuildRotation.QuickRoll();
    }

    /// <summary>Upside down, or back.</summary>
    /// <param name="player">The local player.</param>
    public static void Flip(Player player)
    {
        BuildRotation.Flip();
    }

    /// <summary>Reset rotation, tilt, roll, flip and nudge.</summary>
    /// <param name="player">The local player.</param>
    public static void Reset(Player player)
    {
        BuildRotation.Reset(player);
    }

    /// <summary>Snapping on or off.</summary>
    /// <param name="player">The local player.</param>
    public static void ToggleSnap(Player player)
    {
        player.Message(MessageHud.MessageType.TopLeft, BuildRotation.ToggleSnap() ? "$msg_whitehilt_build_snap_on" : "$msg_whitehilt_build_snap_off");
    }

    /// <summary>Next snap point on the held piece, as vanilla's own key does.</summary>
    /// <param name="player">The local player.</param>
    public static void NextSnapPoint(Player player)
    {
        player.m_manualSnapPoint++;
    }

    /// <summary>Copy the piece under the crosshair, with its full rotation.</summary>
    /// <param name="player">The local player.</param>
    public static void Copy(Player player)
    {
        player.CopyPiece();
    }

    /// <summary>Grid on or off.</summary>
    /// <param name="player">The local player.</param>
    public static void ToggleGrid(Player player)
    {
        string state = BuildRotation.ToggleGrid() ? "$msg_whitehilt_build_grid_on" : "$msg_whitehilt_build_grid_off";
        player.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize(state) + " (" + FormatLength(BuildToolSettings.GridSize.Value) + ")");
    }

    /// <summary>Place a copy of the last piece next to it.</summary>
    /// <param name="player">The local player.</param>
    public static void Stamp(Player player)
    {
        BuildPlacer.Stamp(player);
    }

    /// <summary>Nudge the piece one step.</summary>
    /// <param name="player">The local player.</param>
    /// <param name="forward">1 away, -1 closer.</param>
    /// <param name="right">1 right, -1 left.</param>
    /// <param name="up">1 up, -1 down.</param>
    public static void Nudge(Player player, int forward, int right, int up)
    {
        BuildRotation.Nudge(player, forward, right, up);
    }

    /// <summary>Undo the last placed piece.</summary>
    /// <param name="player">The local player.</param>
    public static void Undo(Player player)
    {
        if (TerrainEdit.LastUndoTime > BuildUndo.LastStepTime && TerrainEdit.Undo(player))
        {
            return;
        }

        BuildUndo.Undo(player);
    }

    /// <summary>Put back the last undone piece.</summary>
    /// <param name="player">The local player.</param>
    public static void Redo(Player player)
    {
        BuildUndo.Redo(player);
    }

    /// <summary>Camera light on or off.</summary>
    /// <param name="player">The local player.</param>
    public static void ToggleLight(Player player)
    {
        player.Message(MessageHud.MessageType.TopLeft, BuildCamera.ToggleLight() ? "$msg_whitehilt_build_light_on" : "$msg_whitehilt_build_light_off");
    }

    /// <summary>Repair mode on or off.</summary>
    /// <param name="player">The local player.</param>
    public static void ToggleRepair(Player player)
    {
        RepairTools.ToggleMode(player);
    }

    /// <summary>
    /// Formats an angle without needless decimals, e.g. "22.5°" or "15°".
    /// </summary>
    /// <param name="degrees">The angle.</param>
    /// <returns>The text.</returns>
    public static string FormatAngle(float degrees)
    {
        return degrees.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "°";
    }

    /// <summary>
    /// Formats a length in metres or centimetres.
    /// </summary>
    /// <param name="metres">The length.</param>
    /// <returns>The text.</returns>
    public static string FormatLength(float metres)
    {
        return Mathf.Abs(metres) < 1f
            ? Mathf.RoundToInt(metres * 100f) + " cm"
            : metres.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " m";
    }

    /// <summary>
    /// Formats a speed factor, e.g. "1.5".
    /// </summary>
    /// <param name="factor">The factor.</param>
    /// <returns>The text.</returns>
    public static string FormatFactor(float factor)
    {
        return factor.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void HandleWheel(Player player)
    {
        bool tilt = Input.GetKey(BuildToolSettings.TiltWheelModifier.Value);
        bool roll = Input.GetKey(BuildToolSettings.RollWheelModifier.Value);
        if (!tilt)
        {
            TiltWheelUsed = false;
        }

        if (!tilt && !roll)
        {
            wheelAmount = 0f;
            return;
        }

        wheelAmount += ZInput.GetMouseScrollWheel();
        int direction = wheelAmount > player.m_scrollAmountThreshold ? 1 : wheelAmount < -player.m_scrollAmountThreshold ? -1 : 0;
        if (direction == 0)
        {
            return;
        }

        wheelAmount = 0f;
        if (tilt)
        {
            BuildRotation.Tilt(direction);
            TiltWheelUsed = true;
        }
        else
        {
            BuildRotation.RollBy(direction);
        }
    }

    private static void ExitTerrainTools(Player player)
    {
        if (HoeTools.Active && !TerrainSettings.HoldingHoe(player))
        {
            HoeTools.Exit();
        }

        if (FarmTools.Active && !TerrainSettings.HoldingCultivator(player))
        {
            FarmTools.Exit();
        }

        if (!TerrainSettings.HoldingHoe(player))
        {
            BuildGizmos.HideCircle();
        }
    }

    private static void WithCamera(Player player, System.Action action)
    {
        if (BuildCamera.Active && MediaPanel.Host != null)
        {
            action();
        }
        else
        {
            player.Message(MessageHud.MessageType.TopLeft, "$msg_whitehilt_media_camera_only");
        }
    }

    private static bool Pressed(ConfigEntry<KeyboardShortcut> key)
    {
        return key.Value.MainKey != KeyCode.None && key.Value.IsDown();
    }
}
