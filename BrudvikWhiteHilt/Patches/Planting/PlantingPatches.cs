using BrudvikWhiteHilt.Planting;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Planting;

/// <summary>
/// Lets the cultivator remove berry bushes, mushrooms, flowers, debris and flora planted with it.
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.RemovePiece))]
public static class RemovePlantablePatch
{
    private static bool Prefix(Player __instance, ref bool __result)
    {
        if (!Plantables.TryRemove(__instance, out bool removed))
        {
            return true;
        }

        __result = removed;
        return false;
    }
}

/// <summary>
/// Adds the plantables once both ObjectDB and ZNetScene exist, whichever of them wakes up last.
/// </summary>
[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
public static class PlantablesZNetScenePatch
{
    private static void Postfix()
    {
        Plantables.Apply();
    }
}
