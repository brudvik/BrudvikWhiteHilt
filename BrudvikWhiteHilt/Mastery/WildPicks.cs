using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Mastery;

/// <summary>
/// The Foraging skill at work: which plants are wild picks, when they are at their best, and sweep picking.
/// </summary>
public static class WildPicks
{
    /// <summary>How far, in metres, sweep picking reaches.</summary>
    public const float SweepRange = 4f;

    // Herbs that are not eaten but still count as wild picks.
    private static readonly HashSet<string> herbs = new() { "Thistle", "Dandelion", "Fiddleheadfern", "SmokePuff", "Flax", "Barley" };

    private static readonly HashSet<Pickable> pickables = new();
    private static bool sweeping;

    /// <summary>
    /// Registers the English text. Call from the plugin's Awake.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("whitehilt_best_time", "At its best now");
    }

    /// <summary>
    /// Remembers a pickable for sweep picking.
    /// </summary>
    /// <param name="pickable">A pickable that just woke up.</param>
    public static void Register(Pickable pickable)
    {
        if (pickables.Count > 2000)
        {
            pickables.RemoveWhere(known => known == null);
        }

        pickables.Add(pickable);
    }

    /// <summary>
    /// True for a wild plant: something to eat or a herb, not a crop and not debris such as stones and branches.
    /// </summary>
    /// <param name="pickable">The pickable.</param>
    /// <returns>True for a wild pick.</returns>
    public static bool IsWild(Pickable pickable)
    {
        if (pickable == null || pickable.m_itemPrefab == null || pickable.m_pickRaiseSkill == Skills.SkillType.Farming)
        {
            return false;
        }

        ItemDrop drop = pickable.m_itemPrefab.GetComponent<ItemDrop>();
        ItemDrop.ItemData.SharedData shared = drop != null ? drop.m_itemData.m_shared : null;
        bool edible = shared != null && shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable && (shared.m_food > 0f || shared.m_foodStamina > 0f);
        return edible || herbs.Contains(pickable.m_itemPrefab.name);
    }

    /// <summary>
    /// True if the plant is at its best now: mushrooms at dawn and in the rain, everything else in the afternoon sun.
    /// </summary>
    /// <param name="pickable">The pickable.</param>
    /// <returns>True at the best time.</returns>
    public static bool IsBestTime(Pickable pickable)
    {
        if (EnvMan.instance == null || pickable?.m_itemPrefab == null)
        {
            return false;
        }

        float day = EnvMan.instance.GetDayFraction();
        bool wet = EnvMan.IsWet();
        string name = pickable.m_itemPrefab.name;
        bool mushroom = name.Contains("Mushroom") || name == "Chanterelle" || name == "Porcini";
        return mushroom ? wet || (day > 0.2f && day < 0.4f) : !wet && day > 0.5f && day < 0.75f;
    }

    /// <summary>
    /// Picks the same plants around a picked one, for players with Sweep Picking.
    /// </summary>
    /// <param name="picked">The plant just picked.</param>
    /// <param name="player">The local player.</param>
    public static void Sweep(Pickable picked, Player player)
    {
        if (sweeping || !Perks.SweepPicking.Has(player))
        {
            return;
        }

        string name = Utils.GetPrefabName(picked.gameObject);
        Vector3 center = picked.transform.position;
        List<Pickable> near = pickables
            .Where(other => other != null && other != picked && !other.GetPicked() && other.CanBePicked()
                && Vector3.Distance(other.transform.position, center) <= SweepRange && Utils.GetPrefabName(other.gameObject) == name)
            .ToList();

        sweeping = true;
        try
        {
            foreach (Pickable other in near)
            {
                other.Interact(player, repeat: false, alt: false);
            }
        }
        finally
        {
            sweeping = false;
        }
    }
}
