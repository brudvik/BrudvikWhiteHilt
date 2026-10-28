using System;
using BrudvikWhiteHilt.Performance;
using HarmonyLib;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Performance;

/// <summary>
/// Ends the loading screen after a portal, a respawn or joining when the destination is ready, instead of after the
/// game's fixed 8 seconds (<see cref="LoadingWait"/>). The patches only move the game's own timer forward to where its
/// fixed wait ends; every check the game makes after that still runs.
/// </summary>
[HarmonyPatch]
public static class LoadingWaitPatches
{
    // Vanilla's teleport: the player is moved after 2 s, and a distant teleport then waits until 8 s.
    private const float TeleportMoveDelay = 2f;
    private const float TeleportFixedWait = 8f;

    /// <summary>
    /// Adds the service that registers the RPCs and answers on the server.
    /// </summary>
    /// <param name="__instance">The game.</param>
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    [HarmonyPostfix]
    public static void GameStartPostfix(Game __instance)
    {
        __instance.gameObject.AddComponent<LoadingWaitService>();
    }

    /// <summary>
    /// A new portal trip asks the server about its destination as the screen fades.
    /// </summary>
    /// <param name="__instance">The player.</param>
    /// <param name="pos">The destination.</param>
    /// <param name="distantTeleport">True for a trip that loads a new area.</param>
    /// <param name="__result">True when the trip started.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.TeleportTo))]
    [HarmonyPostfix]
    public static void TeleportToPostfix(Player __instance, Vector3 pos, bool distantTeleport, bool __result)
    {
        if (!__result || __instance != Player.m_localPlayer)
        {
            return;
        }

        try
        {
            LoadingWait.Forget();
            if (distantTeleport)
            {
                LoadingWait.Prepare(pos);
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning("Loading wait: could not ask about the destination: " + ex.Message);
        }
    }

    /// <summary>
    /// Ends a distant teleport's fixed wait once the destination is ready.
    /// </summary>
    /// <param name="__instance">The player.</param>
    /// <param name="dt">The frame time the game adds to the timer.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdateTeleport))]
    [HarmonyPrefix]
    public static void UpdateTeleportPrefix(Player __instance, float dt)
    {
        if (!__instance.m_teleporting || !__instance.m_distantTeleport || __instance != Player.m_localPlayer)
        {
            return;
        }

        float timer = __instance.m_teleportTimer + dt;
        if (timer <= TeleportMoveDelay || timer > TeleportFixedWait)
        {
            return;
        }

        try
        {
            if (LoadingWait.AreaReady(__instance.m_teleportTargetPos))
            {
                Jotunn.Logger.LogInfo($"Loading wait: portal arrival after {timer:0.0} s instead of {TeleportFixedWait:0} s ("
                    + LoadingWait.Describe() + ").");
                __instance.m_teleportTimer = TeleportFixedWait;
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning("Loading wait failed, the game's own wait applies: " + ex.Message);
        }
    }

    /// <summary>
    /// Ends a respawn's or a login's fixed wait once the spawn point is ready; at a bed, only once the bed is there.
    /// </summary>
    /// <param name="__instance">The game.</param>
    /// <param name="dt">The frame time the game adds to the wait.</param>
    [HarmonyPatch(typeof(Game), nameof(Game.FindSpawnPoint))]
    [HarmonyPrefix]
    public static void FindSpawnPointPrefix(Game __instance, float dt)
    {
        try
        {
            if (__instance.m_respawnWait <= 0f)
            {
                // A new respawn or login: ask afresh.
                LoadingWait.Forget();
            }

            if (__instance.m_respawnWait + dt > __instance.m_respawnLoadDuration)
            {
                return;
            }

            PlayerProfile profile = __instance.m_playerProfile;
            if (profile == null || !SpawnPoint(__instance, profile, out Vector3 point, out bool bed) || !LoadingWait.AreaReady(point))
            {
                return;
            }

            // The game forgets the bed when it does not find it; a bed that has not been built yet must not be lost.
            if (bed && __instance.FindBedNearby(point, 5f) == null)
            {
                return;
            }

            Jotunn.Logger.LogInfo($"Loading wait: spawned after {__instance.m_respawnWait + dt:0.0} s instead of "
                + $"{__instance.m_respawnLoadDuration:0} s ({LoadingWait.Describe()}).");
            __instance.m_respawnWait = __instance.m_respawnLoadDuration;
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning("Loading wait failed, the game's own wait applies: " + ex.Message);
        }
    }

    // The point the game's FindSpawnPoint waits for: the logout point when joining, else the bed.
    private static bool SpawnPoint(Game game, PlayerProfile profile, out Vector3 point, out bool bed)
    {
        bed = false;
        if (!game.m_respawnAfterDeath && profile.HaveLogoutPoint())
        {
            point = profile.GetLogoutPoint();
            return true;
        }

        if (profile.HaveCustomSpawnPoint())
        {
            point = profile.GetCustomSpawnPoint();
            bed = true;
            return true;
        }

        point = Vector3.zero;
        return false;
    }
}
