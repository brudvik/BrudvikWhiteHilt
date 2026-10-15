using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// The Mine Chart the Mistlands posts' trader sells: reading it asks the server where the infested mines nearest the
/// reader lie, and marks the nearest one the reader has not found yet on their map and shows it. The chart is used up
/// when it marks a mine; one that finds none is kept.
/// </summary>
public static class TradingPostMineChart
{
    /// <summary>Prefab name of the chart.</summary>
    public const string PrefabName = "WhiteHilt_MineChart";

    /// <summary>The prefix of the infested mines' location names.</summary>
    public const string MinePrefix = "Mistlands_DvergrTownEntrance";

    /// <summary>How many of the nearest mines the server sends, for the reader to choose the nearest one not yet found.</summary>
    public const int MinesSent = 24;

    private const string ChartSource = "LeatherScraps";
    private const float ChartLength = 0.35f;
    private const Minimap.PinType PinType = Minimap.PinType.Icon3;

    // A mine with a pin this close to it is already marked on the map.
    private const float PinnedRange = 60f;

    // The Mistlands' purple, for the chart's tint.
    private static readonly Color tint = new(0.72f, 0.62f, 0.9f);

    /// <summary>
    /// Whether an item is a Mine Chart.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>True for a Mine Chart.</returns>
    public static bool Is(ItemDrop.ItemData item) => item?.m_dropPrefab != null && item.m_dropPrefab.name == PrefabName;

    /// <summary>
    /// Registers the texts and hooks the prefab event. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(PrefabName), "Mine Chart",
            "A chart from a Mistlands trader showing the way to the nearest infested mine you have not found yet. Read it to mark the mine on your map.");
        Translations.AddEnglish("whitehilt_minechart_pin", "Infested mine");
        Translations.AddEnglish("msg_whitehilt_minechart_none", "The chart shows no infested mine you have not found already.");
        PrefabManager.OnVanillaPrefabsAvailable += AddChart;
    }

    /// <summary>
    /// The store entry for the Mistlands posts, behind the key that opens the Mistlands.
    /// </summary>
    /// <param name="biome">The post's biome index.</param>
    /// <returns>The entry, or null at other posts or with the chart off.</returns>
    public static Trader.TradeItem TradeItem(int biome)
    {
        int price = TradingPostSettings.MineChartPrice.Value;
        ItemDrop chart = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(PrefabName)?.GetComponent<ItemDrop>() : null;
        if (chart == null || price <= 0 || biome != TradingPostGoods.IndexOf("Mistlands"))
        {
            return null;
        }

        return new Trader.TradeItem
        {
            m_prefab = chart,
            m_stack = 1,
            m_tooltip = string.Empty,
            m_price = price,
            m_requiredGlobalKey = TradingPostGoods.OpenKey(biome),
            m_buyPlayerEffects = new EffectList()
        };
    }

    /// <summary>
    /// On the server: the infested mines nearest a place, nearest first.
    /// </summary>
    /// <param name="from">Where the reader stands.</param>
    /// <returns>Up to <see cref="MinesSent"/> mines' positions.</returns>
    public static List<Vector3> NearestMines(Vector3 from)
    {
        if (ZoneSystem.instance == null)
        {
            return new List<Vector3>();
        }

        return ZoneSystem.instance.m_locationInstances.Values
            .Where(instance => instance.m_location?.m_prefabName != null && instance.m_location.m_prefabName.StartsWith(MinePrefix, StringComparison.Ordinal))
            .Select(instance => instance.m_position)
            .OrderBy(position => Utils.DistanceXZ(position, from))
            .Take(MinesSent)
            .ToList();
    }

    /// <summary>
    /// On the reader's machine: marks the nearest of the mines the server sent that the reader has not found yet, shows
    /// it on the map and uses up one chart.
    /// </summary>
    /// <param name="mines">The mines nearest the reader, nearest first.</param>
    public static void Mark(IReadOnlyList<Vector3> mines)
    {
        Player player = Player.m_localPlayer;
        Minimap map = Minimap.instance;
        if (player == null || map == null)
        {
            return;
        }

        Vector3? mine = mines.Cast<Vector3?>().FirstOrDefault(position => !Found(map, position.Value));
        if (mine == null)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_minechart_none");
            return;
        }

        ItemDrop.ItemData chart = player.GetInventory().GetAllItems().FirstOrDefault(Is);
        if (chart == null)
        {
            return;
        }

        player.GetInventory().RemoveOneItem(chart);
        WhiteHiltSound.PlayLocal("sfx_parchment");
        map.DiscoverLocation(mine.Value, PinType, Localization.instance.Localize("$whitehilt_minechart_pin"), showMap: true);
    }

    // A mine is found when the land around it is uncovered on the reader's map or a pin already marks it.
    private static bool Found(Minimap map, Vector3 position)
    {
        if (map.m_pins.Any(pin => Utils.DistanceXZ(pin.m_pos, position) < PinnedRange))
        {
            return true;
        }

        map.WorldToPixel(position, out int x, out int y);
        int index = y * map.m_textureSize + x;
        BitArray explored = map.m_explored;
        return explored != null && x >= 0 && y >= 0 && x < map.m_textureSize && y < map.m_textureSize && explored[index];
    }

    // Registers the chart: a scrap of leather drawn as the treasure maps are, tinted towards the Mistlands' purple.
    private static void AddChart()
    {
        PrefabManager.OnVanillaPrefabsAvailable -= AddChart;
        try
        {
            ItemConfig config = new()
            {
                Name = Translations.Token(Translations.ItemKey(PrefabName)),
                Description = Translations.Token(Translations.ItemKey(PrefabName) + "_description")
            };
            CustomItem item = new(PrefabName, ChartSource, config);
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
                    VisualHelper.ReplaceMesh(item.ItemPrefab, ForagingAssets.LoadMesh("treasuremap"), ForagingAssets.LoadTexture("treasuremap_albedo"), size: ChartLength);
                    VisualHelper.Tint(item.ItemPrefab, tint);
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
            Jotunn.Logger.LogError($"{PrefabName} failed to load: {ex.Message}");
        }
    }
}
