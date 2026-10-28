using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Performance;

/// <summary>
/// Holds back the game's hourly <c>Resources.UnloadUnusedAssets</c> until a quiet moment. <c>Game.Awake</c> starts
/// <c>CollectResourcesCheckPeriodic</c> every 3600 seconds on clients and servers alike, and the unload walks every
/// loaded object (and runs a full garbage collection with it), which freezes a large server for a moment with
/// everyone on it. A dedicated server now waits until no player has been on it for two minutes; a client waits for a
/// loading screen, a portal, sleep or the pause of a single-player game, where the game also unloads by itself. Either
/// still unloads once it is overdue (12 hours on a server, 3 on a client), so memory cannot grow without end. Other
/// unloads (after loading a world, after sleep, from the console) are left alone.
/// </summary>
public static class AssetUnloadScheduler
{
    private const float CheckSeconds = 5f;
    private const float ServerEmptySeconds = 120f;
    private static readonly TimeSpan ServerMaxWait = TimeSpan.FromHours(12);
    private static readonly TimeSpan ClientMaxWait = TimeSpan.FromHours(3);

    private static bool pending;
    private static DateTime pendingSince;
    private static float nextCheck;
    private static float lastPlayerSeen = float.NegativeInfinity;

    /// <summary>
    /// Asked when the hourly unload is due. False holds it back until <see cref="Tick"/> finds a quiet moment.
    /// </summary>
    /// <param name="game">The game.</param>
    /// <returns>True to let the game unload now.</returns>
    public static bool AllowHourly(Game game)
    {
        if (!VanillaFixSettings.AssetUnload.Active || IsQuiet() || Overdue(game))
        {
            pending = false;
            return true;
        }

        if (!pending)
        {
            pending = true;
            pendingSince = DateTime.Now;
            Jotunn.Logger.LogInfo(IsServer()
                ? "Performance: the hourly unloading of unused assets waits until the server has been empty for two minutes."
                : "Performance: the hourly unloading of unused assets waits for a loading screen, sleep or a pause.");
        }

        return false;
    }

    /// <summary>
    /// Runs every frame from <c>Game.Update</c>: notes when a server last had players, and every few seconds unloads a
    /// held-back unload once the moment is quiet or it is overdue.
    /// </summary>
    /// <param name="game">The game.</param>
    public static void Tick(Game game)
    {
        if (Time.realtimeSinceStartup < nextCheck)
        {
            return;
        }

        nextCheck = Time.realtimeSinceStartup + CheckSeconds;
        if (IsServer() && ZNet.instance.GetPeerConnections() > 0)
        {
            lastPlayerSeen = Time.realtimeSinceStartup;
        }

        if (!pending)
        {
            return;
        }

        // Something else unloaded in the meantime (sleep, a world load, the console), so this one is no longer due.
        if (game.m_lastCollectResources >= pendingSince)
        {
            pending = false;
            return;
        }

        if (!VanillaFixSettings.AssetUnload.Active || IsQuiet() || Overdue(game))
        {
            pending = false;
            Jotunn.Logger.LogInfo("Performance: unloading unused assets now, held back since " + pendingSince.ToString("HH:mm") + ".");
            game.CollectResources(displayMessage: !IsServer());
        }
    }

    private static bool IsServer()
    {
        return ZNet.instance != null && ZNet.instance.IsDedicated();
    }

    // A server with nobody on it for a while; a client behind a black screen or paused.
    private static bool IsQuiet()
    {
        if (IsServer())
        {
            return ZNet.instance.GetPeerConnections() == 0 && Time.realtimeSinceStartup - lastPlayerSeen >= ServerEmptySeconds;
        }

        Player player = Player.m_localPlayer;
        return player == null || player.IsTeleporting() || player.IsSleeping() || Game.IsPaused();
    }

    private static bool Overdue(Game game)
    {
        return DateTime.Now - game.m_lastCollectResources >= (IsServer() ? ServerMaxWait : ClientMaxWait);
    }
}
