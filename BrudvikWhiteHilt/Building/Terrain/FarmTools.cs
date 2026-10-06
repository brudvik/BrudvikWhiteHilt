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

    private static readonly AreaPicker area = new();
    private static readonly List<GroupPlacer.Item> items = new();
    private static readonly List<bool> spotOk = new();
    private static readonly List<Vector3> lastGrid = new();
    private static readonly Collider[] hits = new Collider[16];
    private static readonly Collider[] nearby = new Collider[128];
    private static readonly Dictionary<string, float> reaches = new();

    private static int rows = 4;
    private static int columns = 6;
    private static float? spacing;
    private static float yaw;
    private static float wheelAmount;
    private static float nextCheck;
    private static Piece lastGridPiece;
    private static Quaternion lastGridRotation;
    private static int spaceMask;
    private static float? maxGrowRadius;
    private static HashSet<string> crops;

    private static int MaxRowsOrColumns => TerrainSettings.MaxGridRows.Value;

    private static int SpaceMask => spaceMask != 0 ? spaceMask : spaceMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid");

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
        maxGrowRadius = null;
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

    // The cultivator's tool keys: each switches a tool on or off, or refills the last planted grid.
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

    // The planting grid while the cultivator is out: rows, columns and spacing from the nudge keys, rotation from the
    // wheel, and a green or red ghost for each spot (SpotOk). The spacing never goes below what the plant needs, or the
    // plants would stop each other growing. A click plants every free spot; a right click leaves the tool.
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

        // A neighbour's sapling or grown crop must stay outside the grow radius, or the plant beside it stops growing.
        float minimum = plant.m_growRadius + Reach(plant) + SpaceMargin;
        float step = Mathf.Max(minimum, spacing ?? minimum);
        if (Pressed(BuildToolSettings.KeyNudgeForward)) rows = Mathf.Min(MaxRowsOrColumns, rows + 1);
        if (Pressed(BuildToolSettings.KeyNudgeBack)) rows = Mathf.Max(1, rows - 1);
        if (Pressed(BuildToolSettings.KeyNudgeRight)) columns = Mathf.Min(MaxRowsOrColumns, columns + 1);
        if (Pressed(BuildToolSettings.KeyNudgeLeft)) columns = Mathf.Max(1, columns - 1);
        rows = Mathf.Min(MaxRowsOrColumns, rows);
        columns = Mathf.Min(MaxRowsOrColumns, columns);
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

    // Plants the free spots of the grid in one go through the group placer, which checks and takes the cost like single
    // planting. With auto-cultivate on, the ground under each spot is cultivated first, so seeds that need cultivated
    // ground can go straight into grass. The grid is remembered so Refill can plant it again after a harvest.
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

    // The cultivate and harvest tools: the first click sets one corner of a rectangle, the arrows nudge it, and the
    // second click cultivates or harvests the whole area.
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

    // Picks every ripe crop in the rectangle the player may use, as if each were picked by hand, so drops, skill and
    // wards behave as usual. Only crops (the grown prefabs of plants) are picked, not wild berries or mushrooms; the
    // server can switch this off.
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

    // The prefab names of everything a plant grows into that can be picked: the crops. Read from the game's prefabs, so
    // crops added by updates and other mods count too.
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

    // Whether a plant may go at a point: in its biome, on cultivated ground if it needs it (unless auto-cultivate is
    // on), outside others' wards, and with room to grow. The room check is the plant's own (nothing within its grow
    // radius), and it also keeps the new plant from stopping the plants around it.
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

        // As the plant itself checks: anything within its grow radius, other than an unhealthy plant, stops it growing.
        if (Physics.OverlapSphereNonAlloc(point, plant.m_growRadius, hits, SpaceMask) > 0)
        {
            return false;
        }

        // The new plant, as a sapling or grown, must not stop the plants around it either, and their crops not it.
        float reach = Reach(plant);
        float search = reach + Mathf.Max(plant.m_growRadius, MaxGrowRadius()) + 1f;
        int count = Physics.OverlapSphereNonAlloc(point, search, nearby, SpaceMask);
        for (int i = 0; i < count; i++)
        {
            Plant other = nearby[i].GetComponent<Plant>();
            if (other == null)
            {
                continue;
            }

            Vector3 apart = other.transform.position - point;
            float needed = Mathf.Max(other.m_growRadius + reach, plant.m_growRadius + Reach(other));
            if (new Vector2(apart.x, apart.z).magnitude < needed)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// How far a plant's colliders reach sideways from its centre, as a sapling or grown at its largest size.
    /// </summary>
    private static float Reach(Plant plant)
    {
        string name = Utils.GetPrefabName(plant.gameObject);
        if (reaches.TryGetValue(name, out float reach))
        {
            return reach;
        }

        reach = Reach(plant.gameObject, 1f);
        foreach (GameObject grown in plant.m_grownPrefabs)
        {
            if (grown != null)
            {
                reach = Mathf.Max(reach, Reach(grown, Mathf.Max(1f, plant.m_maxScale)));
            }
        }

        reaches[name] = reach;
        return reach;
    }

    private static float Reach(GameObject root, float scale)
    {
        float reach = 0f;
        Matrix4x4 toRootSpace = root.transform.worldToLocalMatrix;
        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
        {
            if (collider.isTrigger || (SpaceMask & (1 << collider.gameObject.layer)) == 0)
            {
                continue;
            }

            Matrix4x4 toRoot = toRootSpace * collider.transform.localToWorldMatrix;
            reach = Mathf.Max(reach, ColliderReach(collider, toRoot) * scale);
        }

        return reach;
    }

    // How far a collider reaches sideways from a plant's root, in the plant's space: how much room the plant takes when
    // it is grown, used to keep neighbouring plants from blocking each other.
    private static float ColliderReach(Collider collider, Matrix4x4 toRoot)
    {
        float size = Mathf.Max(toRoot.GetColumn(0).magnitude, toRoot.GetColumn(1).magnitude, toRoot.GetColumn(2).magnitude);
        switch (collider)
        {
            case SphereCollider sphere:
                return Sideways(toRoot.MultiplyPoint3x4(sphere.center)) + sphere.radius * size;
            case CapsuleCollider capsule:
                Vector3 axis = capsule.direction == 0 ? Vector3.right : capsule.direction == 1 ? Vector3.up : Vector3.forward;
                Vector3 half = axis * Mathf.Max(0f, capsule.height / 2f - capsule.radius);
                return Mathf.Max(Sideways(toRoot.MultiplyPoint3x4(capsule.center + half)), Sideways(toRoot.MultiplyPoint3x4(capsule.center - half)))
                    + capsule.radius * size;
            case BoxCollider box:
                return BoxReach(box.center, box.size / 2f, toRoot);
            case MeshCollider mesh when mesh.sharedMesh != null:
                return BoxReach(mesh.sharedMesh.bounds.center, mesh.sharedMesh.bounds.extents, toRoot);
            default:
                return 0f;
        }
    }

    private static float BoxReach(Vector3 centre, Vector3 extents, Matrix4x4 toRoot)
    {
        float reach = 0f;
        for (int corner = 0; corner < 8; corner++)
        {
            Vector3 sign = new((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f);
            reach = Mathf.Max(reach, Sideways(toRoot.MultiplyPoint3x4(centre + Vector3.Scale(extents, sign))));
        }

        return reach;
    }

    private static float Sideways(Vector3 point)
    {
        return new Vector2(point.x, point.z).magnitude;
    }

    // The largest grow radius of any plant, read once from the game's prefabs: how far to look for a neighbour that a
    // new plant might block.
    private static float MaxGrowRadius()
    {
        if (maxGrowRadius.HasValue || ZNetScene.instance == null)
        {
            return maxGrowRadius ?? 0f;
        }

        float largest = 0f;
        foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
        {
            Plant plant = prefab != null ? prefab.GetComponent<Plant>() : null;
            if (plant != null)
            {
                largest = Mathf.Max(largest, plant.m_growRadius);
            }
        }

        maxGrowRadius = largest;
        return largest;
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
