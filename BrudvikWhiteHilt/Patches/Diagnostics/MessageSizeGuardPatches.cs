using BrudvikWhiteHilt.Diagnostics;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Diagnostics;

/// <summary>
/// Stops a message too large for Steam before it reaches the connection's queue, see <see cref="MessageSizeGuard"/>.
/// </summary>
[HarmonyPatch]
public static class MessageSizeGuardPatches
{
    /// <summary>
    /// Drops a message Steam would refuse.
    /// </summary>
    /// <param name="__instance">The connection.</param>
    /// <param name="pkg">The message.</param>
    /// <returns>False to drop the message.</returns>
    [HarmonyPatch(typeof(ZSteamSocket), nameof(ZSteamSocket.Send))]
    [HarmonyPrefix]
    public static bool SendPrefix(ZSteamSocket __instance, ZPackage pkg)
    {
        return MessageSizeGuard.Allow(pkg, __instance.GetHostName());
    }
}
