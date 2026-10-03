using BrudvikWhiteHilt.Difficulty.Beasts;
using BrudvikWhiteHilt.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Saga;

/// <summary>
/// The kinds of deed in a saga.
/// </summary>
public enum SagaKind
{
    /// <summary>A boss slain.</summary>
    Boss,

    /// <summary>A black beast slain.</summary>
    Beast,

    /// <summary>A listed creature slain.</summary>
    Monster,

    /// <summary>A treasure dug up.</summary>
    Treasure,

    /// <summary>First steps into a biome.</summary>
    Biome,

    /// <summary>A skill milestone.</summary>
    Skill
}

/// <summary>
/// The local player's saga: deeds with the day they happened, kept on the character, and the renown they give.
/// Slain foes and treasures are shared by everyone near, so the whole party has them in their sagas.
/// </summary>
public static class SagaLog
{
    private const string EntriesKey = "whitehilt_saga";
    private const string RenownKey = "whitehilt_saga_renown";
    private const string DeedRpc = "WhiteHiltSagaDeed";
    private const int MaxEntries = 300;

    private static readonly int[] milestones = { 25, 50, 75, 100 };

    /// <summary>
    /// A deed in the saga.
    /// </summary>
    public readonly struct Entry
    {
        /// <summary>
        /// Creates an entry.
        /// </summary>
        /// <param name="day">Game day.</param>
        /// <param name="kind">Kind of deed.</param>
        /// <param name="name">Name or token of what it was about.</param>
        /// <param name="extra">Extra value, e.g. the skill level.</param>
        public Entry(int day, SagaKind kind, string name, string extra)
        {
            Day = day;
            Kind = kind;
            Name = name;
            Extra = extra;
        }

        /// <summary>Game day.</summary>
        public int Day { get; }

        /// <summary>Kind of deed.</summary>
        public SagaKind Kind { get; }

        /// <summary>Name or token.</summary>
        public string Name { get; }

        /// <summary>Extra value.</summary>
        public string Extra { get; }

        /// <summary>
        /// The deed as a localized line.
        /// </summary>
        /// <returns>The text.</returns>
        public string Text()
        {
            string name = Localization.instance.Localize(Name ?? string.Empty);
            string format = Localization.instance.Localize("$whitehilt_saga_" + Kind.ToString().ToLowerInvariant());
            return string.Format(format, name, Extra);
        }
    }

    /// <summary>
    /// The local player's renown.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>Renown.</returns>
    public static int Renown(Player player)
    {
        return player != null && player.m_customData.TryGetValue(RenownKey, out string text)
            && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int renown) ? renown : 0;
    }

    /// <summary>
    /// A player's rank from its renown.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>Rank, 0 for none.</returns>
    public static int Rank(Player player)
    {
        if (SagaSettings.Enabled == null || !SagaSettings.Enabled.Value)
        {
            return 0;
        }

        return Mathf.Min(SagaSettings.MaxRank.Value, Renown(player) / Mathf.Max(1, SagaSettings.RenownPerRank.Value));
    }

    /// <summary>
    /// The local player's deeds, oldest first.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The entries.</returns>
    public static List<Entry> Entries(Player player)
    {
        List<Entry> entries = new();
        if (player == null || !player.m_customData.TryGetValue(EntriesKey, out string text))
        {
            return entries;
        }

        foreach (string line in text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string[] fields = line.Split('|');
            if (fields.Length >= 4 && int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int day)
                && Enum.TryParse(fields[1], out SagaKind kind))
            {
                entries.Add(new Entry(day, kind, fields[2], fields[3]));
            }
        }

        return entries;
    }

    /// <summary>
    /// Registers the deed RPC. Call when the game starts.
    /// </summary>
    public static void RegisterRpc()
    {
        ZRoutedRpc.instance?.Register<int, string, Vector3>(DeedRpc, RPC_Deed);
    }

    /// <summary>
    /// Tells everyone about a deed at a place; those near it add it to their sagas.
    /// </summary>
    /// <param name="kind">Kind of deed.</param>
    /// <param name="name">Name or token of what it was about.</param>
    /// <param name="position">Where it happened.</param>
    public static void Announce(SagaKind kind, string name, Vector3 position)
    {
        if (SagaSettings.Enabled.Value)
        {
            ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, DeedRpc, (int)kind, name ?? string.Empty, position);
        }
    }

    /// <summary>
    /// Notes a slain creature if its death is a deed. Called on the machine that owns the creature.
    /// </summary>
    /// <param name="character">The creature.</param>
    public static void OnDeath(Character character)
    {
        if (character == null || character.IsPlayer() || character.IsTamed())
        {
            return;
        }

        string prefab = global::Utils.GetPrefabName(character.gameObject);
        if (character.IsBoss())
        {
            Announce(SagaKind.Boss, character.m_name, character.transform.position);
        }
        else if (BeastDefinition.ByPrefab(prefab) != null)
        {
            Announce(SagaKind.Beast, character.m_name, character.transform.position);
        }
        else if (SagaSettings.IsListed(prefab))
        {
            Announce(SagaKind.Monster, character.m_name, character.transform.position);
        }
    }

    /// <summary>
    /// Notes the first steps into a biome.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="biomeName">The biome's name token.</param>
    public static void OnBiome(Player player, string biomeName)
    {
        if (player == Player.m_localPlayer && SagaSettings.Enabled.Value)
        {
            Add(player, SagaKind.Biome, biomeName, string.Empty, SagaSettings.RenownBiome.Value);
        }
    }

    /// <summary>
    /// Notes a skill reaching a milestone.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="skill">The skill.</param>
    /// <param name="before">Its level before.</param>
    /// <param name="after">Its level now.</param>
    public static void OnSkill(Player player, Skills.SkillType skill, float before, float after)
    {
        if (player != Player.m_localPlayer || !SagaSettings.Enabled.Value)
        {
            return;
        }

        foreach (int milestone in milestones)
        {
            if (before < milestone && after >= milestone)
            {
                Add(player, SagaKind.Skill, SkillToken(skill), milestone.ToString(CultureInfo.InvariantCulture), SagaSettings.RenownSkill.Value);
            }
        }
    }

    private static void RPC_Deed(long sender, int kind, string name, Vector3 position)
    {
        Player player = Player.m_localPlayer;
        if (player == null || player.IsDead() || !SagaSettings.Enabled.Value
            || global::Utils.DistanceXZ(player.transform.position, position) > SagaSettings.WitnessRange.Value)
        {
            return;
        }

        SagaKind deed = (SagaKind)kind;
        int renown = deed switch
        {
            SagaKind.Boss => SagaSettings.RenownBoss.Value,
            SagaKind.Beast => SagaSettings.RenownBeast.Value,
            SagaKind.Monster => SagaSettings.RenownMonster.Value,
            SagaKind.Treasure => SagaSettings.RenownTreasure.Value,
            _ => 0
        };
        Add(player, deed, name, string.Empty, renown);
    }

    private static void Add(Player player, SagaKind kind, string name, string extra, int renown)
    {
        int day = EnvMan.instance != null ? EnvMan.instance.GetDay() : 0;
        Entry entry = new(day, kind, Clean(name), Clean(extra));
        List<string> lines = player.m_customData.TryGetValue(EntriesKey, out string text)
            ? text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList()
            : new List<string>();
        lines.Add(string.Join("|", day.ToString(CultureInfo.InvariantCulture), kind.ToString(), entry.Name, entry.Extra));
        if (lines.Count > MaxEntries)
        {
            lines.RemoveRange(0, lines.Count - MaxEntries);
        }

        player.m_customData[EntriesKey] = string.Join("\n", lines);
        int rankBefore = Rank(player);
        player.m_customData[RenownKey] = (Renown(player) + Mathf.Max(0, renown)).ToString(CultureInfo.InvariantCulture);
        player.Message(MessageHud.MessageType.TopLeft, string.Format(Localization.instance.Localize("$msg_whitehilt_saga_deed"), entry.Text(), renown));
        int rankAfter = Rank(player);
        if (rankAfter > rankBefore)
        {
            player.Message(MessageHud.MessageType.Center, string.Format(Localization.instance.Localize("$msg_whitehilt_saga_rank"), rankAfter));
        }

        SagaPanel.Refresh();
    }

    // Vanilla skills are named $skill_<name>; skills added by mods get $skill_<number>, which Jotunn names.
    private static string SkillToken(Skills.SkillType skill)
    {
        return "$skill_" + skill.ToString().ToLowerInvariant();
    }

    private static string Clean(string text)
    {
        return (text ?? string.Empty).Replace("|", "/").Replace("\n", " ");
    }
}
