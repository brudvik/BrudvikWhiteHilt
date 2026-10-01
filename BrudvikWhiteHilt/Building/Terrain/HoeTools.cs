using BepInEx.Configuration;
using BrudvikWhiteHilt.Building.Groups;
using System.Globalization;
using UnityEngine;

namespace BrudvikWhiteHilt.Building.Terrain;

/// <summary>
/// The White Hilt hoe's tools: level an area, ramp with an even slope, paint an area, put an area back to the original
/// terrain, a height reference, and a bigger brush for the hoe's own operations. Roads are planned by <see cref="RoadBuilder"/>.
/// </summary>
public static class HoeTools
{
    private const float PlanInterval = 0.3f;
    private const float HeightStep = 0.25f;
    private const float RampSkirt = 2f;
    private const float MinBrush = 1f;

    private static readonly float[] Grades = { 0f, 10f, 15f, 20f, 25f };
    private static readonly float[] Widths = { 3f, 4f, 5f, 6f, 8f };
    private static readonly AreaPicker area = new();

    private static int stage;
    private static float levelHeight;
    private static Vector3 secondCorner;
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
        Reset
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

    private static void HandleKeys(Player player)
    {
        ConfigEntry<KeyboardShortcut>[] keys =
        {
            TerrainSettings.KeyRoad, TerrainSettings.KeyArea, TerrainSettings.KeyShape, TerrainSettings.KeyPaint, TerrainSettings.KeyReset,
            TerrainSettings.KeyReference
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
            else
            {
                SetReference(player);
            }
        }
    }

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

    private static void TickLevel(Player player)
    {
        bool aimed = AreaPicker.Aim(out Vector3 aim);
        if (stage < 2 && aimed)
        {
            area.HandleArrows(aim);
        }

        if (stage == 0)
        {
            BuildGizmos.HideBox();
            HintLine = string.Format(Localization.instance.Localize("$whitehilt_terrain_hint_rect"), Localization.instance.Localize("$whitehilt_terrain_level"));
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
                Vector3 second = area.Second(aim);
                area.Show(second, Reference ?? area.First.Value.y);
                Vector2 size = area.Size(second);
                HintLine = string.Format(Localization.instance.Localize("$whitehilt_terrain_hint_rect2"), Localization.instance.Localize("$whitehilt_terrain_level"),
                    Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y));
                if (Clicked(0))
                {
                    secondCorner = second;
                    levelHeight = Reference ?? area.First.Value.y;
                    plan = null;
                    stage = 2;
                }
            }
        }
        else
        {
            if (BuildToolSettings.KeyNudgeUp.Value.IsDown())
            {
                levelHeight += HeightStep;
                plan = null;
            }

            if (BuildToolSettings.KeyNudgeDown.Value.IsDown())
            {
                levelHeight -= HeightStep;
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
                TerrainShapes.Plan final = TerrainShapes.Level(area.First.Value, secondCorner, levelHeight, PaintFor(Paving));
                if (TerrainShapes.Run(player, final))
                {
                    stage = 0;
                    area.Clear();
                    return;
                }
            }

            area.Show(secondCorner, levelHeight);
            if (plan == null || Time.unscaledTime >= nextPlan)
            {
                nextPlan = Time.unscaledTime + PlanInterval;
                plan = TerrainShapes.Level(area.First.Value, secondCorner, levelHeight, PaintFor(Paving));
            }

            Vector2 size = area.Size(secondCorner);
            HintLine = string.Format(Localization.instance.Localize("$whitehilt_terrain_hint_level"), Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y),
                Metres(levelHeight), Mathf.RoundToInt(plan.Dig), Mathf.RoundToInt(plan.Fill), Status(plan));
        }

        Back();
    }

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

    private static void TickRectangle(Player player)
    {
        bool aimed = AreaPicker.Aim(out Vector3 aim);
        string name = Localization.instance.Localize(Mode == ToolMode.Paint ? "$whitehilt_terrain_paint" : "$whitehilt_terrain_reset");
        if (!area.First.HasValue)
        {
            BuildGizmos.HideBox();
            HintLine = string.Format(Localization.instance.Localize("$whitehilt_terrain_hint_rect"), name);
            if (Clicked(0) && aimed)
            {
                area.SetFirst(aim);
            }
        }
        else if (aimed)
        {
            area.HandleArrows(aim);
            Vector3 second = area.Second(aim);
            area.Show(second, aim.y);
            Vector2 size = area.Size(second);
            string cost = string.Empty;
            if (Mode == ToolMode.Paint)
            {
                cost = "   " + TerrainCost.Text(TerrainCost.Amount(PaintFor(Paving) == TerrainModifier.PaintType.Paved || Paving == Surface.None ? size.x * size.y : 0f, 0f));
            }

            HintLine = string.Format(Localization.instance.Localize("$whitehilt_terrain_hint_rect2"), name, Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y)) + cost;
            if (Clicked(0))
            {
                if (Mode == ToolMode.Paint)
                {
                    TerrainShapes.Run(player, TerrainShapes.PaintArea(area.First.Value, second, PaintFor(Paving) ?? TerrainModifier.PaintType.Paved));
                }
                else
                {
                    ResetArea(player, area.First.Value, second);
                }

                area.Clear();
                return;
            }
        }

        Back();
    }

    private static void ResetArea(Player player, Vector3 a, Vector3 b)
    {
        Vector2 min = new(Mathf.Min(a.x, b.x), Mathf.Min(a.z, b.z));
        Vector2 max = new(Mathf.Max(a.x, b.x), Mathf.Max(a.z, b.z));
        if ((max.x - min.x) * (max.y - min.y) > TerrainSettings.MaxArea.Value)
        {
            player.Message(MessageHud.MessageType.Center, string.Format(Localization.instance.Localize("$msg_whitehilt_terrain_too_big"),
                Mathf.RoundToInt((max.x - min.x) * (max.y - min.y)), Mathf.RoundToInt(TerrainSettings.MaxArea.Value)));
            return;
        }

        for (float x = min.x; x <= max.x; x += 2f)
        {
            for (float z = min.y; z <= max.y; z += 2f)
            {
                Vector3 point = new(x, a.y, z);
                if (!PrivateArea.CheckAccess(point, 0f, true) || Location.IsInsideNoBuildLocation(point))
                {
                    return;
                }
            }
        }

        TerrainEdit.Job job = new();
        Vector3 centre = new((min.x + max.x) / 2f, a.y, (min.y + max.y) / 2f);
        TerrainEdit.Reset(job, point => point.x >= min.x - 0.01f && point.x <= max.x + 0.01f && point.z >= min.y - 0.01f && point.z <= max.y + 0.01f,
            centre, Vector2.Distance(min, max) / 2f + 1f);
        TerrainEdit.Complete(job);
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
}
