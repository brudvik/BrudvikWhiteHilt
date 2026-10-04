using BrudvikWhiteHilt.Pieces.Ships;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Ships;

/// <summary>
/// Adds the sailing help to every ship and handles its keys for the local player.
/// </summary>
[HarmonyPatch]
public static class ShipAssistPatches
{
    /// <summary>
    /// Adds the help when a ship wakes.
    /// </summary>
    /// <param name="__instance">The ship.</param>
    [HarmonyPatch(typeof(Ship), nameof(Ship.Awake))]
    [HarmonyPostfix]
    public static void ShipAwake(Ship __instance)
    {
        ShipAssist.Attach(__instance);
        ShipMooring.Attach(__instance);
    }

    /// <summary>Restores authoritative controls and checks ownership before the ship's physics update.</summary>
    /// <param name="__instance">The ship.</param>
    [HarmonyPatch(typeof(Ship), nameof(Ship.CustomFixedUpdate))]
    [HarmonyPrefix]
    public static void ShipPhysics(Ship __instance)
    {
        __instance.GetComponent<ShipAssist>()?.PreparePhysics();
    }

    /// <summary>Replaces vanilla's position jumps on White Hilt ships another player owns.</summary>
    /// <param name="__instance">Network transform about to follow its owner.</param>
    /// <returns>False when the ship was smoothed instead.</returns>
    [HarmonyPatch(typeof(ZSyncTransform), nameof(ZSyncTransform.ClientSync))]
    [HarmonyPrefix]
    public static bool PassengerSync(ZSyncTransform __instance)
    {
        return !Pieces.Ships.WhiteHiltShip.ShipPassengerSync.TryStep(__instance);
    }

    /// <summary>Delays helm ownership while a vanilla cargo open request is being handled.</summary>
    /// <param name="__instance">The container sharing the ship's network object.</param>
    [HarmonyPatch(typeof(Container), nameof(Container.RPC_RequestOpen))]
    [HarmonyPrefix]
    public static void ContainerOwnershipRequest(Container __instance)
    {
        __instance.m_nview?.GetComponent<ShipAssist>()?.ReserveContainerOwnership();
    }

    /// <summary>Delays helm ownership while a vanilla cargo stack request is being handled.</summary>
    /// <param name="__instance">The container sharing the ship's network object.</param>
    [HarmonyPatch(typeof(Container), nameof(Container.RPC_RequestStack))]
    [HarmonyPrefix]
    public static void ContainerStackOwnershipRequest(Container __instance)
    {
        ContainerOwnershipRequest(__instance);
    }

    /// <summary>
    /// Thins the fog near a White Hilt Ship with the Mast Wisp, after the game has set it.
    /// </summary>
    /// <param name="dt">Seconds since the last update.</param>
    [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.SetEnv))]
    [HarmonyPostfix]
    public static void SetEnv(float dt)
    {
        // Without a camera the game leaves the fog as it was, and scaling it again would thin it every update.
        if (Utils.GetMainCamera() != null)
        {
            ShipFog.Apply(dt);
        }
    }

    /// <summary>
    /// Updates the speed and heading read-out after the game's ship HUD.
    /// </summary>
    /// <param name="__instance">The HUD.</param>
    /// <param name="player">The local player.</param>
    [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateShipHud))]
    [HarmonyPostfix]
    public static void UpdateShipHud(Hud __instance, Player player)
    {
        ShipHud.Update(__instance, player);
    }

    /// <summary>
    /// Handles the sailing keys for the local player, and puts them on deck after travelling to a ship portal.
    /// </summary>
    /// <param name="__instance">The player.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    [HarmonyPostfix]
    public static void PlayerUpdate(Player __instance)
    {
        if (__instance == Player.m_localPlayer)
        {
            ShipAssist.Tick(__instance);
            Pieces.Navigation.ShipRoutePlanner.Tick(__instance);
            Pieces.Ships.WhiteHiltShip.ShipPortalArrival.Tick(__instance);
            ManOverboard.Tick(__instance);
        }
    }

    /// <summary>
    /// Registers the man overboard RPCs for this game session.
    /// </summary>
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    [HarmonyPostfix]
    public static void GameStart()
    {
        ManOverboard.RegisterRpcs();
    }

    /// <summary>
    /// Shows the lifeline prompt under the crosshair.
    /// </summary>
    /// <param name="__instance">The HUD.</param>
    /// <param name="player">The local player.</param>
    [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateCrosshair))]
    [HarmonyPostfix]
    public static void UpdateCrosshair(Hud __instance, Player player)
    {
        ManOverboard.UpdateCrosshair(__instance, player);
    }
}
