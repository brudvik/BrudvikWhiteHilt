using HarmonyLib;
using System.Collections.Generic;

namespace BrudvikWhiteHilt.Patches.Diagnostics;

/// <summary>
/// Says more about two warnings the game repeats without saying why. "Missing prefab hash" comes every frame on a
/// client while an object it has no prefab for is near (the server deletes such objects, clients only warn): this logs,
/// once per hash, where the object is and who owns it, so it can be found and its maker named. "Trying to add item to
/// occupied slot" means something keeps adding to a full inventory: this logs, once per caller, the stack that did it.
/// </summary>
[HarmonyPatch]
public static class WorldWarningPatches
{
    private static readonly HashSet<int> missing = new();
    private static readonly HashSet<string> fullCallers = new();

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.CreateObject))]
    [HarmonyPrefix]
    private static void CreateObjectPrefix(ZNetScene __instance, ZDO zdo)
    {
        int prefab = zdo.GetPrefab();
        if (prefab == 0 || __instance.GetPrefab(prefab) != null || !missing.Add(prefab))
        {
            return;
        }

        Jotunn.Logger.LogWarning($"Unknown prefab {prefab}: an object at {zdo.GetPosition()} owned by {zdo.GetOwner()}, made by a mod or version this game does not have. " +
            "Its warning repeats while it is near; this is said once.");
    }

    [HarmonyPatch(typeof(ZLog), nameof(ZLog.LogWarning))]
    [HarmonyPostfix]
    private static void LogWarningPostfix(object o)
    {
        if (o is not string text || !text.StartsWith("Trying to add item to occupied slot"))
        {
            return;
        }

        string stack = System.Environment.StackTrace;
        int start = stack.IndexOf("Inventory.AddItem");
        string caller = start >= 0 ? stack.Substring(start, System.Math.Min(600, stack.Length - start)) : stack;
        if (fullCallers.Add(caller))
        {
            Jotunn.Logger.LogWarning($"An item was added to a full inventory, from:\n{caller}");
        }
    }
}
