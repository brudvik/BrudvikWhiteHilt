using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Sound;
using HarmonyLib;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Sound;

/// <summary>
/// Updates the indoor weather sound with the game's audio, and gives thunder claps its filters.
/// </summary>
[HarmonyPatch]
public static class IndoorSoundPatches
{
    /// <summary>
    /// Fades the weather sounds after the game's audio update.
    /// </summary>
    /// <param name="__instance">The audio manager.</param>
    [HarmonyPatch(typeof(AudioMan), nameof(AudioMan.Update))]
    [HarmonyPostfix]
    public static void AudioManUpdate(AudioMan __instance)
    {
        if (!VisualHelper.IsHeadless)
        {
            IndoorSound.Tick(__instance);
        }
    }

    /// <summary>
    /// Marks that a thunder clap is being spawned.
    /// </summary>
    [HarmonyPatch(typeof(Thunder), nameof(Thunder.DoThunder))]
    [HarmonyPrefix]
    public static void DoThunder()
    {
        IndoorSound.CapturingThunder = true;
    }

    /// <summary>
    /// Clears the thunder mark again.
    /// </summary>
    /// <param name="__exception">An exception thrown by the method, passed on.</param>
    /// <returns>The same exception.</returns>
    [HarmonyPatch(typeof(Thunder), nameof(Thunder.DoThunder))]
    [HarmonyFinalizer]
    public static Exception DoThunderFinalizer(Exception __exception)
    {
        IndoorSound.CapturingThunder = false;
        return __exception;
    }

    /// <summary>
    /// Gives the sound of a thunder clap the indoor filters.
    /// </summary>
    /// <param name="__result">The spawned effect objects.</param>
    [HarmonyPatch(typeof(EffectList), nameof(EffectList.Create))]
    [HarmonyPostfix]
    public static void EffectCreate(GameObject[] __result)
    {
        if (IndoorSound.CapturingThunder)
        {
            IndoorSound.AttachSpawned(__result);
        }
    }
}
