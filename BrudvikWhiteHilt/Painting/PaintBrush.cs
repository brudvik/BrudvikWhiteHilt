using BrudvikWhiteHilt.Building;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Painting;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Painting;

/// <summary>
/// What the White Hilt Paint Brush does. Its build menu holds four actions, each a repair-style piece, so vanilla aims
/// and clicks as when repairing: paint, stain, remove paint and pick a colour. Using a paint pot loads the brush with
/// its colour; painting uses up pots of that colour, one piece each. The mouse wheel sets a radius.
/// </summary>
public static class PaintBrush
{
    /// <summary>
    /// The brush's actions.
    /// </summary>
    public enum BrushAction
    {
        /// <summary>Bleached texture, then the colour.</summary>
        Paint,

        /// <summary>The colour over the texture.</summary>
        Stain,

        /// <summary>Back to the vanilla look.</summary>
        Remove,

        /// <summary>Loads the brush with a painted piece's colour.</summary>
        Pick
    }

    /// <summary>The brush's actions: prefab name, English name and description.</summary>
    public static readonly (BrushAction Action, string Prefab, string Name, string Description)[] Actions =
    {
        (BrushAction.Paint, "piece_whitehilt_brush_paint", "Paint", "Covers the piece in the brush's colour; also light colours and white."),
        (BrushAction.Stain, "piece_whitehilt_brush_stain", "Stain", "Stains the piece: the grain shows through, and the colour can only darken it."),
        (BrushAction.Remove, "piece_whitehilt_brush_remove", "Remove paint", "Takes the paint off. Costs nothing."),
        (BrushAction.Pick, "piece_whitehilt_brush_pick", "Pick colour", "Loads the brush with the colour of a painted piece.")
    };

    private const string ColorKey = "whitehilt_brush_color";
    private const float ColumnHalfHeight = 32f;
    private const float ScanInterval = 0.2f;
    private const float PreviewSeconds = 0.3f;
    private const float RefreshSeconds = 0.25f;
    private const int MaxEffects = 8;

    private static readonly List<WearNTear> targets = new();

    private static Text hud;
    private static float wheelAmount;
    private static float nextScan;
    private static float nextRefresh;
    private static bool circleShown;

    /// <summary>The brush radius in metres, 0 for only the aimed piece.</summary>
    public static float Radius => Mathf.Clamp(PaintSettings.Radius.Value, 0f, PaintSettings.MaxRadius.Value);

    /// <summary>
    /// Registers the English text of the actions.
    /// </summary>
    public static void RegisterTranslations()
    {
        foreach ((_, string prefab, string name, string description) in Actions)
        {
            Translations.AddEnglishNameAndDescription(prefab, name, description);
        }
    }

    /// <summary>
    /// True for the White Hilt Paint Brush.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>True for the brush.</returns>
    public static bool IsBrush(ItemDrop.ItemData item)
    {
        return item?.m_shared.m_name == Translations.Token(Translations.ItemKey(WhiteHiltPaintBrush.PrefabName));
    }

    /// <summary>
    /// True for one of the brush's action pieces.
    /// </summary>
    /// <param name="piece">The piece.</param>
    /// <returns>True for a brush action.</returns>
    public static bool IsPaintPiece(Piece piece)
    {
        return ActionOf(piece) != null;
    }

    /// <summary>
    /// True if the mouse wheel sets the brush radius.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>True with a painting action selected.</returns>
    public static bool WheelSetsRadius(Player player)
    {
        return Selected(player, out BrushAction action) && action != BrushAction.Pick;
    }

    /// <summary>
    /// Handles the radius, the preview and the read-out. Called every frame for the local player.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Tick(Player player)
    {
        ItemDrop.ItemData brush = player.GetRightItem();
        bool holding = IsBrush(brush) && player.InPlaceMode() && !player.IsDead();
        UpdateHud(player, holding ? brush : null);
        if (!holding || !Selected(player, out BrushAction action) || action == BrushAction.Pick || !player.TakeInput() || Hud.IsPieceSelectionVisible())
        {
            HideCircle();
            return;
        }

        HandleWheel(player);
        float radius = Radius;
        if (radius <= 0f || !Aim(player, out Vector3 centre))
        {
            HideCircle();
            return;
        }

        BuildGizmos.ShowCircle(centre, radius);
        circleShown = true;
        if (Time.unscaledTime < nextScan)
        {
            return;
        }

        nextScan = Time.unscaledTime + ScanInterval;
        FindTargets(player, centre, radius);
        int preview = action == BrushAction.Remove ? 0 : BrushValue(brush, action);
        foreach (WearNTear piece in targets)
        {
            Preview(piece, preview);
        }
    }

    /// <summary>
    /// Paints, stains, cleans or picks instead of repairing, when a brush action is selected.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="tool">The tool in hand.</param>
    /// <param name="repairPiece">The selected repair-style piece.</param>
    /// <returns>True if the brush handled the click.</returns>
    public static bool HandleClick(Player player, ItemDrop.ItemData tool, Piece repairPiece)
    {
        BrushAction? action = ActionOf(repairPiece);
        if (action == null || !IsBrush(tool))
        {
            return false;
        }

        if (action == BrushAction.Pick)
        {
            Pick(player, tool);
            return true;
        }

        if (!CollectTargets(player))
        {
            player.Message(MessageHud.MessageType.TopLeft, "$msg_whitehilt_brush_nothing");
            return true;
        }

        if (action == BrushAction.Remove)
        {
            int cleaned = 0;
            foreach (WearNTear piece in targets.Where(piece => PaintedPieces.Get(piece) != 0))
            {
                PaintedPieces.Set(piece, 0);
                cleaned++;
            }

            Finish(player, tool, cleaned);
            player.Message(MessageHud.MessageType.TopLeft, string.Format(Localization.instance.Localize("$msg_whitehilt_brush_cleaned"), cleaned));
            return true;
        }

        if (!TryGetColor(tool, out Color32 color))
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_brush_empty");
            return true;
        }

        int value = PaintColor.Pack(color, action == BrushAction.Paint ? PaintMode.Paint : PaintMode.Stain);
        List<ItemDrop.ItemData> pots = Pots(player, color);
        int painted = 0;
        foreach (WearNTear piece in targets)
        {
            if (PaintedPieces.Get(piece) == value)
            {
                continue;
            }

            ItemDrop.ItemData pot = pots.FirstOrDefault(candidate => candidate.m_durability >= 1f);
            if (pot == null)
            {
                player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_brush_no_paint");
                break;
            }

            PaintedPieces.Set(piece, value);
            if (painted < MaxEffects)
            {
                Piece info = piece.GetComponent<Piece>();
                if (info != null)
                {
                    info.m_placeEffect.Create(piece.transform.position, piece.transform.rotation, null, 1f, -1, player.GetZDOID());
                }
            }

            painted++;
            pot.m_durability -= 1f;
            if (pot.m_durability < 1f)
            {
                player.GetInventory().RemoveItem(pot);
                pots.Remove(pot);
            }
        }

        Finish(player, tool, painted);
        if (painted > 0)
        {
            player.Message(MessageHud.MessageType.TopLeft, string.Format(Localization.instance.Localize("$msg_whitehilt_brush_painted"), painted));
        }

        return true;
    }

    /// <summary>
    /// Loads the brush in the inventory with a pot's colour.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="pot">The pot used.</param>
    public static void LoadFrom(Player player, ItemDrop.ItemData pot)
    {
        if (!WhiteHiltPaintPot.TryGetColor(pot, out Color32 color))
        {
            return;
        }

        ItemDrop.ItemData brush = player.GetRightItem();
        if (!IsBrush(brush))
        {
            brush = player.GetInventory().GetAllItems().FirstOrDefault(IsBrush);
        }

        if (brush == null)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_brush_none");
            return;
        }

        brush.m_customData[ColorKey] = PaintColor.ToHex(color);
        player.Message(MessageHud.MessageType.TopLeft, string.Format(Localization.instance.Localize("$msg_whitehilt_brush_loaded"), PaintColor.Swatch(color)));
        nextRefresh = 0f;
    }

    /// <summary>
    /// Creates the icon of a brush action.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <returns>The icon, or null on a server without graphics.</returns>
    public static Sprite CreateIcon(BrushAction action)
    {
        if (VisualHelper.IsHeadless)
        {
            return null;
        }

        const int size = 64;
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                pixels[y * size + x] = IconPixel(action, dx, dy, r);
            }
        }

        Texture2D texture = new(size, size, TextureFormat.RGBA32, false) { name = $"whitehilt_brush_{action}".ToLowerInvariant() };
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    // One pixel of a brush action's icon, drawn in code: a colour wheel for paint, and so on.
    private static Color32 IconPixel(BrushAction action, float dx, float dy, float r)
    {
        Color32 clear = new(0, 0, 0, 0);
        Color32 rim = new(230, 224, 210, 255);
        switch (action)
        {
            case BrushAction.Paint:
                if (r > 0.92f)
                {
                    return clear;
                }

                if (r > 0.82f)
                {
                    return rim;
                }

                float hue = Mathf.Repeat(Mathf.Atan2(dy, dx) / (2f * Mathf.PI), 1f);
                return Color.HSVToRGB(hue, Mathf.Clamp01(r / 0.82f), 1f);
            case BrushAction.Stain:
                if (r > 0.92f)
                {
                    return clear;
                }

                if (r > 0.82f)
                {
                    return rim;
                }

                float grain = 0.5f + 0.5f * Mathf.Sin(dy * 18f + Mathf.Sin(dx * 5f) * 2f);
                return Color.Lerp(new Color(0.35f, 0.2f, 0.1f), new Color(0.6f, 0.38f, 0.2f), grain);
            case BrushAction.Remove:
                if (r > 0.92f)
                {
                    return clear;
                }

                bool cross = (Mathf.Abs(dx - dy) < 0.16f || Mathf.Abs(dx + dy) < 0.16f) && r < 0.7f;
                return cross ? new Color32(200, 40, 30, 255) : r > 0.82f ? rim : new Color32(120, 110, 100, 255);
            default:
                bool ring = r > 0.62f && r < 0.86f;
                bool dot = r < 0.24f;
                bool line = Mathf.Abs(dx) < 0.06f || Mathf.Abs(dy) < 0.06f;
                return ring || dot || (line && r < 0.95f) ? rim : clear;
        }
    }

    private static BrushAction? ActionOf(Piece piece)
    {
        if (piece == null)
        {
            return null;
        }

        foreach ((BrushAction action, string prefab, _, _) in Actions)
        {
            if (piece.m_name == Translations.Token(prefab))
            {
                return action;
            }
        }

        return null;
    }

    private static bool Selected(Player player, out BrushAction action)
    {
        action = BrushAction.Paint;
        Piece piece = player != null && player.InPlaceMode() ? player.m_buildPieces?.GetSelectedPiece() : null;
        BrushAction? found = ActionOf(piece);
        if (found == null)
        {
            return false;
        }

        action = found.Value;
        return true;
    }

    private static bool TryGetColor(ItemDrop.ItemData brush, out Color32 color)
    {
        color = default;
        return brush != null && brush.m_customData.TryGetValue(ColorKey, out string hex) && PaintColor.TryParseHex(hex, out color);
    }

    private static int BrushValue(ItemDrop.ItemData brush, BrushAction action)
    {
        return TryGetColor(brush, out Color32 color) ? PaintColor.Pack(color, action == BrushAction.Paint ? PaintMode.Paint : PaintMode.Stain) : 0;
    }

    private static List<ItemDrop.ItemData> Pots(Player player, Color32 color)
    {
        string hex = PaintColor.ToHex(color);
        return player.GetInventory().GetAllItems()
            .Where(item => WhiteHiltPaintPot.IsPot(item) && WhiteHiltPaintPot.TryGetColor(item, out Color32 potColor) && PaintColor.ToHex(potColor) == hex)
            .OrderBy(item => item.m_durability)
            .ToList();
    }

    private static void Pick(Player player, ItemDrop.ItemData tool)
    {
        WearNTear piece = player.GetHoveringPiece()?.GetComponent<WearNTear>();
        if (piece == null || !PaintColor.Unpack(PaintedPieces.Get(piece), out Color32 color, out _))
        {
            player.Message(MessageHud.MessageType.TopLeft, "$msg_whitehilt_brush_unpainted");
            return;
        }

        tool.m_customData[ColorKey] = PaintColor.ToHex(color);
        player.Message(MessageHud.MessageType.TopLeft, string.Format(Localization.instance.Localize("$msg_whitehilt_brush_picked"), PaintColor.Swatch(color)));
        nextRefresh = 0f;
    }

    private static void Finish(Player player, ItemDrop.ItemData tool, int changed)
    {
        if (changed <= 0)
        {
            return;
        }

        player.FaceLookDirection();
        player.m_zanim.SetTrigger(tool.m_shared.m_attack.m_attackAnimation);
        player.UseStamina(player.GetBuildStamina());
        nextScan = 0f;
        nextRefresh = 0f;
    }

    // The aimed piece, or every piece of a player's building in the radius that the player may change.
    private static bool CollectTargets(Player player)
    {
        targets.Clear();
        float radius = Radius;
        if (radius > 0f && Aim(player, out Vector3 centre))
        {
            FindTargets(player, centre, radius);
        }
        else
        {
            Piece hovering = player.GetHoveringPiece();
            WearNTear piece = hovering != null ? hovering.GetComponent<WearNTear>() : null;
            if (piece != null && hovering.IsPlacedByPlayer() && PrivateArea.CheckAccess(piece.transform.position))
            {
                targets.Add(piece);
            }
        }

        return targets.Count > 0;
    }

    // The player-built pieces within the brush's radius and height that the player may change (no one else's ward).
    private static void FindTargets(Player player, Vector3 centre, float radius)
    {
        targets.Clear();
        float radiusSquared = radius * radius;
        foreach (WearNTear piece in WearNTear.GetAllInstances())
        {
            if (piece == null || piece.m_nview == null || !piece.m_nview.IsValid())
            {
                continue;
            }

            Vector3 position = piece.transform.position;
            float dx = position.x - centre.x;
            float dz = position.z - centre.z;
            if (dx * dx + dz * dz > radiusSquared || Mathf.Abs(position.y - centre.y) > ColumnHalfHeight)
            {
                continue;
            }

            Piece info = piece.GetComponent<Piece>();
            if (info != null && info.IsPlacedByPlayer() && PrivateArea.CheckAccess(position, 0f, flash: false))
            {
                targets.Add(piece);
            }
        }
    }

    // Shows the brush colour on a piece for a moment; the highlight reset puts its own paint back.
    private static void Preview(WearNTear piece, int value)
    {
        if (MaterialMan.instance == null)
        {
            return;
        }

        Color color = PaintColor.Unpack(value, out Color32 paint, out _) ? paint : new Color(0.85f, 0.85f, 0.85f);
        MaterialMan.instance.SetValue(piece.gameObject, ShaderProps._Color, color);
        piece.CancelInvoke(nameof(WearNTear.ResetHighlight));
        piece.Invoke(nameof(WearNTear.ResetHighlight), PreviewSeconds);
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
        float next = Radius + direction;
        PaintSettings.Radius.Value = next < 1f ? 0f : Mathf.Min(next, PaintSettings.MaxRadius.Value);
        nextScan = 0f;
        nextRefresh = 0f;
    }

    // The point the player looks at, within placement reach (the build camera's reach while it is on).
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

    private static void HideCircle()
    {
        if (circleShown)
        {
            circleShown = false;
            BuildGizmos.HideCircle();
        }
    }

    // Shows the brush's colour, the paint left and the radius at the bottom of the screen while the brush is held.
    private static void UpdateHud(Player player, ItemDrop.ItemData brush)
    {
        if (brush == null || Hud.instance == null)
        {
            if (hud != null && hud.gameObject.activeSelf)
            {
                hud.gameObject.SetActive(false);
            }

            return;
        }

        if (hud == null)
        {
            GameObject go = GUIManager.Instance.CreateText(string.Empty, Hud.instance.m_rootObject.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 200f), GUIManager.Instance.AveriaSerifBold, 18, Color.white, true, Color.black, 700f, 30f, false);
            go.name = "WhiteHiltBrushReadout";
            hud = go.GetComponent<Text>();
            hud.alignment = TextAnchor.MiddleCenter;
            hud.raycastTarget = false;
        }

        if (!hud.gameObject.activeSelf)
        {
            hud.gameObject.SetActive(true);
        }

        if (Time.unscaledTime < nextRefresh)
        {
            return;
        }

        nextRefresh = Time.unscaledTime + RefreshSeconds;
        string radius = Radius <= 0f
            ? Localization.instance.Localize("$whitehilt_brush_single")
            : Radius.ToString("0", CultureInfo.InvariantCulture) + " m";
        hud.text = TryGetColor(brush, out Color32 color)
            ? string.Format(Localization.instance.Localize("$whitehilt_brush_hud"), PaintColor.Swatch(color),
                Pots(player, color).Sum(pot => Mathf.FloorToInt(pot.m_durability)), radius)
            : string.Format(Localization.instance.Localize("$whitehilt_brush_hud_empty"), radius);
    }
}
