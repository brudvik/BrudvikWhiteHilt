#nullable enable annotations

using BepInEx;
using BepInEx.Configuration;
using BrudvikWhiteHilt.Chests.Constants;
using BrudvikWhiteHilt.Progression;
using System;
using System.Collections.Generic;
using System.IO;

namespace BrudvikWhiteHilt.Chests.Configuration
{
    /// <summary>
    /// Config for the chests, section "Chests" and "Chests.&lt;Category&gt;". Chest contents are server-synced so every
    /// player sees the same chests. On the first start the values of the former BrudvikStackedChest config are taken over.
    /// </summary>
    public class ChestSettings
    {
        private const string Section = "Chests";
        private const string OldConfigFile = "com.jotunn.BrudvikStackedChest.cfg";

        // Items the automatic sorting cannot place correctly on its own.
        private static readonly Dictionary<ChestCategory, string> DefaultIncludes = new()
        {
            { ChestCategory.Wood, "Root" },
            { ChestCategory.Stone, "Flint" },
            { ChestCategory.Seed, "AncientSeed" },
            { ChestCategory.Animal, "Chitin" },
            { ChestCategory.Material, "Softtissue" },
            { ChestCategory.Treasure, "DragonEgg" }
        };

        private readonly Dictionary<ChestCategory, ConfigEntry<string>> includes = new();
        private readonly Dictionary<ChestCategory, ConfigEntry<string>> excludes = new();

        // Where each entry lived in the old mod's config file.
        private readonly List<(string Section, string Key, ConfigEntryBase Entry)> oldLocations = new();

        /// <summary>
        /// When enabled, the generated item list for every chest is written to the log.
        /// </summary>
        public ConfigEntry<bool> DumpItemLists { get; }

        /// <summary>
        /// Decides which items the chests supply without limit.
        /// </summary>
        public ConfigEntry<ChestMode> Mode { get; }

        /// <summary>
        /// In <see cref="ChestMode.Linear"/> mode, the number of full stacks a chest must hold to unlock an item.
        /// </summary>
        public ConfigEntry<int> UnlockStacks { get; }

        /// <summary>
        /// When enabled, chests keep their contents sorted from the top left, unlimited items first.
        /// </summary>
        public ConfigEntry<bool> SortContents { get; }

        /// <summary>
        /// When enabled, a Learn all button teaches the player the items in a chest they have not learned yet.
        /// </summary>
        public ConfigEntry<bool> LearnAll { get; }

        /// <summary>
        /// When enabled, the Learn all button also adds trophies to the player's trophy list.
        /// </summary>
        public ConfigEntry<bool> LearnTrophies { get; }

        /// <summary>
        /// When enabled, carts and ship holds keep the stacks of unlimited items full.
        /// </summary>
        public ConfigEntry<bool> UnlimitedCargo { get; }

        /// <summary>
        /// When enabled, the front of a chest shows whether it is empty and how full it is.
        /// </summary>
        public ConfigEntry<bool> ShowIndicators { get; }

        /// <summary>
        /// When enabled, the contents of the chest under the crosshair are shown as item icons.
        /// </summary>
        public ConfigEntry<bool> ShowHoverPanel { get; }

        /// <summary>
        /// How far away the chests for an item pointed at in the inventory light up; 0 for none.
        /// </summary>
        public ConfigEntry<float> FindRange { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChestSettings"/> class and binds all entries.
        /// <see cref="WhiteHiltConfig.Initialize"/> must have run first.
        /// </summary>
        public ChestSettings()
        {
            // Must be read before the first Bind of the section writes it to the file.
            var firstStart = !WhiteHiltConfig.IsStored(Section, "Mode");

            DumpItemLists = Remember("General", WhiteHiltConfig.BindLocal(Section, "DumpItemLists", false,
                "Write the automatically generated item list for every chest to the BepInEx log."));

            // Linear is how a new player meets the chests; a config file that already holds a mode keeps it.
            Mode = Remember("General", WhiteHiltConfig.BindAdminOnly(Section, "Mode", ChestMode.Linear,
                "Linear (default): store a full stack of an item in a chest to make it unlimited for the whole world. " +
                "Full: every item is always available. " +
                "Discovered: an item is unlimited once any player in the world has discovered it. " +
                "In Linear and Discovered, items that do not stack are never duplicated. " +
                "Switching from a more generous mode removes the items the new mode does not supply."));

            UnlockStacks = Remember("General", WhiteHiltConfig.BindAdminOnly(Section, "UnlockStacks", 1,
                "Linear mode: the number of full stacks of an item a chest must hold before the item is unlocked.",
                new AcceptableValueRange<int>(1, 10)));

            SortContents = Remember("General", WhiteHiltConfig.BindAdminOnly(Section, "SortContents", true,
                "Keep the chest contents sorted from the top left: unlimited items first, then items stored by players, " +
                "each by item type and name. Turn off to arrange the chests yourself; new items are then placed in the " +
                "first free slot from the top."));

            LearnAll = Remember("General", WhiteHiltConfig.BindAdminOnly(Section, "LearnAll", true,
                "Show a Learn all button on chests that hold items the player has not learned yet. Learning an item " +
                "unlocks the recipes and build pieces that need it, as if the player had picked it up."));

            LearnTrophies = Remember("General", WhiteHiltConfig.BindAdminOnly(Section, "LearnTrophies", true,
                "Let the Learn all button learn trophies as well, which adds them to the player's trophy list."));

            UnlimitedCargo = WhiteHiltConfig.BindAdminOnly(Section, "UnlimitedCargo", true,
                "Carts and ship holds keep the stacks of unlimited items full, like the Everlasting Chest: bring some of an " +
                "item that is unlimited and it never runs out while you build or craft from the cargo. Cargo never unlocks " +
                "items and never adds items you did not bring.");

            ShowIndicators = Remember("Display", WhiteHiltConfig.BindLocal(Section, "ShowIndicators", true,
                "Grey out the icon on the front of empty chests and show bars under it: how many slots are used, and " +
                "in the Linear and Discovered modes how many of the chest's items are unlimited."));

            ShowHoverPanel = Remember("Display", WhiteHiltConfig.BindLocal(Section, "ShowHoverPanel", true,
                "Show the contents of the chest you look at as item icons below the crosshair."));

            FindRange = WhiteHiltConfig.BindLocal(Section, "FindRange", 30f,
                "With the inventory open, the chests and wall drawers an item belongs in light up while you point at it in " +
                "your own inventory, within this many metres; 0 for none. The item's tooltip names the chest either way.",
                new AcceptableValueRange<float>(0f, 100f));

            foreach (ChestCategory category in Enum.GetValues(typeof(ChestCategory)))
            {
                if (category == ChestCategory.None) continue;

                var section = $"{Section}.{category}";
                var oldSection = $"Chest.{category}";
                DefaultIncludes.TryGetValue(category, out var defaultInclude);
                WhiteHiltConfig.SetSectionLabel(section, $"$bsc_chest_{category.ToString().ToLowerInvariant()}");

                includes[category] = Remember(oldSection, WhiteHiltConfig.BindAdminOnly(section, "Include", defaultInclude ?? string.Empty,
                    "Comma-separated item prefab names that are always placed in this chest, overriding the automatic sorting."));
                excludes[category] = Remember(oldSection, WhiteHiltConfig.BindAdminOnly(section, "Exclude", string.Empty,
                    "Comma-separated item prefab names that are never placed in this chest."));
            }

            if (firstStart) ImportOldConfig();
        }

        /// <summary>
        /// Gets the prefab names that are forced into the chest of the given category.
        /// </summary>
        /// <param name="category">The chest category.</param>
        /// <returns>The configured prefab names.</returns>
        public IEnumerable<string> GetIncluded(ChestCategory category) => Split(includes, category);

        /// <summary>
        /// Gets the prefab names that must never appear in the chest of the given category.
        /// </summary>
        /// <param name="category">The chest category.</param>
        /// <returns>The configured prefab names.</returns>
        public HashSet<string> GetExcluded(ChestCategory category) => new(Split(excludes, category));

        private ConfigEntry<T> Remember<T>(string oldSection, ConfigEntry<T> entry)
        {
            oldLocations.Add((oldSection, entry.Definition.Key, entry));
            return entry;
        }

        /// <summary>
        /// Takes over the values of the former mod's config file. The mode matters most: a chest restocked in another
        /// mode than before removes the stacks the new mode does not supply.
        /// </summary>
        private void ImportOldConfig()
        {
            var path = Path.Combine(Paths.ConfigPath, OldConfigFile);
            if (!File.Exists(path))
            {
                Jotunn.Logger.LogInfo($"Chests: no {OldConfigFile} to take over, the chest settings start at their defaults (Mode = {Mode.Value}).");
                return;
            }

            try
            {
                var values = ReadConfigFile(path);
                foreach (var (oldSection, key, entry) in oldLocations)
                {
                    if (!values.TryGetValue((oldSection, key), out var value)) continue;

                    entry.SetSerializedValue(value);
                    Jotunn.Logger.LogInfo($"Chests: took over [{oldSection}] {key} = {entry.GetSerializedValue()} from {OldConfigFile}.");
                }
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogError($"Chests: could not take over {OldConfigFile}. Check [{Section}] Mode before opening any chest!");
                Jotunn.Logger.LogError(ex);
            }
        }

        private static Dictionary<(string Section, string Key), string> ReadConfigFile(string path)
        {
            var values = new Dictionary<(string Section, string Key), string>();
            var section = string.Empty;
            foreach (var raw in File.ReadAllLines(path))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    section = line.Substring(1, line.Length - 2).Trim();
                    continue;
                }

                var equals = line.IndexOf('=');
                if (equals > 0) values[(section, line.Substring(0, equals).Trim())] = line.Substring(equals + 1).Trim();
            }
            return values;
        }

        private static IEnumerable<string> Split(Dictionary<ChestCategory, ConfigEntry<string>> entries, ChestCategory category)
        {
            if (!entries.TryGetValue(category, out var entry) || string.IsNullOrWhiteSpace(entry.Value)) yield break;

            foreach (var part in entry.Value.Split(','))
            {
                var name = part.Trim();
                if (name.Length > 0) yield return name;
            }
        }
    }
}
