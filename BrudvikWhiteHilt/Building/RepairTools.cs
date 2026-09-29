using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace BrudvikWhiteHilt.Building;

/// <summary>
/// Repair mode for build tools with a repair piece (the hammer): one click repairs every damaged piece in a round
/// column around the point you aim at. The mouse wheel sets the radius; below the smallest size only the aimed piece is
/// repaired, as in vanilla.
/// </summary>
public static class RepairTools
{
    private const float MinRadius = 1f;
    private const float ColumnHalfHeight = 64f;
    private const float ScanInterval = 0.2f;
    private const float HighlightSeconds = 0.3f;
    private const int MaxEffects = 8;

    private static readonly List<WearNTear> damaged = new();
    private static readonly Dictionary<string, bool> stationInRange = new();

    private static PieceTable previousTable;
    private static Piece previousPiece;
    private static float wheelAmount;
    private static float nextScan;
    private static bool circleShown;

    /// <summary>Help for repair mode, for the key hint.</summary>
    public static string HintLine { get; private set; } = string.Empty;

    /// <summary>The repair radius in metres, or 0 for only the aimed piece.</summary>
    public static float Radius => BuildToolSettings.RepairAreaEnabled.Value
        ? Mathf.Clamp(BuildToolSettings.RepairRadius.Value, 0f, BuildToolSettings.RepairMaxRadius.Value)
        : 0f;

    /// <summary>
    /// True if the player's selected piece is the repair piece.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>True in repair mode.</returns>
    public static bool Selected(Player player)
    {
        Piece piece = player != null && player.InPlaceMode() ? player.m_buildPieces?.GetSelectedPiece() : null;
        return piece != null && piece.m_repairPiece;
    }

    /// <summary>
    /// True if the mouse wheel sets the repair radius instead of turning or tilting the piece.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>True for the radius.</returns>
    public static bool WheelSetsRadius(Player player)
    {
        return BuildToolSettings.RepairAreaEnabled.Value && Selected(player);
    }

    /// <summary>
    /// Text for the toolbar button.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <returns>E.g. "on (10 m)".</returns>
    public static string StateText(Player player)
    {
        string state = Localization.instance.Localize(Selected(player) ? "$whitehilt_build_on" : "$whitehilt_build_off");
        return state + " (" + RadiusText() + ")";
    }

    /// <summary>
    /// Handles the radius and the preview. Called every frame while a build tool is held.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Tick(Player player)
    {
        if (!WheelSetsRadius(player))
        {
            Hide();
            return;
        }

        HandleWheel(player);
        float radius = Radius;
        if (radius <= 0f || !Aim(player, out Vector3 centre))
        {
            Hide();
            HintLine = string.Format(Localization.instance.Localize(radius <= 0f ? "$whitehilt_repair_hint_single" : "$whitehilt_repair_hint_aim"), RadiusText());
            return;
        }

        BuildGizmos.ShowCircle(centre, radius);
        circleShown = true;
        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + ScanInterval;
            FindDamaged(centre, radius);
            foreach (WearNTear piece in damaged)
            {
                Highlight(piece);
            }
        }

        HintLine = string.Format(Localization.instance.Localize("$whitehilt_repair_hint"), RadiusText(), damaged.Count);
    }

    /// <summary>
    /// Switches to the repair piece, or back to the piece held before.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void ToggleMode(Player player)
    {
        PieceTable table = player.m_buildPieces;
        if (table == null || !player.InPlaceMode())
        {
            return;
        }

        if (Selected(player))
        {
            if (previousTable == table && previousPiece != null)
            {
                player.SetSelectedPiece(previousPiece);
            }

            previousPiece = null;
            return;
        }

        Piece before = table.GetSelectedPiece();
        Piece repair = FindRepairPiece(table);
        if (repair == null || !player.SetSelectedPiece(repair))
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_repair_none");
            return;
        }

        previousTable = table;
        previousPiece = before;
    }

    /// <summary>
    /// Repairs every damaged piece around the aimed point. Called instead of vanilla's repair while the radius is above 0.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="tool">The build tool.</param>
    /// <returns>False if vanilla should repair the aimed piece instead.</returns>
    public static bool RepairArea(Player player, ItemDrop.ItemData tool)
    {
        float radius = Radius;
        if (radius <= 0f || !Aim(player, out Vector3 point))
        {
            return false;
        }

        FindDamaged(point, radius);
        if (damaged.Count == 0)
        {
            player.Message(MessageHud.MessageType.TopLeft, "$msg_whitehilt_repair_nothing");
            return true;
        }

        float factor = BuildToolSettings.RepairAreaCost.Value;
        float stamina = player.GetBuildStamina() * factor;
        float eitr = tool.m_shared.m_attack.m_attackEitr * factor;
        float wear = tool.m_shared.m_useDurabilityDrain * factor * Game.m_durabilityRate;
        int repaired = 0;
        int blocked = 0;
        bool tired = false;
        stationInRange.Clear();
        foreach (WearNTear piece in damaged)
        {
            if (!player.HaveStamina(stamina) || (eitr > 0f && !player.HaveEitr(eitr)) || (tool.m_shared.m_useDurability && tool.m_durability <= 0f))
            {
                tired = true;
                break;
            }

            Piece info = piece.GetComponent<Piece>();
            if (!StationInRange(player, info) || !PrivateArea.CheckAccess(piece.transform.position, 0f, flash: false))
            {
                blocked++;
                continue;
            }

            if (!piece.Repair())
            {
                continue;
            }

            if (repaired < MaxEffects)
            {
                info.m_placeEffect.Create(piece.transform.position, piece.transform.rotation, null, 1f, -1, player.GetZDOID());
            }

            repaired++;
            player.UseStamina(stamina);
            player.UseEitr(eitr);
            if (tool.m_shared.m_useDurability)
            {
                tool.m_durability = Mathf.Max(0f, tool.m_durability - wear);
            }
        }

        if (repaired > 0)
        {
            player.FaceLookDirection();
            player.m_zanim.SetTrigger(tool.m_shared.m_attack.m_attackAnimation);
        }

        string text = string.Format(Localization.instance.Localize("$msg_whitehilt_repair_done"), repaired, damaged.Count);
        if (blocked > 0)
        {
            text += "\n" + string.Format(Localization.instance.Localize("$msg_whitehilt_repair_blocked"), blocked);
        }

        if (tired)
        {
            text += "\n" + Localization.instance.Localize("$msg_whitehilt_repair_tired");
        }

        player.Message(MessageHud.MessageType.TopLeft, text);
        nextScan = 0f;
        return true;
    }

    private static void HandleWheel(Player player)
    {
        wheelAmount += ZInput.GetMouseScrollWheel();
        int direction = wheelAmount > player.m_scrollAmountThreshold ? 1 : wheelAmount < -player.m_scrollAmountThreshold ? -1 : 0;
        if (direction == 0)
        {
            return;
        }

        wheelAmount = 0f;
        float current = Radius;
        float next = current <= 0f ? (direction > 0 ? MinRadius : 0f) : current + direction;
        BuildToolSettings.RepairRadius.Value = next < MinRadius ? 0f : Mathf.Min(next, BuildToolSettings.RepairMaxRadius.Value);
        nextScan = 0f;
    }

    private static void FindDamaged(Vector3 point, float radius)
    {
        damaged.Clear();
        float radiusSquared = radius * radius;
        foreach (WearNTear piece in WearNTear.GetAllInstances())
        {
            if (piece == null || piece.m_nview == null || !piece.m_nview.IsValid())
            {
                continue;
            }

            Vector3 position = piece.transform.position;
            float dx = position.x - point.x;
            float dz = position.z - point.z;
            if (dx * dx + dz * dz > radiusSquared || Mathf.Abs(position.y - point.y) > ColumnHalfHeight)
            {
                continue;
            }

            Piece info = piece.GetComponent<Piece>();
            if (info != null && info.IsPlacedByPlayer() && piece.m_nview.GetZDO().GetFloat(ZDOVars.s_health, piece.m_health) < piece.m_health)
            {
                damaged.Add(piece);
            }
        }

        damaged.Sort((a, b) => FlatDistance(a, point).CompareTo(FlatDistance(b, point)));
    }

    private static float FlatDistance(WearNTear piece, Vector3 point)
    {
        Vector3 position = piece.transform.position;
        return new Vector2(position.x - point.x, position.z - point.z).sqrMagnitude;
    }

    private static bool StationInRange(Player player, Piece piece)
    {
        CraftingStation station = piece.m_craftingStation;
        if (station == null || player.m_noPlacementCost || ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoWorkbench))
        {
            return true;
        }

        if (!stationInRange.TryGetValue(station.m_name, out bool inRange))
        {
            inRange = CraftingStation.HaveBuildStationInRange(station.m_name, player.transform.position) != null;
            stationInRange[station.m_name] = inRange;
        }

        return inRange;
    }

    private static void Highlight(WearNTear piece)
    {
        ZDO zdo = piece.m_nview.GetZDO();
        float health = zdo != null && piece.m_health > 0f ? Mathf.Clamp01(zdo.GetFloat(ZDOVars.s_health, piece.m_health) / piece.m_health) : 1f;
        Color color = Color.Lerp(new Color(1f, 0.15f, 0.1f), new Color(1f, 0.85f, 0.2f), health);
        MaterialMan.instance.SetValue(piece.gameObject, ShaderProps._EmissionColor, color * 0.4f);
        MaterialMan.instance.SetValue(piece.gameObject, ShaderProps._Color, color);
        piece.CancelInvoke(nameof(WearNTear.ResetHighlight));
        piece.Invoke(nameof(WearNTear.ResetHighlight), HighlightSeconds);
    }

    private static bool Aim(Player player, out Vector3 point)
    {
        point = default;
        if (GameCamera.instance == null)
        {
            return false;
        }

        Transform view = GameCamera.instance.transform;
        float reach = BuildCamera.Active ? BuildToolSettings.MaxPlaceDistance.Value : player.m_maxPlaceDistance;
        if (!Physics.Raycast(view.position, view.forward, out RaycastHit hit, Mathf.Max(reach, 50f), player.m_removeRayMask)
            || Vector3.Distance(hit.point, player.m_eye.position) > reach)
        {
            return false;
        }

        point = hit.point;
        return true;
    }

    private static Piece FindRepairPiece(PieceTable table)
    {
        foreach (GameObject prefab in table.m_pieces)
        {
            Piece piece = prefab != null ? prefab.GetComponent<Piece>() : null;
            if (piece != null && piece.m_repairPiece)
            {
                return piece;
            }
        }

        return null;
    }

    private static string RadiusText()
    {
        float radius = Radius;
        return radius <= 0f
            ? Localization.instance.Localize("$whitehilt_repair_single")
            : radius.ToString("0", CultureInfo.InvariantCulture) + " m";
    }

    private static void Hide()
    {
        HintLine = string.Empty;
        damaged.Clear();
        if (circleShown)
        {
            circleShown = false;
            BuildGizmos.HideCircle();
        }
    }
}
