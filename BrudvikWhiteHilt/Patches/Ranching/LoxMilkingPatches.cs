using BrudvikWhiteHilt.Ranching;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Ranching;

/// <summary>
/// Milking tame lox by crouching and using them, see <see cref="LoxMilking"/>.
/// </summary>
[HarmonyPatch]
public static class LoxMilkingPatches
{
    /// <summary>
    /// Registers the milking RPC on lox.
    /// </summary>
    /// <param name="__instance">The animal.</param>
    [HarmonyPatch(typeof(Tameable), nameof(Tameable.Awake))]
    [HarmonyPostfix]
    private static void RegisterRpc(Tameable __instance)
    {
        LoxMilking.RegisterRpc(__instance);
    }

    /// <summary>
    /// Use while crouching on a tame lox milks it instead of the vanilla command.
    /// </summary>
    /// <param name="__instance">The animal.</param>
    /// <param name="user">The player.</param>
    /// <param name="hold">True while the key is held.</param>
    /// <param name="alt">True with Shift, which renames.</param>
    /// <param name="__result">True when milked or refused.</param>
    /// <returns>False to skip vanilla.</returns>
    [HarmonyPatch(typeof(Tameable), nameof(Tameable.Interact))]
    [HarmonyPrefix]
    private static bool Milk(Tameable __instance, Humanoid user, bool hold, bool alt, ref bool __result)
    {
        if (alt || hold || user is not Player player || !player.IsCrouching() || !LoxMilking.TryMilk(__instance, user))
        {
            return true;
        }

        __result = true;
        return false;
    }

    /// <summary>
    /// Shows whether a tame lox can be milked.
    /// </summary>
    /// <param name="__instance">The animal.</param>
    /// <param name="__result">The hover text.</param>
    [HarmonyPatch(typeof(Tameable), nameof(Tameable.GetHoverText))]
    [HarmonyPostfix]
    private static void ShowMilking(Tameable __instance, ref string __result)
    {
        if (!string.IsNullOrEmpty(__result))
        {
            __result += Localization.instance.Localize(LoxMilking.HoverText(__instance));
        }
    }
}
