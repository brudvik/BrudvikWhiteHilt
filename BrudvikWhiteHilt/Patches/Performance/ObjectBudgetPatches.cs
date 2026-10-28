using System;
using System.Collections.Generic;
using BrudvikWhiteHilt.Performance;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Performance;

/// <summary>
/// Makes the objects that come into view within a time budget per frame (<see cref="ObjectBudget"/>).
/// </summary>
[HarmonyPatch]
public static class ObjectBudgetPatches
{
    /// <summary>
    /// Makes this pass's objects within the budget, or lets the game's method run behind a loading screen, on a
    /// dedicated server or with the setting off.
    /// </summary>
    /// <param name="__instance">The scene.</param>
    /// <param name="currentNearObjects">The objects in the active area.</param>
    /// <param name="currentDistantObjects">The distant objects.</param>
    /// <returns>False when the objects were made here.</returns>
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.CreateObjects))]
    [HarmonyPrefix]
    public static bool CreateObjectsPrefix(ZNetScene __instance, List<ZDO> currentNearObjects, List<ZDO> currentDistantObjects)
    {
        try
        {
            return !ObjectBudget.TryCreate(__instance, currentNearObjects, currentDistantObjects);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning("Object budget failed, the game's own method runs: " + ex.Message);
            return true;
        }
    }
}
