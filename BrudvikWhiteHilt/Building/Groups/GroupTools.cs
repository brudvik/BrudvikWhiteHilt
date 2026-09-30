using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Building.Groups;

/// <summary>
/// The group tools and their keys: selecting pieces, copying, moving, tearing down, saving blueprints, pasting,
/// and the line and area tool. Only one mode is active at a time; the right mouse button leaves it.
/// </summary>
public static class GroupTools
{
    private const float CheckInterval = 0.25f;
    private const float PasteReachWithoutCamera = 30f;
    private const float FollowGroundBelowHeight = 1.6f;

    private static readonly List<GroupPlacer.Item> items = new();
    private static readonly List<(Piece Piece, PieceSnapshot Snapshot)> pasteParts = new();

    private static Blueprint clipboard;
    private static Blueprint pasteSource;
    private static List<Piece> cutSource;
    private static int unknownPieces;
    private static float pasteYaw;
    private static Vector3 pasteOffset;
    private static Vector3? lineStart;
    private static string problem;
    private static float nextCheck;
    private static int checkedCount = -1;
    private static float wheelAmount;

    /// <summary>
    /// The group tool modes.
    /// </summary>
    public enum ToolMode
    {
        /// <summary>No group tool.</summary>
        None,

        /// <summary>Picking pieces.</summary>
        Select,

        /// <summary>Placing what was copied, moved or loaded.</summary>
        Paste,

        /// <summary>A row of the held piece.</summary>
        Line,

        /// <summary>An area filled with the held piece.</summary>
        Area
    }

    /// <summary>The current mode.</summary>
    public static ToolMode Mode { get; private set; }

    /// <summary>True while a group tool is in use; vanilla placement is paused then.</summary>
    public static bool Active => Mode != ToolMode.None;

    /// <summary>True once a Ctrl shortcut was used during the current hold of Ctrl, so the build camera stops sinking.</summary>
    public static bool CtrlComboUsed { get; private set; }

    /// <summary>Help for the current mode, for the key hint.</summary>
    public static string HintLine { get; private set; } = string.Empty;

    /// <summary>
    /// Handles the group keys and the current mode. Called every frame while a build tool is held.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Tick(Player player)
    {
        BuildSelection.Tick();
        if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
        {
            CtrlComboUsed = false;
        }

        HandleKeys(player);
        switch (Mode)
        {
            case ToolMode.Select:
                TickSelect(player);
                break;
            case ToolMode.Paste:
                TickPaste(player);
                break;
            case ToolMode.Line:
            case ToolMode.Area:
                TickLine(player);
                break;
            default:
                HintLine = string.Empty;
                break;
        }
    }

    /// <summary>
    /// Leaves the current mode.
    /// </summary>
    public static void Exit()
    {
        Mode = ToolMode.None;
        lineStart = null;
        cutSource = null;
        BuildSelection.CancelBox();
        BuildGizmos.HideBox();
        GroupGhost.Hide();
        HintLine = string.Empty;
    }

    /// <summary>
    /// Starts placing a blueprint or copied pieces.
    /// </summary>
    /// <param name="blueprint">What to place.</param>
    /// <param name="cut">The pieces to tear down when placed (a move), or null for a copy.</param>
    public static void StartPaste(Blueprint blueprint, List<Piece> cut)
    {
        Exit();
        pasteSource = blueprint;
        cutSource = cut;
        pasteParts.Clear();
        unknownPieces = 0;
        foreach (PieceSnapshot snapshot in blueprint.Pieces)
        {
            Piece piece = GroupPlacer.Resolve(snapshot.Prefab);
            if (piece != null)
            {
                pasteParts.Add((piece, snapshot));
            }
            else
            {
                unknownPieces++;
            }
        }

        pasteYaw = 0f;
        pasteOffset = Vector3.zero;
        checkedCount = -1;
        Mode = ToolMode.Paste;
    }

    private static void HandleKeys(Player player)
    {
        ConfigEntry<KeyboardShortcut>[] keys =
        {
            GroupSettings.KeySelect, GroupSettings.KeyClear, GroupSettings.KeyCopy, GroupSettings.KeyCut, GroupSettings.KeyPaste,
            GroupSettings.KeyDelete, GroupSettings.KeySave, GroupSettings.KeyBlueprints, GroupSettings.KeyLine
        };
        ConfigEntry<KeyboardShortcut> pressed = keys.FirstOrDefault(key => key.Value.MainKey != KeyCode.None && key.Value.IsDown());
        if (pressed == null)
        {
            return;
        }

        CtrlComboUsed = true;
        if (!GroupSettings.Enabled.Value)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_group_disabled");
            return;
        }

        if (pressed == GroupSettings.KeySelect)
        {
            ToggleSelect();
        }
        else if (pressed == GroupSettings.KeyClear)
        {
            BuildSelection.Clear();
        }
        else if (pressed == GroupSettings.KeyCopy || pressed == GroupSettings.KeyCut)
        {
            CopySelection(player, pressed == GroupSettings.KeyCut);
        }
        else if (pressed == GroupSettings.KeyPaste)
        {
            Paste(player);
        }
        else if (pressed == GroupSettings.KeyDelete)
        {
            DeleteSelection(player);
        }
        else if (pressed == GroupSettings.KeySave)
        {
            SaveSelection(player);
        }
        else if (pressed == GroupSettings.KeyBlueprints)
        {
            BlueprintPanel.Toggle();
        }
        else if (pressed == GroupSettings.KeyLine)
        {
            CycleLine(player);
        }
    }

    /// <summary>
    /// Selection mode on or off.
    /// </summary>
    public static void ToggleSelect()
    {
        if (Mode == ToolMode.Select)
        {
            Exit();
            BuildSelection.Clear();
        }
        else
        {
            Exit();
            Mode = ToolMode.Select;
        }
    }

    /// <summary>
    /// Pastes what was copied last.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Paste(Player player)
    {
        if (clipboard == null || clipboard.Pieces.Count == 0)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_group_nothing");
            return;
        }

        StartPaste(clipboard, null);
    }

    /// <summary>
    /// Line, then area, then off; the first press picks what suits the held piece.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void CycleLine(Player player)
    {
        Piece piece = player.m_buildPieces?.GetSelectedPiece();
        if (Mode != ToolMode.Line && Mode != ToolMode.Area)
        {
            if (piece == null || !BuildPlacer.CanPlaceWithoutGhost(player, piece))
            {
                player.Message(MessageHud.MessageType.Center, piece == null ? "$msg_whitehilt_group_no_piece" : "$msg_whitehilt_build_stamp_unsupported");
                return;
            }

            Exit();
            Mode = IsFlat(piece) ? ToolMode.Area : ToolMode.Line;
            return;
        }

        bool startedAsArea = IsFlat(piece);
        bool second = (Mode == ToolMode.Area) == startedAsArea;
        if (second)
        {
            Mode = Mode == ToolMode.Area ? ToolMode.Line : ToolMode.Area;
            checkedCount = -1;
        }
        else
        {
            Exit();
        }
    }

    private static void CopySelection(Player player, bool cut)
    {
        List<Piece> pieces = BuildSelection.Pieces();
        if (pieces.Count == 0)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_group_nothing");
            return;
        }

        if (cut)
        {
            int blocked = pieces.Count(piece => !GroupPlacer.CanRemove(player, piece));
            if (blocked > 0)
            {
                player.Message(MessageHud.MessageType.Center, string.Format(Localization.instance.Localize("$msg_whitehilt_group_blocked"), blocked));
                return;
            }
        }

        Blueprint copy = Blueprint.FromPieces(pieces);
        copy.Name = Localization.instance.Localize(cut ? "$whitehilt_group_moving" : "$whitehilt_group_copy");
        if (!cut)
        {
            player.Message(MessageHud.MessageType.TopLeft, string.Format(Localization.instance.Localize("$msg_whitehilt_group_copied"), pieces.Count));
        }

        StartPaste(copy, cut ? pieces : null);
        if (!cut)
        {
            clipboard = copy;
        }
    }

    private static void DeleteSelection(Player player)
    {
        List<Piece> pieces = BuildSelection.Pieces();
        if (pieces.Count == 0)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_group_nothing");
            return;
        }

        List<Piece> allowed = pieces.Where(piece => GroupPlacer.CanRemove(player, piece)).ToList();
        List<PieceSnapshot> removed = GroupPlacer.Remove(player, allowed, GroupPlacer.Refund.Recoverable);
        BuildUndo.RecordRemoved(removed);
        BuildSelection.Clear();
        player.Message(MessageHud.MessageType.TopLeft, string.Format(Localization.instance.Localize("$msg_whitehilt_group_removed"), removed.Count));
        if (allowed.Count < pieces.Count)
        {
            player.Message(MessageHud.MessageType.Center,
                string.Format(Localization.instance.Localize("$msg_whitehilt_group_skipped"), pieces.Count - allowed.Count));
        }
    }

    private static void SaveSelection(Player player)
    {
        List<Piece> pieces = BuildSelection.Pieces();
        if (pieces.Count == 0)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_group_nothing");
            return;
        }

        Blueprint blueprint = Blueprint.FromPieces(pieces);
        blueprint.Creator = player.GetPlayerName();
        NameDialog.Ask(string.Empty, name =>
        {
            blueprint.Name = name;
            BlueprintStore.Save(blueprint);
            BlueprintPanel.Reload();
            player.Message(MessageHud.MessageType.TopLeft, string.Format(Localization.instance.Localize("$msg_whitehilt_group_saved"), name));
        });
    }

    private static void TickSelect(Player player)
    {
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool hasPoint = Aim(player, player.m_placeRayMask, out RaycastHit pointHit);
        if (BuildSelection.BoxStart.HasValue && hasPoint)
        {
            BuildGizmos.ShowBox(BuildSelection.Box(BuildSelection.BoxStart.Value, pointHit.point));
        }
        else
        {
            BuildGizmos.HideBox();
        }

        if (BuildToolSettings.KeyNudgeUp.Value.IsDown())
        {
            BuildSelection.ChangeBoxHeight(1f);
        }

        if (BuildToolSettings.KeyNudgeDown.Value.IsDown())
        {
            BuildSelection.ChangeBoxHeight(-1f);
        }

        if (Clicked(0))
        {
            if (shift || BuildSelection.BoxStart.HasValue)
            {
                if (hasPoint)
                {
                    BuildSelection.BoxClick(pointHit.point);
                }
            }
            else if (Aim(player, player.m_removeRayMask, out RaycastHit pieceHit))
            {
                Piece piece = pieceHit.collider.GetComponentInParent<Piece>();
                if (piece != null && GroupPlacer.Resolve(Utils.GetPrefabName(piece.gameObject)) != null && !Planting.Plantables.IsWild(piece))
                {
                    BuildSelection.Toggle(piece);
                }
            }
        }

        if (Clicked(1))
        {
            if (BuildSelection.BoxStart.HasValue)
            {
                BuildSelection.CancelBox();
            }
            else
            {
                Exit();
                BuildSelection.Clear();
                return;
            }
        }

        HintLine = string.Format(Localization.instance.Localize("$whitehilt_group_hint_select"), BuildSelection.Count,
            Mathf.RoundToInt(BuildSelection.BoxHeight), KeyName(GroupSettings.KeyCopy), KeyName(GroupSettings.KeyCut),
            KeyName(GroupSettings.KeyDelete), KeyName(GroupSettings.KeySave), KeyName(GroupSettings.KeyClear));
    }

    private static void TickPaste(Player player)
    {
        if (pasteSource == null || pasteParts.Count == 0)
        {
            Exit();
            return;
        }

        int turn = Wheel(player);
        pasteYaw = Mathf.Repeat(pasteYaw + turn * BuildRotation.Step, 360f);
        pasteOffset += NudgeInput(BuildRotation.GridOn ? BuildToolSettings.GridSize.Value : BuildToolSettings.NudgeStep.Value);

        items.Clear();
        if (Aim(player, player.m_placeRayMask, out RaycastHit hit))
        {
            Vector3 anchor = hit.point;
            if (BuildRotation.GridOn)
            {
                float size = Mathf.Max(0.05f, BuildToolSettings.GridSize.Value);
                anchor.x = Mathf.Round(anchor.x / size) * size;
                anchor.z = Mathf.Round(anchor.z / size) * size;
            }

            anchor += pasteOffset;
            Quaternion rotation = Quaternion.Euler(0f, pasteYaw, 0f);
            foreach ((Piece piece, PieceSnapshot snapshot) in pasteParts)
            {
                items.Add(new GroupPlacer.Item
                {
                    Piece = piece,
                    Position = anchor + rotation * snapshot.Position,
                    Rotation = rotation * snapshot.Rotation,
                    Text = snapshot.Text
                });
            }
        }

        bool free = cutSource != null;
        UpdateCheck(player, !free);
        ShowGhost();

        if (Clicked(0) && items.Count > 0)
        {
            PlacePaste(player);
        }
        else if (Clicked(1))
        {
            Exit();
            return;
        }

        string status = problem ?? Localization.instance.Localize("$whitehilt_group_ready");
        if (unknownPieces > 0)
        {
            status += "   " + string.Format(Localization.instance.Localize("$whitehilt_group_unknown"), unknownPieces);
        }

        HintLine = string.Format(Localization.instance.Localize("$whitehilt_group_hint_paste"), pasteSource.Name, pasteParts.Count, status);
    }

    private static void PlacePaste(Player player)
    {
        UpdateCheck(player, cutSource == null, force: true);
        if (problem != null)
        {
            player.Message(MessageHud.MessageType.Center, problem);
            return;
        }

        if (cutSource == null)
        {
            GroupPlacer.Place(player, items, pay: true);
            checkedCount = -1;
            return;
        }

        int blocked = cutSource.Count(piece => !GroupPlacer.CanRemove(player, piece));
        if (blocked > 0)
        {
            player.Message(MessageHud.MessageType.Center, string.Format(Localization.instance.Localize("$msg_whitehilt_group_blocked"), blocked));
            return;
        }

        List<PieceSnapshot> removed = GroupPlacer.Remove(player, cutSource, GroupPlacer.Refund.None);
        GroupPlacer.Place(player, items, pay: false, removed);
        BuildSelection.Clear();
        Exit();
    }

    private static void TickLine(Player player)
    {
        Piece piece = player.m_buildPieces?.GetSelectedPiece();
        if (piece == null || !BuildPlacer.CanPlaceWithoutGhost(player, piece))
        {
            Exit();
            return;
        }

        if (Mode == ToolMode.Area)
        {
            int turn = Wheel(player);
            player.m_placeRotation += turn;
        }

        items.Clear();
        bool hasPoint = Aim(player, player.m_placeRayMask, out RaycastHit hit);
        Vector3 point = hasPoint ? Snap(hit.point) : Vector3.zero;
        if (hasPoint)
        {
            if (lineStart.HasValue)
            {
                if (Mode == ToolMode.Line)
                {
                    LineItems(piece, lineStart.Value, point);
                }
                else
                {
                    AreaItems(player, piece, lineStart.Value, point);
                }
            }
            else
            {
                items.Add(new GroupPlacer.Item { Piece = piece, Position = point, Rotation = Quaternion.Euler(0f, BuildRotation.Yaw(player), 0f) });
            }
        }

        UpdateCheck(player, pay: true);
        ShowGhost();

        if (Clicked(0) && hasPoint)
        {
            if (!lineStart.HasValue)
            {
                lineStart = point;
                checkedCount = -1;
            }
            else
            {
                UpdateCheck(player, pay: true, force: true);
                if (problem != null)
                {
                    player.Message(MessageHud.MessageType.Center, problem);
                }
                else
                {
                    GroupPlacer.Place(player, items, pay: true);

                    // A line carries on from where it ended, so walls can go round a house.
                    lineStart = Mode == ToolMode.Line ? point : null;
                    checkedCount = -1;
                }
            }
        }
        else if (Clicked(1))
        {
            if (lineStart.HasValue)
            {
                lineStart = null;
            }
            else
            {
                Exit();
                return;
            }
        }

        string kind = Localization.instance.Localize(Mode == ToolMode.Line ? "$whitehilt_group_line_line" : "$whitehilt_group_line_area");
        string action = lineStart.HasValue
            ? string.Format(Localization.instance.Localize("$whitehilt_group_line_place"), items.Count)
            : Localization.instance.Localize("$whitehilt_group_line_start");
        HintLine = string.Format(Localization.instance.Localize("$whitehilt_group_hint_line"), Localization.instance.Localize(piece.m_name), kind, action,
            KeyName(GroupSettings.KeyLine), problem ?? Localization.instance.Localize("$whitehilt_group_ready"));
    }

    private static void LineItems(Piece piece, Vector3 start, Vector3 end)
    {
        BuildPlacer.Range(piece, Vector3.right, out float minX, out float maxX);
        BuildPlacer.Range(piece, Vector3.forward, out float minZ, out float maxZ);
        BuildPlacer.Range(piece, Vector3.up, out float minY, out float maxY);
        bool alongX = maxX - minX >= maxZ - minZ;
        float min = alongX ? minX : minZ;
        float span = alongX ? maxX - minX : maxZ - minZ;
        if (span < 0.05f)
        {
            span = 1f;
            min = -0.5f;
        }

        Vector3 delta = end - start;
        delta.y = 0f;
        float length = delta.magnitude;
        Vector3 direction = length > 0.01f ? delta / length : Vector3.ProjectOnPlane(GameCamera.instance.transform.forward, Vector3.up).normalized;
        float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg - (alongX ? 90f : 0f);
        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
        int count = Mathf.Max(1, Mathf.RoundToInt(length / span));
        bool followGround = maxY - minY < FollowGroundBelowHeight && ZoneSystem.instance != null;
        for (int i = 0; i < count; i++)
        {
            Vector3 position = start + direction * (i * span - min);
            position.y = followGround ? ZoneSystem.instance.GetGroundHeight(position) : start.y;
            items.Add(new GroupPlacer.Item { Piece = piece, Position = position, Rotation = rotation });
        }
    }

    private static void AreaItems(Player player, Piece piece, Vector3 start, Vector3 end)
    {
        BuildPlacer.Range(piece, Vector3.right, out float minX, out float maxX);
        BuildPlacer.Range(piece, Vector3.forward, out float minZ, out float maxZ);
        float spanX = Mathf.Max(0.05f, maxX - minX);
        float spanZ = Mathf.Max(0.05f, maxZ - minZ);
        Quaternion rotation = Quaternion.Euler(0f, BuildRotation.Yaw(player), 0f);
        Vector3 local = Quaternion.Inverse(rotation) * (end - start);
        int countX = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(local.x) / spanX));
        int countZ = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(local.z) / spanZ));
        bool positiveX = local.x >= 0f;
        bool positiveZ = local.z >= 0f;
        for (int x = 0; x < countX; x++)
        {
            for (int z = 0; z < countZ; z++)
            {
                float offsetX = (positiveX ? x * spanX : -(x + 1) * spanX) - minX;
                float offsetZ = (positiveZ ? z * spanZ : -(z + 1) * spanZ) - minZ;
                items.Add(new GroupPlacer.Item { Piece = piece, Position = start + rotation * new Vector3(offsetX, 0f, offsetZ), Rotation = rotation });
            }
        }
    }

    private static void UpdateCheck(Player player, bool pay, bool force = false)
    {
        if (items.Count == 0)
        {
            problem = null;
            return;
        }

        if (force || items.Count != checkedCount || Time.unscaledTime >= nextCheck)
        {
            nextCheck = Time.unscaledTime + CheckInterval;
            checkedCount = items.Count;
            problem = GroupPlacer.Check(player, items, pay);
        }
    }

    private static void ShowGhost()
    {
        if (items.Count == 0)
        {
            GroupGhost.Hide();
        }
        else
        {
            GroupGhost.Show(items, problem == null);
        }
    }

    private static bool Aim(Player player, int mask, out RaycastHit hit)
    {
        hit = default;
        if (GameCamera.instance == null)
        {
            return false;
        }

        Transform view = GameCamera.instance.transform;
        float reach = BuildCamera.Active ? BuildToolSettings.MaxPlaceDistance.Value : PasteReachWithoutCamera;
        return Physics.Raycast(view.position, view.forward, out hit, reach, mask);
    }

    private static Vector3 Snap(Vector3 point)
    {
        if (!BuildRotation.GridOn)
        {
            return point;
        }

        float size = Mathf.Max(0.05f, BuildToolSettings.GridSize.Value);
        point.x = Mathf.Round(point.x / size) * size;
        point.z = Mathf.Round(point.z / size) * size;
        return point;
    }

    private static bool Clicked(int button)
    {
        return !BuildToolbar.CursorMode && Input.GetMouseButtonDown(button);
    }

    private static int Wheel(Player player)
    {
        wheelAmount += ZInput.GetMouseScrollWheel();
        int direction = wheelAmount > player.m_scrollAmountThreshold ? 1 : wheelAmount < -player.m_scrollAmountThreshold ? -1 : 0;
        if (direction != 0)
        {
            wheelAmount = 0f;
        }

        return direction;
    }

    private static Vector3 NudgeInput(float step)
    {
        Vector3 move = Vector3.zero;
        Transform view = GameCamera.instance.transform;
        Vector3 forward = AxisAligned(view.forward);
        Vector3 right = AxisAligned(view.right);
        if (BuildToolSettings.KeyNudgeForward.Value.IsDown()) move += forward;
        if (BuildToolSettings.KeyNudgeBack.Value.IsDown()) move -= forward;
        if (BuildToolSettings.KeyNudgeRight.Value.IsDown()) move += right;
        if (BuildToolSettings.KeyNudgeLeft.Value.IsDown()) move -= right;
        if (BuildToolSettings.KeyNudgeUp.Value.IsDown()) move += Vector3.up;
        if (BuildToolSettings.KeyNudgeDown.Value.IsDown()) move -= Vector3.up;
        return move * step;
    }

    private static Vector3 AxisAligned(Vector3 direction)
    {
        direction.y = 0f;
        return Mathf.Abs(direction.x) > Mathf.Abs(direction.z)
            ? new Vector3(Mathf.Sign(direction.x), 0f, 0f)
            : new Vector3(0f, 0f, Mathf.Sign(direction.z));
    }

    private static bool IsFlat(Piece piece)
    {
        if (piece == null)
        {
            return false;
        }

        BuildPlacer.Range(piece, Vector3.right, out float minX, out float maxX);
        BuildPlacer.Range(piece, Vector3.forward, out float minZ, out float maxZ);
        BuildPlacer.Range(piece, Vector3.up, out float minY, out float maxY);
        return maxY - minY < 0.5f * Mathf.Min(maxX - minX, maxZ - minZ);
    }

    private static string KeyName(ConfigEntry<KeyboardShortcut> key)
    {
        return BuildToolSettings.KeyName(key);
    }

    /// <summary>
    /// Asks the player for a name with the game's own text dialog.
    /// </summary>
    public sealed class NameDialog : TextReceiver
    {
        private readonly string current;
        private readonly Action<string> done;

        private NameDialog(string current, Action<string> done)
        {
            this.current = current;
            this.done = done;
        }

        /// <summary>
        /// Opens the dialog.
        /// </summary>
        /// <param name="current">The name to start with.</param>
        /// <param name="done">Gets the new name if one was given.</param>
        public static void Ask(string current, Action<string> done)
        {
            TextInput.instance?.RequestText(new NameDialog(current, done), "$whitehilt_group_name", 60);
        }

        /// <inheritdoc/>
        public string GetText()
        {
            return current;
        }

        /// <inheritdoc/>
        public void SetText(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                done(text.Trim());
            }
        }
    }
}
