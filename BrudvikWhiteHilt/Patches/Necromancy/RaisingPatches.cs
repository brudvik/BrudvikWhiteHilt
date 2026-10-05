using BrudvikWhiteHilt.Necromancy;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Necromancy;

/// <summary>
/// Hooks the Necromancer's Staff's waking into death, gravestones and the game session.
/// </summary>
[HarmonyPatch]
public static class RaisingPatches
{
    /// <summary>
    /// Registers the waking's RPCs for the session.
    /// </summary>
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    [HarmonyPostfix]
    public static void RegisterRpcs()
    {
        Raising.RegisterRpcs();
    }

    /// <summary>
    /// Remembers the dying player's skills; the gravestone is made before the death lowers them.
    /// </summary>
    /// <param name="__instance">The dying player.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
    [HarmonyPrefix]
    public static void BeforeDeath(Player __instance)
    {
        Raising.BeforeDeath(__instance);
    }

    /// <summary>
    /// Marks a new gravestone with the time of death and the skills before it.
    /// </summary>
    /// <param name="__instance">The gravestone.</param>
    /// <param name="ownerUID">Its owner's player ID.</param>
    [HarmonyPatch(typeof(TombStone), nameof(TombStone.Setup))]
    [HarmonyPostfix]
    public static void GraveMade(TombStone __instance, long ownerUID)
    {
        Raising.GraveMade(__instance, ownerUID);
    }

    /// <summary>
    /// Tells a necromancer at a friend's grave whether, and at what price, the staff can wake them.
    /// </summary>
    /// <param name="__instance">The gravestone.</param>
    /// <param name="__result">The hover text.</param>
    [HarmonyPatch(typeof(TombStone), nameof(TombStone.GetHoverText))]
    [HarmonyPostfix]
    public static void HoverText(TombStone __instance, ref string __result)
    {
        string line = Raising.HoverLine(__instance);
        if (line != null)
        {
            __result += line;
        }
    }

    /// <summary>
    /// With the staff in hand, using a friend's grave wakes them instead of opening it.
    /// </summary>
    /// <param name="__instance">The gravestone.</param>
    /// <param name="character">Who uses it.</param>
    /// <param name="hold">Whether the key is held down.</param>
    /// <param name="__result">The use's result when the staff takes it.</param>
    /// <returns>False when the staff takes the use.</returns>
    [HarmonyPatch(typeof(TombStone), nameof(TombStone.Interact))]
    [HarmonyPrefix]
    public static bool Use(TombStone __instance, Humanoid character, bool hold, ref bool __result)
    {
        if (hold || !(character is Player player) || !Raising.TryBegin(__instance, player))
        {
            return true;
        }

        __result = true;
        return false;
    }

    /// <summary>
    /// Lets an unanswered call lapse.
    /// </summary>
    /// <param name="__instance">The player.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    [HarmonyPostfix]
    public static void Update(Player __instance)
    {
        if (__instance == Player.m_localPlayer)
        {
            Raising.Update(__instance);
        }
    }
}
