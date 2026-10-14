using Jotunn.Entities;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// Sets the trading posts up when the game starts: the marker prefab every post has at its origin, and the console
/// command whitehilt_tradepost. The pieces themselves are made like every White Hilt piece (<see cref="TradingPostPieceBase"/>).
/// </summary>
public static class TradingPostRegistry
{
    /// <summary>
    /// Binds the settings and registers the prefab and the command. Call once from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        TradingPostSettings.Initialize();
        PrefabManager.OnVanillaPrefabsAvailable += CreateMarker;
        CommandManager.Instance.AddConsoleCommand(new TradingPostCommand());
    }

    // The marker: an empty, saved network object with the post's site behaviour, no model and no collider.
    private static void CreateMarker()
    {
        PrefabManager.OnVanillaPrefabsAvailable -= CreateMarker;
        GameObject prefab = PrefabManager.Instance.CreateEmptyPrefab(TradingPostKeys.MarkerPrefabName);
        foreach (Component part in prefab.GetComponents<Component>())
        {
            if (part is MeshRenderer || part is MeshFilter || part is Collider)
            {
                Object.DestroyImmediate(part);
            }
        }

        prefab.GetComponent<ZNetView>().m_persistent = true;
        prefab.AddComponent<TradingPostSite>();
        PrefabManager.Instance.AddPrefab(new CustomPrefab(prefab, false));
    }

    private sealed class TradingPostCommand : ConsoleCommand
    {
        public override string Name => "whitehilt_tradepost";

        public override string Help => "[here|remove|list] Admins: build a trading post where you stand, its jetty running out the way you look (here), remove the nearest (remove), or list them (list).";

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
                default:
                    TradingPostService.RequestList();
                    break;
            }
        }

        public override List<string> CommandOptionList()
        {
            return new List<string> { "here", "remove", "list" };
        }
    }
}
