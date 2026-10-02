using BrudvikWhiteHilt.Difficulty.Beasts;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Treasure;

/// <summary>
/// Fills a dug-up treasure chest: black beast trophies of beasts whose boss has been defeated, and draws from the loot
/// list in the settings.
/// </summary>
public static class TreasureLoot
{
    /// <summary>The loot list a new config file starts with.</summary>
    public const string DefaultLoot =
        "WhiteHiltCranberryMead:1-2:10, WhiteHiltLingonberryMead:1-2:10, WhiteHiltRoserootMead:1-2:8, WhiteHiltCrowberryWine:1-2:8, " +
        "WhiteHiltSweetGaleAle:1-2:8, WhiteHiltSmokedFish:2-4:8, WhiteHiltSmokedWolfJerky:2-4:8, WhiteHiltSweetGaleSausages:2-4:8, " +
        "WhiteHiltArrows:20-40:10, WhiteHiltBolts:10-20:6, GiftOfFreya:1-1:4, GiftOfThor:1-1:4, GiftOfOdin:1-1:3, GiftOfNjord:1-1:4, " +
        "GiftOfSkadi:1-1:4, WhiteHiltBronzeRune:1-1:3, WhiteHiltIronRune:1-1:2, WhiteHiltLingonberries:5-10:6, WhiteHiltCrowberries:5-10:5, " +
        "WhiteHiltRoseroot:3-6:5, Coins:50-150:8";

    /// <summary>
    /// Puts the treasure into a chest's inventory.
    /// </summary>
    /// <param name="inventory">The chest's inventory.</param>
    public static void Fill(Inventory inventory)
    {
        List<GameObject> trophies = BeastDefinition.All
            .Where(beast => ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(beast.BossKey))
            .Select(beast => ObjectDB.instance.GetItemPrefab(beast.TrophyName))
            .Where(prefab => prefab != null)
            .ToList();
        for (int i = 0; i < TreasureSettings.TrophyCount.Value && trophies.Count > 0; i++)
        {
            inventory.AddItem(trophies[Random.Range(0, trophies.Count)], 1);
        }

        List<Entry> entries = Parse(TreasureSettings.Loot.Value);
        float total = entries.Sum(entry => entry.Weight);
        for (int i = 0; i < TreasureSettings.LootRolls.Value && total > 0f; i++)
        {
            float pick = Random.Range(0f, total);
            Entry chosen = entries.Last();
            foreach (Entry entry in entries)
            {
                pick -= entry.Weight;
                if (pick <= 0f)
                {
                    chosen = entry;
                    break;
                }
            }

            inventory.AddItem(chosen.Prefab, Random.Range(chosen.Min, chosen.Max + 1));
        }
    }

    private static List<Entry> Parse(string text)
    {
        List<Entry> entries = new();
        foreach (string part in (text ?? string.Empty).Split(','))
        {
            string[] fields = part.Trim().Split(':');
            if (fields.Length == 0 || fields[0].Length == 0)
            {
                continue;
            }

            GameObject prefab = ObjectDB.instance.GetItemPrefab(fields[0]);
            if (prefab == null)
            {
                Jotunn.Logger.LogWarning($"Treasure loot: no item called {fields[0]}, skipped");
                continue;
            }

            int min = 1;
            int max = 1;
            float weight = 1f;
            if (fields.Length > 1)
            {
                string[] range = fields[1].Split('-');
                int.TryParse(range[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out min);
                max = min;
                if (range.Length > 1)
                {
                    int.TryParse(range[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out max);
                }
            }

            if (fields.Length > 2)
            {
                float.TryParse(fields[2], NumberStyles.Float, CultureInfo.InvariantCulture, out weight);
            }

            min = Mathf.Max(1, min);
            entries.Add(new Entry(prefab, min, Mathf.Max(min, max), Mathf.Max(0f, weight)));
        }

        return entries;
    }

    private readonly struct Entry
    {
        public Entry(GameObject prefab, int min, int max, float weight)
        {
            Prefab = prefab;
            Min = min;
            Max = max;
            Weight = weight;
        }

        public GameObject Prefab { get; }

        public int Min { get; }

        public int Max { get; }

        public float Weight { get; }
    }
}
