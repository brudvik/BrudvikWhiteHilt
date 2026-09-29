using BepInEx.Configuration;
using BrudvikWhiteHilt.Building.Groups;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Building.Terrain;

/// <summary>
/// The White Hilt cultivator's tools: plant a grid of rows and columns in one go (cultivating the ground first if wanted),
/// replant the empty places of the last grid, cultivate an area, and harvest every ripe crop in an area.
/// </summary>
public static class FarmTools
{
    private const float CheckInterval = 0.25f;
    private const float SpacingStep = 0.1f;
    private const float SpaceMargin = 0.05f;
    private const int MaxRowsOrColumns = 30;

    private static readonly AreaPicker area = new();
    private static readonly List<GroupPlacer.Item> items = new();
    private static readonly List<bool> spotOk = new();
    private static readonly List<Vector3> lastGrid = new();
    private static readonly Collider[] hits = new Collider[16];

    private static int rows = 4;
    private static int columns = 6;
    private static float? spacing;
    private static float yaw;
    private static float wheelAmount;
    private static float nextCheck;
    private static Piece lastGridPiece;
    private static Quaternion lastGridRotation;
    private static int spaceMask;
    private static HashSet<string> crops;

    /// <summary>
    /// The cultivator tool modes.
    /// </summary>
    public enum ToolMode
    {
        /// <summary>No tool.</summary>
        None,

        /// <summary>Plant a grid.</summary>
        Grid,

        /// <summary>Cultivate an area.</summary>
        Cultivate,

        /// <summary>Harvest an area.</summary>
        Harvest
    }

    /// <summary>The current mode.</summary>
    public static ToolMode Mode { get; private set; }

    /// <summary>True while a cultivator tool is in use; vanilla placement is paused then.</summary>
    public static bool Active => Mode != ToolMode.None;

    /// <summary>Whether the grid cultivates the ground under each place first.</summary>
    public static bool AutoCultivate { get; private set; } = true;

    /// <summary>Help for the current mode, for the key hint.</summary>
    public static string HintLine { get; private set; } = string.Empty;

    /// <summary>Short grid description for the panel.</summary>
    public static string GridText => $"{rows} x {columns}";

    /// <summary>
    /// Handles the cultivator keys and the current tool. Called every frame while the White Hilt cultivator is held.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Tick(Player player)
    {
        if (GroupTools.Active && Active)
        {
            Exit();
        }

        HandleKeys(player);
        switch (Mode)
        {
            case ToolMode.Grid:
                TickGrid(player);
                break;
            case ToolMode.Cultivate:
            case ToolMode.Harvest:
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
        area.Clear();
        items.Clear();
        GroupGhost.Hide();
        BuildGizmos.HideBox();
        HintLine = string.Empty;
    }

    /// <summary>
    /// Cultivating under the grid on or off.
    /// </summary>
    public static void ToggleAutoCultivate()
    {
        AutoCultivate = !AutoCultivate;
    }

    /// <summary>
    /// Plants the empty places of the last grid again.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Refill(Player player)
    {
        if (lastGridPiece == null || lastGrid.Count == 0)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_farm_no_grid");
            return;
        }

        Plant plant = lastGridPiece.GetComponent<Plant>();
        items.Clear();
        spotOk.Clear();
        foreach (Vector3 spot in lastGrid)
        {
            Vector3 point = Ground(spot);
            items.Add(new GroupPlacer.Item { Piece = lastGridPiece, Position = point, Rotation = lastGridRotation });
            spotOk.Add(SpotOk(lastGridPiece, plant, point));
        }

        PlantSpots(player, lastGridPiece, remember: false);
        items.Clear();
    }

    private static void HandleKeys(Player player)
    {
        ConfigEntry<KeyboardShortcut>[] keys = { TerrainSettings.KeyShape, TerrainSettings.KeyArea, TerrainSettings.KeyRoad, TerrainSettings.KeyHarvest };
        foreach (ConfigEntry<KeyboardShortcut> key in keys)
        {
            if (key.Value.MainKey == KeyCode.None || !key.Value.IsDown())
            {
                continue;
            }

            if (key == TerrainSettings.KeyShape)
            {
                Toggle(ToolMode.Grid);
            }
            else if (key == TerrainSettings.KeyArea)
            {
                Toggle(ToolMode.Cultivate);
            }
            else if (key == TerrainSettings.KeyRoad)
            {
                Refill(player);
            }
            else
            {
                Toggle(ToolMode.Harvest);
            }
        }
    }

    private static void TickGrid(Player player)
    {
        Piece piece = player.m_buildPieces?.GetSelectedPiece();
        Plant plant = piece != null ? piece.GetComponent<Plant>() : null;
        if (plant == null || plant.m_attachDistance > 0f)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_farm_no_plant");
            Exit();
            return;
        }

        float minimum = plant.m_growRadius + SpaceMargin;
        float step = Mathf.Max(minimum, spacing ?? minimum);
        if (Pressed(BuildToolSettings.KeyNudgeForward)) rows = Mathf.Min(MaxRowsOrColumns, rows + 1);
        if (Pressed(BuildToolSettings.KeyNudgeBack)) rows = Mathf.Max(1, rows - 1);
        if (Pressed(BuildToolSettings.KeyNudgeRight)) columns = Mathf.Min(MaxRowsOrColumns, columns + 1);
        if (Pressed(BuildToolSettings.KeyNudgeLeft)) columns = Mathf.Max(1, columns - 1);
        if (Pressed(BuildToolSettings.KeyNudgeUp)) spacing = step + SpacingStep;
        if (Pressed(BuildToolSettings.KeyNudgeDown)) spacing = Mathf.Max(minimum, step - SpacingStep);
        step = Mathf.Max(minimum, spacing ?? minimum);

        wheelAmount += ZInput.GetMouseScrollWheel();
        if (Mathf.Abs(wheelAmount) > player.m_scrollAmountThreshold)
        {
            yaw = Mathf.Repeat(yaw + Mathf.Sign(wheelAmount) * BuildRotation.Step, 360f);
            wheelAmount = 0f;
        }

        items.Clear();
        bool aimed = AreaPicker.Aim(out Vector3 centre);
        if (aimed)
        {
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    Vector3 offset = new((column - (columns - 1) / 2f) * step, 0f, (row - (rows - 1) / 2f) * step);
                    items.Add(new GroupPlacer.Item { Piece = piece, Position = Ground(centre + rotation * offset), Rotation = rotation });
                }
            }
        }

        if (items.Count != spotOk.Count || Time.unscaledTime >= nextCheck)
        {
            nextCheck = Time.unscaledTime + CheckInterval;
            spotOk.Clear();
            foreach (GroupPlacer.Item item in items)
            {
                spotOk.Add(SpotOk(piece, plant, item.Position));
            }
        }

        if (items.Count == 0)
        {
            GroupGhost.Hide();
        }
        else
        {
            GroupGhost.Show(items, true, index => index < spotOk.Count && spotOk[index]);
        }

        int good = spotOk.Count(ok => ok);
        string status = string.Format(Localization.instance.Localize("$whitehilt_farm_grid_status"), good, items.Count, CostText(player, piece, good));
        HintLine = string.Format(Localization.instance.Localize("$whitehilt_farm_hint_grid"), Localization.instance.Localize(piece.m_name), rows, columns,
            step.ToString("0.0#", System.Globalization.CultureInfo.InvariantCulture), status);

        if (Clicked(0) && aimed)
        {
            PlantSpots(player, piece, remember: true);
        }
        else if (Clicked(1))
        {
            Exit();
        }
    }

    private static void PlantSpots(Player player, Piece piece, bool remember)
    {
        List<GroupPlacer.Item> plantHere = new();
        for (int i = 0; i < items.Count && i < spotOk.Count; i++)
        {
            if (spotOk[i])
            {
                plantHere.Add(items[i]);
            }
        }

        if (plantHere.Count == 0)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_farm_nothing");
            return;
        }

        string problem = GroupPlacer.Check(player, plantHere, pay: true);
        if (problem != null)
        {
            player.Message(MessageHud.MessageType.Center, problem);
            return;
        }

        if (AutoCultivate && piece.m_cultivatedGroundOnly)
        {
            TerrainEdit.Job job = new() { Undoable = false };
            float radius = Mathf.Max(0.6f, piece.GetComponent<Plant>().m_growRadius);
            TerrainOp.Settings cultivate = TerrainEdit.Paint(TerrainModifier.PaintType.Cultivate, radius);
            foreach (GroupPlacer.Item item in plantHere)
            {
                Heightmap heightmap = Heightmap.FindHeightmap(item.Position);
                if (heightmap != null && !heightmap.IsCultivated(item.Position))
                {
                    job.Ops.Add((item.Position, cultivate));
                }
            }

            if (job.Ops.Count > 0)
            {
                TerrainEdit.Enqueue(job);
            }
        }

        GroupPlacer.Place(player, plantHere, pay: true);
        if (remember)
        {
            lastGridPiece = piece;
            lastGridRotation = plantHere[0].Rotation;
            lastGrid.Clear();
            lastGrid.AddRange(items.Select(item => item.Position));
        }
    }

    private static void TickRectangle(Player player)
    {
        bool aimed = AreaPicker.Aim(out Vector3 aim);
        string name = Localization.instance.Localize(Mode == ToolMode.Cultivate ? "$whitehilt_farm_cultivate" : "$whitehilt_farm_harvest");
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
            HintLine = string.Format(Localization.instance.Localize("$whitehilt_terrain_hint_rect2"), name, Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y));
            if (Clicked(0))
            {
                if (Mode == ToolMode.Cultivate)
                {
                    TerrainShapes.Run(player, TerrainShapes.PaintArea(area.First.Value, second, TerrainModifier.PaintType.Cultivate));
                }
                else
                {
                    Harvest(player, area.First.Value, second);
                }

                area.Clear();
                return;
            }
        }

        if (Clicked(1))
        {
            if (area.First.HasValue)
            {
                area.Clear();
            }
            else
            {
                Exit();
            }
        }
    }

    private static void Harvest(Player player, Vector3 a, Vector3 b)
    {
        if (!TerrainSettings.HarvestArea.Value)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_farm_harvest_off");
            return;
        }

        crops ??= FindCrops();
        Vector3 min = Vector3.Min(a, b);
        Vector3 max = Vector3.Max(a, b);
        Vector3 centre = (min + max) / 2f;
        float radius = Vector2.Distance(new Vector2(min.x, min.z), new Vector2(max.x, max.z)) / 2f + 1f;
        HashSet<Pickable> picked = new();
        foreach (Collider collider in Physics.OverlapSphere(centre, radius))
        {
            Pickable pickable = collider.GetComponentInParent<Pickable>();
            Vector3 at = pickable != null ? pickable.transform.position : default;
            if (pickable == null || picked.Contains(pickable) || at.x < min.x || at.x > max.x || at.z < min.z || at.z > max.z
                || !crops.Contains(Utils.GetPrefabName(pickable.gameObject)) || !pickable.CanBePicked() || !PrivateArea.CheckAccess(at, 0f, false))
            {
                continue;
            }

            if (pickable.Interact(player, false, false))
            {
                picked.Add(pickable);
            }
        }

        player.Message(MessageHud.MessageType.TopLeft, string.Format(Localization.instance.Localize("$msg_whitehilt_farm_harvested"), picked.Count));
    }

    private static HashSet<string> FindCrops()
    {
        HashSet<string> names = new();
        if (ZNetScene.instance == null)
        {
            return names;
        }

        foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
        {
            Plant plant = prefab != null ? prefab.GetComponent<Plant>() : null;
            if (plant == null)
            {
                continue;
            }

            foreach (GameObject grown in plant.m_grownPrefabs)
            {
                if (grown != null && grown.GetComponent<Pickable>() != null)
                {
                    names.Add(grown.name);
                }
            }
        }

        return names;
    }

    private static bool SpotOk(Piece piece, Plant plant, Vector3 point)
    {
        Heightmap heightmap = Heightmap.FindHeightmap(point);
        if (heightmap == null || (heightmap.GetBiome(point) & plant.m_biome) == 0)
        {
            return false;
        }

        if (piece.m_cultivatedGroundOnly && !AutoCultivate && !heightmap.IsCultivated(point))
        {
            return false;
        }

        if (!PrivateArea.CheckAccess(point, 0f, false))
        {
            return false;
        }

        if (spaceMask == 0)
        {
            spaceMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid");
        }

        // As the plant itself checks: anything within its grow radius, other than an unhealthy plant, stops it growing.
        return Physics.OverlapSphereNonAlloc(point, plant.m_growRadius, hits, spaceMask) == 0;
    }

    private static string CostText(Player player, Piece piece, int count)
    {
        List<string> parts = new();
        foreach (Piece.Requirement requirement in piece.m_resources)
        {
            if (requirement.m_resItem != null && requirement.m_amount > 0)
            {
                int need = requirement.m_amount * count;
                int have = GroupPlacer.Have(player, requirement.m_resItem);
                parts.Add($"{Localization.instance.Localize(requirement.m_resItem.m_itemData.m_shared.m_name)} {have}/{need}");
            }
        }

        return string.Join(", ", parts);
    }

    private static Vector3 Ground(Vector3 point)
    {
        return Heightmap.GetHeight(point, out float height) ? new Vector3(point.x, height, point.z) : point;
    }

    private static bool Pressed(ConfigEntry<KeyboardShortcut> key)
    {
        return key.Value.MainKey != KeyCode.None && key.Value.IsDown();
    }

    private static bool Clicked(int button)
    {
        return !BuildToolbar.CursorMode && Input.GetMouseButtonDown(button);
    }
}
