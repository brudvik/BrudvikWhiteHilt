using BrudvikWhiteHilt.Helpers;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Chanterelle = BrudvikWhiteHilt.Items.Foraging.Chanterelle.Chanterelle;
using Crowberries = BrudvikWhiteHilt.Items.Foraging.Crowberries.Crowberries;
using Lingonberries = BrudvikWhiteHilt.Items.Foraging.Lingonberries.Lingonberries;
using Porcini = BrudvikWhiteHilt.Items.Foraging.Porcini.Porcini;

namespace BrudvikWhiteHilt.Ranching;

/// <summary>
/// Each tameable animal has favourite foods. They tame it faster and keep it fed longer, and it picks them first from a trough.
/// White Hilt forageables among them are added to the animal's diet.
/// </summary>
public static class FavoriteFoods
{
    /// <summary>
    /// How much faster an animal fed its favourite food tames.
    /// </summary>
    public const float TamingSpeed = 1.5f;

    /// <summary>
    /// How much longer an animal fed its favourite food stays fed.
    /// </summary>
    public const float FedDuration = 2f;

    private static readonly Dictionary<string, string[]> favoritesByCreature = new()
    {
        ["Boar"] = new[] { Chanterelle.PrefabName, Porcini.PrefabName },
        ["Wolf"] = new[] { "Sausages" },
        ["Lox"] = new[] { Crowberries.PrefabName },
        ["Hen"] = new[] { Lingonberries.PrefabName },
        ["Asksvin"] = new[] { "MushroomSmokePuff" }
    };

    // Shared item names ($item_...) per creature prefab, filled when the prefabs are registered.
    private static readonly Dictionary<string, HashSet<string>> sharedNamesByCreature = new();

    /// <summary>
    /// Registers the English text. Call from the plugin's Awake.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("whitehilt_favorite_food", "Favourite food");
    }

    /// <summary>
    /// Adds the favourite foods to the animals' diets and resolves their names. Runs for every ZNetScene and is idempotent.
    /// </summary>
    public static void AddToDiets()
    {
        foreach (KeyValuePair<string, string[]> entry in favoritesByCreature)
        {
            MonsterAI ai = PrefabManager.Instance.GetPrefab(entry.Key)?.GetComponent<MonsterAI>();
            if (ai == null)
            {
                Jotunn.Logger.LogWarning($"Favourite foods: creature {entry.Key} not found");
                continue;
            }

            ai.m_consumeItems ??= new List<ItemDrop>();
            HashSet<string> sharedNames = new();
            foreach (string itemName in entry.Value)
            {
                ItemDrop item = PrefabManager.Instance.GetPrefab(itemName)?.GetComponent<ItemDrop>();
                if (item == null)
                {
                    continue;
                }

                sharedNames.Add(item.m_itemData.m_shared.m_name);
                if (!ai.m_consumeItems.Any(food => food != null && food.m_itemData.m_shared.m_name == item.m_itemData.m_shared.m_name))
                {
                    ai.m_consumeItems.Add(item);
                }
            }

            sharedNamesByCreature[entry.Key] = sharedNames;
        }
    }

    /// <summary>
    /// Whether <paramref name="item"/> is a favourite food of <paramref name="creature"/>.
    /// </summary>
    /// <param name="creature">The animal.</param>
    /// <param name="item">The food.</param>
    /// <returns>True for a favourite.</returns>
    public static bool IsFavorite(GameObject creature, ItemDrop.ItemData item)
    {
        return item != null && TryGetSharedNames(creature, out HashSet<string> names) && names.Contains(item.m_shared.m_name);
    }

    /// <summary>
    /// The favourite foods of <paramref name="creature"/>, as localization tokens.
    /// </summary>
    /// <param name="creature">The animal.</param>
    /// <returns>The tokens, empty when the animal has none.</returns>
    public static IEnumerable<string> GetFavoriteTokens(GameObject creature)
    {
        return TryGetSharedNames(creature, out HashSet<string> names) ? names : Enumerable.Empty<string>();
    }

    private static bool TryGetSharedNames(GameObject creature, out HashSet<string> names)
    {
        names = null;
        return creature != null && sharedNamesByCreature.TryGetValue(Utils.GetPrefabName(creature), out names);
    }
}
