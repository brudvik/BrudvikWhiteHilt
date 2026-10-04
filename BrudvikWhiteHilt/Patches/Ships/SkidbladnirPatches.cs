using BrudvikWhiteHilt.Pieces.Ships.Skidbladnir;
using HarmonyLib;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Ships;

/// <summary>Allows ordinary building only on the sailing home's supported surfaces.</summary>
[HarmonyPatch]
public static class SkidbladnirPatches
{
    private static SkidbladnirShip placing;

    /// <summary>Preserves independent furniture views when the ship is unloaded or the world closes.</summary>
    /// <param name="__instance">View being removed from the scene.</param>
    [HarmonyPatch(typeof(ZNetView), nameof(ZNetView.ResetZDO))]
    [HarmonyPrefix]
    public static void Unload(ZNetView __instance)
    {
        if (__instance.GetComponent<SkidbladnirShip>() != null) ShipFurniture.BeforeShipUnload(__instance.gameObject);
    }

    /// <summary>Adds the saved attachment reader to ordinary networked building pieces.</summary>
    /// <param name="__instance">Awakened piece.</param>
    [HarmonyPatch(typeof(Piece), nameof(Piece.Awake))]
    [HarmonyPostfix]
    public static void PieceAwake(Piece __instance)
    {
        if (__instance.GetComponent<Ship>() == null && __instance.GetComponent<ZNetView>() != null
            && __instance.GetComponent<ShipFurniture>() == null)
            __instance.gameObject.AddComponent<ShipFurniture>();
    }

    /// <summary>Extends the vanilla placement ray to this specific ship, retaining range and collision checks.</summary>
    /// <param name="__instance">Building player.</param>
    /// <param name="__result">Whether a surface was found.</param>
    /// <param name="point">Surface hit position.</param>
    /// <param name="normal">Surface normal.</param>
    /// <param name="piece">Ordinary supporting piece, if any.</param>
    /// <param name="heightmap">Terrain beneath the ray.</param>
    /// <param name="waterSurface">Water hit, if any.</param>
    /// <param name="water">Whether the selected piece uses water placement.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.PieceRayTest))]
    [HarmonyPostfix]
    public static void BuildingRay(Player __instance, ref bool __result, ref Vector3 point, ref Vector3 normal,
        ref Piece piece, ref Heightmap heightmap, ref Collider waterSurface, bool water)
    {
        if (!SkidbladnirSettings.Building.Value || water || GameCamera.instance == null || __instance.m_placementGhost == null) return;
        Piece selected = __instance.m_placementGhost.GetComponent<Piece>();
        if (!Allowed(selected)) return;
        if (!Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, out RaycastHit hit,
            50f, __instance.m_placeRayMask | LayerMask.GetMask("vehicle"), QueryTriggerInteraction.Ignore)) return;
        SkidbladnirShip ship = hit.collider.GetComponentInParent<SkidbladnirShip>();
        if (ship == null) return;
        if (!ship.CanBuild || Vector3.Distance(__instance.m_eye.position, hit.point) >= __instance.m_maxPlaceDistance + selected.m_extraPlacementDistance)
        {
            __result = false;
            return;
        }
        point = hit.point;
        normal = hit.normal;
        piece = hit.collider.GetComponentInParent<Piece>();
        if (piece != null && piece.GetComponent<Ship>() != null) piece = null;
        heightmap = null;
        waterSurface = null;
        __result = true;
    }

    /// <summary>Captures the support ship for the piece creation transaction.</summary>
    /// <param name="piece">Selected build prefab.</param>
    /// <param name="pos">Proposed world position.</param>
    /// <param name="__state">Outer placement context.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
    [HarmonyPrefix]
    public static void BeginPlacement(Piece piece, Vector3 pos, out SkidbladnirShip __state)
    {
        __state = placing;
        placing = SkidbladnirSettings.Building.Value && Allowed(piece) ? ShipFurniture.Below(pos) : null;
    }

    /// <summary>Clears the transaction even if another placement hook throws.</summary>
    /// <param name="__state">Outer placement context.</param>
    /// <param name="__exception">Original error, if any.</param>
    /// <returns>The unchanged original error.</returns>
    [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
    [HarmonyFinalizer]
    public static Exception EndPlacement(SkidbladnirShip __state, Exception __exception)
    {
        placing = __state;
        return __exception;
    }

    /// <summary>Stores the new furniture's local pose before its first network transmission.</summary>
    /// <param name="__instance">Newly created piece.</param>
    [HarmonyPatch(typeof(Piece), nameof(Piece.SetCreator))]
    [HarmonyPostfix]
    public static void Created(Piece __instance)
    {
        if (placing == null || __instance.GetComponent<Ship>() != null) return;
        ShipFurniture furniture = __instance.GetComponent<ShipFurniture>() ?? __instance.gameObject.AddComponent<ShipFurniture>();
        furniture.Attach(placing);
    }

    /// <summary>Uses the ship as a stable foundation without suppressing damage or removal of furnishings.</summary>
    /// <param name="__instance">Structural component.</param>
    /// <returns>False for persisted ship furnishings.</returns>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.UpdateSupport))]
    [HarmonyPrefix]
    public static bool Support(WearNTear __instance)
    {
        if (!(__instance.GetComponent<ShipFurniture>()?.IsAttached ?? false)) return true;
        __instance.m_support = __instance.GetMaxSupport();
        if (__instance.m_nview.IsOwner()) __instance.m_nview.GetZDO().Set(ZDOVars.s_support, __instance.m_support);
        return false;
    }

    /// <summary>Protects saved furniture from removal of its parent ship.</summary>
    /// <param name="__instance">Requested piece.</param>
    /// <returns>False if the sailing home must first be emptied.</returns>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Remove))]
    [HarmonyPrefix]
    public static bool Remove(WearNTear __instance)
    {
        if (__instance.GetComponent<SkidbladnirShip>() == null || !ShipFurniture.HasFurniture(__instance.gameObject)) return true;
        Player.m_localPlayer?.Message(MessageHud.MessageType.Center, "$whitehilt_skidbladnir_empty");
        return false;
    }

    /// <summary>Applies the same speed cap after helm and autopilot physics.</summary>
    /// <param name="__instance">Updated ship.</param>
    [HarmonyPatch(typeof(Ship), nameof(Ship.CustomFixedUpdate))]
    [HarmonyPostfix]
    public static void PhysicsStep(Ship __instance) => __instance.GetComponent<SkidbladnirShip>()?.LimitSpeed();

    private static bool Allowed(Piece piece) => piece != null && !piece.m_groundPiece && !piece.m_groundOnly
        && !piece.m_waterPiece && !piece.m_cultivatedGroundOnly && piece.GetComponent<Ship>() == null
        && piece.GetComponent<Vagon>() == null && piece.GetComponent<Plant>() == null
        && piece.GetComponent<TerrainModifier>() == null && piece.GetComponent<TerrainOp>() == null;
}