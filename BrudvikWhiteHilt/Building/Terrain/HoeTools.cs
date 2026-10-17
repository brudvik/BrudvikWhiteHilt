using BepInEx.Configuration;
using BrudvikWhiteHilt.Building.Groups;
using BrudvikWhiteHilt.Building.Moats;
using System.Globalization;
using UnityEngine;

namespace BrudvikWhiteHilt.Building.Terrain;

/// <summary>
/// The White Hilt hoe's tools: level an area, ramp with an even slope, paint an area, put an area back to the original
/// terrain, a height reference, and a bigger brush for the hoe's own operations. Roads are planned by <see cref="RoadBuilder"/>.
/// The areas can be turned with the wheel, given a size on the panel, and levelled tile by tile next to the last one.
/// </summary>
public static class HoeTools
{
    private const float PlanInterval = 0.3f;
    private const float HeightStep = 0.25f;
    private const float RampSkirt = 2f;
    private const float MinBrush = 1f;

    private static readonly float[] Grades = { 0f, 10f, 15f, 20f, 25f };
    private static readonly float[] Widths = { 3f, 4f, 5f, 6f, 8f };
    private static readonly Vector2[] Sizes = { new(1f, 40f), new(4f, 4f), new(8f, 8f), new(16f, 16f), new(40f, 40f) };
    private static readonly AreaPicker area = new();

    private static int stage;
    private static float levelHeight;
    private static GroundRect levelRect;
    private static GroundRect? lastLevel;
    private static float lastHeight;
    private static float turnAmount;
    private static Vector3? rampStart;
    private static TerrainShapes.Plan plan;
    private static float nextPlan;
    private static float wheelAmount;
    private static float brushRadius;

    private static float MaxBrush => TerrainSettings.MaxBrushRadius.Value;

    /// <summary>
    /// The hoe tool modes.
    /// </summary>
    public enum ToolMode
    {
        /// <summary>No tool.</summary>
        None,

        /// <summary>Level an area.</summary>
        Level,

        /// <summary>Ramp with an even slope.</summary>
        Ramp,

        /// <summary>Paint an area.</summary>
        Paint,

        /// <summary>Put an area back to the original terrain.</summary>
        Reset,

        /// <summary>Dig a moat.</summary>
        Moat
    }

    /// <summary>
    /// Ground surfaces the tools can lay.
    /// </summary>
    public enum Surface
    {
        /// <summary>Leave the ground as it is.</summary>
        None,

        /// <summary>Paved stone.</summary>
        Stone,

        /// <summary>Dirt.</summary>
        Dirt,

        /// <summary>Grass, the ground's own look.</summary>
        Grass
    }

    /// <summary>The current mode.</summary>
    public static ToolMode Mode { get; private set; }

    /// <summary>True while a hoe tool is in use; vanilla placement is paused then.</summary>
    public static bool Active => Mode != ToolMode.None;

    /// <summary>Help for the current mode, for the key hint.</summary>
    public static string HintLine { get; private set; } = string.Empty;

    /// <summary>Surface for levelling, ramps, roads and painting.</summary>
    public static Surface Paving { get; private set; } = Surface.Stone;

    /// <summary>Locked ramp slope in percent, or 0 for free.</summary>
    public static float Grade => Grades[gradeIndex];

    /// <summary>Width of ramps and new roads in metres.</summary>
    public static float Width { get; private set; }

    /// <summary>Whether new roads get an even slope between their points instead of following the ground.</summary>
    public static bool EvenRoad { get; private set; }

    /// <summary>Radius of the big brush in metres, or 0 for the hoe's own size.</summary>
    public static float BrushRadius
    {
        get => Mathf.Min(brushRadius, MaxBrush);
        private set => brushRadius = value;
    }

    /// <summary>The height reference, or null.</summary>
    public static float? Reference { get; private set; }

    /// <summary>The size chosen for the areas, for the panel.</summary>
    public static string SizeText => area.Preset.HasValue
        ? $"{area.Preset.Value.x:0} x {area.Preset.Value.y:0} m"
        : Localization.instance.Localize("$whitehilt_terrain_size_free");

    /// <summary>The heading of the areas, for the panel.</summary>
    public static string TurnText => Mathf.RoundToInt(area.Yaw) + "°";

    private static int gradeIndex;

    /// <summary>
    /// The paint for a surface.
    /// </summary>
    /// <param name="surface">The surface.</param>
    /// <returns>The paint, or null for none.</returns>
    public static TerrainModifier.PaintType? PaintFor(Surface surface)
    {
        return surface switch
        {
            Surface.Stone => TerrainModifier.PaintType.Paved,
            Surface.Dirt => TerrainModifier.PaintType.Dirt,
            Surface.Grass => TerrainModifier.PaintType.Reset,
            _ => null
        };
    }

    /// <summary>
    /// Name of a surface.
    /// </summary>
    /// <param name="surface">The surface.</param>
    /// <returns>The text.</returns>
    public static string SurfaceName(Surface surface)
    {
        return Localization.instance.Localize("$whitehilt_terrain_surface_" + surface.ToString().ToLowerInvariant());
    }

    /// <summary>
    /// Handles the hoe keys and the current tool. Called every frame while the White Hilt hoe is held.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Tick(Player player)
    {
        if (Width <= 0f)
        {
            Width = Mathf.Clamp(TerrainSettings.RoadWidth.Value, 1f, 12f);
        }

        if (GroupTools.Active && Active)
        {
            Exit();
        }

        HandleKeys(player);
        UpdateBrush(player);
        switch (Mode)
        {
            case ToolMode.Level:
                TickLevel(player);
                break;
            case ToolMode.Ramp:
                TickRamp(player);
                break;
            case ToolMode.Paint:
            case ToolMode.Reset:
                TickRectangle(player);
                break;
            case ToolMode.Moat:
                HintLine = string.Empty;
                if (!MoatTool.Tick(player))
                {
                    Exit();
                }

                break;
            default:
                HintLine = string.Empty;
                break;
        }
    }

    /// <summary>
    /// Starts a tool, or stops it if it is already on.
    /// </summary>
    /// <param name="mode">The tool.</param>
    public static void Toggle(ToolMode mode)
    {
        bool same = Mode == mode;
        Exit();
        if (!same)
        {
            GroupTools.Exit();
            Mode = mode;
        }
    }

    /// <summary>
    /// Leaves the current tool.
    /// </summary>
    public static void Exit()
    {
        Mode = ToolMode.None;
        stage = 0;
        rampStart = null;
        plan = null;
        area.Clear();
        BuildGizmos.HideBox();
        MoatTool.Clear();
        HintLine = string.Empty;
    }

    /// <summary>
    /// Sets the height reference to where the camera aims.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void SetReference(Player player)
    {
        if (AreaPicker.Aim(out Vector3 point))
        {
            Reference = point.y;
            player.Message(MessageHud.MessageType.TopLeft, string.Format(Localization.instance.Localize("$msg_whitehilt_terrain_reference"), Metres(point.y)));
        }
    }

    /// <summary>
    /// Next surface.
    /// </summary>
    public static void CycleSurface()
    {
        Paving = (Surface)(((int)Paving + 1) % 4);
        plan = null;
    }

    /// <summary>
    /// Next size for the areas: free, then 1 x 40, 4 x 4, 8 x 8, 16 x 16 and 40 x 40 m.
    /// </summary>
    public static void CycleSize()
    {
        int index = area.Preset.HasValue ? System.Array.IndexOf(Sizes, area.Preset.Value) : -1;
        area.Preset = index + 1 < Sizes.Length ? Sizes[index + 1] : null;
        plan = null;
    }

    /// <summary>
    /// Asks for a size for the areas, such as "40 x 40"; one number makes a square.
    /// </summary>
    public static void AskSize()
    {
        SizeDialog.Ask(area.Preset.HasValue ? SizeText : string.Empty, text =>
        {
            if (AreaPicker.TryParseSize(text, out Vector2 size))
            {
                area.Preset = size;
                plan = null;
            }
            else
            {
                Player.m_localPlayer?.Message(MessageHud.MessageType.Center, "$msg_whitehilt_terrain_size_bad");
            }
        });
    }

    /// <summary>
    /// Turns the areas the way the camera looks, or back along the world's axes.
    /// </summary>
    public static void ToggleTurn()
    {
        if (area.Yaw != 0f || GameCamera.instance == null)
        {
            area.Yaw = 0f;
            return;
        }

        float step = BuildRotation.Step;
        area.Yaw = Mathf.Repeat(Mathf.Round(GameCamera.instance.transform.eulerAngles.y / step) * step, 360f);
    }

    /// <summary>
    /// Levels the tile next to the last levelled area, on the side the camera looks to, at the same height to begin with.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void NextTile(Player player)
    {
        if (!lastLevel.HasValue)
        {
            player.Message(MessageHud.MessageType.TopLeft, "$msg_whitehilt_terrain_no_tile");
            return;
        }

        if (Mode != ToolMode.Level)
        {
            Exit();
            GroupTools.Exit();
            Mode = ToolMode.Level;
        }

        Vector3 look = GameCamera.instance != null ? GameCamera.instance.transform.forward : player.transform.forward;
        levelRect = lastLevel.Value.Next(look);
        area.SetFirstExact(levelRect.Corner);
        levelHeight = lastHeight;
        plan = null;
        stage = 2;
    }

    /// <summary>
    /// Next locked slope.
    /// </summary>
    public static void CycleGrade()
    {
        gradeIndex = (gradeIndex + 1) % Grades.Length;
    }

    /// <summary>
    /// Makes ramps and new roads wider or narrower, 1 to 12 m.
    /// </summary>
    /// <param name="metres">Metres to add.</param>
    public static void ChangeWidth(float metres)
    {
        Width = Mathf.Clamp(Width + metres, 1f, 12f);
        plan = null;
    }

    /// <summary>
    /// Next width.
    /// </summary>
    public static void CycleWidth()
    {
        int index = System.Array.IndexOf(Widths, Width);
        Width = Widths[(index + 1) % Widths.Length];
        plan = null;
    }

    /// <summary>
    /// Road height between road points: following the ground or an even slope.
    /// </summary>
    public static void ToggleEvenRoad()
    {
        EvenRoad = !EvenRoad;
    }

    /// <summary>
    /// True if the mouse wheel with the tilt modifier sets the brush size instead of tilting.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <returns>True for the brush.</returns>
    public static bool WheelSetsBrush(Player player)
    {
        return !Active && TerrainSettings.HoldingHoe(player) && player.m_buildPieces?.GetSelectedPiece()?.GetComponent<TerrainOp>() != null;
    }

    /// <summary>
    /// Formats a height or length in metres with one decimal.
    /// </summary>
    /// <param name="metres">The value.</param>
    /// <returns>The text.</returns>
    public static string Metres(float metres)
    {
        return metres.ToString("0.0", CultureInfo.InvariantCulture) + " m";
    }

    // The hoe's tool keys: each switches a tool on or off, starts planning a road or sets the reference height.
    private static void HandleKeys(Player player)
    {
        ConfigEntry<KeyboardShortcut>[] keys =
        {
            TerrainSettings.KeyRoad, TerrainSettings.KeyArea, TerrainSettings.KeyShape, TerrainSettings.KeyPaint, TerrainSettings.KeyReset,
            TerrainSettings.KeyReference, TerrainSettings.KeyHarvest
        };
        foreach (ConfigEntry<KeyboardShortcut> key in keys)
        {
            if (key.Value.MainKey == KeyCode.None || !key.Value.IsDown())
            {
                continue;
            }

            if (key == TerrainSettings.KeyRoad)
            {
                Exit();
                RoadBuilder.StartPlanning(player);
            }
            else if (key == TerrainSettings.KeyArea)
            {
                Toggle(ToolMode.Level);
            }
            else if (key == TerrainSettings.KeyShape)
            {
                Toggle(ToolMode.Ramp);
            }
            else if (key == TerrainSettings.KeyPaint)
            {
                Toggle(ToolMode.Paint);
            }
            else if (key == TerrainSettings.KeyReset)
            {
                Toggle(ToolMode.Reset);
            }
            else if (key == TerrainSettings.KeyHarvest)
            {
                Toggle(ToolMode.Moat);
            }
            else
            {
                SetReference(player);
            }
        }
    }

    // The brush size for the vanilla hoe pieces: the wheel with the tilt modifier held makes the brush larger or
    // smaller, shown as a circle around the ghost. Size 0 leaves vanilla's own brush.
    private static void UpdateBrush(Player player)
    {
        if (!WheelSetsBrush(player))
        {
            BuildGizmos.HideCircle();
            return;
        }

        if (Input.GetKey(BuildToolSettings.TiltWheelModifier.Value))
        {
            wheelAmount += ZInput.GetMouseScrollWheel();
            int direction = wheelAmount > player.m_scrollAmountThreshold ? 1 : wheelAmount < -player.m_scrollAmountThreshold ? -1 : 0;
            if (direction != 0)
            {
                wheelAmount = 0f;
                float next = BrushRadius <= 0f ? (direction > 0 ? MinBrush : 0f) : BrushRadius + direction * 0.5f;
                BrushRadius = next < MinBrush ? 0f : Mathf.Min(next, MaxBrush);
            }
        }

        GameObject ghost = player.m_placementGhost;
        if (BrushRadius > 0f && ghost != null && ghost.activeSelf)
        {
            BuildGizmos.ShowCircle(ghost.transform.position, BrushRadius);
        }
        else
        {
            BuildGizmos.HideCircle();
        }
    }

    // The level tool in three steps: the first two clicks set the rectangle (the wheel turns it), the height starts at
    // the first corner (or the reference) and PgUp/PgDn change it, and the third click levels the area. The cost and how
    // much is dug and filled are shown before it is done. The stamp key levels the tile next to the last one.
    private static void TickLevel(Player player)
    {
        bool aimed = AreaPicker.Aim(out Vector3 aim);
        if (stage < 2)
        {
            HandleTurn(player);
            if (aimed)
            {
                area.HandleArrows(aim);
            }
        }

        if (KeyDown(BuildToolSettings.KeyStamp))
        {
            NextTile(player);
        }

        if (stage == 0)
        {
            if (aimed && area.Preset.HasValue)
            {
                AreaPicker.ShowRect(area.Rect(aim), Reference ?? aim.y);
            }
            else
            {
                BuildGizmos.HideBox();
            }

            HintLine = string.Format(Localization.instance.Localize("$whitehilt_terrain_hint_rect"), Localization.instance.Localize("$whitehilt_terrain_level"))
                + TurnHint();
            if (Clicked(0) && aimed)
            {
                area.SetFirst(aim);
                stage = 1;
            }
        }
        else if (stage == 1)
        {
            if (aimed)
            {
                GroundRect rect = area.Rect(aim);
                AreaPicker.ShowRect(rect, Reference ?? area.First.Value.y);
                HintLine = string.Format(Localization.instance.Localize("$whitehilt_terrain_hint_rect2"), Localization.instance.Localize("$whitehilt_terrain_level"),
                    Mathf.RoundToInt(rect.Extent.x), Mathf.RoundToInt(rect.Extent.y)) + TurnHint();
                if (Clicked(0))
                {
                    levelRect = rect;
                    levelHeight = Reference ?? area.First.Value.y;
                    plan = null;
                    stage = 2;
                }
            }
        }
        else
        {
            if (KeyDown(BuildToolSettings.KeyNudgeUp))
            {
                levelHeight += HeightStepNow();
                plan = null;
            }

            if (KeyDown(BuildToolSettings.KeyNudgeDown))
            {
                levelHeight -= HeightStepNow();
                plan = null;
            }

            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (Clicked(0) && shift && aimed)
            {
                levelHeight = aim.y;
                plan = null;
            }
            else if (Clicked(0))
            {
                TerrainShapes.Plan final = TerrainShapes.Level(levelRect, levelHeight, PaintFor(Paving));
                if (TerrainShapes.Run(player, final))
                {
                    lastLevel = levelRect;
                    lastHeight = levelHeight;
                    stage = 0;
                    area.Clear();
                    return;
                }
            }

            AreaPicker.ShowRect(levelRect, levelHeight);
            if (plan == null || Time.unscaledTime >= nextPlan)
            {
                nextPlan = Time.unscaledTime + PlanInterval;
                plan = TerrainShapes.Level(levelRect, levelHeight, PaintFor(Paving));
            }

            Vector2 size = levelRect.Extent;
            HintLine = string.Format(Localization.instance.Localize("$whitehilt_terrain_hint_level"), Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y),
                Metres(levelHeight), Mathf.RoundToInt(plan.Dig), Mathf.RoundToInt(plan.Fill), Status(plan));
        }

        Back();
    }

    // PgUp/PgDn move the height a quarter metre, a whole metre with Shift and a tenth with Ctrl.
    private static float HeightStepNow()
    {
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            return 1f;
        }

        return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ? 0.1f : HeightStep;
    }

    // The wheel turns the areas by the rotation step, as it turns the cultivator's grid.
    private static void HandleTurn(Player player)
    {
        turnAmount += ZInput.GetMouseScrollWheel();
        if (Mathf.Abs(turnAmount) > player.m_scrollAmountThreshold)
        {
            area.Yaw = Mathf.Repeat(area.Yaw + Mathf.Sign(turnAmount) * BuildRotation.Step, 360f);
            turnAmount = 0f;
        }
    }

    private static string TurnHint()
    {
        return "   " + string.Format(Localization.instance.Localize("$whitehilt_terrain_hint_turn"), TurnText);
    }

    // The ramp tool: the first click sets the start, the end follows the aim (or the chosen slope), and the second
    // click builds a band of terrain between them. Length, rise and slope are shown while aiming.
    private static void TickRamp(Player player)
    {
        bool aimed = AreaPicker.Aim(out Vector3 aim);
        if (BuildToolSettings.KeyNudgeForward.Value.IsDown())
        {
            ChangeWidth(1f);
        }

        if (BuildToolSettings.KeyNudgeBack.Value.IsDown())
        {
            ChangeWidth(-1f);
        }

        string info = string.Empty;
        if (rampStart.HasValue && aimed)
        {
            Vector3 start = rampStart.Value;
            Vector3 end = RampEnd(start, aim);
            ShowRamp(start, end);
            if (plan == null || Time.unscaledTime >= nextPlan)
            {
                nextPlan = Time.unscaledTime + PlanInterval;
                plan = TerrainShapes.Band(new[] { start, end }, Width, RampSkirt, PaintFor(Paving));
            }

            float length = Vector2.Distance(new Vector2(start.x, start.z), new Vector2(end.x, end.z));
            float rise = end.y - start.y;
            float slope = length > 0.01f ? rise / length * 100f : 0f;
            info = string.Format(Localization.instance.Localize("$whitehilt_terrain_ramp_info"), Mathf.RoundToInt(length), Metres(rise),
                Mathf.RoundToInt(Mathf.Abs(slope)), Mathf.RoundToInt(Mathf.Atan(Mathf.Abs(slope) / 100f) * Mathf.Rad2Deg) + "°", Status(plan));

            if (Clicked(0) && TerrainShapes.Run(player, TerrainShapes.Band(new[] { start, end }, Width, RampSkirt, PaintFor(Paving))))
            {
                // The next ramp starts where this one ended, for switchbacks.
                rampStart = end;
                plan = null;
            }
        }
        else
        {
            BuildGizmos.HideBox();
            if (Clicked(0) && aimed)
            {
                rampStart = aim;
            }
        }

        HintLine = string.Format(Localization.instance.Localize("$whitehilt_terrain_hint_ramp"),
            Localization.instance.Localize(rampStart.HasValue ? "$whitehilt_terrain_ramp_end" : "$whitehilt_terrain_ramp_start"), Mathf.RoundToInt(Width), info);

        if (Clicked(1))
        {
            if (rampStart.HasValue)
            {
                rampStart = null;
                plan = null;
            }
            else
            {
                Exit();
            }
        }
    }

    // The paint and reset tools: the first click sets one corner of a rectangle and the second paints or resets the
    // whole area. Painting shows its stone cost while aiming.
    private static void TickRectangle(Player player)
    {
        bool aimed = AreaPicker.Aim(out Vector3 aim);
        string name = Localization.instance.Localize(Mode == ToolMode.Paint ? "$whitehilt_terrain_paint" : "$whitehilt_terrain_reset");
        HandleTurn(player);
        if (!area.First.HasValue)
        {
            if (aimed && area.Preset.HasValue)
            {
                AreaPicker.ShowRect(area.Rect(aim), aim.y);
            }
            else
            {
                BuildGizmos.HideBox();
            }

            HintLine = string.Format(Localization.instance.Localize("$whitehilt_terrain_hint_rect"), name) + TurnHint();
            if (Clicked(0) && aimed)
            {
                area.SetFirst(aim);
            }
        }
        else if (aimed)
        {
            area.HandleArrows(aim);
            GroundRect rect = area.Rect(aim);
            AreaPicker.ShowRect(rect, aim.y);
            Vector2 size = rect.Extent;
            string cost = string.Empty;
            if (Mode == ToolMode.Paint)
            {
                cost = "   " + TerrainCost.Text(TerrainCost.Amount(PaintFor(Paving) == TerrainModifier.PaintType.Paved || Paving == Surface.None ? rect.Area : 0f, 0f));
            }

            HintLine = string.Format(Localization.instance.Localize("$whitehilt_terrain_hint_rect2"), name, Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y))
                + cost + TurnHint();
            if (Clicked(0))
            {
                if (Mode == ToolMode.Paint)
                {
                    TerrainShapes.Run(player, TerrainShapes.PaintArea(rect, PaintFor(Paving) ?? TerrainModifier.PaintType.Paved));
                }
                else
                {
                    ResetArea(player, rect);
                }

                area.Clear();
                return;
            }
        }

        Back();
    }

    // Puts the terrain in a rectangle back as the world generated it. Refused when the area is too large or any of it
    // lies in a ward or a place where building is not allowed. Moats inside it are forgotten too.
    private static void ResetArea(Player player, GroundRect rect)
    {
        if (rect.Area > TerrainSettings.MaxArea.Value)
        {
            player.Message(MessageHud.MessageType.Center, string.Format(Localization.instance.Localize("$msg_whitehilt_terrain_too_big"),
                Mathf.RoundToInt(rect.Area), Mathf.RoundToInt(TerrainSettings.MaxArea.Value)));
            return;
        }

        rect.Bounds(0f, out Vector2 min, out Vector2 max);
        for (float x = min.x; x <= max.x; x += 2f)
        {
            for (float z = min.y; z <= max.y; z += 2f)
            {
                Vector3 point = new(x, rect.Corner.y, z);
                if (rect.OutsideDistance(x, z) <= 1f && (!PrivateArea.CheckAccess(point, 0f, true) || Location.IsInsideNoBuildLocation(point)))
                {
                    return;
                }
            }
        }

        TerrainEdit.Job job = new();
        Vector3 centre = rect.Corner + rect.Right * (rect.Size.x / 2f) + rect.Forward * (rect.Size.y / 2f);
        TerrainEdit.Reset(job, point => rect.OutsideDistance(point.x, point.z) <= 0.01f, centre, rect.Extent.magnitude / 2f + 1f);
        TerrainEdit.Complete(job);
        MoatSection.RemoveInside(min, max);
    }

    private static Vector3 RampEnd(Vector3 start, Vector3 aim)
    {
        if (Grade <= 0f)
        {
            return aim;
        }

        float length = Vector2.Distance(new Vector2(start.x, start.z), new Vector2(aim.x, aim.z));
        float sign = aim.y >= start.y ? 1f : -1f;
        return new Vector3(aim.x, start.y + sign * length * Grade / 100f, aim.z);
    }

    private static void ShowRamp(Vector3 start, Vector3 end)
    {
        Vector3 along = end - start;
        along.y = 0f;
        Vector3 side = Vector3.Cross(Vector3.up, along.sqrMagnitude > 0.0001f ? along.normalized : Vector3.forward) * (Width / 2f);
        Vector3 lift = Vector3.up * 0.15f;
        BuildGizmos.ShowPath(new[] { start + side + lift, end + side + lift, end - side + lift, start - side + lift, start + side + lift });
    }

    private static string Status(TerrainShapes.Plan shown)
    {
        if (shown == null)
        {
            return string.Empty;
        }

        string text = shown.Problem ?? TerrainCost.Text(shown.Cost);
        if (shown.HitsLimit)
        {
            text += "   " + Localization.instance.Localize("$msg_whitehilt_terrain_limit");
        }

        return text;
    }

    // A right click steps back: one stage of the level tool, the rectangle's first corner, or out of the tool.
    private static void Back()
    {
        if (!Clicked(1))
        {
            return;
        }

        if (Mode == ToolMode.Level && stage > 0)
        {
            stage--;
            if (stage == 0)
            {
                area.Clear();
            }

            plan = null;
            return;
        }

        if (area.First.HasValue)
        {
            area.Clear();
            return;
        }

        Exit();
    }

    private static bool Clicked(int button)
    {
        return !BuildToolbar.CursorMode && Input.GetMouseButtonDown(button);
    }

    // A key by its main key alone, so it still counts with Shift or Ctrl held for a bigger or smaller step.
    private static bool KeyDown(ConfigEntry<KeyboardShortcut> key)
    {
        return key.Value.MainKey != KeyCode.None && Input.GetKeyDown(key.Value.MainKey);
    }

    // Asks for a size with the game's own text dialog.
    private sealed class SizeDialog : TextReceiver
    {
        private readonly string current;
        private readonly System.Action<string> done;

        private SizeDialog(string current, System.Action<string> done)
        {
            this.current = current;
            this.done = done;
        }

        public static void Ask(string current, System.Action<string> done)
        {
            TextInput.instance?.RequestText(new SizeDialog(current, done), "$whitehilt_terrain_size_ask", 20);
        }

        public string GetText()
        {
            return current;
        }

        public void SetText(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                done(text.Trim());
            }
        }
    }
}
