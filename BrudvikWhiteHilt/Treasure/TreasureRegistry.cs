using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Treasure;

/// <summary>
/// Creates the treasure map Hildir sells, the mound of dug earth with its cairn, and the treasure chest, and puts the
/// map on Hildir's list.
/// </summary>
public static class TreasureRegistry
{
    /// <summary>Prefab name of the mound over a buried treasure.</summary>
    public const string MoundPrefabName = "WhiteHiltTreasureMound";

    /// <summary>Prefab name of the dug-up treasure chest.</summary>
    public const string ChestPrefabName = "WhiteHiltTreasureChest";

    private const string TraderPrefabName = "Hildir";
    private const string MoundSource = "mudpile";
    private const string ChestSource = "TreasureChest_meadows";
    private const string MapSource = "LeatherScraps";

    // Real sizes of the bundle models, which are stored one unit high.
    private const float MoundHeight = 0.4653f;
    private const float MoundSink = 0.14f;
    private const float CairnHeight = 1.7116f;
    private const float ChestHeight = 0.66f;
    private const float ChestWidth = 1.0f;
    private const float ChestDepth = 0.56f;
    private const float MapLength = 0.35f;

    private static readonly Vector3 cairnOffset = new(1.25f, -0.04f, 0.85f);

    /// <summary>Dust that rises from a mound for a player carrying its map: the puff of a blow into mud, without its sound.</summary>
    public static EffectList DustEffect { get; private set; }

    /// <summary>Played when the chest comes up.</summary>
    public static EffectList UnearthEffect { get; private set; }

    /// <summary>
    /// Registers the texts and hooks the prefab events. Call from the plugin's Awake, after the settings.
    /// </summary>
    public static void Initialize()
    {
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(TreasureMapItem.PrefabName), "Treasure Map",
            "A scrap of a map from Hildir, with a cross where something lies buried. Use it to unroll it and compare it with your own map. Bring a pickaxe.");
        Translations.AddEnglish("whitehilt_treasure_mound", "Dug earth");
        Translations.AddEnglish("whitehilt_treasure_mound_hint", "Someone has dug here. Dig with a pickaxe");
        Translations.AddEnglish("whitehilt_treasure_chest", "Treasure chest");
        Translations.AddEnglish("whitehilt_treasure_title", "Treasure map");
        Translations.AddEnglish("whitehilt_treasure_plundered", "Plundered");
        Translations.AddEnglish("whitehilt_treasure_unrolling", "Unrolling the map...");
        Translations.AddEnglish("whitehilt_treasure_close", "Close");
        Translations.AddEnglish("whitehilt_treasure_tip_unmarked", "Not marked yet: use it to have a treasure buried for it.");
        Translations.AddEnglish("whitehilt_treasure_tip_waiting", "A treasure waits where the cross is.");
        Translations.AddEnglish("whitehilt_treasure_tip_plundered", "Plundered: the treasure has been dug up.");
        Translations.AddEnglish("whitehilt_treasure_note", "{0}, {1}.");
        Translations.AddEnglish("whitehilt_treasure_note_landmark", "{0}, {1}. From {3}: {2}.");
        Translations.AddEnglish("whitehilt_treasure_by_sea", "by the sea");
        Translations.AddEnglish("whitehilt_treasure_by_water", "by the water");
        Translations.AddEnglish("whitehilt_treasure_in_forest", "among the trees");
        Translations.AddEnglish("whitehilt_treasure_in_hills", "in the hills");
        Translations.AddEnglish("whitehilt_treasure_in_open", "in open land");
        Translations.AddEnglish("whitehilt_treasure_scale", "{0} m");
        Translations.AddEnglish("msg_whitehilt_treasure_pending", "Hildir's map is still being marked.");
        Translations.AddEnglish("msg_whitehilt_treasure_marked", "The map is marked. Use it to unroll it.");
        Translations.AddEnglish("msg_whitehilt_treasure_noplace", "Hildir finds no treasure in the lands you know. Explore more first.");
        Translations.AddEnglish("msg_whitehilt_treasure_toomany", "You already have treasures waiting in the ground. Dig them up first.");
        Translations.AddEnglish("msg_whitehilt_treasure_warm", "The ground nearby looks dug up...");

        PrefabManager.OnVanillaPrefabsAvailable += AddPrefabs;
        PrefabManager.OnPrefabsRegistered += AddToTrader;
    }

    /// <summary>
    /// Applies changed settings to Hildir's list, also for traders already in the world.
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

    private static void AddPrefabs()
    {
        PrefabManager.OnVanillaPrefabsAvailable -= AddPrefabs;
        AddMap();
        AddMound();
        AddChest();
    }

    private static void AddMap()
    {
        try
        {
            ItemConfig config = new()
            {
                Name = Translations.Token(Translations.ItemKey(TreasureMapItem.PrefabName)),
                Description = Translations.Token(Translations.ItemKey(TreasureMapItem.PrefabName) + "_description")
            };
            CustomItem item = new(TreasureMapItem.PrefabName, MapSource, config);
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
                    VisualHelper.ReplaceMesh(item.ItemPrefab, ForagingAssets.LoadMesh("treasuremap"), ForagingAssets.LoadTexture("treasuremap_albedo"), size: MapLength);
                    Sprite icon = VisualHelper.RenderIcon(item.ItemPrefab);
                    if (icon != null)
                    {
                        shared.m_icons = Enumerable.Repeat(icon, Mathf.Max(1, shared.m_icons?.Length ?? 0)).ToArray();
                    }
                }
                catch (Exception ex)
                {
                    Jotunn.Logger.LogWarning($"{TreasureMapItem.PrefabName}: no custom look: {ex.Message}");
                }
            }

            ItemManager.Instance.AddItem(item);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError("Treasure map failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    private static void AddMound()
    {
        try
        {
            GameObject mound = PrefabManager.Instance.CreateClonedPrefab(MoundPrefabName, MoundSource);
            Destructible destructible = mound.GetComponent<Destructible>();
            BuriedTreasure buried = mound.AddComponent<BuriedTreasure>();
            if (destructible != null)
            {
                buried.m_hitEffect = destructible.m_hitEffect;
                UnityEngine.Object.DestroyImmediate(destructible);
            }

            foreach (Component unwanted in mound.GetComponents<Component>().Where(component => component is HoverText || component is LODGroup))
            {
                UnityEngine.Object.DestroyImmediate(unwanted);
            }

            foreach (Collider collider in mound.GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            foreach (ParticleSystem particles in mound.GetComponentsInChildren<ParticleSystem>(true))
            {
                UnityEngine.Object.DestroyImmediate(particles.gameObject);
            }

            GameObject collision = new("collider") { layer = mound.layer };
            collision.transform.SetParent(mound.transform, false);
            BoxCollider box = collision.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.12f, 0f);
            box.size = new Vector3(1.5f, 0.3f, 1.3f);

            if (!VisualHelper.IsHeadless)
            {
                VisualHelper.HideRenderers(mound);
                Renderer template = ChestTemplate();
                VisualHelper.CreateModel(mound.transform, ForagingAssets.LoadMesh("treasuremound"), ForagingAssets.LoadTexture("treasuremound_albedo"),
                    template, new Vector3(0f, -MoundSink, 0f), Quaternion.identity, MoundHeight);
                AddCairn(mound.transform, template);
            }

            PrefabManager.Instance.AddPrefab(new CustomPrefab(mound, false));
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError("Treasure mound failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    private static void AddChest()
    {
        try
        {
            GameObject chest = PrefabManager.Instance.CreateClonedPrefab(ChestPrefabName, ChestSource);
            Container container = chest.GetComponent<Container>();
            container.m_name = Translations.Token("whitehilt_treasure_chest");
            container.m_defaultItems = new DropTable();
            container.m_autoDestroyEmpty = true;
            Piece piece = chest.GetComponent<Piece>();
            if (piece != null)
            {
                piece.m_name = container.m_name;
                piece.m_canBeRemoved = false;
            }

            foreach (MeshCollider collider in chest.GetComponentsInChildren<MeshCollider>(true))
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            GameObject collision = new("collider") { layer = chest.layer };
            collision.transform.SetParent(chest.transform, false);
            BoxCollider box = collision.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, ChestHeight / 2f - 0.04f, 0f);
            box.size = new Vector3(ChestWidth, ChestHeight, ChestDepth);

            if (!VisualHelper.IsHeadless)
            {
                Renderer template = VisualHelper.HideRenderers(chest);
                VisualHelper.CreateModel(chest.transform, ForagingAssets.LoadMesh("treasurechest"), ForagingAssets.LoadTexture("treasurechest_albedo"), template,
                    new Vector3(0f, -0.04f, 0f), Quaternion.identity, ChestHeight);
                AddCairn(chest.transform, template);
            }

            PrefabManager.Instance.AddPrefab(new CustomPrefab(chest, false));
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError("Treasure chest failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    // The cairn stands beside the mound and stays when the chest comes up, since the chest has one in the same place.
    private static void AddCairn(Transform parent, Renderer template)
    {
        VisualHelper.CreateModel(parent, ForagingAssets.LoadMesh("treasurecairn"), ForagingAssets.LoadTexture("treasurecairn_albedo"),
            template, cairnOffset, Quaternion.Euler(0f, 35f, 0f), CairnHeight);
    }

    // The vanilla chest's wood material takes our textures well and is a plain piece shader, unlike the mud pile's.
    private static Renderer ChestTemplate()
    {
        GameObject source = PrefabManager.Instance.GetPrefab(ChestSource);
        return source.GetComponentsInChildren<MeshRenderer>(true).First();
    }

    // Runs for every ZNetScene, so it must be idempotent.
    private static void AddToTrader()
    {
        CreateEffects();
        Trader trader = PrefabManager.Instance.GetPrefab(TraderPrefabName)?.GetComponent<Trader>();
        if (trader == null)
        {
            Jotunn.Logger.LogWarning($"Treasure: trader {TraderPrefabName} not found, treasure maps cannot be bought");
            return;
        }

        UpdateTrader(trader);
    }

    private static void UpdateTrader(Trader trader)
    {
        ItemDrop map = PrefabManager.Instance.GetPrefab(TreasureMapItem.PrefabName)?.GetComponent<ItemDrop>();
        if (map == null)
        {
            return;
        }

        Trader.TradeItem entry = trader.m_items.Find(candidate => candidate.m_prefab == map);
        if (!TreasureSettings.Enabled.Value)
        {
            if (entry != null)
            {
                trader.m_items.Remove(entry);
            }

            return;
        }

        if (entry == null)
        {
            entry = new Trader.TradeItem { m_prefab = map, m_stack = 1, m_buyPlayerEffects = new EffectList() };
            trader.m_items.Add(entry);
        }

        entry.m_price = TreasureSettings.Price.Value;
        entry.m_requiredGlobalKey = TreasureSettings.RequiredGlobalKey.Value?.Trim() ?? string.Empty;
    }

    private static void CreateEffects()
    {
        if (DustEffect != null)
        {
            return;
        }

        DustEffect = Effects("vfx_MudHit");
        UnearthEffect = Effects("vfx_MudDestroyed", "sfx_MudDestroyed");
    }

    private static EffectList Effects(params string[] names)
    {
        return new EffectList
        {
            m_effectPrefabs = names.Select(name => PrefabManager.Instance.GetPrefab(name))
                .Where(prefab => prefab != null)
                .Select(prefab => new EffectList.EffectData { m_prefab = prefab, m_enabled = true })
                .ToArray()
        };
    }
}
