using BrudvikWhiteHilt.Building.Groups;
using BrudvikWhiteHilt.Helpers;
using HarmonyLib;
using System;

namespace BrudvikWhiteHilt.Patches.Building;

/// <summary>
/// Hooks the shared blueprint service onto the game, and remembers which connection a routed RPC came in on, so the
/// server can confirm who sent it.
/// </summary>
[HarmonyPatch]
public static class SharedBlueprintPatches
{
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    [HarmonyPostfix]
    private static void GameStartPostfix(Game __instance)
    {
        __instance.gameObject.AddComponent<SharedBlueprints>();
    }

    [HarmonyPatch(typeof(ZRoutedRpc), nameof(ZRoutedRpc.RPC_RoutedRPC))]
    [HarmonyPrefix]
    private static void RoutedRpcPrefix(ZRpc rpc)
    {
        RoutedRpcSender.Current = rpc;
    }

    [HarmonyPatch(typeof(ZRoutedRpc), nameof(ZRoutedRpc.RPC_RoutedRPC))]
    [HarmonyFinalizer]
    private static Exception RoutedRpcFinalizer(Exception __exception)
    {
        RoutedRpcSender.Current = null;
        return __exception;
    }
}
