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
/// Makes <c>ZDOExtraData.Set</c> for floats, ints, longs, vectors and rotations, and <c>ZDOExtraData.Update</c> for
/// vectors, call <see cref="ZdoValueWrites"/> instead of <c>ZDOHelper.InitAndSet</c> and <c>ZDOHelper.Update</c>, so an
/// unchanged value is recognised without boxing it. The call is swapped, not the method replaced, so the generic
/// <c>BinarySearchDictionary</c> code in assembly_utils is not patched at all. Strings and byte arrays are left alone:
/// they never boxed.
/// </summary>
[HarmonyPatch]
public static class ZdoWritePatches
{
    private static readonly Type[] ValueTypes = { typeof(float), typeof(int), typeof(long), typeof(Vector3), typeof(Quaternion) };

    /// <summary>
    /// The setters to change: one <c>Set</c> per value type, and the vector <c>Update</c>.
    /// </summary>
    /// <returns>The methods found; a missing one is logged and skipped.</returns>
    [HarmonyTargetMethods]
    public static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (Type type in ValueTypes)
        {
            MethodInfo set = AccessTools.Method(typeof(ZDOExtraData), nameof(ZDOExtraData.Set), new[] { typeof(ZDOID), typeof(int), type });
            if (set != null)
            {
                yield return set;
            }
            else
            {
                Jotunn.Logger.LogWarning($"Performance: ZDOExtraData.Set for {type.Name} was not found; it keeps the game's own compare.");
            }
        }

        MethodInfo update = AccessTools.Method(typeof(ZDOExtraData), nameof(ZDOExtraData.Update), new[] { typeof(ZDOID), typeof(int), typeof(Vector3) });
        if (update != null)
        {
            yield return update;
        }
    }

    /// <summary>
    /// Swaps the one call to <c>ZDOHelper.InitAndSet</c> or <c>ZDOHelper.Update</c> for the matching method of
    /// <see cref="ZdoValueWrites"/>, which takes the same arguments. Leaves the method as it is when it holds no such call.
    /// </summary>
    /// <param name="instructions">Vanilla's code.</param>
    /// <param name="original">The method being changed, for the log.</param>
    /// <returns>The changed code.</returns>
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
    {
        MethodInfo vanillaSet = GenericDefinition(typeof(ZDOHelper), nameof(ZDOHelper.InitAndSet));
        MethodInfo vanillaUpdate = GenericDefinition(typeof(ZDOHelper), nameof(ZDOHelper.Update));
        MethodInfo oursSet = GenericDefinition(typeof(ZdoValueWrites), nameof(ZdoValueWrites.InitAndSet));
        MethodInfo oursUpdate = GenericDefinition(typeof(ZdoValueWrites), nameof(ZdoValueWrites.Update));

        List<CodeInstruction> code = instructions.ToList();
        bool swapped = false;
        foreach (CodeInstruction instruction in code)
        {
            if (instruction.opcode != OpCodes.Call || instruction.operand is not MethodInfo called || !called.IsGenericMethod)
            {
                continue;
            }

            MethodInfo definition = called.GetGenericMethodDefinition();
            MethodInfo ours = definition == vanillaSet ? oursSet : definition == vanillaUpdate ? oursUpdate : null;
            Type[] arguments = called.GetGenericArguments();
            if (ours == null || arguments.Length != 1 || !ValueTypes.Contains(arguments[0]))
            {
                continue;
            }

            instruction.operand = ours.MakeGenericMethod(arguments);
            swapped = true;
        }

        if (!swapped)
        {
            Jotunn.Logger.LogWarning($"Performance: {original?.Name}({original?.GetParameters().LastOrDefault()?.ParameterType.Name}) "
                                     + "no longer calls ZDOHelper as expected; it keeps the game's own compare.");
        }

        return code;
    }

    private static MethodInfo GenericDefinition(Type type, string name)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(method => method.Name == name && method.IsGenericMethodDefinition && method.GetParameters().Length == 4);
    }
}
