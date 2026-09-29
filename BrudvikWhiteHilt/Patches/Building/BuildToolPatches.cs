using BrudvikWhiteHilt.Building;
using BrudvikWhiteHilt.Building.Groups;
using BrudvikWhiteHilt.Building.Media;
using BrudvikWhiteHilt.Building.Terrain;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Building;

/// <summary>
/// Hooks the build camera and the build toolbar into the game: camera and input, the full piece rotation,
/// snapping, grid and nudge, copying and undo.
/// </summary>
[HarmonyPatch]
public static class BuildToolPatches
{
    /// <summary>
    /// Handles the build hotkeys for the local player.
    /// </summary>
    /// <param name="__instance">The player.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    [HarmonyPostfix]
    public static void PlayerUpdate(Player __instance)
    {
        if (__instance == Player.m_localPlayer)
        {
            BuildTools.Tick(__instance);
        }
    }

    /// <summary>
    /// Puts the game camera on the build camera while it is on.
    /// </summary>
    /// <param name="__instance">The game camera.</param>
    /// <param name="dt">Unscaled frame time.</param>
    /// <returns>False to skip vanilla's camera update.</returns>
    [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateCamera))]
    [HarmonyPrefix]
    public static bool UpdateCamera(GameCamera __instance, float dt)
    {
        if (!BuildCamera.Active)
        {
            return true;
        }

        if (Player.m_localPlayer == null || __instance.m_freeFly)
        {
            BuildCamera.Stop(Player.m_localPlayer);
            return true;
        }

        BuildCamera.UpdateCamera(__instance, dt);
        return false;
    }

    /// <summary>
    /// Frees the cursor while the toolbar's cursor key is held.
    /// </summary>
    [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateMouseCapture))]
    [HarmonyPostfix]
    public static void UpdateMouseCapture()
    {
        if (BuildToolbar.CursorMode && !(MediaPanel.IsOpen && Input.GetMouseButton(1)))
        {
            ZCursor.LockState = CursorLockMode.None;
            ZCursor.Show();
        }
    }

    /// <summary>
    /// Keeps the player still while the camera flies or the cursor is out.
    /// </summary>
    /// <param name="__result">False: no movement or look input.</param>
    /// <returns>False to skip vanilla while blocked.</returns>
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.TakeInput))]
    [HarmonyPrefix]
    public static bool PlayerControllerTakeInput(ref bool __result)
    {
        if (!BuildCamera.Active && !BuildToolbar.CursorMode)
        {
            return true;
        }

        __result = false;
        return false;
    }

    /// <summary>
    /// Aims placement from the build camera, and keeps a click on the toolbar from placing a piece.
    /// </summary>
    /// <param name="__instance">The player.</param>
    /// <param name="takeInput">Whether vanilla reads build input this frame.</param>
    /// <param name="__state">The heading before vanilla's update.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacement))]
    [HarmonyPrefix]
    public static void UpdatePlacement(Player __instance, ref bool takeInput, out int __state)
    {
        __state = __instance.m_placeRotation;
        if (__instance != Player.m_localPlayer)
        {
            return;
        }

        if (BuildToolbar.CursorMode || MediaMode.PhotoView || FilmPlayer.Playing || GroupTools.Active || HoeTools.Active || FarmTools.Active)
        {
            takeInput = false;
        }

        BuildCamera.MoveEye(__instance);
    }

    /// <summary>
    /// Keeps vanilla from also turning the piece while the wheel tilts or rolls it.
    /// </summary>
    /// <param name="__instance">The player.</param>
    /// <param name="__state">The heading before vanilla's update.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacement))]
    [HarmonyPostfix]
    public static void UpdatePlacementPostfix(Player __instance, int __state)
    {
        if (__instance == Player.m_localPlayer && BuildTools.WheelModifierHeld)
        {
            __instance.m_placeRotation = __state;
        }
    }

    /// <summary>
    /// Puts the player's eye back after the frame.
    /// </summary>
    /// <param name="__instance">The player.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.LateUpdate))]
    [HarmonyPostfix]
    public static void PlayerLateUpdate(Player __instance)
    {
        if (__instance == Player.m_localPlayer)
        {
            BuildCamera.RestoreEye(__instance);
        }
    }

    /// <summary>
    /// Replaces vanilla's yaw-only ghost rotation with the full rotation (tilt, roll, flip, copied yaw).
    /// </summary>
    /// <param name="instructions">Vanilla's code.</param>
    /// <returns>The patched code.</returns>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> UpdatePlacementGhostTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo euler = AccessTools.Method(typeof(Quaternion), nameof(Quaternion.Euler), new[] { typeof(float), typeof(float), typeof(float) });
        MethodInfo compose = AccessTools.Method(typeof(BuildRotation), nameof(BuildRotation.Compose));
        bool replaced = false;
        foreach (CodeInstruction instruction in instructions)
        {
            if (!replaced && instruction.Calls(euler))
            {
                replaced = true;
                yield return new CodeInstruction(OpCodes.Call, compose).MoveLabelsFrom(instruction).MoveBlocksFrom(instruction);
                continue;
            }

            yield return instruction;
        }

        if (!replaced)
        {
            Jotunn.Logger.LogWarning("Build toolbar: could not find the ghost rotation; tilt and roll will not work.");
        }
    }

    /// <summary>
    /// Clears the snapped flag before vanilla places the ghost.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
    [HarmonyPrefix]
    public static void UpdatePlacementGhostPrefix()
    {
        BuildRotation.SnappedThisFrame = false;
    }

    /// <summary>
    /// Applies the grid and the nudge after vanilla has placed the ghost.
    /// </summary>
    /// <param name="__instance">The player.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
    [HarmonyPostfix]
    public static void UpdatePlacementGhostPostfix(Player __instance)
    {
        if (__instance != Player.m_localPlayer)
        {
            return;
        }

        if (MediaMode.HideUi || GroupTools.Active || HoeTools.Active || FarmTools.Active)
        {
            if (__instance.m_placementGhost != null)
            {
                __instance.m_placementGhost.SetActive(false);
            }

            if (__instance.m_placementMarkerInstance != null)
            {
                __instance.m_placementMarkerInstance.SetActive(false);
            }

            return;
        }

        BuildRotation.AdjustGhost(__instance);
    }

    /// <summary>
    /// Keeps pieces from lighting up under the crosshair on photos and films.
    /// </summary>
    /// <param name="__instance">The player.</param>
    /// <returns>False to skip vanilla while media hides the build helpers.</returns>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdateWearNTearHover))]
    [HarmonyPrefix]
    public static bool UpdateWearNTearHover(Player __instance)
    {
        if (__instance != Player.m_localPlayer || !MediaMode.HideUi)
        {
            return true;
        }

        __instance.m_hoveringPiece = null;
        return false;
    }

    /// <summary>
    /// Esc stops a playing film instead of opening the game menu.
    /// </summary>
    /// <returns>False to skip the menu while a film plays.</returns>
    [HarmonyPatch(typeof(Menu), nameof(Menu.Update))]
    [HarmonyPrefix]
    public static bool MenuUpdate()
    {
        if (!FilmPlayer.Playing)
        {
            return true;
        }

        if (ZInput.GetKeyDown(KeyCode.Escape))
        {
            FilmPlayer.Stop();
        }

        return false;
    }

    /// <summary>
    /// Switches snapping off when the toolbar says so.
    /// </summary>
    /// <param name="a">Vanilla's snap point on the ghost.</param>
    /// <param name="b">Vanilla's snap point on the other piece.</param>
    /// <param name="__result">False: nothing to snap to.</param>
    /// <returns>False to skip vanilla while snapping is off.</returns>
    [HarmonyPatch(typeof(Player), nameof(Player.FindClosestSnapPoints))]
    [HarmonyPrefix]
    public static bool FindClosestSnapPoints(ref Transform a, ref Transform b, ref bool __result)
    {
        if (!BuildRotation.SnapOff)
        {
            return true;
        }

        a = null;
        b = null;
        __result = false;
        return false;
    }

    /// <summary>
    /// Notes that the ghost snapped, so the grid leaves it alone.
    /// </summary>
    /// <param name="__result">True if a snap point was found.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.FindClosestSnapPoints))]
    [HarmonyPostfix]
    public static void FoundSnapPoints(bool __result)
    {
        if (__result)
        {
            BuildRotation.SnappedThisFrame = true;
        }
    }

    /// <summary>
    /// Copies the tilt and roll of the copied piece too, not just its heading.
    /// </summary>
    /// <param name="__instance">The player.</param>
    /// <param name="__result">True if a piece was copied.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.CopyPiece))]
    [HarmonyPostfix]
    public static void CopyPiece(Player __instance, bool __result)
    {
        if (!__result || __instance != Player.m_localPlayer || GameCamera.instance == null)
        {
            return;
        }

        Transform view = GameCamera.instance.transform;
        if (Physics.Raycast(view.position, view.forward, out RaycastHit hit, 50f, __instance.m_removeRayMask))
        {
            Piece piece = hit.collider.GetComponentInParent<Piece>();
            if (piece != null)
            {
                BuildRotation.CopyFrom(__instance, piece);
            }
        }
    }

    /// <summary>
    /// Marks that the local player is placing a piece, so the new piece can be recorded for undo.
    /// </summary>
    /// <param name="__instance">The player.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
    [HarmonyPrefix]
    public static void PlacePiecePrefix(Player __instance)
    {
        BuildUndo.Capturing = __instance == Player.m_localPlayer;
    }

    /// <summary>
    /// Ends the recording and clears the nudge for the next piece.
    /// </summary>
    /// <param name="__instance">The player.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
    [HarmonyFinalizer]
    public static void PlacePieceFinalizer(Player __instance)
    {
        BuildUndo.Capturing = false;
        if (__instance == Player.m_localPlayer)
        {
            BuildRotation.ClearNudge();
        }
    }

    /// <summary>
    /// Records the piece the local player just placed.
    /// </summary>
    /// <param name="__instance">The new piece.</param>
    [HarmonyPatch(typeof(Piece), nameof(Piece.SetCreator))]
    [HarmonyPostfix]
    public static void SetCreator(Piece __instance)
    {
        if (BuildUndo.Capturing)
        {
            BuildUndo.Record(__instance);
        }
    }

    /// <summary>
    /// Lets the demister ball follow the build camera.
    /// </summary>
    /// <param name="__instance">The demister effect.</param>
    [HarmonyPatch(typeof(SE_Demister), nameof(SE_Demister.UpdateStatusEffect))]
    [HarmonyPostfix]
    public static void DemisterFollowsCamera(SE_Demister __instance)
    {
        if (BuildCamera.Active && __instance.m_character == Player.m_localPlayer && __instance.m_ballInstance != null)
        {
            __instance.m_ballInstance.transform.position = BuildCamera.Position;
        }
    }
}
