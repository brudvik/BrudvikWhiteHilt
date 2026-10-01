#nullable enable annotations

using BepInEx;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace BrudvikWhiteHilt.Chests
{
    /// <summary>
    /// Counts the contents of every placed chest straight from the saved world data and compares it with the previous
    /// count, so nothing can disappear unnoticed when the chests move from BrudvikStackedChest into White Hilt. Needs
    /// neither the chest prefabs nor the chest module, so it also counts while the old mod is still installed.
    /// </summary>
    public static class ChestCensus
    {
        // Must match the chest names in ChestModule; they are the saved prefab names of every placed chest.
        private static readonly string[] PrefabNames =
        {
            "BSWoodChest", "BSStoneChest", "BSMetalChest", "BSFoodChest", "BSMaterialChest", "BSAnimalChest", "BSSeedChest",
            "BSTrophyChest", "BSTreasureChest", "BSToolsChest", "BSArmorChest", "BSWeaponChest", "BSPotionChest", "BSEmptyChest"
        };

        private const int MaxReportedLines = 30;

        /// <summary>
        /// Registers the console command <c>whitehilt_chest_census</c>.
        /// </summary>
        public static void RegisterCommand()
        {
            CommandManager.Instance.AddConsoleCommand(new CensusCommand());
        }

        /// <summary>
        /// Takes a census when a world starts on the server or the host.
        /// </summary>
        public static void OnWorldStarted()
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer()) return;

            Run(null);
        }

        /// <summary>
        /// Counts every chest, writes the count to a file and reports what has decreased since the previous count.
        /// </summary>
        /// <param name="context">The console to write to, or null for the log only.</param>
        public static void Run(Terminal? context)
        {
            try
            {
                if (ZDOMan.instance == null || ZNet.World == null) return;

                var current = Count();
                var folder = Path.Combine(Paths.ConfigPath, "BrudvikStackedChest", "census");
                var world = string.Concat(ZNet.World.m_name.Split(Path.GetInvalidFileNameChars()));
                var prefix = $"{world}_{ZNet.World.m_uid}_";
                Directory.CreateDirectory(folder);

                var previousPath = Directory.GetFiles(folder, prefix + "*.tsv").OrderBy(path => path, StringComparer.Ordinal).LastOrDefault();
                var path = Path.Combine(folder, prefix + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".tsv");
                File.WriteAllLines(path, Write(current));

                var stacks = current.Values.Sum(chest => chest.Items.Count);
                var items = current.Values.Sum(chest => chest.Items.Values.Sum());
                Report(context, $"Chest census: {current.Count} chests, {stacks} item kinds, {items} items, saved to {path}", false);
                foreach (var chest in current.Values.Where(chest => chest.Unreadable))
                {
                    Report(context, $"Chest census: could not read the contents of {chest.Prefab} {chest.Id}", true);
                }

                if (previousPath != null) Compare(context, Read(previousPath), current, Path.GetFileName(previousPath));
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogError("Chest census failed!");
                Jotunn.Logger.LogError(ex);
            }
        }

        private static Dictionary<string, ChestRecord> Count()
        {
            var hashes = new Dictionary<int, string>();
            foreach (var name in PrefabNames)
            {
                hashes[name.GetStableHashCode()] = name;
            }

            var chests = new Dictionary<string, ChestRecord>(StringComparer.Ordinal);
            foreach (var zdo in ZDOMan.instance.m_objectsByID.Values)
            {
                if (!hashes.TryGetValue(zdo.GetPrefab(), out var prefab)) continue;

                var chest = new ChestRecord($"{zdo.m_uid.UserID}:{zdo.m_uid.ID}", prefab);
                chest.Unreadable = !ReadItems(zdo.GetByteArray(ZDOVars.s_items), chest.Items);
                chests[chest.Id] = chest;
            }
            return chests;
        }

        // Same format as Container.Save / Inventory.Load; only reads, so it is safe on any ZDO.
        private static bool ReadItems(byte[]? data, Dictionary<string, int> items)
        {
            if (data == null || data.Length == 0) return true;

            try
            {
                var package = new ZPackage(data);
                var version = (global::Version.Item)package.ReadInt();
                if (version < global::Version.Item.Smaller) return false;

                int count = package.ReadUShort();
                for (var i = 0; i < count; i++)
                {
                    var (hash, item) = ItemDrop.ItemData.Load(package, version);
                    if (hash == 0) continue;

                    var key = $"{ItemName(hash)}\t{item.m_quality}";
                    items.TryGetValue(key, out var amount);
                    items[key] = amount + item.m_stack;
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string ItemName(int hash)
        {
            var prefab = ObjectDB.instance == null ? null : ObjectDB.instance.GetItemPrefab(hash);
            return prefab == null ? $"#{hash}" : prefab.name;
        }

        private static IEnumerable<string> Write(Dictionary<string, ChestRecord> chests)
        {
            yield return "chest\tprefab\titem\tquality\tamount";
            foreach (var chest in chests.Values.OrderBy(chest => chest.Id, StringComparer.Ordinal))
            {
                // An empty chest still gets a line, so a vanished chest shows up in the comparison.
                if (chest.Items.Count == 0) yield return $"{chest.Id}\t{chest.Prefab}\t-\t0\t0";

                foreach (var pair in chest.Items.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                {
                    yield return $"{chest.Id}\t{chest.Prefab}\t{pair.Key}\t{pair.Value}";
                }
            }
        }

        private static Dictionary<string, ChestRecord> Read(string path)
        {
            var chests = new Dictionary<string, ChestRecord>(StringComparer.Ordinal);
            foreach (var line in File.ReadAllLines(path).Skip(1))
            {
                var parts = line.Split('\t');
                if (parts.Length != 5) continue;

                if (!chests.TryGetValue(parts[0], out var chest)) chests[parts[0]] = chest = new ChestRecord(parts[0], parts[1]);
                if (parts[2] == "-") continue;

                chest.Items[$"{parts[2]}\t{parts[3]}"] = int.Parse(parts[4], CultureInfo.InvariantCulture);
            }
            return chests;
        }

        private static void Compare(Terminal? context, Dictionary<string, ChestRecord> before, Dictionary<string, ChestRecord> after, string beforeName)
        {
            var losses = new List<string>();
            foreach (var old in before.Values)
            {
                if (!after.TryGetValue(old.Id, out var now))
                {
                    losses.Add($"{old.Prefab} {old.Id} is gone ({old.Items.Values.Sum()} items)");
                    continue;
                }

                foreach (var pair in old.Items)
                {
                    now.Items.TryGetValue(pair.Key, out var amount);
                    if (amount < pair.Value) losses.Add($"{old.Prefab} {old.Id}: {pair.Key.Replace('\t', ' ')} {pair.Value} -> {amount}");
                }
            }

            if (losses.Count == 0)
            {
                Report(context, $"Chest census: nothing has decreased since {beforeName}", false);
                return;
            }

            // Players take items out and Linear mode drops surplus unlimited stacks, so a decrease is not always a loss.
            Report(context, $"Chest census: {losses.Count} decreases since {beforeName} (taking items out also counts):", true);
            foreach (var line in losses.Take(MaxReportedLines))
            {
                Report(context, "  " + line, true);
            }
        }

        private static void Report(Terminal? context, string text, bool warning)
        {
            if (warning) Jotunn.Logger.LogWarning(text);
            else Jotunn.Logger.LogInfo(text);
            context?.AddString(text);
        }

        private sealed class ChestRecord
        {
            public ChestRecord(string id, string prefab)
            {
                Id = id;
                Prefab = prefab;
            }

            public string Id { get; }

            public string Prefab { get; }

            public Dictionary<string, int> Items { get; } = new(StringComparer.Ordinal);

            public bool Unreadable { get; set; }
        }

        private sealed class CensusCommand : ConsoleCommand
        {
            public override string Name => "whitehilt_chest_census";

            public override string Help => "Counts the contents of every chest and compares them with the previous count (server or host only).";

            public override void Run(string[] args)
            {
                Run(args, Console.instance);
            }

            public override void Run(string[] args, Terminal context)
            {
                if (ZNet.instance == null || !ZNet.instance.IsServer())
                {
                    context?.AddString("Only the server sees every chest. It takes a census at every world start, see BepInEx/config/BrudvikStackedChest/census there.");
                    return;
                }

                ChestCensus.Run(context);
            }
        }
    }
}
