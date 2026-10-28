using System;
using BrudvikWhiteHilt.Performance;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Performance;

/// <summary>
/// Two fixes on the way world data travels: nested packages are written without a copy, and the server marks the data
/// players send it for the next save.
/// </summary>
[HarmonyPatch]
public static class NetworkFixPatches
{
    /// <summary>
    /// Writes a nested package without copying it first, see <see cref="PackageWriter"/>. It replaces vanilla's method,
    /// so it runs last and does nothing when an earlier prefix has already written the package (Valheim Community
    /// Patch's does the same, and does not look whether it is the first); two writes would break the message.
    /// </summary>
    /// <param name="__instance">The package written to.</param>
    /// <param name="pkg">The package written.</param>
    /// <param name="__runOriginal">False when an earlier prefix has written it.</param>
    /// <returns>False when the package was written here.</returns>
    [HarmonyPatch(typeof(ZPackage), nameof(ZPackage.Write), typeof(ZPackage))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    public static bool WritePackage(ZPackage __instance, ZPackage pkg, bool __runOriginal)
    {
        if (!__runOriginal || !VanillaFixSettings.PackageWrite.Active)
        {
            return __runOriginal;
        }

        try
        {
            return !PackageWriter.WriteNested(__instance, pkg);
        }
        catch (Exception ex)
        {
            // The flush and the buffer are the steps that can fail, and both come before anything is written.
            VanillaFixSettings.PackageWrite.Fail("writing a nested package", ex);
            return true;
        }
    }

    /// <summary>
    /// Marks the save chunk of an object whose data came from a player, so the next world save writes it. Since the
    /// chunked save format, a save rewrites only chunks marked as changed. A change made on the server marks its chunk
    /// (<c>ZDO.IncreaseDataRevision</c>), but <c>ZDOMan.RPC_ZDOData</c>, the only caller of <c>Deserialize</c>, sets the
    /// received revision and data directly and marks nothing: a new object is put in its sector before it is known to be
    /// persistent. So what a player built or put in a chest reached the disk only if something else in the same chunk
    /// changed on the server before the save. A portal is saved from its own list, which is marked instead.
    /// </summary>
    /// <param name="__instance">The object just read from the network.</param>
    [HarmonyPatch(typeof(ZDO), nameof(ZDO.Deserialize))]
    [HarmonyPostfix]
    public static void MarkReceivedForSave(ZDO __instance)
    {
        if (!VanillaFixSettings.SaveClientChanges.Active || ZNet.instance == null || !ZNet.instance.IsServer()
            || ZDOMan.instance == null || !__instance.Persistent)
        {
            return;
        }

        try
        {
            if (Game.instance != null && Game.instance.PortalPrefabHash.Contains(__instance.GetPrefab()))
            {
                ZDOMan.instance.SetDirtyPortals();
            }
            else
            {
                ZDOMan.instance.SetDirtySector(__instance);
            }
        }
        catch (Exception ex)
        {
            VanillaFixSettings.SaveClientChanges.Fail("marking a player's change for the save", ex);
        }
    }
}
