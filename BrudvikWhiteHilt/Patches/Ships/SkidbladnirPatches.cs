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

    /// <summary>Rejects unsupported drawers and off-ship workshops before the placement transaction.</summary>
    /// <param name="__instance">Building player.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
    [HarmonyPostfix]
    public static void DrawerPlacement(Player __instance)
    {
        GameObject ghost = __instance.m_placementGhost;
        if (ghost == null || !ghost.activeSelf) return;
        var drawer = ghost.GetComponent<global::BrudvikWhiteHilt.Chests.Piece.WallDrawer>();
        if (drawer != null) drawer.SnapToWall();
        if (__instance.m_placementStatus != Player.PlacementStatus.Valid) return;
        var workshop = ghost.GetComponent<ShipWorkshop>();
        if ((drawer == null || drawer.HasWall()) && (workshop == null || workshop.CanPlace())) return;
        __instance.m_placementStatus = Player.PlacementStatus.Invalid;
        ghost.GetComponent<Piece>().SetInvalidPlacementHeightlight(true);
    }

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
    [HarmonyPatch(typeof(Player), nameof(Player.PieceRayTest))]
    [HarmonyPostfix]
    public static void BuildingRay(Player __instance, ref bool __result, ref Vector3 point, ref Vector3 normal,
        ref Piece piece, ref Heightmap heightmap, ref Collider waterSurface)
    {
        // Stations are m_noInWater, which makes vanilla pass water=true; water pieces are excluded by Allowed.
        if (!SkidbladnirSettings.Building.Value || GameCamera.instance == null || __instance.m_placementGhost == null) return;
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
        global::BrudvikWhiteHilt.Building.BuildRotation.Frame = ship.transform;
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

    /// <summary>Lets the ship's decks count as a roof for furnishings below them; vanilla's roof ray skips ships.</summary>
    /// <param name="obj">Piece being checked.</param>
    /// <param name="position">Start of the roof ray.</param>
    /// <param name="roofObject">The roof found.</param>
    /// <param name="heightOffset">Height of the ray's start above the position.</param>
    /// <param name="__result">Whether a roof was found.</param>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.RoofCheck))]
    [HarmonyPostfix]
    public static void ShipRoof(Transform obj, Vector3 position, ref GameObject roofObject, float heightOffset, ref bool __result)
    {
        if (__result || obj == null || !(obj.GetComponent<ShipFurniture>()?.IsAttached ?? false)) return;
        // Vanilla's own ray: a 0.1 m sphere straight up for 100 m.
        if (Physics.SphereCast(position + Vector3.up * heightOffset, 0.1f, Vector3.up, out RaycastHit hit, 100f,
            LayerMask.GetMask("vehicle"), QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<SkidbladnirShip>() != null)
        {
            roofObject = hit.collider.gameObject;
            __result = true;
        }
    }

    /// <summary>Brings the camera in below deck and keeps it inside the hull, which vanilla's camera passes through.</summary>
    /// <param name="__instance">The game camera.</param>
    /// <param name="pos">Camera position chosen by vanilla.</param>
    [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.GetCameraPosition))]
    [HarmonyPostfix]
    public static void LowerDeckCamera(GameCamera __instance, ref Vector3 pos)
    {
        float reach = SkidbladnirSettings.LowerDeckCameraDistance.Value;
        Player player = Player.m_localPlayer;
        if (reach <= 0f || player == null || player.InIntro()) return;
        SkidbladnirShip ship = SkidbladnirShip.BelowDeck(player.m_eye.position);
        if (ship == null) return;
        Vector3 eye = __instance.GetOffsetedEyePos();
        Vector3 offset = pos - eye;
        float distance = Mathf.Min(offset.magnitude, reach);
        if (distance < 0.01f) return;
        Vector3 direction = offset.normalized;
        foreach (RaycastHit hit in Physics.SphereCastAll(eye, __instance.m_raycastWidth, direction, distance,
            LayerMask.GetMask("vehicle"), QueryTriggerInteraction.Ignore))
        {
            if (hit.distance > 0f && hit.collider.GetComponentInParent<SkidbladnirShip>() == ship)
                distance = Mathf.Min(distance, hit.distance);
        }
        pos = eye + direction * distance;
    }

    private static bool Allowed(Piece piece) => piece != null && !piece.m_groundPiece && !piece.m_groundOnly
        && !piece.m_waterPiece && !piece.m_cultivatedGroundOnly && piece.GetComponent<Ship>() == null
        && piece.GetComponent<Vagon>() == null && piece.GetComponent<Plant>() == null
        && piece.GetComponent<TerrainModifier>() == null && piece.GetComponent<TerrainOp>() == null;
}