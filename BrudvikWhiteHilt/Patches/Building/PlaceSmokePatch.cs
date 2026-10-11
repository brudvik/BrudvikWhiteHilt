using BrudvikWhiteHilt.Building;
using HarmonyLib;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Building;

/// <summary>
/// Leaves out the puff of dust and smoke Valheim shows where a piece is built, unless <c>BuildTools.PlaceSmoke</c> is on.
/// </summary>
/// <remarks>
/// A piece's place effect is a list of a sound and one or more particle prefabs, created while the player places it.
/// Only the effects created then are touched, and of them only those that show particles and make no sound, so the
/// knock of the hammer stays. The effects are made on this machine only, so other players keep their own setting.
/// </remarks>
[HarmonyPatch]
internal static class PlaceSmokePatch
{
    [HarmonyPatch(typeof(EffectList), nameof(EffectList.Create))]
    [HarmonyPostfix]
    private static void LeaveOutSmoke(GameObject[] __result)
    {
        if (!BuildUndo.Capturing || BuildToolSettings.PlaceSmoke == null || BuildToolSettings.PlaceSmoke.Value || __result == null)
        {
            return;
        }

        foreach (GameObject effect in __result)
        {
            if (effect == null || effect.GetComponentInChildren<ParticleSystem>(true) == null
                || effect.GetComponentInChildren<AudioSource>(true) != null)
            {
                continue;
            }

            // Some puffs (vfx_Place_wood_pole) are network objects: a plain Destroy leaves a dead view in ZNetScene,
            // which then throws every frame once the player walks away and stops all unloading.
            if (effect.GetComponent<ZNetView>() != null && ZNetScene.instance != null)
            {
                ZNetScene.instance.Destroy(effect);
            }
            else
            {
                Object.Destroy(effect);
            }
        }
    }
}
