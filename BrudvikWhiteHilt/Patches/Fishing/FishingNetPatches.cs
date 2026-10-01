using BrudvikWhiteHilt.Pieces.Fishing;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Fishing;

/// <summary>
/// Hooks for the Net Winch and the Shore Net: the barrel's size and hover text, mending, Fishing experience when
/// the barrel is opened, and where a net may be placed.
/// </summary>
[HarmonyPatch]
public static class FishingNetPatches
{
    private static string lastProblem;

    /// <summary>
    /// Sizes the winch's barrel from the config before its inventory is made.
    /// </summary>
    /// <param name="__instance">The container.</param>
    [HarmonyPatch(typeof(Container), nameof(Container.Awake))]
    [HarmonyPrefix]
    private static void SizeBarrel(Container __instance)
    {
        if (__instance.GetComponent<NetWinchComponent>() != null)
        {
            __instance.m_width = NetWinch.BarrelWidth;
            __instance.m_height = FishingNetSettings.BarrelRows.Value;
        }
    }

    /// <summary>
    /// Adds what the winch and its nets are doing to the hover text.
    /// </summary>
    /// <param name="__instance">The container.</param>
    /// <param name="__result">Hover text.</param>
    [HarmonyPatch(typeof(Container), nameof(Container.GetHoverText))]
    [HarmonyPostfix]
    private static void WinchHover(Container __instance, ref string __result)
    {
        NetWinchComponent winch = __instance.GetComponent<NetWinchComponent>();
        if (winch != null && !string.IsNullOrEmpty(__result))
        {
            __result += winch.StatusText();
        }
    }

    /// <summary>
    /// The alternative use on a winch mends its nets instead of opening the barrel.
    /// </summary>
    /// <param name="__instance">The container.</param>
    /// <param name="character">Who uses it.</param>
    /// <param name="hold">True while the key is held.</param>
    /// <param name="alt">True for the alternative use.</param>
    /// <param name="__result">Whether the use was handled.</param>
    /// <returns>False to skip opening the barrel.</returns>
    [HarmonyPatch(typeof(Container), nameof(Container.Interact))]
    [HarmonyPrefix]
    private static bool Mend(Container __instance, Humanoid character, bool hold, bool alt, ref bool __result)
    {
        NetWinchComponent winch = alt && !hold ? __instance.GetComponent<NetWinchComponent>() : null;
        if (winch == null)
        {
            return true;
        }

        __result = winch.Mend(character as Player);
        return false;
    }

    /// <summary>
    /// Gives the player who opens the barrel Fishing experience for the fish in it.
    /// </summary>
    /// <param name="__instance">The container.</param>
    /// <param name="granted">Whether the barrel opened.</param>
    [HarmonyPatch(typeof(Container), nameof(Container.RPC_OpenResponse))]
    [HarmonyPostfix]
    private static void ClaimSkill(Container __instance, bool granted)
    {
        if (granted)
        {
            __instance.GetComponent<NetWinchComponent>()?.ClaimSkill(Player.m_localPlayer);
        }
    }

    /// <summary>
    /// A Shore Net can only be placed near a winch with room for it, where the water is deep enough.
    /// </summary>
    /// <param name="__instance">The building player.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
    [HarmonyPostfix]
    private static void CheckNetPlacement(Player __instance)
    {
        FishingNetComponent net = __instance.m_placementGhost != null ? __instance.m_placementGhost.GetComponent<FishingNetComponent>() : null;
        if (net == null || !__instance.m_placementGhost.activeSelf || __instance.m_placementStatus != Player.PlacementStatus.Valid)
        {
            lastProblem = null;
            return;
        }

        string problem = net.PlacementProblem();
        if (problem == null)
        {
            lastProblem = null;
            return;
        }

        __instance.m_placementStatus = Player.PlacementStatus.Invalid;
        __instance.SetPlacementGhostValid(false);
        if (problem != lastProblem)
        {
            lastProblem = problem;
            __instance.Message(MessageHud.MessageType.TopLeft, problem);
        }
    }
}
