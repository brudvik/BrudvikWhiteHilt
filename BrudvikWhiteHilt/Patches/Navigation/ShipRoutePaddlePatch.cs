using BrudvikWhiteHilt.Pieces.Navigation;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace BrudvikWhiteHilt.Patches.Navigation;

/// <summary>
/// Lets a ship sailing its route row at the slowest speed. Without a helmsman the game sets Slow back to Stop, so the
/// ship counts as steered while its route rows.
/// </summary>
[HarmonyPatch(typeof(Ship), nameof(Ship.CustomFixedUpdate))]
public static class ShipRoutePaddlePatch
{
    /// <summary>
    /// Swaps the game's helmsman check in the ship's update for <see cref="Steered"/>.
    /// </summary>
    /// <param name="instructions">The original code.</param>
    /// <returns>The patched code.</returns>
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo original = AccessTools.Method(typeof(Ship), nameof(Ship.HaveControllingPlayer));
        MethodInfo replacement = AccessTools.Method(typeof(ShipRoutePaddlePatch), nameof(Steered));
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.Calls(original))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
            }

            yield return instruction;
        }
    }

    /// <summary>
    /// True when someone holds the helm, or the ship rows its route.
    /// </summary>
    /// <param name="ship">The ship.</param>
    /// <returns>True if the ship is steered.</returns>
    public static bool Steered(Ship ship)
    {
        if (ship.HaveControllingPlayer())
        {
            return true;
        }

        ShipRoute route = ship.GetComponent<ShipRoute>();
        return route != null && route.Rowing;
    }
}
