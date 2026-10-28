using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BrudvikWhiteHilt.Performance;
using HarmonyLib;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Performance;

/// <summary>
/// Puts the stand-ins of <see cref="FrameGarbage"/> in place of the vanilla code that makes garbage every frame.
/// </summary>
[HarmonyPatch]
public static class FrameGarbagePatches
{
    /// <summary>
    /// In <c>Player.Update</c>, makes the hotbar key names come from <see cref="FrameGarbage.HotbarKeyName"/> instead of
    /// <c>string.Format</c>. The pattern is <c>ldstr "Hotbar{0}..."; load the index; box int; call string.Format</c>: the
    /// box is dropped and the call swapped, so the stack is the same. Leaves the method alone when the pattern is gone.
    /// </summary>
    /// <param name="instructions">Vanilla's code.</param>
    /// <returns>The changed code.</returns>
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> HotbarKeysTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo format = AccessTools.Method(typeof(string), nameof(string.Format), new[] { typeof(string), typeof(object) });
        MethodInfo name = AccessTools.Method(typeof(FrameGarbage), nameof(FrameGarbage.HotbarKeyName));
        List<CodeInstruction> code = instructions.ToList();
        int swapped = 0;
        for (int i = 3; i < code.Count; i++)
        {
            if (code[i].Calls(format) && code[i - 1].opcode == OpCodes.Box && Equals(code[i - 1].operand, typeof(int))
                && code[i - 3].opcode == OpCodes.Ldstr && code[i - 3].operand is string text && text.StartsWith("Hotbar", StringComparison.Ordinal))
            {
                code[i - 1].opcode = OpCodes.Nop;
                code[i - 1].operand = null;
                code[i].operand = name;
                swapped++;
            }
        }

        if (swapped == 0)
        {
            Jotunn.Logger.LogWarning("Performance: Player.Update no longer formats the hotbar keys as expected; the game's own way is used.");
        }

        return code;
    }

    /// <summary>
    /// Sums the worn gear's modifiers without reflection, see <see cref="FrameGarbage.UpdateModifiers"/>. It replaces
    /// vanilla's method, so it runs last and stands back when an earlier prefix has already done it; postfixes, like
    /// the backpack's extra accessories, still add to the sums afterwards.
    /// </summary>
    /// <param name="__instance">The player.</param>
    /// <param name="__runOriginal">False when an earlier prefix has done it.</param>
    /// <returns>False when the sums were made here.</returns>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdateModifiers))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    public static bool UpdateModifiers(Player __instance, bool __runOriginal)
    {
        if (!__runOriginal || !VanillaFixSettings.EquipmentModifiers.Active)
        {
            return __runOriginal;
        }

        try
        {
            return !FrameGarbage.UpdateModifiers(__instance);
        }
        catch (Exception ex)
        {
            VanillaFixSettings.EquipmentModifiers.Fail("summing the equipment modifiers", ex);
            return true;
        }
    }

    /// <summary>
    /// Measures the active-area distance without an array, see <see cref="FrameGarbage.ChebyshevDistance"/>. It
    /// replaces vanilla's method, so it runs last and stands back when an earlier prefix has already answered.
    /// </summary>
    /// <param name="a">One point.</param>
    /// <param name="b">The other point.</param>
    /// <param name="__result">The distance.</param>
    /// <param name="__runOriginal">False when an earlier prefix has answered.</param>
    /// <returns>False when the distance was measured here.</returns>
    [HarmonyPatch(typeof(global::Utils), nameof(global::Utils.ChebyshevDistance), typeof(Vector3), typeof(Vector3))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    public static bool ChebyshevDistance(Vector3 a, Vector3 b, ref float __result, bool __runOriginal)
    {
        if (!__runOriginal || !VanillaFixSettings.ZoneDistance.Active)
        {
            return __runOriginal;
        }

        __result = FrameGarbage.ChebyshevDistance(a, b);
        return false;
    }
}
