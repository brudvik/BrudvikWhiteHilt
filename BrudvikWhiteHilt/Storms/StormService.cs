using BrudvikWhiteHilt.Difficulty;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// Runs on the <see cref="Game"/> object. On the server it decides when a storm comes, warns of it, lets it rage and
/// sends the state to every client; it also answers the admins' storm commands.
/// </summary>
public class StormService : MonoBehaviour
{
    private const string StateRpc = "WhiteHiltStorm";
    private const string AdminRpc = "WhiteHiltStormAdmin";
    private const string AdminReplyRpc = "WhiteHiltStormReply";
    private const float CheckInterval = 60f;
    private const float SendInterval = 30f;

    private double nextCheck;
    private double calmUntil;
    private float nextSend;
    private int number;

    /// <summary>
    /// Sends an admin command to the server.
    /// </summary>
    /// <param name="command">start, warn or stop.</param>
    public static void SendAdmin(string command)
    {
        if (ZRoutedRpc.instance == null || ZNet.instance == null)
        {
            Print("Join a world first.");
            return;
        }

        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), AdminRpc, command ?? string.Empty);
    }

    private static void Print(string message)
    {
        if (global::Console.instance != null)
        {
            global::Console.instance.Print(message);
        }
    }

    private void Start()
    {
        StormState.Reset();
        ZRoutedRpc.instance?.Register<ZPackage>(StateRpc, RPC_State);
        ZRoutedRpc.instance?.Register<string>(AdminRpc, RPC_Admin);
        ZRoutedRpc.instance?.Register<string>(AdminReplyRpc, RPC_AdminReply);
    }

    private void Update()
    {
        if (ZNet.instance == null || ZRoutedRpc.instance == null || !ZNet.instance.IsServer())
        {
            return;
        }

        try
        {
            UpdateServer(ZNet.instance.GetTimeSeconds());
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Storms: update failed: {ex.Message}");
            nextCheck = ZNet.instance.GetTimeSeconds() + CheckInterval;
        }
    }

    // On the server: moves the storm on when its phase is over, and in the calm rolls now and then for a new one.
    private void UpdateServer(double now)
    {
        bool changed = false;
        if (!StormSettings.Enabled.Value && StormState.Phase != StormPhase.Calm)
        {
            End(now);
            changed = true;
        }

        if (StormState.Phase == StormPhase.Brewing && now >= StormState.PhaseEnds)
        {
            Warn(now);
            changed = true;
        }
        else if (StormState.Phase == StormPhase.Warning && now >= StormState.PhaseEnds)
        {
            Rage(now);
            changed = true;
        }
        else if (StormState.Phase == StormPhase.Raging && now >= StormState.PhaseEnds)
        {
            End(now);
            changed = true;
        }
        else if (StormState.Phase == StormPhase.Calm && now >= nextCheck)
        {
            nextCheck = now + CheckInterval;
            if (ShouldStart(now))
            {
                number++;
                if (StormSettings.ForecastMinutes.Value > 0f)
                {
                    Brew(now);
                }
                else
                {
                    Warn(now);
                }

                changed = true;
            }
        }

        if (changed || Time.time >= nextSend)
        {
            Broadcast();
        }
    }

    private bool ShouldStart(double now)
    {
        if (!StormSettings.Enabled.Value || now < calmUntil || EnvMan.instance == null)
        {
            return false;
        }

        if (EnvMan.instance.GetDay() < StormSettings.GraceDays.Value || (StormSettings.NotWithBloodMoon.Value && DifficultyState.BloodMoon))
        {
            return false;
        }

        float chance = StormState.ChancePerCheck(StormSettings.StormsPerDay.Value, CheckInterval, EnvMan.instance.m_dayLengthSec);
        return UnityEngine.Random.value < chance;
    }

    // Decided, and foretold by the weather forecast before everyone is warned.
    private void Brew(double now)
    {
        StormState.Set(StormPhase.Brewing, now + StormSettings.ForecastMinutes.Value * 60.0, number);
        Jotunn.Logger.LogInfo($"Storms: storm {number} is brewing; the warning comes in {StormSettings.ForecastMinutes.Value:0.#} minutes");
    }

    private void Warn(double now)
    {
        StormState.Set(StormPhase.Warning, now + StormSettings.WarningMinutes.Value * 60.0, number);
        Jotunn.Logger.LogInfo($"Storms: storm {number} is coming in {StormSettings.WarningMinutes.Value:0.#} minutes");
    }

    private void Rage(double now)
    {
        float min = Mathf.Min(StormSettings.MinMinutes.Value, StormSettings.MaxMinutes.Value);
        float max = Mathf.Max(StormSettings.MinMinutes.Value, StormSettings.MaxMinutes.Value);
        float minutes = UnityEngine.Random.Range(min, max);
        StormState.Set(StormPhase.Raging, now + minutes * 60.0, number);
        Jotunn.Logger.LogInfo($"Storms: storm {number} breaks for {minutes:0.#} minutes");
    }

    private void End(double now)
    {
        if (StormState.Raging)
        {
            StormState.Passed(now);
        }

        StormState.Set(StormPhase.Calm, 0.0, number);
        calmUntil = now + StormSettings.MinutesBetween.Value * 60.0;
        Jotunn.Logger.LogInfo($"Storms: storm {number} passes");
    }

    private void Broadcast()
    {
        nextSend = Time.time + SendInterval;
        ZPackage package = StormState.Write();
        foreach (ZNetPeer peer in ZNet.instance.GetPeers())
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, StateRpc, package);
        }
    }

    private void RPC_State(long sender, ZPackage package)
    {
        if (sender == ZRoutedRpc.instance.GetServerPeerID() && !ZNet.instance.IsServer())
        {
            StormState.Read(package);
        }
    }

    // On the server: an admin's command to warn of a storm, start one now or stop it.
    private void RPC_Admin(long sender, string command)
    {
        if (!ZNet.instance.IsServer())
        {
            return;
        }

        if (!IsAdmin(sender))
        {
            Reply(sender, "Only admins can do that.");
            return;
        }

        double now = ZNet.instance.GetTimeSeconds();
        switch (command?.ToLowerInvariant())
        {
            case "warn":
                if (StormState.Phase == StormPhase.Calm)
                {
                    number++;
                }

                Warn(now);
                Reply(sender, $"A storm is coming in {StormSettings.WarningMinutes.Value:0.#} minutes.");
                break;
            case "stop":
                End(now);
                calmUntil = now;
                Reply(sender, "The storm is stopped.");
                break;
            default:
                if (StormState.Phase == StormPhase.Calm)
                {
                    number++;
                }

                Rage(now);
                Reply(sender, "The storm breaks.");
                break;
        }

        Broadcast();
    }

    private static bool IsAdmin(long sender)
    {
        if (sender == ZRoutedRpc.instance.m_id)
        {
            return true;
        }

        ZNetPeer peer = ZNet.instance.GetPeer(sender);
        return peer?.m_socket != null && ZNet.instance.IsAdmin(peer.m_socket.GetHostName());
    }

    private static void Reply(long target, string message)
    {
        ZRoutedRpc.instance.InvokeRoutedRPC(target, AdminReplyRpc, message);
    }

    private void RPC_AdminReply(long sender, string message)
    {
        if (sender == ZRoutedRpc.instance.GetServerPeerID())
        {
            Print(message);
        }
    }
}
