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

    private const int Version = 2;
    private const int MaxPins = 4000;
    private const int MaxSkills = 512;
    private const int MaxMaterials = 8000;
    private const int MaxStations = 500;
    private const int MaxTexts = 2000;
    private const int MaxTextLength = 4000;
    private const int MaxNameLength = 200;

    // The pin types a bench shares: marks a player set and places they found, never deaths, beds or pings.
    private static readonly HashSet<int> sharedPinTypes = new()
    {
        (int)Minimap.PinType.Icon0, (int)Minimap.PinType.Icon1, (int)Minimap.PinType.Icon2, (int)Minimap.PinType.Icon3,
        (int)Minimap.PinType.Icon4, (int)Minimap.PinType.Boss, (int)Minimap.PinType.Hildir1, (int)Minimap.PinType.Hildir2,
        (int)Minimap.PinType.Hildir3
    };

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

    /// <summary>Map pins written in the bench, by the writer who set them.</summary>
    public readonly List<Pin> Pins = new();

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
    /// One map pin written in the bench.
    /// </summary>
    public readonly struct Pin
    {
        /// <summary>
        /// Creates a pin.
        /// </summary>
        /// <param name="type">The pin type, as its number.</param>
        /// <param name="name">The text on the pin.</param>
        /// <param name="position">Where it is in the world.</param>
        /// <param name="author">The writer who set it.</param>
        public Pin(int type, string name, Vector3 position, string author)
        {
            Type = type;
            Name = name;
            Position = position;
            Author = author;
        }

        /// <summary>The pin type, as its number.</summary>
        public int Type { get; }

        /// <summary>The text on the pin.</summary>
        public string Name { get; }

        /// <summary>Where it is in the world.</summary>
        public Vector3 Position { get; }

        /// <summary>The writer who set it.</summary>
        public string Author { get; }
    }

    /// <summary>
    /// Whether a bench shares pins of a type.
    /// </summary>
    /// <param name="type">The pin type, as its number.</param>
    /// <returns>True for marks and found places.</returns>
    public static bool IsSharedPinType(int type) => sharedPinTypes.Contains(type);

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

        if (Minimap.instance != null)
        {
            foreach (Minimap.PinData pin in Minimap.instance.m_pins)
            {
                if (pin.m_save && pin.m_ownerID == 0L && IsSharedPinType((int)pin.m_type) && library.Pins.Count < MaxPins)
                {
                    library.Pins.Add(new Pin((int)pin.m_type, Clip(pin.m_name ?? string.Empty, MaxNameLength), pin.m_pos, name));
                }
            }
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
            int version = package.ReadInt();
            if (version < 1 || version > Version)
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

            if (version >= 2)
            {
                int pins = package.ReadInt();
                for (int i = 0; i < pins; i++)
                {
                    int type = package.ReadInt();
                    string name = package.ReadString();
                    Vector3 position = package.ReadVector3();
                    string author = package.ReadString();
                    if (library.Pins.Count < MaxPins && IsSharedPinType(type) && name.Length <= MaxNameLength && author.Length <= MaxNameLength && IsInWorld(position))
                    {
                        library.Pins.Add(new Pin(type, name, position, author));
                    }
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

        package.Write(Pins.Count);
        foreach (Pin pin in Pins)
        {
            package.Write(pin.Type);
            package.Write(pin.Name ?? string.Empty);
            package.Write(pin.Position);
            package.Write(pin.Author ?? string.Empty);
        }

        return Utils.Compress(package.GetArray());
    }

    /// <summary>
    /// Adds what another library holds: better skill levels replace worse ones, everything else is joined, and the
    /// writers are listed by when they last wrote.
    /// </summary>
    /// <param name="other">What is written now.</param>
    /// <param name="maxWriters">Most writers kept.</param>
    /// <param name="maxPins">Most pins kept.</param>
    /// <param name="pinMergeDistance">A pin of the same type closer than this many metres counts as the same.</param>
    public void Merge(SchoolLibrary other, int maxWriters, int maxPins = MaxPins, float pinMergeDistance = 5f)
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

        // A writer's pins replace what they wrote before, so a pin they removed leaves the bench too.
        HashSet<string> rewriting = new(other.Writers.Select(writer => writer.Name), StringComparer.Ordinal);
        Pins.RemoveAll(pin => rewriting.Contains(pin.Author));
        foreach (Pin pin in other.Pins)
        {
            if (Pins.Count >= Mathf.Min(maxPins, MaxPins))
            {
                break;
            }

            if (!Pins.Any(existing => existing.Type == pin.Type && Utils.DistanceXZ(existing.Position, pin.Position) < pinMergeDistance))
            {
                Pins.Add(pin);
            }
        }
    }

    // Bytes may come from another player, so a position must be a real point inside the world.
    private static bool IsInWorld(Vector3 position)
    {
        return !float.IsNaN(position.x) && !float.IsInfinity(position.x) && !float.IsNaN(position.y) && !float.IsInfinity(position.y)
            && !float.IsNaN(position.z) && !float.IsInfinity(position.z) && Mathf.Abs(position.x) <= 20000f && Mathf.Abs(position.z) <= 20000f;
    }

    private static string Clip(string text, int length)
    {
        return text.Length <= length ? text : text.Substring(0, length);
    }
}
