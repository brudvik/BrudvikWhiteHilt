using System;
using System.Reflection;
using BepInEx.Bootstrap;

namespace BrudvikWhiteHilt.Performance;

/// <summary>
/// Asks Valheim Community Patch, when it is installed, whether one of its fixes is running. It fixes several of the same
/// vanilla faults as <see cref="VanillaFixSettings"/>, in the same methods, so where its fix is on ours stands down and
/// the game is not patched twice for one fault. The question goes to the mod's public API by reflection, so there is no
/// build dependency on it.
/// </summary>
public static class CommunityPatchCoexistence
{
    /// <summary>Valheim Community Patch's plugin id.</summary>
    public const string Guid = "MidnightsFX.ValheimCommunityPatch";

    // GetFixState's answer for "applied, switched on and not stood down".
    private const int FixStateActive = 1;

    private static Func<string, int> getFixState;
    private static bool looked;

    /// <summary>
    /// True when Valheim Community Patch is loaded. A plugin that loads after this one is only listed once BepInEx gets
    /// to it, so ask after start-up (the main menu or a world), not from Awake.
    /// </summary>
    public static bool IsInstalled => Chainloader.PluginInfos.ContainsKey(Guid);

    /// <summary>
    /// Whether Valheim Community Patch has the given fix applied and switched on. False when the mod is absent, too old
    /// to answer, or the question fails.
    /// </summary>
    /// <param name="fixId">The fix's id, which is the name of its patch class, such as "ZPackageWriteAllocPatch".</param>
    /// <returns>True when its fix is running.</returns>
    public static bool HasActiveFix(string fixId)
    {
        if (string.IsNullOrEmpty(fixId) || !IsInstalled)
        {
            return false;
        }

        try
        {
            Func<string, int> state = Resolve();
            return state != null && state(fixId) == FixStateActive;
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Performance: could not ask Valheim Community Patch about {fixId} ({ex.GetType().Name}); "
                                     + "White Hilt's own fix stays on.");
            return false;
        }
    }

    // Finds ValheimCommunityPatch.APIReceiver.GetFixState(string) in the loaded plugin's assembly, once.
    private static Func<string, int> Resolve()
    {
        if (looked)
        {
            return getFixState;
        }

        if (!Chainloader.PluginInfos.TryGetValue(Guid, out BepInEx.PluginInfo info) || info.Instance == null)
        {
            return null;
        }

        looked = true;
        Type receiver = info.Instance.GetType().Assembly.GetType("ValheimCommunityPatch.APIReceiver", false);
        MethodInfo method = receiver?.GetMethod("GetFixState", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
        if (method != null && method.ReturnType == typeof(int))
        {
            getFixState = (Func<string, int>)Delegate.CreateDelegate(typeof(Func<string, int>), method);
        }
        else
        {
            Jotunn.Logger.LogInfo("Performance: this Valheim Community Patch cannot say which fixes it runs; White Hilt's own fixes stay on.");
        }

        return getFixState;
    }
}
