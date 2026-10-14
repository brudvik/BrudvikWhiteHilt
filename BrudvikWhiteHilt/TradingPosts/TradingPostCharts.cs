using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// The Trading Post Charts Hildir sells, one per biome, each once the world has reached that biome. Reading one asks
/// the server where the biome's posts stand and puts a pin on the reader's own map for each. The chart is not used up,
/// so it can be read again after a death or handed to a friend.
/// </summary>
public static class TradingPostCharts
{
    private const string TraderPrefabName = "Hildir";
    private const string ChartSource = "LeatherScraps";
    private const float ChartLength = 0.35f;
    private const Minimap.PinType PinType = Minimap.PinType.Icon1;

    // A post's pin may stand at its marker or by its trader; one this close to the trader is the post's.
    private const float VisitedPinRange = 120f;

    // The biomes' names in English for the charts' names; the Norwegian ones are in Norwegian.json.
    private static readonly string[] biomeNames = { "Meadows", "Black Forest", "Swamp", "Mountains", "Plains", "Mistlands", "Ashlands", "Deep North" };

    /// <summary>
    /// The prefab name of a biome's chart.
    /// </summary>
    /// <param name="biome">The biome index (<see cref="TradingPostGoods.Biomes"/>).</param>
    /// <returns>The prefab name.</returns>
    public static string PrefabName(int biome) => $"WhiteHilt_TradeChart_{TradingPostGoods.Biomes[biome]}";

    /// <summary>
    /// The biome a chart shows, or -1 for anything else.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The biome index.</returns>
    public static int BiomeOf(ItemDrop.ItemData item)
    {
        string name = item?.m_dropPrefab != null ? item.m_dropPrefab.name : null;
        for (int biome = 0; name != null && biome < TradingPostGoods.Biomes.Length; biome++)
        {
            if (name == PrefabName(biome))
            {
                return biome;
            }
        }

        return -1;
    }

    /// <summary>
    /// Registers the texts and hooks the prefab events. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        for (int biome = 0; biome < TradingPostGoods.Biomes.Length; biome++)
        {
            Translations.AddEnglishNameAndDescription(Translations.ItemKey(PrefabName(biome)), $"Trading Post Chart: {biomeNames[biome]}",
                $"A chart from Hildir of the trading posts on the coasts of the {biomeNames[biome]}. Read it to mark them on your map.");
        }

        Translations.AddEnglish("whitehilt_tradepost_pin", "Trading post");
        Translations.AddEnglish("whitehilt_tradepost_pin_known", "Trading post ($1)");
        Translations.AddEnglish("msg_whitehilt_tradechart_marked", "$1 trading posts marked on your map.");
        Translations.AddEnglish("msg_whitehilt_tradechart_none", "The chart shows no trading post on these coasts.");
        PrefabManager.OnVanillaPrefabsAvailable += AddCharts;
        PrefabManager.OnPrefabsRegistered += AddToHildir;
    }

    /// <summary>
    /// Puts pins on the local player's map for the posts the server named, skipping those already marked.
    /// </summary>
    /// <param name="positions">Where the posts stand.</param>
    public static void Mark(IReadOnlyList<Vector3> positions)
    {
        Player player = Player.m_localPlayer;
        if (Minimap.instance == null || player == null)
        {
            return;
        }

        if (positions.Count == 0)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_tradechart_none");
            return;
        }

        string name = Localization.instance.Localize("$whitehilt_tradepost_pin");
        foreach (Vector3 position in positions)
        {
            if (PostPin(position, name) == null)
            {
                Minimap.instance.AddPin(position, PinType, name, save: true, isChecked: false);
            }
        }

        player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_whitehilt_tradechart_marked", positions.Count.ToString()));
    }

    /// <summary>
    /// Names a post's pin on the local player's map after what the post is known for, once they have traded there; a
    /// post without a pin gets one.
    /// </summary>
    /// <param name="position">Where the post's trader stands.</param>
    /// <param name="kind">What the post is known for.</param>
    public static void MarkVisited(Vector3 position, GearKind kind)
    {
        if (Minimap.instance == null)
        {
            return;
        }

        string plain = Localization.instance.Localize("$whitehilt_tradepost_pin");
        string known = Localization.instance.Localize("$whitehilt_tradepost_pin_known", TradingPostGear.KindName(kind));
        Minimap.PinData pin = PostPin(position, plain);
        if (pin != null && pin.m_name == known)
        {
            return;
        }

        Vector3 at = pin != null ? pin.m_pos : position;
        bool done = pin != null && pin.m_checked;
        if (pin != null)
        {
            Minimap.instance.RemovePin(pin);
        }

        Minimap.instance.AddPin(at, PinType, known, save: true, isChecked: done);
    }

    // The pin of the post at a place: the nearest trading post pin, named after what the post is known for or not.
    private static Minimap.PinData PostPin(Vector3 position, string plain)
    {
        return Minimap.instance.m_pins
            .Where(candidate => candidate.m_type == PinType && candidate.m_name != null && candidate.m_name.StartsWith(plain)
                && Utils.DistanceXZ(candidate.m_pos, position) < VisitedPinRange)
            .OrderBy(candidate => Utils.DistanceXZ(candidate.m_pos, position))
            .FirstOrDefault();
    }

    // Registers the charts: a scrap of leather drawn as the treasure maps are, tinted towards each biome's colour.
    private static void AddCharts()
    {
        PrefabManager.OnVanillaPrefabsAvailable -= AddCharts;
        for (int biome = 0; biome < TradingPostGoods.Biomes.Length; biome++)
        {
            AddChart(biome);
        }
    }

    private static void AddChart(int biome)
    {
        string prefabName = PrefabName(biome);
        try
        {
            ItemConfig config = new()
            {
                Name = Translations.Token(Translations.ItemKey(prefabName)),
                Description = Translations.Token(Translations.ItemKey(prefabName) + "_description")
            };
            CustomItem item = new(prefabName, ChartSource, config);
            ItemDrop.ItemData.SharedData shared = item.ItemDrop.m_itemData.m_shared;
            shared.m_weight = 0.1f;
            shared.m_maxStackSize = 1;
            shared.m_itemType = ItemDrop.ItemData.ItemType.Misc;
            shared.m_value = 0;
            shared.m_teleportable = true;
            if (!VisualHelper.IsHeadless)
            {
                try
                {
                    TraderLook look = TraderLooks.Get((Heightmap.Biome)Enum.Parse(typeof(Heightmap.Biome), TradingPostGoods.Biomes[biome]));
                    VisualHelper.ReplaceMesh(item.ItemPrefab, ForagingAssets.LoadMesh("treasuremap"), ForagingAssets.LoadTexture("treasuremap_albedo"), size: ChartLength);
                    VisualHelper.Tint(item.ItemPrefab, Color.Lerp(Color.white, TraderLook.ToColour(look.main), 0.45f));
                    Sprite icon = VisualHelper.RenderIcon(item.ItemPrefab);
                    if (icon != null)
                    {
                        shared.m_icons = Enumerable.Repeat(icon, Mathf.Max(1, shared.m_icons?.Length ?? 0)).ToArray();
                    }
                }
                catch (Exception ex)
                {
                    Jotunn.Logger.LogWarning($"{prefabName}: no custom look: {ex.Message}");
                }
            }

            ItemManager.Instance.AddItem(item);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{prefabName} failed to load: {ex.Message}");
        }
    }

    // Puts the charts on Hildir's list, each behind the key that opens its biome's posts, at the configured price.
    private static void AddToHildir()
    {
        Trader hildir = PrefabManager.Instance.GetPrefab(TraderPrefabName)?.GetComponent<Trader>();
        if (hildir == null)
        {
            Jotunn.Logger.LogWarning($"Trading posts: trader {TraderPrefabName} not found, the charts cannot be bought");
            return;
        }

        for (int biome = 0; biome < TradingPostGoods.Biomes.Length; biome++)
        {
            ItemDrop chart = PrefabManager.Instance.GetPrefab(PrefabName(biome))?.GetComponent<ItemDrop>();
            if (chart == null || hildir.m_items.Any(entry => entry.m_prefab == chart))
            {
                continue;
            }

            hildir.m_items.Add(new Trader.TradeItem
            {
                m_prefab = chart,
                m_stack = 1,
                m_price = TradingPostSettings.ChartPrice.Value,
                m_requiredGlobalKey = TradingPostGoods.OpenKey(biome),
                m_buyPlayerEffects = new EffectList()
            });
        }
    }
}
