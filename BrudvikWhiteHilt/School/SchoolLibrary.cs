using BrudvikWhiteHilt.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.School;

/// <summary>
/// What has been written in a School Bench: the best level of each skill and who reached it, and every material,
/// crafting station and runestone text the writers knew. Like the map table's map, what each writer adds is merged in,
/// so nothing is lost when someone who knows less writes after someone who knows more.
/// </summary>
public sealed class SchoolLibrary
{
    /// <summary>Most bytes a library may unpack to.</summary>
    public const int MaxBytes = 4 * 1024 * 1024;

    private const int Version = 1;
    private const int MaxSkills = 512;
    private const int MaxMaterials = 8000;
    private const int MaxStations = 500;
    private const int MaxTexts = 2000;
    private const int MaxTextLength = 4000;
    private const int MaxNameLength = 200;

    /// <summary>Those who wrote, oldest first.</summary>
    public readonly List<Writer> Writers = new();

    /// <summary>The best level of each skill, by skill id.</summary>
    public readonly Dictionary<int, SkillRecord> Skills = new();

    /// <summary>Known materials by their shared name (<c>$item_...</c>).</summary>
    public readonly HashSet<string> Materials = new(StringComparer.Ordinal);

    /// <summary>Known crafting stations by their name, with the highest level known.</summary>
    public readonly Dictionary<string, int> Stations = new(StringComparer.Ordinal);

    /// <summary>Known texts: label and text.</summary>
    public readonly Dictionary<string, string> Texts = new(StringComparer.Ordinal);

    /// <summary>
    /// One who wrote in the bench.
    /// </summary>
    public readonly struct Writer
    {
        /// <summary>
        /// Creates a writer.
        /// </summary>
        /// <param name="name">The player's name.</param>
        /// <param name="day">The game day they last wrote.</param>
        public Writer(string name, int day)
        {
            Name = name;
            Day = day;
        }

        /// <summary>The player's name.</summary>
        public string Name { get; }

        /// <summary>The game day they last wrote.</summary>
        public int Day { get; }
    }

    /// <summary>
    /// The best level written for a skill.
    /// </summary>
    public readonly struct SkillRecord
    {
        /// <summary>
        /// Creates a record.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="teacher">Who wrote it.</param>
        public SkillRecord(float level, string teacher)
        {
            Level = level;
            Teacher = teacher;
        }

        /// <summary>The level.</summary>
        public float Level { get; }

        /// <summary>Who wrote it.</summary>
        public string Teacher { get; }
    }

    /// <summary>
    /// What a player knows, ready to be written in a bench.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="day">Today's game day.</param>
    /// <returns>The player's knowledge.</returns>
    public static SchoolLibrary FromPlayer(Player player, int day)
    {
        SchoolLibrary library = new();
        string name = player.GetPlayerName();
        library.Writers.Add(new Writer(name, day));
        foreach (Skills.Skill skill in player.GetSkills().GetSkillList())
        {
            if (skill?.m_info != null && skill.m_level >= 1f)
            {
                library.Skills[(int)skill.m_info.m_skill] = new SkillRecord(skill.m_level, name);
            }
        }

        library.Materials.UnionWith(player.m_knownMaterial);
        foreach (KeyValuePair<string, int> station in player.m_knownStations)
        {
            library.Stations[station.Key] = station.Value;
        }

        foreach (KeyValuePair<string, string> text in player.m_knownTexts)
        {
            library.Texts[text.Key] = text.Value;
        }

        return library;
    }

    /// <summary>
    /// Reads a library from packed bytes. Broken data gives an empty library, and lists are cut at sane sizes, since
    /// the bytes may come from another player.
    /// </summary>
    /// <param name="packed">Bytes from <see cref="ToBytes"/>.</param>
    /// <returns>The library.</returns>
    public static SchoolLibrary FromBytes(byte[] packed)
    {
        SchoolLibrary library = new();
        byte[] data = BoundedGzip.Decompress(packed, MaxBytes);
        if (data == null)
        {
            return library;
        }

        try
        {
            ZPackage package = new(data);
            if (package.ReadInt() != Version)
            {
                return library;
            }

            int writers = package.ReadInt();
            for (int i = 0; i < writers; i++)
            {
                string name = Clip(package.ReadString(), MaxNameLength);
                int day = package.ReadInt();
                if (i < 1000)
                {
                    library.Writers.Add(new Writer(name, day));
                }
            }

            int skills = package.ReadInt();
            for (int i = 0; i < skills; i++)
            {
                int id = package.ReadInt();
                float level = Mathf.Clamp(package.ReadSingle(), 0f, 100f);
                string teacher = Clip(package.ReadString(), MaxNameLength);
                if (library.Skills.Count < MaxSkills && !float.IsNaN(level))
                {
                    library.Skills[id] = new SkillRecord(level, teacher);
                }
            }

            int materials = package.ReadInt();
            for (int i = 0; i < materials; i++)
            {
                string material = package.ReadString();
                if (library.Materials.Count < MaxMaterials && material.Length <= MaxNameLength)
                {
                    library.Materials.Add(material);
                }
            }

            int stations = package.ReadInt();
            for (int i = 0; i < stations; i++)
            {
                string station = package.ReadString();
                int level = package.ReadInt();
                if (library.Stations.Count < MaxStations && station.Length <= MaxNameLength)
                {
                    library.Stations[station] = Mathf.Clamp(level, 1, 100);
                }
            }

            int texts = package.ReadInt();
            for (int i = 0; i < texts; i++)
            {
                string label = package.ReadString();
                string text = package.ReadString();
                if (library.Texts.Count < MaxTexts && label.Length > 0 && label.Length <= MaxNameLength && text.Length <= MaxTextLength)
                {
                    library.Texts[label] = text;
                }
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"School: a library could not be read and is taken as empty: {ex.Message}");
            return new SchoolLibrary();
        }

        return library;
    }

    /// <summary>
    /// The library packed for a ZDO or an RPC.
    /// </summary>
    /// <returns>The packed bytes.</returns>
    public byte[] ToBytes()
    {
        ZPackage package = new();
        package.Write(Version);
        package.Write(Writers.Count);
        foreach (Writer writer in Writers)
        {
            package.Write(writer.Name ?? string.Empty);
            package.Write(writer.Day);
        }

        package.Write(Skills.Count);
        foreach (KeyValuePair<int, SkillRecord> skill in Skills)
        {
            package.Write(skill.Key);
            package.Write(skill.Value.Level);
            package.Write(skill.Value.Teacher ?? string.Empty);
        }

        package.Write(Materials.Count);
        foreach (string material in Materials)
        {
            package.Write(material);
        }

        package.Write(Stations.Count);
        foreach (KeyValuePair<string, int> station in Stations)
        {
            package.Write(station.Key);
            package.Write(station.Value);
        }

        package.Write(Texts.Count);
        foreach (KeyValuePair<string, string> text in Texts)
        {
            package.Write(text.Key);
            package.Write(text.Value ?? string.Empty);
        }

        return Utils.Compress(package.GetArray());
    }

    /// <summary>
    /// Adds what another library holds: better skill levels replace worse ones, everything else is joined, and the
    /// writers are listed by when they last wrote.
    /// </summary>
    /// <param name="other">What is written now.</param>
    /// <param name="maxWriters">Most writers kept.</param>
    public void Merge(SchoolLibrary other, int maxWriters)
    {
        foreach (Writer writer in other.Writers)
        {
            Writers.RemoveAll(existing => existing.Name == writer.Name);
            Writers.Add(writer);
        }

        if (Writers.Count > maxWriters)
        {
            Writers.RemoveRange(0, Writers.Count - maxWriters);
        }

        foreach (KeyValuePair<int, SkillRecord> skill in other.Skills)
        {
            if (!Skills.TryGetValue(skill.Key, out SkillRecord existing) || skill.Value.Level > existing.Level)
            {
                Skills[skill.Key] = skill.Value;
            }
        }

        foreach (string material in other.Materials.Where(_ => Materials.Count < MaxMaterials))
        {
            Materials.Add(material);
        }

        foreach (KeyValuePair<string, int> station in other.Stations)
        {
            if ((!Stations.TryGetValue(station.Key, out int level) || station.Value > level) && (Stations.ContainsKey(station.Key) || Stations.Count < MaxStations))
            {
                Stations[station.Key] = station.Value;
            }
        }

        foreach (KeyValuePair<string, string> text in other.Texts)
        {
            if (Texts.Count < MaxTexts || Texts.ContainsKey(text.Key))
            {
                Texts[text.Key] = text.Value;
            }
        }
    }

    private static string Clip(string text, int length)
    {
        return text.Length <= length ? text : text.Substring(0, length);
    }
}
