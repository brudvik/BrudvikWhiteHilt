using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Party;

/// <summary>
/// Shares the local player's health, stamina and eitr with everyone for the party list, and keeps what the others
/// share. Vanilla only syncs the health of players nearby and never their stamina, so each client sends its own
/// status to all, at most twice a second and only when something changed, with a reminder every few seconds.
/// </summary>
public class PartyNetwork : MonoBehaviour
{
    private const string StatusRpc = "WhiteHiltPartyStatus";
    private const float CheckInterval = 0.25f;
    private const float MinSendInterval = 0.5f;
    private const float KeepAlive = 5f;
    private const float Forget = 12f;
    private const float ForgetDead = 120f;

    private static PartyNetwork instance;

    private readonly Dictionary<long, Entry> statuses = new();
    private readonly List<long> expired = new();

    private PartyStatus sent;
    private float lastSend;
    private float nextCheck;

    /// <summary>
    /// A status received from a player.
    /// </summary>
    public sealed class Entry
    {
        /// <summary>The player's session ID.</summary>
        public long Uid;

        /// <summary>The last status.</summary>
        public PartyStatus Status;

        /// <summary>The status before it, for spotting hard hits.</summary>
        public PartyStatus Previous;

        /// <summary>When the last status arrived, in unscaled time.</summary>
        public float Received;
    }

    /// <summary>
    /// The statuses known, the local player's included.
    /// </summary>
    public static IEnumerable<Entry> Entries => instance != null ? instance.statuses.Values : Enumerable.Empty<Entry>();

    private void Start()
    {
        instance = this;
        ZRoutedRpc.instance?.Register<ZPackage>(StatusRpc, RPC_Status);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        if (Time.unscaledTime < nextCheck || ZNet.instance == null || ZRoutedRpc.instance == null)
        {
            return;
        }

        nextCheck = Time.unscaledTime + CheckInterval;
        Send();
        ForgetOld();
    }

    private void Send()
    {
        Player player = Player.m_localPlayer;
        if (player == null || !PartySettings.AllowParty.Value)
        {
            return;
        }

        PartyStatus status = Capture(player);
        float now = Time.unscaledTime;
        bool changed = status.DiffersFrom(sent);
        if ((!changed || now - lastSend < MinSendInterval) && now - lastSend < KeepAlive)
        {
            return;
        }

        ZPackage package = new();
        status.Write(package);
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, StatusRpc, package);
        sent = status;
        lastSend = now;
    }

    private static PartyStatus Capture(Player player)
    {
        return new PartyStatus
        {
            Name = player.GetPlayerName(),
            Health = player.GetHealth(),
            MaxHealth = player.GetMaxHealth(),
            Stamina = player.GetStamina(),
            MaxStamina = player.GetMaxStamina(),
            Eitr = player.GetEitr(),
            MaxEitr = player.GetMaxEitr(),
            Dead = player.IsDead(),
            Effects = Effects(player)
        };
    }

    // Effects with an icon, the ones that hurt first: burning, poison and frost are what a friend may need help with.
    private static int[] Effects(Player player)
    {
        return player.GetSEMan().GetStatusEffects()
            .Where(effect => effect != null && effect.m_icon != null)
            .OrderBy(effect => effect is SE_Burning || effect is SE_Poison || effect is SE_Frost ? 0 : 1)
            .Take(PartyStatus.MaxEffects)
            .Select(effect => effect.NameHash())
            .ToArray();
    }

    private void RPC_Status(long sender, ZPackage package)
    {
        PartyStatus status = PartyStatus.Read(package);
        if (status == null)
        {
            return;
        }

        if (!statuses.TryGetValue(sender, out Entry entry))
        {
            entry = new Entry { Uid = sender };
            statuses[sender] = entry;
        }

        entry.Previous = entry.Status;
        entry.Status = status;
        entry.Received = Time.unscaledTime;
    }

    // Players who stopped sending have left or the server stopped allowing it. A dead player sends nothing until they
    // respawn, so they are kept longer.
    private void ForgetOld()
    {
        expired.Clear();
        float now = Time.unscaledTime;
        foreach (Entry entry in statuses.Values)
        {
            if (now - entry.Received > (entry.Status.Dead ? ForgetDead : Forget))
            {
                expired.Add(entry.Uid);
            }
        }

        foreach (long uid in expired)
        {
            statuses.Remove(uid);
        }
    }
}
