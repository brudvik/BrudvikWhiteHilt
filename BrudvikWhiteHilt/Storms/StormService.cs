using BrudvikWhiteHilt.Difficulty;
using BrudvikWhiteHilt.Difficulty.Beasts;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// Runs on the <see cref="Game"/> object. On the server it decides when a storm comes and whether it is Ragnarok, warns
/// of it, lets it rage with its eye in the middle and sends the state to every client; it also answers the admins' storm
/// commands.
/// </summary>
public class StormService : MonoBehaviour
{
    private const string StateRpc = "WhiteHiltStorm";
    private const string AdminRpc = "WhiteHiltStormAdmin";
    private const string AdminReplyRpc = "WhiteHiltStormReply";
    private const float CheckInterval = 60f;
    private const float SendInterval = 30f;
    private const float RagnarokLonger = 1.5f;
    private const float RagnarokEarlier = 2f;
    private const float ShortestWithEye = 120f;

    private double nextCheck;
    private double calmUntil;
    private float nextSend;
    private int number;
    private int knownBosses = -1;
    private bool ragnarokDue;

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
            WatchBosses();
            if (ShouldStart(now))
            {
                number++;
                StormState.SetStorm(RollRagnarok(), 0.0, 0.0);
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

    // Counts the fallen bosses: when one more has fallen, the next storm is Ragnarok. The count of a world just loaded
    // is taken as it is.
    private void WatchBosses()
    {
        if (ZoneSystem.instance == null)
        {
            return;
        }

        int bosses = BeastDefinition.BossKeys.Count(ZoneSystem.instance.GetGlobalKey);
        if (knownBosses >= 0 && bosses > knownBosses)
        {
            ragnarokDue = true;
            Jotunn.Logger.LogInfo("Storms: a boss has fallen; the next storm is Ragnarok");
        }

        knownBosses = bosses;
    }

    // Ragnarok comes only once a boss has fallen: always after a new one, else now and then.
    private bool RollRagnarok()
    {
        if (!StormSettings.Ragnarok.Value || knownBosses <= 0)
        {
            return false;
        }

        bool ragnarok = ragnarokDue || UnityEngine.Random.value < StormSettings.RagnarokChance.Value / 100f;
        ragnarokDue = false;
        return ragnarok;
    }

    // Decided, and foretold by the weather forecast before everyone is warned; Ragnarok twice as early.
    private void Brew(double now)
    {
        float minutes = StormSettings.ForecastMinutes.Value * (StormState.Ragnarok ? RagnarokEarlier : 1f);
        StormState.Set(StormPhase.Brewing, now + minutes * 60.0, number);
        Jotunn.Logger.LogInfo($"Storms: {Name()} is brewing; the warning comes in {minutes:0.#} minutes");
    }

    private void Warn(double now)
    {
        float minutes = StormSettings.WarningMinutes.Value * (StormState.Ragnarok ? RagnarokEarlier : 1f);
        StormState.Set(StormPhase.Warning, now + minutes * 60.0, number);
        Jotunn.Logger.LogInfo($"Storms: {Name()} is coming in {minutes:0.#} minutes");
    }

    // Rages for its time, Ragnarok half as long again, with the eye somewhere in the middle of a storm long enough.
    private void Rage(double now)
    {
        float min = Mathf.Min(StormSettings.MinMinutes.Value, StormSettings.MaxMinutes.Value);
        float max = Mathf.Max(StormSettings.MinMinutes.Value, StormSettings.MaxMinutes.Value);
        float seconds = UnityEngine.Random.Range(min, max) * 60f * (StormState.Ragnarok ? RagnarokLonger : 1f);
        float eye = seconds >= ShortestWithEye ? StormSettings.EyeSeconds.Value : 0f;
        (double eyeStarts, double eyeEnds, double ends) = StormState.PlanEye(now, seconds, eye, UnityEngine.Random.Range(0.4f, 0.55f));
        StormState.SetStorm(StormState.Ragnarok, eyeStarts, eyeEnds);
        StormState.Set(StormPhase.Raging, ends, number);
        string eyeText = eye > 0f ? $", the eye after {(eyeStarts - now) / 60.0:0.#}" : string.Empty;
        Jotunn.Logger.LogInfo($"Storms: {Name()} breaks for {seconds / 60f:0.#} minutes{eyeText}");
    }

    private string Name() => StormState.Ragnarok ? $"storm {number} (Ragnarok)" : $"storm {number}";

    private void End(double now)
    {
        if (StormState.Raging)
        {
            StormState.Passed(now);
        }

        StormState.Set(StormPhase.Calm, 0.0, number);
        StormState.SetStorm(false, 0.0, 0.0);
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
        string order = command?.ToLowerInvariant();
        if (StormState.Phase == StormPhase.Calm && order != "stop")
        {
            number++;
        }

        switch (order)
        {
            case "warn":
                Warn(now);
                Reply(sender, $"A storm is coming in {StormSettings.WarningMinutes.Value:0.#} minutes.");
                break;
            case "stop":
                End(now);
                calmUntil = now;
                Reply(sender, "The storm is stopped.");
                break;
            case "ragnarok":
                StormState.SetStorm(true, 0.0, 0.0);
                Rage(now);
                Reply(sender, "Ragnarok breaks.");
                break;
            default:
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
