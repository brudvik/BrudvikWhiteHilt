using BrudvikWhiteHilt.SeaBattles;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.SeaBattles;

/// <summary>
/// What lets a Nail Ship sail with nobody aboard and keeps its helm and hold shut while its crew lives: the ship's
/// physics counts a crew, the helm and the hold refuse the player, and the crew creatures keep to their posts.
/// </summary>
[HarmonyPatch]
public static class RaiderShipPatches
{
    /// <summary>
    /// Swaps the game's count of players aboard for <see cref="RaiderShip.CrewCount"/> in the ship's physics. Vanilla
    /// stops a ship and brakes it when no player is aboard; a Nail Ship under sail must not be stopped. The count is
    /// read as <c>m_players.Count</c>, so each <c>ldfld m_players</c> followed by <c>get_Count</c> gets the new call.
    /// </summary>
    /// <param name="instructions">The original code.</param>
    /// <returns>The patched code.</returns>
    [HarmonyPatch(typeof(Ship), nameof(Ship.CustomFixedUpdate))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> CrewCountTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> patched = ReplaceCount(instructions, AccessTools.Field(typeof(Ship), "m_players"),
            AccessTools.PropertyGetter(typeof(List<Player>), nameof(List<Player>.Count)),
            AccessTools.Method(typeof(RaiderShip), nameof(RaiderShip.CrewCount)), out int replaced);
        if (replaced == 0)
        {
            Jotunn.Logger.LogWarning("Nail Ship: Ship.CustomFixedUpdate no longer reads m_players.Count, so Nail Ships will stop when nobody is aboard. The game has changed.");
        }

        return patched;
    }

    /// <summary>
    /// Replaces every read of a list's count that directly follows a load of the given field with a call. The members
    /// are parameters so the logic is tested without loading the game's ship, which .NET Framework cannot.
    /// </summary>
    /// <param name="instructions">The original code.</param>
    /// <param name="players">The field that holds the list.</param>
    /// <param name="getCount">The list's <c>get_Count</c>.</param>
    /// <param name="replacement">The method that takes the list and returns the count.</param>
    /// <param name="replaced">How many reads were replaced.</param>
    /// <returns>The code with the new calls.</returns>
    public static List<CodeInstruction> ReplaceCount(IEnumerable<CodeInstruction> instructions, FieldInfo players, MethodInfo getCount,
        MethodInfo replacement, out int replaced)
    {
        List<CodeInstruction> codes = instructions.ToList();
        replaced = 0;
        for (int i = 1; i < codes.Count; i++)
        {
            if (codes[i].Calls(getCount) && codes[i - 1].LoadsField(players))
            {
                codes[i].opcode = OpCodes.Call;
                codes[i].operand = replacement;
                replaced++;
            }
        }

        return codes;
    }

    /// <summary>
    /// Gives a Nail Ship under sail the wind behind it, when the setting says so. The game's own tailwind (the Mast Wisp
    /// and the White Hilt ships) turns the wind of the local player's ship, which a Nail Ship is not; so the sail force
    /// itself is taken from the ship's heading.
    /// </summary>
    /// <param name="__instance">The ship.</param>
    /// <param name="sailSize">How much sail is up, 0 to 1.</param>
    /// <param name="__result">The sail force.</param>
    /// <returns>False when the force was set here, so the game's method does not run.</returns>
    [HarmonyPatch(typeof(Ship), "GetSailForce")]
    [HarmonyPrefix]
    public static bool Tailwind(Ship __instance, float sailSize, ref Vector3 __result)
    {
        if (!SeaBattleSettings.Tailwind.Value || !RaiderShip.Sails(__instance))
        {
            return true;
        }

        __result = RaiderShip.TailwindForce(__instance, sailSize);
        return false;
    }

    /// <summary>
    /// Keeps the helm of a Nail Ship shut while its crew lives.
    /// </summary>
    /// <param name="__instance">The helm.</param>
    /// <param name="character">Who uses it.</param>
    /// <param name="repeat">True while the key is held.</param>
    /// <param name="__result">What the game's method returns; set when the helm is refused.</param>
    /// <returns>False when the helm is refused, so the game's method does not run.</returns>
    [HarmonyPatch(typeof(ShipControlls), nameof(ShipControlls.Interact))]
    [HarmonyPrefix]
    public static bool Helm(ShipControlls __instance, Humanoid character, bool repeat, ref bool __result)
    {
        if (!RaiderShip.CrewHolds(__instance.m_ship))
        {
            return true;
        }

        if (!repeat)
        {
            character.Message(MessageHud.MessageType.Center, "$msg_whitehilt_raider_helm_held");
        }

        __result = false;
        return false;
    }

    /// <summary>
    /// Keeps the hold of a Nail Ship shut while its crew lives.
    /// </summary>
    /// <param name="__instance">The container.</param>
    /// <param name="character">Who uses it.</param>
    /// <param name="hold">True while the key is held.</param>
    /// <param name="__result">What the game's method returns; set when the hold is refused.</param>
    /// <returns>False when the hold is refused, so the game's method does not run.</returns>
    [HarmonyPatch(typeof(Container), nameof(Container.Interact))]
    [HarmonyPrefix]
    public static bool Hold(Container __instance, Humanoid character, bool hold, ref bool __result)
    {
        RaiderShip raider = __instance.GetComponentInParent<RaiderShip>();
        if (raider == null || !RaiderShip.CrewHolds(raider.Ship))
        {
            return true;
        }

        if (!hold)
        {
            character.Message(MessageHud.MessageType.Center, "$msg_whitehilt_raider_hold_guarded");
        }

        __result = true;
        return false;
    }

    /// <summary>
    /// Lets a crew member hold its post, leap or turn back after the game's AI has run. Costs a count and a return
    /// for every other creature, and nothing when no crew is loaded.
    /// </summary>
    /// <param name="__instance">The creature's AI.</param>
    /// <param name="__result">True when the AI ran, which it does on the creature's owner.</param>
    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
    [HarmonyPostfix]
    public static void CrewAI(MonsterAI __instance, bool __result)
    {
        if (RaiderCrew.Count == 0 || !__result)
        {
            return;
        }

        RaiderCrew.Of(__instance)?.Hold();
    }

    /// <summary>
    /// Keeps a moored Nail Ship where it lies after each physics step. Costs a count and a return for every ship when
    /// no Nail Ship is loaded.
    /// </summary>
    /// <param name="__instance">The ship.</param>
    [HarmonyPatch(typeof(Ship), nameof(Ship.CustomFixedUpdate))]
    [HarmonyPostfix]
    public static void HoldMoored(Ship __instance)
    {
        if (RaiderShip.Loaded == 0)
        {
            return;
        }

        RaiderShip.Of(__instance)?.HoldMoored();
    }

    /// <summary>
    /// Makes a creature a crew member when its ZDO says it is one: after a reload, or when another machine made it.
    /// </summary>
    /// <param name="__instance">The creature.</param>
    [HarmonyPatch(typeof(Character), nameof(Character.Awake))]
    [HarmonyPostfix]
    public static void CharacterAwake(Character __instance)
    {
        RaiderCrew.Attach(__instance);
    }
}
