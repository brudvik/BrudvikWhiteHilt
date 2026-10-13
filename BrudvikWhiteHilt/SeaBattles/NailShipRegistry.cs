using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Ships.Ballista;
using BrudvikWhiteHilt.SeaBattles.Raids;
using BrudvikWhiteHilt.SeaBattles.Weapons;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles;

/// <summary>
/// Registers the Nail Ship: a clone of the vanilla longship with a hull of its own health, a sail that pulls as hard as
/// the settings say, no way for the hammer to take it apart, the dark look of Naglfar's fleet and the
/// <see cref="RaiderShip"/> that sails it. It also registers the Raid Chart and the hoard chest of a draugr harbour.
/// </summary>
public static class NailShipRegistry
{
    /// <summary>Prefab name of the Nail Ship.</summary>
    public const string ShipName = "WhiteHilt_NailShip";

    /// <summary>Prefab name of the Raid Chart.</summary>
    public const string RaidChartName = "WhiteHiltRaidChart";

    /// <summary>Prefab name of the hoard chest of a draugr harbour.</summary>
    public const string HoardName = "WhiteHilt_RaidHoard";

    private const string VanillaShip = "VikingShip";
    private const string ChartSource = "LeatherScraps";
    private const string HoardSource = "TreasureChest_swamp";

    // The bundle's treasure map is stored one unit long; the chart is drawn as long as the treasure maps are.
    private const float ChartLength = 0.35f;
    private static readonly Color chartTint = new(0.55f, 0.68f, 0.62f);

    /// <summary>
    /// Registers the prefab once the vanilla prefabs can be cloned. Call from the plugin's Awake, after the Kraken's.
    /// </summary>
    public static void Initialize()
    {
        PrefabManager.OnVanillaPrefabsAvailable += Add;
    }

    // Clones the longship and changes what differs. A failure is logged and leaves the mod without Nail Ships, not
    // without the rest of the prefabs. The chart and the hoard each have their own try, so one failing keeps the others.
    private static void Add()
    {
        PrefabManager.OnVanillaPrefabsAvailable -= Add;
        AddShip();
        AddRaidChart();
        AddHoard();
    }

    private static void AddShip()
    {
        try
        {
            GameObject prefab = PrefabManager.Instance.CreateClonedPrefab(ShipName, VanillaShip);
            SetUpPiece(prefab);
            SetUpHull(prefab);
            SetUpSail(prefab);

            // A sea encounter must not outlive the players: left alone, the clone keeps the longship's persistent
            // ZDO, and a Nail Ship nobody sails near would sit in the saved world for ever. Raid ships are made by
            // the server with a persistent ZDO of their own.
            prefab.GetComponent<ZNetView>().m_persistent = false;

            prefab.AddComponent<RaiderShip>();
            AddWeapons(prefab);
            NailShipLook.Apply(prefab);
            PrefabManager.Instance.AddPrefab(new CustomPrefab(prefab, false));
            Jotunn.Logger.LogInfo("Nail Ship added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError("Nail Ship failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    // It is a piece of the vanilla longship, but in no piece table: the hammer must not take it apart for materials.
    private static void SetUpPiece(GameObject prefab)
    {
        Piece piece = prefab.GetComponent<Piece>();
        piece.m_name = Translations.Token("whitehilt_nailship");
        piece.m_canBeRemoved = false;
        piece.m_resources = Array.Empty<Piece.Requirement>();
    }

    // The longship's hull resists pierce damage; a Nail Ship takes it in full, so arrows and ballista bolts hurt it.
    private static void SetUpHull(GameObject prefab)
    {
        WearNTear hull = prefab.GetComponent<WearNTear>();
        hull.m_health = SeaBattleSettings.HullHealth.Value;
        hull.m_damages.m_pierce = HitData.DamageModifier.Normal;
    }

    private static void SetUpSail(GameObject prefab)
    {
        Ship ship = prefab.GetComponent<Ship>();
        ship.m_sailForceFactor *= SeaBattleSettings.SailForceFactor.Value;
    }

    // The ship's weapons, and the player ballista's part on its bow, built for show: nobody can man it, it stays hidden until
    // the world has reached the stage from which Nail Ships carry one, and the crew's weapons run from NailShipWeapons.
    private static void AddWeapons(GameObject prefab)
    {
        ShipBallista ballista = BallistaModel.Build(prefab.transform);
        if (ballista != null)
        {
            ballista.m_decor = true;
        }

        prefab.AddComponent<NailShipWeapons>();
    }

    // The Raid Chart is a scrap of leather drawn as the treasure maps are, in the green-grey of Naglfar's fleet. It has
    // no recipe and no trader: it is found in a Nail Ship's hold. Using it is done by RaidService.
    private static void AddRaidChart()
    {
        try
        {
            ItemConfig config = new()
            {
                Name = Translations.Token(Translations.ItemKey(RaidChartName)),
                Description = Translations.Token(Translations.ItemKey(RaidChartName) + "_description")
            };
            CustomItem item = new(RaidChartName, ChartSource, config);
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
                    VisualHelper.Tint(item.ItemPrefab, chartTint);
                    Sprite icon = VisualHelper.RenderIcon(item.ItemPrefab);
                    if (icon != null)
                    {
                        shared.m_icons = Enumerable.Repeat(icon, Mathf.Max(1, shared.m_icons?.Length ?? 0)).ToArray();
                    }
                }
                catch (Exception ex)
                {
                    Jotunn.Logger.LogWarning($"{RaidChartName}: no custom look: {ex.Message}");
                }
            }

            ItemManager.Instance.AddItem(item);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError("Raid Chart failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    // The hoard is a clone of the vanilla Swamp treasure chest with its own loot taken away, as it is filled by
    // RaidHoard. It cannot be taken apart with the hammer, and it has no hull to break, since breaking a chest open
    // would drop its loot without the guards being dead.
    private static void AddHoard()
    {
        try
        {
            GameObject hoard = PrefabManager.Instance.CreateClonedPrefab(HoardName, HoardSource);
            Container container = hoard.GetComponent<Container>();
            container.m_name = Translations.Token("whitehilt_raid_hoard");
            container.m_defaultItems = new DropTable();
            container.m_autoDestroyEmpty = false;
            Piece piece = hoard.GetComponent<Piece>();
            if (piece != null)
            {
                piece.m_name = container.m_name;
                piece.m_canBeRemoved = false;
            }

            WearNTear wear = hoard.GetComponent<WearNTear>();
            if (wear != null)
            {
                UnityEngine.Object.DestroyImmediate(wear);
            }

            hoard.AddComponent<RaidHoard>();
            PrefabManager.Instance.AddPrefab(new CustomPrefab(hoard, false));
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError("Raid hoard failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }
}
