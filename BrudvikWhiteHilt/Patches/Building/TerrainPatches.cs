using BrudvikWhiteHilt.Building.Terrain;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Building;

/// <summary>
/// Hooks the White Hilt hoe and cultivator tools into the game: road points on the map, the big brush, and keeping
/// the game's own keys quiet while a Ctrl shortcut is used with a build tool.
/// </summary>
[HarmonyPatch]
public static class TerrainPatches
{
    private static readonly MethodInfo cloneSettings = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);

    // The game reads these buttons without looking at Ctrl, so Ctrl+R would also sheathe the tool, Ctrl+F use the
    // Forsaken power, and so on.
    private static readonly HashSet<string> quietWithCtrl = new() { "Hide", "GP", "OpenRadial", "OpenEmote", "AutoPickup", "Sit", "ToggleWalk", "AutoRun", "Map" };

    /// <summary>
    /// While planning a road, a click on the map adds a road point.
    /// </summary>
    /// <param name="__instance">The map.</param>
    /// <returns>False while planning.</returns>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnMapLeftClick))]
    [HarmonyPrefix]
    public static bool RoadPoint(Minimap __instance)
    {
        if (!RoadBuilder.Planning)
        {
            return true;
        }

        RoadBuilder.OnMapClick(__instance.ScreenToWorldPoint(ZInput.pointerPosition));
        return false;
    }

    /// <summary>
    /// While planning a road, a double-click adds the last point and finishes.
    /// </summary>
    /// <param name="__instance">The map.</param>
    /// <returns>False while planning.</returns>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnMapDblClick))]
    [HarmonyPrefix]
    public static bool RoadEnd(Minimap __instance)
    {
        if (!RoadBuilder.Planning)
        {
            return true;
        }

        RoadBuilder.OnMapDoubleClick(__instance.ScreenToWorldPoint(ZInput.pointerPosition));
        return false;
    }

    /// <summary>
    /// With the big brush, the White Hilt hoe's own operations work over the chosen radius, costing more for the larger area.
    /// </summary>
    /// <param name="__instance">The player.</param>
    /// <param name="piece">The piece being placed.</param>
    /// <param name="pos">Where.</param>
    /// <param name="rot">Rotation.</param>
    /// <returns>False when the big brush took over.</returns>
    [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool BigBrush(Player __instance, Piece piece, Vector3 pos, Quaternion rot)
    {
        if (__instance != Player.m_localPlayer || !BrushScale(__instance, piece, out TerrainOp op, out float scale))
        {
            return true;
        }

        float extra = Mathf.Max(0f, scale * scale - 1f);
        foreach (Piece.Requirement requirement in piece.m_resources)
        {
            int amount = Mathf.CeilToInt(requirement.m_amount * extra);
            if (requirement.m_resItem != null && amount > 0 && !TerrainCost.TryPayItem(__instance, requirement.m_resItem, amount))
            {
                return true;
            }
        }

        TerrainOp.Settings settings = (TerrainOp.Settings)cloneSettings.Invoke(op.m_settings, null);
        settings.m_levelRadius *= scale;
        settings.m_raiseRadius *= scale;
        settings.m_smoothRadius *= scale;
        settings.m_paintRadius *= scale;
        TerrainEdit.Job job = new();
        job.Ops.Add((pos, settings));
        TerrainEdit.Enqueue(job);
        piece.m_placeEffect.Create(pos, rot);
        return false;
    }

    /// <summary>
    /// What the big brush adds to a hoe piece's cost, as a share of it (0 without the big brush), so the chests can be
    /// fetched from for all of it before the piece is placed.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="piece">The piece about to be placed.</param>
    /// <returns>The extra share, e.g. 3 for four times the area.</returns>
    public static float BrushExtra(Player player, Piece piece)
    {
        return BrushScale(player, piece, out _, out float scale) ? Mathf.Max(0f, scale * scale - 1f) : 0f;
    }

    private static bool BrushScale(Player player, Piece piece, out TerrainOp op, out float scale)
    {
        op = null;
        scale = 1f;
        if (piece == null || HoeTools.BrushRadius <= 0f || !TerrainSettings.HoldingHoe(player))
        {
            return false;
        }

        op = piece.GetComponent<TerrainOp>();
        float radius = op != null ? op.m_settings.GetRadius() : 0f;
        if (radius <= 0.01f)
        {
            return false;
        }

        scale = HoeTools.BrushRadius / radius;
        return true;
    }

    /// <summary>
    /// Keeps the game's own buttons from firing along with a Ctrl shortcut while a build tool is held.
    /// </summary>
    /// <param name="name">The button.</param>
    /// <param name="__result">Whether it was pressed.</param>
    [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButtonDown))]
    [HarmonyPostfix]
    public static void QuietWithCtrl(string name, ref bool __result)
    {
        if (__result && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && quietWithCtrl.Contains(name)
            && Player.m_localPlayer != null && Player.m_localPlayer.InPlaceMode())
        {
            __result = false;
        }
    }
}
