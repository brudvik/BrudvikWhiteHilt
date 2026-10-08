using BrudvikWhiteHilt.Party;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Party;

/// <summary>
/// Starts sharing party status when the game starts, and updates the party list under the hotbar with the HUD.
/// </summary>
[HarmonyPatch]
public static class PartyPatches
{
    /// <summary>
    /// Starts sharing party status; on a dedicated server it only hears the statuses, so the relayed calls have a
    /// receiver.
    /// </summary>
    /// <param name="__instance">The game.</param>
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    [HarmonyPostfix]
    public static void GameStartPostfix(Game __instance)
    {
        __instance.gameObject.AddComponent<PartyNetwork>();
    }

    /// <summary>
    /// Shows, hides and updates the party list after the game's HUD update.
    /// </summary>
    /// <param name="__instance">The HUD.</param>
    [HarmonyPatch(typeof(Hud), nameof(Hud.Update))]
    [HarmonyPostfix]
    public static void HudUpdate(Hud __instance)
    {
        PartyPanel.Update(__instance);
    }
}
