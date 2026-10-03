using BrudvikWhiteHilt.Building.Moats;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Building;

/// <summary>
/// Creatures down in a dug ditch move slower and slide down its steep sides for a while. Player overrides the speed
/// factors, so these patches on the base class only reach creatures.
/// </summary>
[HarmonyPatch]
public static class MoatPatches
{
    /// <summary>
    /// Slows a creature's jog while it is in a ditch.
    /// </summary>
    /// <param name="__instance">The character.</param>
    /// <param name="__result">The speed factor.</param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Character), nameof(Character.GetJogSpeedFactor))]
    public static void JogSpeedPostfix(Character __instance, ref float __result)
    {
        __result *= MoatEffects.SpeedFactor(__instance);
    }

    /// <summary>
    /// Slows a creature's run while it is in a ditch.
    /// </summary>
    /// <param name="__instance">The character.</param>
    /// <param name="__result">The speed factor.</param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Character), nameof(Character.GetRunSpeedFactor))]
    public static void RunSpeedPostfix(Character __instance, ref float __result)
    {
        __result *= MoatEffects.SpeedFactor(__instance);
    }

    /// <summary>
    /// Lets a creature slide down steep ground in a ditch, as players do everywhere. The tiny GetSlideAngle may be
    /// inlined, so its call in ApplySlide is swapped for <see cref="SlideAngle"/>.
    /// </summary>
    /// <param name="instructions">The original instructions.</param>
    /// <returns>The changed instructions.</returns>
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Character), nameof(Character.ApplySlide))]
    public static IEnumerable<CodeInstruction> ApplySlideTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo original = AccessTools.Method(typeof(Character), nameof(Character.GetSlideAngle));
        MethodInfo replacement = AccessTools.Method(typeof(MoatPatches), nameof(SlideAngle));
        bool swapped = false;
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.Calls(original))
            {
                swapped = true;
                yield return new CodeInstruction(OpCodes.Call, replacement);
                continue;
            }

            yield return instruction;
        }

        if (!swapped)
        {
            Jotunn.Logger.LogWarning("Moats: Character.ApplySlide has changed; creatures will not slide in ditches.");
        }
    }

    /// <summary>
    /// The slope above which a character slides: the game's own, or the ditch's for a creature down in one.
    /// </summary>
    /// <param name="character">The character.</param>
    /// <returns>Degrees.</returns>
    public static float SlideAngle(Character character)
    {
        float angle = character.GetSlideAngle();
        return MoatEffects.Slides(character) ? Mathf.Min(angle, MoatSettings.CreatureSlideAngle.Value) : angle;
    }
}
