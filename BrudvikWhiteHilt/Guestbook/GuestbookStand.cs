using BrudvikWhiteHilt.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Guestbook;

/// <summary>
/// The kinds of guestbook entry.
/// </summary>
public enum GuestEntryKind
{
    /// <summary>A player came by.</summary>
    Visit,

    /// <summary>A player built a piece.</summary>
    Built,

    /// <summary>A player tore down a piece.</summary>
    Removed,

    /// <summary>Enemies attacked.</summary>
    Raid,

    /// <summary>A raid was beaten off.</summary>
    RaidEnd
}

/// <summary>
/// A guestbook on its stand: everyone can read who came by, who built and tore down what, and when the place was
/// raided, within <see cref="GuestbookSettings.Radius"/>. The book's owner watches for visitors and raids and writes
/// the entries to its ZDO; building and tearing down are reported to it by the player who did it.
/// </summary>
public class GuestbookStand : MonoBehaviour, Interactable, Hoverable
{
    private const string LogRpc = "WhiteHiltGuestLog";
    private const float TickSeconds = 5f;

    private static readonly int entriesKey = "whitehilt_guestbook".GetStableHashCode();
    private static readonly List<GuestbookStand> stands = new();

    private readonly Dictionary<string, double> lastSeen = new();
    private ZNetView nview;
    private Piece piece;
    private float nextTick;
    private float raidStarted = -1f;
    private float lastEnemy;
    private bool primed;

    /// <summary>
    /// One entry in the book.
    /// </summary>
    public readonly struct Entry
    {
        /// <summary>
        /// Creates an entry.
        /// </summary>
        /// <param name="day">Game day.</param>
        /// <param name="minute">Game minute of the day.</param>
        /// <param name="kind">Kind.</param>
        /// <param name="who">Player name or creature list.</param>
        /// <param name="what">Piece name token, or minutes.</param>
        /// <param name="count">How many times.</param>
        public Entry(int day, int minute, GuestEntryKind kind, string who, string what, int count)
        {
            Day = day;
            Minute = minute;
            Kind = kind;
            Who = who;
            What = what;
            Count = count;
        }

        /// <summary>Game day.</summary>
        public int Day { get; }

        /// <summary>Game minute of the day.</summary>
        public int Minute { get; }

        /// <summary>Kind.</summary>
        public GuestEntryKind Kind { get; }

        /// <summary>Player name or creature list.</summary>
        public string Who { get; }

        /// <summary>Piece name token, or minutes.</summary>
        public string What { get; }

        /// <summary>How many times.</summary>
        public int Count { get; }

        /// <summary>
        /// The entry as a localized line.
        /// </summary>
        /// <returns>The text.</returns>
        public string Text()
        {
            string what = Localization.instance.Localize(What ?? string.Empty) + (Count > 1 ? $" x{Count}" : string.Empty);
            string format = Localization.instance.Localize("$whitehilt_guest_" + Kind.ToString().ToLowerInvariant());
            return string.Format(format, Localization.instance.Localize(Who ?? string.Empty), what);
        }

        /// <summary>
        /// The game time of the entry.
        /// </summary>
        /// <returns>The text.</returns>
        public string Time()
        {
            return string.Format(Localization.instance.Localize("$whitehilt_guest_time"), Day, $"{Minute / 60:00}:{Minute % 60:00}");
        }

        /// <summary>
        /// The entry as a stored line.
        /// </summary>
        /// <returns>The line.</returns>
        public string Serialize()
        {
            return string.Join("|", Day.ToString(CultureInfo.InvariantCulture), Minute.ToString(CultureInfo.InvariantCulture), Kind.ToString(),
                Clean(Who), Clean(What), Count.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Reads a stored line.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="entry">The entry.</param>
        /// <returns>False if the line is broken.</returns>
        public static bool TryParse(string line, out Entry entry)
        {
            entry = default;
            string[] fields = line.Split('|');
            if (fields.Length < 6 || !int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int day)
                || !int.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int minute)
                || !Enum.TryParse(fields[2], out GuestEntryKind kind)
                || !int.TryParse(fields[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out int count))
            {
                return false;
            }

            entry = new Entry(day, minute, kind, fields[3], fields[4], count);
            return true;
        }

        private static string Clean(string text)
        {
            return (text ?? string.Empty).Replace("|", "/").Replace("\n", " ");
        }
    }

    /// <summary>
    /// Tells the guestbooks near a place that a player built or tore down a piece there.
    /// </summary>
    /// <param name="kind">Built or removed.</param>
    /// <param name="position">Where.</param>
    /// <param name="pieceName">The piece's name token.</param>
    public static void Report(GuestEntryKind kind, Vector3 position, string pieceName)
    {
        Player player = Player.m_localPlayer;
        if (player == null)
        {
            return;
        }

        float radius = GuestbookSettings.Radius.Value;
        foreach (GuestbookStand stand in stands)
        {
            if (stand != null && stand.nview != null && stand.nview.IsValid() && global::Utils.DistanceXZ(stand.transform.position, position) <= radius)
            {
                stand.nview.InvokeRPC(LogRpc, (int)kind, player.GetPlayerName(), pieceName ?? string.Empty);
            }
        }
    }

    /// <summary>
    /// The entries in the book, oldest first.
    /// </summary>
    /// <returns>The entries.</returns>
    public List<Entry> Entries()
    {
        List<Entry> entries = new();
        string text = nview != null && nview.IsValid() ? nview.GetZDO().GetString(entriesKey) : string.Empty;
        foreach (string line in text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (Entry.TryParse(line, out Entry entry))
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    /// <inheritdoc/>
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || user != Player.m_localPlayer)
        {
            return false;
        }

        GuestbookPanel.Open(this);
        return true;
    }

    /// <inheritdoc/>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        return false;
    }

    /// <inheritdoc/>
    public string GetHoverText()
    {
        return Localization.instance.Localize($"{GetHoverName()}\n[<color=yellow><b>$KEY_Use</b></color>] $whitehilt_guest_read");
    }

    /// <inheritdoc/>
    public string GetHoverName()
    {
        return piece != null ? Localization.instance.Localize(piece.m_name) : string.Empty;
    }

    /// <inheritdoc/>
    public float GetHoverOffset()
    {
        return 0f;
    }

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        piece = GetComponent<Piece>();
        if (nview == null || nview.GetZDO() == null)
        {
            enabled = false;
            return;
        }

        nview.Register<int, string, string>(LogRpc, RPC_Log);
        stands.Add(this);
    }

    private void OnDestroy()
    {
        stands.Remove(this);
    }

    private void Update()
    {
        if (Time.time < nextTick || nview == null || !nview.IsValid() || !nview.IsOwner())
        {
            return;
        }

        nextTick = Time.time + TickSeconds;
        WatchVisitors();
        WatchRaids();
    }

    private void WatchVisitors()
    {
        double now = ZNet.instance.GetTimeSeconds();
        double gap = GuestbookSettings.VisitGapMinutes.Value * GameSecondsPerMinute();
        float radius = GuestbookSettings.Radius.Value;
        foreach (Player player in Player.GetAllPlayers())
        {
            if (player == null || player.IsDead() || global::Utils.DistanceXZ(player.transform.position, transform.position) > radius)
            {
                continue;
            }

            string name = player.GetPlayerName();
            bool returning = primed && (!lastSeen.TryGetValue(name, out double seen) || now - seen > gap);
            lastSeen[name] = now;
            if (returning)
            {
                Append(GuestEntryKind.Visit, name, string.Empty);
            }
        }

        // Whoever is already here when the book loads or changes hands came by before.
        primed = true;
    }

    private void WatchRaids()
    {
        float radius = GuestbookSettings.Radius.Value;
        Dictionary<string, int> enemies = new();
        foreach (Character character in Character.GetAllCharacters())
        {
            if (character == null || character.IsPlayer() || character.IsDead() || character.IsTamed()
                || global::Utils.DistanceXZ(character.transform.position, transform.position) > radius)
            {
                continue;
            }

            BaseAI ai = character.GetBaseAI();
            if (ai != null && (ai.IsAlerted() || ai.HuntPlayer()) && IsHostile(character, ai))
            {
                enemies.TryGetValue(character.m_name, out int count);
                enemies[character.m_name] = count + 1;
            }
        }

        if (enemies.Count > 0)
        {
            lastEnemy = Time.time;
            if (raidStarted < 0f)
            {
                raidStarted = Time.time;
                string list = string.Join(", ", enemies.Select(enemy => Localization.instance.Localize(enemy.Key) + (enemy.Value > 1 ? $" x{enemy.Value}" : string.Empty)));
                Append(GuestEntryKind.Raid, string.Empty, list);
            }
        }
        else if (raidStarted >= 0f && Time.time - lastEnemy > GuestbookSettings.RaidQuietSeconds.Value)
        {
            int minutes = Mathf.Max(1, Mathf.RoundToInt((lastEnemy - raidStarted) / 60f));
            raidStarted = -1f;
            Append(GuestEntryKind.RaidEnd, string.Empty, minutes.ToString(CultureInfo.InvariantCulture));
        }
    }

    private void RPC_Log(long sender, int kind, string who, string what)
    {
        if (nview.IsOwner())
        {
            Append((GuestEntryKind)kind, who, what);
        }
    }

    // Owner only. Building the same piece again soon after is counted on the last entry.
    private void Append(GuestEntryKind kind, string who, string what)
    {
        int day = EnvMan.instance != null ? EnvMan.instance.GetDay() : 0;
        int minute = EnvMan.instance != null ? Mathf.FloorToInt(EnvMan.instance.GetDayFraction() * 1440f) : 0;
        List<Entry> entries = Entries();
        Entry entry = new(day, minute, kind, who, what, 1);
        if ((kind == GuestEntryKind.Built || kind == GuestEntryKind.Removed) && entries.Count > 0)
        {
            Entry last = entries[entries.Count - 1];
            float minutes = (day - last.Day) * 1440f + minute - last.Minute;
            if (last.Kind == kind && last.Who == who && last.What == what && minutes <= GuestbookSettings.MergeMinutes.Value)
            {
                entries[entries.Count - 1] = new Entry(last.Day, last.Minute, kind, who, what, last.Count + 1);
                Save(entries);
                return;
            }
        }

        entries.Add(entry);
        Save(entries);
    }

    private void Save(List<Entry> entries)
    {
        int max = GuestbookSettings.MaxEntries.Value;
        if (entries.Count > max)
        {
            entries.RemoveRange(0, entries.Count - max);
        }

        nview.GetZDO().Set(entriesKey, string.Join("\n", entries.Select(entry => entry.Serialize())));
        GuestbookPanel.Refresh(this);
    }

    private static double GameSecondsPerMinute()
    {
        float day = EnvMan.instance != null ? EnvMan.instance.m_dayLengthSec : 1800f;
        return day / 1440.0;
    }

    private static bool IsHostile(Character character, BaseAI ai)
    {
        switch (character.GetFaction())
        {
            case Character.Faction.Players:
            case Character.Faction.AnimalsVeg:
            case Character.Faction.PlayerSpawned:
                return false;
            case Character.Faction.Dverger:
                return ai.IsAggravated();
            default:
                return true;
        }
    }
}
