using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles.Escorts;

/// <summary>
/// The Escort Contract: a scrap of leather drawn as the treasure maps are, in the blue of Haldor's trade, sold once the
/// sea battles are open to the world: by the traders of the trading posts, or by Haldor when the posts are off. It has no recipe. Using it is done by <see cref="EscortService"/>.
/// The goods of the trader are set up the way the other traders' are: idempotent, for every new world scene, and for the
/// traders already standing in the world, and the contract is taken away again when the feature is switched off.
/// </summary>
public static class EscortContract
{
    /// <summary>Prefab name of the Escort Contract.</summary>
    public const string PrefabName = "WhiteHilt_EscortContract";

    private const string TraderPrefabName = "Haldor";
    private const string ItemSource = "LeatherScraps";

    // The bundle's treasure map is stored one unit long; the contract is drawn as long as the treasure maps are.
    private const float ContractLength = 0.35f;
    private static readonly Color contractTint = new(0.45f, 0.62f, 0.9f);

    /// <summary>
    /// Registers the item and puts it on Haldor's list. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        PrefabManager.OnVanillaPrefabsAvailable += AddItem;
        PrefabManager.OnPrefabsRegistered += AddToTrader;
    }

    /// <summary>
    /// Applies changed settings to Haldor's list, also for traders already in the world.
    /// </summary>
    public static void ApplyConfig()
    {
        if (ZNetScene.instance == null)
        {
            return;
        }

        AddToTrader();
        foreach (Trader trader in UnityEngine.Object.FindObjectsByType<Trader>(FindObjectsSortMode.None))
        {
            if (Utils.GetPrefabName(trader.gameObject) == TraderPrefabName)
            {
                UpdateTrader(trader);
            }
        }
    }

    /// <summary>
    /// Whether an item is an Escort Contract.
    /// </summary>
    /// <param name="item">The item, or null.</param>
    /// <returns>True for the contract.</returns>
    public static bool Is(ItemDrop.ItemData item)
    {
        return item?.m_dropPrefab != null && item.m_dropPrefab.name == PrefabName;
    }

    // A scrap of leather drawn as the treasure maps are, tinted blue. A failing look keeps the leather's own.
    private static void AddItem()
    {
        PrefabManager.OnVanillaPrefabsAvailable -= AddItem;
        try
        {
            ItemConfig config = new()
            {
                Name = Translations.Token(Translations.ItemKey(PrefabName)),
                Description = Translations.Token(Translations.ItemKey(PrefabName) + "_description")
            };
            CustomItem item = new(PrefabName, ItemSource, config);
            ItemDrop.ItemData.SharedData shared = item.ItemDrop.m_itemData.m_shared;
            shared.m_weight = 0.1f;
            shared.m_maxStackSize = 10;
            shared.m_itemType = ItemDrop.ItemData.ItemType.Misc;
            shared.m_value = 0;
            shared.m_teleportable = true;
            if (!VisualHelper.IsHeadless)
            {
                try
                {
                    VisualHelper.ReplaceMesh(item.ItemPrefab, ForagingAssets.LoadMesh("treasuremap"), ForagingAssets.LoadTexture("treasuremap_albedo"), size: ContractLength);
                    VisualHelper.Tint(item.ItemPrefab, contractTint);
                    Sprite icon = VisualHelper.RenderIcon(item.ItemPrefab);
                    if (icon != null)
                    {
                        shared.m_icons = Enumerable.Repeat(icon, Mathf.Max(1, shared.m_icons?.Length ?? 0)).ToArray();
                    }
                }
                catch (Exception ex)
                {
                    Jotunn.Logger.LogWarning($"{PrefabName}: no custom look: {ex.Message}");
                }
            }

            ItemManager.Instance.AddItem(item);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError("Escort Contract failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    // Runs for every ZNetScene, so it must be idempotent.
    private static void AddToTrader()
    {
        Trader trader = PrefabManager.Instance.GetPrefab(TraderPrefabName)?.GetComponent<Trader>();
        if (trader == null)
        {
            Jotunn.Logger.LogWarning($"Escort: trader {TraderPrefabName} not found, contracts cannot be bought");
            return;
        }

        UpdateTrader(trader);
    }

    // Adds the contract to the trader's goods at the configured price and behind the key of the sea battles, or takes it
    // away when escorts or sea battles are switched off.
    private static void UpdateTrader(Trader trader)
    {
        ItemDrop contract = PrefabManager.Instance.GetPrefab(PrefabName)?.GetComponent<ItemDrop>();
        if (contract == null)
        {
            return;
        }

        Trader.TradeItem entry = trader.m_items.Find(candidate => candidate.m_prefab == contract);

        // With trading posts the contract moves to their traders (TradingPostStore), so escorts begin at a post.
        if (!EscortSettings.Enabled.Value || !SeaBattleSettings.Enabled.Value || TradingPosts.TradingPostSettings.Enabled.Value)
        {
            if (entry != null)
            {
                trader.m_items.Remove(entry);
            }

            return;
        }

        if (entry == null)
        {
            // StoreGui.BuySelectedItem plays these after every purchase without a null check.
            entry = new Trader.TradeItem { m_prefab = contract, m_stack = 1, m_tooltip = string.Empty, m_buyPlayerEffects = new EffectList() };
            trader.m_items.Add(entry);
        }

        entry.m_price = EscortSettings.ContractPrice.Value;
        entry.m_requiredGlobalKey = SeaBattleSettings.RequiredKey.Value?.Trim() ?? string.Empty;
    }
}
