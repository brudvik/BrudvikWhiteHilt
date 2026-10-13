using BrudvikWhiteHilt.SeaBattles;
using BrudvikWhiteHilt.SeaBattles.Raids;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.SeaBattles;

/// <summary>
/// Puts the raids into the game: using a Raid Chart asks the server for a draugr harbour, and the hoard chest stays shut
/// while the harbour's guards live.
/// </summary>
[HarmonyPatch]
public static class RaidPatches
{
    /// <summary>
    /// Using a Raid Chart asks for a harbour instead of equipping or consuming the item. Only for the local player; the
    /// chart is used up when the harbour has been made.
    /// </summary>
    /// <param name="__instance">Who uses the item.</param>
    /// <param name="item">The item.</param>
    /// <returns>False when the item is a Raid Chart, so the game's own use does not run.</returns>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UseItem))]
    [HarmonyPrefix]
    public static bool UseChart(Humanoid __instance, ItemDrop.ItemData item)
    {
        if (item?.m_dropPrefab == null || item.m_dropPrefab.name != NailShipRegistry.RaidChartName
            || __instance is not Player player || player != Player.m_localPlayer)
        {
            return true;
        }

        RaidService.Request(player);
        return false;
    }

    /// <summary>
    /// Keeps a hoard shut while its guards live; the first time it opens with none left, tells the chest's owner.
    /// </summary>
    /// <param name="__instance">The container.</param>
    /// <param name="character">Who uses it.</param>
    /// <param name="hold">True while the key is held, which the game ignores.</param>
    /// <param name="__result">What the game's method returns; set when the hoard is refused.</param>
    /// <returns>False when the hoard is refused, so the game's method does not run.</returns>
    [HarmonyPatch(typeof(Container), nameof(Container.Interact))]
    [HarmonyPrefix]
    public static bool Hoard(Container __instance, Humanoid character, bool hold, ref bool __result)
    {
        RaidHoard hoard = hold ? null : __instance.GetComponent<RaidHoard>();
        if (hoard == null)
        {
            return true;
        }

        if (hoard.GuardsAlive())
        {
            character.Message(MessageHud.MessageType.Center, "$msg_whitehilt_raid_guarded");
            __result = true;
            return false;
        }

        hoard.Opened();
        return true;
    }
}
