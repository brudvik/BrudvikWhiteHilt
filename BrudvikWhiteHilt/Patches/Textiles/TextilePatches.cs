using BrudvikWhiteHilt.Textiles;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Textiles;

/// <summary>
/// Shows dyed banners, sails and capes.
/// </summary>
[HarmonyPatch]
public static class TextilePatches
{
    /// <summary>
    /// Shows a piece's or ship's saved dye and listens for new dye.
    /// </summary>
    /// <param name="__instance">The piece.</param>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Awake))]
    [HarmonyPostfix]
    public static void WearNTearAwake(WearNTear __instance)
    {
        DyedCloth.OnAwake(__instance);
    }

    /// <summary>
    /// Keeps capes in their dye.
    /// </summary>
    /// <param name="__instance">The equipment visuals.</param>
    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.UpdateVisuals))]
    [HarmonyPostfix]
    public static void UpdateVisuals(VisEquipment __instance)
    {
        Dyeing.RefreshCape(__instance);
    }
}
