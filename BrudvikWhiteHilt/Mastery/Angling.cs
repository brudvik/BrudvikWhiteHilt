using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Mastery;

/// <summary>
/// The Fishing milestones: the fight on the line, snags, legendary fish, double catches and the catch log.
/// </summary>
public static class Angling
{
    /// <summary>Custom data key on a legendary fish.</summary>
    public const string LegendaryKey = "whitehilt_legendary";

    private const string LogPrefix = "whitehilt_catch_";
    private const float EasePerSecond = 0.5f;
    private const float WarnAt = 0.5f;
    private const float PendingSeconds = 5f;
    private const int LegendaryLevel = 5;

    private static readonly Dictionary<FishingFloat, float> tension = new();

    private static readonly (string Prefab, int Min, int Max, float Weight)[] snags =
    {
        ("Coins", 5, 20, 30f), ("Amber", 1, 1, 25f), ("LeatherScraps", 2, 4, 20f), ("AmberPearl", 1, 1, 15f), ("Resin", 2, 4, 10f)
    };

    private static GameObject pendingLegendary;
    private static float pendingUntil;

    /// <summary>
    /// Registers the English text. Call from the plugin's Awake.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("msg_whitehilt_fishing_strain", "The line strains ({0}%), ease off!");
        Translations.AddEnglish("msg_whitehilt_fishing_legendary", "A legendary catch!");
        Translations.AddEnglish("msg_whitehilt_fishing_snag", "The hook also brought up");
        Translations.AddEnglish("msg_whitehilt_fishing_double", "Two on one hook!");
        Translations.AddEnglish("whitehilt_legendary", "Legendary");
        Translations.AddEnglish("whitehilt_catch_log", "Catch log");
        Translations.AddEnglish("whitehilt_catch_log_entry", "{0}: {1} caught, largest level {2}");
        Translations.AddEnglish("whitehilt_catch_log_legendary", "{0} legendary");
    }

    /// <summary>
    /// The fight on the line: reeling while the fish runs strains the line until it snaps. Runs on the fisher's machine.
    /// </summary>
    /// <param name="fishingFloat">The float, owned here.</param>
    /// <returns>False if the line snapped and vanilla should not run.</returns>
    public static bool UpdateFight(FishingFloat fishingFloat)
    {
        if (!MasterySettings.Fishing.Value || fishingFloat.m_nview == null || !fishingFloat.m_nview.IsValid() || !fishingFloat.m_nview.IsOwner())
        {
            return true;
        }

        Fish fish = fishingFloat.GetCatch();
        Character owner = fishingFloat.GetOwner();
        tension.TryGetValue(fishingFloat, out float strain);
        float dt = Time.fixedDeltaTime;
        if (fish != null && owner is Player player && owner.IsBlocking() && fish.IsEscaping())
        {
            float level = player.GetSkillLevel(Skills.SkillType.Fishing);
            float rate = MasterySettings.LineStrain.Value * (1f - 0.4f * Perks.Factor(level))
                * (Perks.SteadyHands.ReachedAt(level) ? 1f - MasterySettings.SteadyHandsReduction.Value : 1f);
            strain += rate * dt;
            if (strain >= WarnAt)
            {
                fishingFloat.Message(Perks.Text("$msg_whitehilt_fishing_strain", Mathf.RoundToInt(strain * 100f).ToString()), prioritized: true);
            }
        }
        else
        {
            strain = Mathf.Max(0f, strain - EasePerSecond * dt);
        }

        if (fish == null || strain < 1f)
        {
            if (tension.Count > 50)
            {
                foreach (FishingFloat gone in tension.Keys.Where(known => known == null).ToList())
                {
                    tension.Remove(gone);
                }
            }

            tension[fishingFloat] = strain;
            return true;
        }

        tension.Remove(fishingFloat);
        fishingFloat.Message("$msg_fishing_linebroke", prioritized: true);
        fish.OnHooked(null);
        fishingFloat.m_lineBreakEffect.Create(fishingFloat.transform.position, Quaternion.identity);
        fishingFloat.m_nview.Destroy();
        return false;
    }

    /// <summary>
    /// Rolls whether a fish being caught is legendary. The fish gets its size when the player picks it up.
    /// </summary>
    /// <param name="fish">The fish.</param>
    /// <param name="owner">The fisher.</param>
    public static void BeforeCatch(Fish fish, Character owner)
    {
        if (owner is not Player player || player != Player.m_localPlayer || fish == null || fish.GetComponent<ItemDrop>() == null)
        {
            return;
        }

        if (Random.value < Perks.LegendaryChance(player.GetSkillLevel(Skills.SkillType.Fishing)))
        {
            pendingLegendary = fish.gameObject;
            pendingUntil = Time.time + PendingSeconds;
        }
    }

    /// <summary>
    /// Makes a fish legendary as it is picked up, if it was rolled so.
    /// </summary>
    /// <param name="picked">The object picked up.</param>
    public static void OnPickup(GameObject picked)
    {
        if (picked == null || picked != pendingLegendary || Time.time > pendingUntil)
        {
            return;
        }

        pendingLegendary = null;
        ItemDrop drop = picked.GetComponent<ItemDrop>();
        if (drop == null)
        {
            return;
        }

        drop.m_itemData.m_quality = Mathf.Max(drop.m_itemData.m_quality, LegendaryLevel, drop.m_itemData.m_shared.m_maxQuality);
        drop.m_itemData.m_customData[LegendaryKey] = "1";
        Player.m_localPlayer?.Message(MessageHud.MessageType.Center, "$msg_whitehilt_fishing_legendary");
    }

    /// <summary>
    /// After a catch: snags, a second fish, and the catch log.
    /// </summary>
    /// <param name="fish">The fish caught.</param>
    /// <param name="owner">The fisher.</param>
    public static void AfterCatch(Fish fish, Character owner)
    {
        if (owner is not Player player || player != Player.m_localPlayer || fish == null || !MasterySettings.Fishing.Value)
        {
            return;
        }

        float level = player.GetSkillLevel(Skills.SkillType.Fishing);
        GameObject prefab = ZNetScene.instance?.GetPrefab(Utils.GetPrefabName(fish.gameObject));
        ItemDrop drop = fish.GetComponent<ItemDrop>();
        int quality = drop != null ? drop.m_itemData.m_quality : 1;
        bool legendary = drop != null && drop.m_itemData.m_customData.ContainsKey(LegendaryKey);
        Log(player, prefab != null ? prefab.name : Utils.GetPrefabName(fish.gameObject), quality, legendary);

        if (Random.value < Perks.SnagChance(level))
        {
            Snag(player);
        }

        if (prefab != null && Perks.DoubleCatch.ReachedAt(level) && Random.value < MasterySettings.DoubleCatchChance.Value && player.GetInventory().AddItem(prefab, 1))
        {
            player.Message(MessageHud.MessageType.TopLeft, "$msg_whitehilt_fishing_double");
        }
    }

    /// <summary>
    /// The catch log as lines for the skill book.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>Localized lines.</returns>
    public static IEnumerable<string> LogLines(Player player)
    {
        foreach (KeyValuePair<string, string> entry in player.m_customData.Where(entry => entry.Key.StartsWith(LogPrefix)).OrderBy(entry => entry.Key))
        {
            string[] parts = entry.Value.Split(';');
            GameObject prefab = ObjectDB.instance?.GetItemPrefab(entry.Key.Substring(LogPrefix.Length));
            string name = prefab != null && prefab.GetComponent<ItemDrop>() is ItemDrop drop ? drop.m_itemData.m_shared.m_name : entry.Key.Substring(LogPrefix.Length);
            string line = Perks.Text("$whitehilt_catch_log_entry", Localization.instance.Localize(name), Part(parts, 0), Part(parts, 1));
            if (Part(parts, 2) != "0")
            {
                line += ", " + Perks.Text("$whitehilt_catch_log_legendary", Part(parts, 2));
            }

            yield return line;
        }
    }

    private static void Log(Player player, string prefabName, int quality, bool legendary)
    {
        string key = LogPrefix + prefabName;
        string[] parts = player.m_customData.TryGetValue(key, out string text) ? text.Split(';') : new string[0];
        int count = int.TryParse(Part(parts, 0), out int c) ? c : 0;
        int best = int.TryParse(Part(parts, 1), out int b) ? b : 0;
        int legendaries = int.TryParse(Part(parts, 2), out int l) ? l : 0;
        player.m_customData[key] = $"{count + 1};{Mathf.Max(best, quality)};{legendaries + (legendary ? 1 : 0)}";
    }

    private static string Part(string[] parts, int index)
    {
        return parts.Length > index ? parts[index] : "0";
    }

    private static void Snag(Player player)
    {
        float roll = Random.value * snags.Sum(entry => entry.Weight);
        foreach ((string prefabName, int min, int max, float weight) in snags)
        {
            roll -= weight;
            if (roll > 0f)
            {
                continue;
            }

            GameObject prefab = ZNetScene.instance?.GetPrefab(prefabName);
            int amount = Random.Range(min, max + 1);
            if (prefab != null && player.GetInventory().AddItem(prefab, amount))
            {
                string name = prefab.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_name ?? prefabName;
                player.Message(MessageHud.MessageType.TopLeft, $"$msg_whitehilt_fishing_snag {name} x{amount}", 0, prefab.GetComponent<ItemDrop>()?.m_itemData.GetIcon());
            }

            return;
        }
    }
}
