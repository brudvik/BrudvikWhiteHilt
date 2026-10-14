using Jotunn.Entities;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// Sets the trading posts up when the game starts: the marker prefab every post has at its origin, the traders and
/// their clothes, and the console command whitehilt_tradepost. The pieces themselves are made like every White Hilt piece (<see cref="TradingPostPieceBase"/>).
/// </summary>
public static class TradingPostRegistry
{
    /// <summary>
    /// Binds the settings and registers the prefab and the command. Call once from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        TradingPostSettings.Initialize();
        TraderClothes.RegisterTranslations();
        TradingPostTrader.RegisterTranslations();
        TradingPostCharts.Initialize();
        TradingPostSellPicker.RegisterTranslations();
        TradingPostStore.RegisterTranslations();
        TradingPostGear.RegisterTranslations();
        TradingSkill.Register();
        PrefabManager.OnVanillaPrefabsAvailable += CreateMarker;
        CommandManager.Instance.AddConsoleCommand(new TradingPostCommand());
    }

    // The marker: an empty, saved network object with the post's site behaviour, no model and no collider. It is also
    // a location where nothing may be built, like Haldor's camp, and it levels the land behind the landing.
    private static void CreateMarker()
    {
        PrefabManager.OnVanillaPrefabsAvailable -= CreateMarker;
        TraderClothes.Create();
        TradingPostTrader.CreatePrefab();
        GameObject prefab = PrefabManager.Instance.CreateEmptyPrefab(TradingPostKeys.MarkerPrefabName);
        foreach (Component part in prefab.GetComponents<Component>())
        {
            if (part is MeshRenderer || part is MeshFilter || part is Collider)
            {
                Object.DestroyImmediate(part);
            }
        }

        prefab.GetComponent<ZNetView>().m_persistent = true;
        TradingPostLayout layout = TradingPostLayout.Load();
        Location location = prefab.AddComponent<Location>();
        location.m_noBuild = true;
        location.m_exteriorRadius = layout.dome.radius;
        location.m_noBuildRadiusOverride = layout.dome.radius;
        location.m_clearArea = false;

        foreach (TradingPostLevel level in layout.level ?? new TradingPostLevel[0])
        {
            AddLevelling(prefab.transform, level);
        }

        prefab.AddComponent<TradingPostSite>();
        PrefabManager.Instance.AddPrefab(new CustomPrefab(prefab, false));
    }

    // A round terrain modifier like the ones vanilla locations use: the ground inside the radius is levelled to the
    // modifier's height and smoothed into the land around out to the smoothing radius.
    private static void AddLevelling(Transform marker, TradingPostLevel level)
    {
        GameObject child = new("whitehilt_tradingpost_level");
        child.transform.SetParent(marker, false);
        child.transform.localPosition = TradingPostLayout.ToVector(level.centre);
        TerrainModifier modifier = child.AddComponent<TerrainModifier>();
        modifier.m_level = true;
        modifier.m_levelRadius = level.radius;
        modifier.m_levelOffset = 0f;
        modifier.m_smooth = true;
        modifier.m_smoothRadius = level.smooth;
        modifier.m_smoothPower = 3f;
        modifier.m_square = false;
        modifier.m_paintCleared = false;
        modifier.m_playerModifiction = false;
        modifier.m_useTerrainCompiler = false;
    }

    private sealed class TradingPostCommand : ConsoleCommand
    {
        public override string Name => "whitehilt_tradepost";

        public override string Help => "[here|remove|list|place] Admins: build a trading post where you stand, its jetty running out the way you look (here), remove the nearest (remove), list them (list), or search the coasts for the posts each biome still lacks (place).";

        public override void Run(string[] args)
        {
            switch (args.Length > 0 ? args[0] : "list")
            {
                case "here":
                    TradingPostService.RequestHere();
                    break;
                case "remove":
                    TradingPostService.RequestRemove();
                    break;
                case "place":
                    TradingPostService.RequestPlace();
                    break;
                default:
                    TradingPostService.RequestList();
                    break;
            }
        }

        public override List<string> CommandOptionList()
        {
            return new List<string> { "here", "remove", "list", "place" };
        }
    }
}
