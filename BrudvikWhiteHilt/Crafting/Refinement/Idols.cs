using BrudvikWhiteHilt.TradingPosts;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Crafting.Refinement;

/// <summary>
/// The game's idols for the Forge of Potential, one Battle and one Protection idol for each tier from Wooden to
/// Bloodgold, which follow the biomes in order. They are hard to find, so the trading posts sell their biome's, and
/// idols can be exchanged for the next tier's, or for the other kind (<see cref="RefinementSettings"/>).
/// </summary>
public static class Idols
{
    private static readonly string[] kinds = { "Weapon", "Armor" };

    // The exchange recipes, with the idols each takes, so a change in the config reaches them without a restart.
    private static readonly List<(Recipe Recipe, bool Kind)> recipes = new();

    /// <summary>
    /// The prefab name of an idol.
    /// </summary>
    /// <param name="tier">The tier, 0 (Wooden) to 7 (Bloodgold), as the biomes in <see cref="TradingPostGoods.Biomes"/>.</param>
    /// <param name="kind">"Weapon" for Battle, "Armor" for Protection.</param>
    /// <returns>The prefab name.</returns>
    public static string PrefabName(int tier, string kind) => $"Upgrader{tier}{kind}";

    /// <summary>
    /// Registers the exchange recipes once the vanilla prefabs exist. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        PrefabManager.OnVanillaPrefabsAvailable += AddRecipes;
    }

    /// <summary>
    /// The idols a biome's trader sells, behind the key that opens the biome, at a player's prices.
    /// </summary>
    /// <param name="biome">The biome index, which is the idols' tier.</param>
    /// <param name="player">The player who trades, or null for the prices at Trading 0.</param>
    /// <returns>The store's entries.</returns>
    public static IEnumerable<Trader.TradeItem> TradeItems(int biome, Player player)
    {
        if (!RefinementSettings.IdolsAtTraders.Value || ObjectDB.instance == null || biome < 0 || biome >= TradingPostGoods.Biomes.Length)
        {
            yield break;
        }

        int price = TradingSkill.Price(RefinementOdds.IdolPrice(RefinementSettings.IdolPrice.Value, TradingPostGoods.PlainWorth(biome)), player);
        foreach (string kind in kinds)
        {
            ItemDrop idol = ObjectDB.instance.GetItemPrefab(PrefabName(biome, kind))?.GetComponent<ItemDrop>();
            if (idol == null)
            {
                continue;
            }

            yield return new Trader.TradeItem
            {
                m_prefab = idol,
                m_stack = 1,
                m_tooltip = string.Empty,
                m_price = price,
                m_requiredGlobalKey = TradingPostGoods.OpenKey(biome),
                // The store window plays these after every purchase without checking for none.
                m_buyPlayerEffects = new EffectList()
            };
        }
    }

    // Each tier's idols from the tier below, and each kind from the other, at the configured station. An idol the
    // game does not have is left out.
    private static void AddRecipes()
    {
        PrefabManager.OnVanillaPrefabsAvailable -= AddRecipes;
        try
        {
            for (int tier = 0; tier < TradingPostGoods.Biomes.Length; tier++)
            {
                foreach (string kind in kinds)
                {
                    string other = kind == kinds[0] ? kinds[1] : kinds[0];
                    if (tier > 0)
                    {
                        AddRecipe($"Recipe_WhiteHiltIdol{tier}{kind}", PrefabName(tier, kind), PrefabName(tier - 1, kind), false);
                    }

                    AddRecipe($"Recipe_WhiteHiltIdol{tier}{kind}From{other}", PrefabName(tier, kind), PrefabName(tier, other), true);
                }
            }

            RefinementSettings.Exchange.SettingChanged += (_, _) => ApplyRecipes();
            RefinementSettings.ExchangeCost.SettingChanged += (_, _) => ApplyRecipes();
            RefinementSettings.ExchangeKind.SettingChanged += (_, _) => ApplyRecipes();
            ApplyRecipes();
            Jotunn.Logger.LogInfo($"Idol exchange added! ({recipes.Count} recipes)");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError("Idol exchange failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    private static void AddRecipe(string name, string idol, string from, bool kind)
    {
        if (PrefabManager.Instance.GetPrefab(idol)?.GetComponent<ItemDrop>() == null || PrefabManager.Instance.GetPrefab(from)?.GetComponent<ItemDrop>() == null)
        {
            Jotunn.Logger.LogWarning($"Idol exchange: the game has no {idol} or {from}, left out");
            return;
        }

        CustomRecipe recipe = new(new RecipeConfig
        {
            Name = name,
            Item = idol,
            Amount = 1,
            CraftingStation = RefinementSettings.ExchangeStation.Value,
            Requirements = new RequirementConfig[]
            {
                new() { Item = from, Amount = kind ? 1 : RefinementSettings.ExchangeCost.Value }
            }
        });
        ItemManager.Instance.AddRecipe(recipe);
        recipes.Add((recipe.Recipe, kind));
    }

    // The recipe objects stay registered with the game, so changing them in place reaches the station at once.
    private static void ApplyRecipes()
    {
        foreach ((Recipe recipe, bool kind) in recipes)
        {
            recipe.m_enabled = RefinementSettings.Exchange.Value && (!kind || RefinementSettings.ExchangeKind.Value);
            if (!kind && recipe.m_resources.Length > 0)
            {
                recipe.m_resources[0].m_amount = Mathf.Max(1, RefinementSettings.ExchangeCost.Value);
            }
        }
    }
}
